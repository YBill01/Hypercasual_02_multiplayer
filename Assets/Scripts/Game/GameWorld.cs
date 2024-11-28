using Cysharp.Threading.Tasks;
using GameName.Data;
using GameName.PlayerProfile;
using R3;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameWorld : MonoBehaviour, /*IPausable,*/ IUpdatable, ISaveable<PlayerData>, IDisposable
{
	[Header("Objects")]
	[Space]
	[SerializeField]
	private Transform m_playerContainer;

	[Space]
	[SerializeField]
	private Building[] m_buildings;

	[Space]
	[SerializeField]
	private Storage[] m_storages;

	[Space]
	[SerializeField]
	private Transform m_collectingContainer;

	public bool IsPlayerSpawned => _player != null;

	private CollectingBehaviour _collectingBehaviour;

	private CompositeDisposable _compositeDisposable;

	private NPlayer _player;
	private List<NPlayer> _players;

	private List<StockData> _stocksData;

	public struct StockData : IEquatable<StockData>
	{
		public int clientId;
		public Stock stock;

		public bool Equals(StockData other)
		{
			return clientId == other.clientId && stock.Equals(other.stock);
		}
	}

	private NetworkGameWorld _networkGameWorld;

	private IObjectResolver _resolver;

	private Profile _profile;
	private VCamera _vCamera;
	private InputPlayerControl _inputPlayerControl;
	private StockEvents _stockEvents;
	private CollectingEvents _collectingEvents;
	private StockBehaviour _stockBehaviour;
	private NetworkConfigData _networkConfig;

	[Inject]
	public void Construct(
		IObjectResolver resolver,
		Profile profile,
		VCamera vCamera,
		InputPlayerControl inputPlayerControl,
		//CPlayer player,
		StockEvents stockEvents,
		CollectingEvents collectingEvents,
		StockBehaviour stockBehaviour,
		NetworkConfigData networkConfig)
	{
		_resolver = resolver;

		_profile = profile;
		_vCamera = vCamera;
		_inputPlayerControl = inputPlayerControl;
		//_player = player;
		_stockEvents = stockEvents;
		_collectingEvents = collectingEvents;
		_stockBehaviour = stockBehaviour;
		_networkConfig = networkConfig;

		foreach (Building building in m_buildings)
		{
			_resolver.InjectGameObject(building.gameObject);
		}
		
		foreach (Storage storage in m_storages)
		{
			_resolver.InjectGameObject(storage.gameObject);
		}
	}

	private void Start()
	{
		_players = new List<NPlayer>();
		_stocksData = new List<StockData>();

		_collectingBehaviour = new CollectingBehaviour(m_collectingContainer);
		
		_compositeDisposable = new CompositeDisposable();

		_collectingEvents.OnTransfer
			.Subscribe(x => CollectingOnTransfer(x))
			.AddTo(_compositeDisposable);

		NetworkGameWorld.OnSpawn += NetworkGameWorldOnSpawn;

		RegisterStocks();
	}

	private void NetworkGameWorldOnSpawn(NetworkGameWorld networkGameWorld)
	{
		_networkGameWorld = networkGameWorld;
		_networkGameWorld.Register(this);

		if (_networkGameWorld.IsServer)
		{
			_stockEvents.OnTransferExecute
				.Subscribe(x => StockOnTransferExecute(x))
				.AddTo(_compositeDisposable);

			/*_stockEvents.OnAdd
				.Subscribe(x => StockOnAdd(x))
				.AddTo(_compositeDisposable);*/
			
			/*_stockEvents.OnTake
				.Subscribe(x => StockOnTake(x))
				.AddTo(_compositeDisposable);*/

			foreach (Building building in m_buildings)
			{
				building.Init();
			}
		}
		else
		{
			foreach (Building building in m_buildings)
			{
				building.Stock.safetyChecks = false;
			}

			/*foreach (Storage storage in m_storages)
			{
				storage.Stock.safetyChecks = false;
			}*/

			_networkGameWorld.NetworkBuildingList.OnListChanged += NetworkBuildingListOnListChanged;
			//_networkGameWorld.NetworkStockTransfer.OnValueChanged += NetworkStockTransferOnValueChanged;
		}
	}

	public void RegisterPlayer(NPlayer player)
	{
		_resolver.Inject(player);

		RegisterStocks(player);

		if (player.IsOwner)
		{
			_player = player;

			_vCamera.SetFollowTarget(_player.CameraTarget);

			_inputPlayerControl.SetPlayerController(_player.GetComponent<NPlayerController>());

			if (_networkGameWorld.IsServer)
			{
				SetSaveData(_profile.Get<PlayerData>().Data);
			}
			else
			{
				SetNetworData();
			}
		}
		else
		{
			_players.Add(player);

			if (_networkGameWorld.IsServer)
			{
				SendStockData(player.OwnerClientId);
			}
		}

		player.GetComponent<NetworkObject>().TrySetParent(m_playerContainer);

		player.Register(this);
	}
	public void UnregisterPlayer(NPlayer player)
	{
		if (player.IsOwner)
		{
			_player = null;
		}
		else
		{
			_players.Remove(player);
		}

		player.Unregister();

		_stocksData.Remove(new StockData
		{
			clientId = (int)player.ClientId.Value,
			stock = player.Stock
		});

		_collectingBehaviour.ClearAll();
	}

	private void RegisterStocks(NPlayer player = null)
	{
		if (player != null)
		{
			_stocksData.Add(new StockData
			{
				clientId = (int)player.ClientId.Value,
				stock = player.Stock
			});
		}
		else
		{
			for (int i = 0; i < m_buildings.Length; i++)
			{
				_stocksData.Add(new StockData
				{
					clientId = -1,
					stock = m_buildings[i].Stock
				});
			}

			for (int i = 0; i < m_storages.Length; i++)
			{
				_stocksData.Add(new StockData
				{
					clientId = -1,
					stock = m_storages[i].Stock
				});
			}
		}
	}

	private void SendStockData(ulong clientId)
	{
		NetworkGameWorld.StockItemData[] data = Array.Empty<NetworkGameWorld.StockItemData>();

		for (int i = 0; i < _stocksData.Count; i++)
		{
			NetworkGameWorld.StockItemData[] data1 = _stocksData[i].stock.NetworkClientGetData(i, _stocksData[i].clientId);
			NetworkGameWorld.StockItemData[] data2 = new NetworkGameWorld.StockItemData[data.Length + data1.Length];

			int idx = 0;

			for (int k = 0; k < data.Length; k++)
				data2[idx++] = data[k];
			for (int j = 0; j < data1.Length; j++)
				data2[idx++] = data1[j];

			data = data2;
		}

		_networkGameWorld.SetStockDataServerRpc(clientId, data);
	}
	public async void SetStockData(NetworkGameWorld.StockItemData[] data)
	{
		await UniTask.NextFrame();

		for (int i = 0; i < data.Length; i++)
		{
			if (data[i].clientId > -1)
			{
				_stocksData.First(x => x.clientId == data[i].clientId)
					.stock.NetworkClientSetData(data[i]);
			}
			else
			{
				_stocksData[data[i].stockIndex].stock.NetworkClientSetData(data[i]);
			}
		}

		NetworkGameWorld.StockItemData[] dataEmpty = Array.Empty<NetworkGameWorld.StockItemData>();
		for (int i = 0; i < _stocksData.Count; i++)
		{
			_stocksData[i].stock.NetworkClientSetData(dataEmpty);
		}
	}

	/*public void SetPause(bool pause)
	{
		//_player.SetPause(pause);
	}*/

	public void OnUpdate(float deltaTime)
	{
		_collectingBehaviour.OnUpdate(deltaTime);

		if (IsPlayerSpawned)
		{
			_player.OnUpdate(deltaTime);
		}

		foreach (NPlayer player in _players)
		{
			player.OnUpdate(deltaTime);
		}

		if (_networkGameWorld.IsServer)
		{
			foreach (Building building in m_buildings)
			{
				building.OnUpdate(deltaTime);
			}

			_networkGameWorld.UpdateBuildingData(m_buildings);

			foreach (Storage storage in m_storages)
			{
				storage.OnUpdate(deltaTime);
			}

			_stockBehaviour.Execute();
		}
		else
		{
			foreach (StockData stockData in _stocksData)
			{
				stockData.stock.OnUpdate(deltaTime);
			}

			_stockBehaviour.ExecuteOnce(true);
		}
	}

	public void SetNetworkStockTransfer(NetworkGameWorld.StockTransferData[] data)
	{
		for (int i = 0; i < data.Length; i++)
		{
			_stockEvents.OnTransfer.OnNext(new StockTransfer
			{
				item = new StockItem
				{
					type = (ItemType)data[i].type
				},
				stockIn = _stocksData[data[i].stockInIndex].stock,
				stockOut = _stocksData[data[i].stockOutIndex].stock
			});
		}
	}

	public void SetNetworkStockAdd(NetworkGameWorld.StockOperationData data)
	{
		/*if (_stocksData[data.stockIndex].stock.TryAdd(new StockItem
		{
			type = (ItemType)data.type
		}))
		{
			Debug.Log("SetNetworkStockAdd else -------------------------");
		}*/

		/*_stocksData[data.stockIndex].stock.TryAdd(new StockItem
		{
			type = (ItemType)data.type
		});*/
	}
	public void SetNetworkStockTake(NetworkGameWorld.StockOperationData data)
	{
		/*if (_stocksData[data.stockIndex].stock.TryTake(new StockItem
		{
			type = (ItemType)data.type
		}))
		{
			Debug.Log("SetNetworkStockTake else -------------------------");
		}*/

		/*_stocksData[data.stockIndex].stock.TryTake(new StockItem
		{
			type = (ItemType)data.type
		});*/
	}

	private void StockOnTransferExecute(List<StockTransfer> transferList)
	{
		NetworkGameWorld.StockTransferData[] data = new NetworkGameWorld.StockTransferData[transferList.Count];

		for (int i = 0; i < transferList.Count; i++)
		{
			data[i] = new NetworkGameWorld.StockTransferData
			{
				type = (int)transferList[i].item.type,

				stockInIndex = _stocksData.FindIndex(x => x.stock == transferList[i].stockIn),
				stockOutIndex = _stocksData.FindIndex(x => x.stock == transferList[i].stockOut)
			};
		}

		_networkGameWorld.SetStockTransferDataServerRpc(data);
	}
	
	private void StockOnAdd((Stock stock, StockItem item) stockOperation)
	{
		_networkGameWorld.SetStockAddDataServerRpc(new NetworkGameWorld.StockOperationData
		{
			type = (int)stockOperation.item.type,

			stockIndex = _stocksData.FindIndex(x => x.stock == stockOperation.stock)
		});
	}
	private void StockOnTake((Stock stock, StockItem item) stockOperation)
	{
		_networkGameWorld.SetStockTakeDataServerRpc(new NetworkGameWorld.StockOperationData
		{
			type = (int)stockOperation.item.type,

			stockIndex = _stocksData.FindIndex(x => x.stock == stockOperation.stock)
		});
	}

	private void CollectingOnTransfer(StockCollectingTransfer transfer)
	{
		_collectingBehaviour.Create<StockCollectingInfo>(
			transfer.originInfo,
			transfer.targetInfo,
			transfer.prefab,
			transfer.originInfo.source,
			transfer.targetInfo.source,
			transfer.originInfo.transform,
			transfer.targetInfo.transform,
			transfer.duration);
	}

	public void Dispose()
	{
		_inputPlayerControl.SetPlayerController(null);

		_compositeDisposable.Dispose();

		_networkGameWorld.NetworkBuildingList.OnListChanged -= NetworkBuildingListOnListChanged;
		//_networkGameWorld.NetworkStockTransfer.OnValueChanged -= NetworkStockTransferOnValueChanged;
		_networkGameWorld.Unregister();
		_networkGameWorld = null;

		NetworkGameWorld.OnSpawn -= NetworkGameWorldOnSpawn;
	}

	public PlayerData GetSaveData()
	{
		PlayerData playerData = _profile.Get<PlayerData>().Data;

		playerData.playerStats = _player.GetSaveData();

		playerData.playerStats.cameraZoom = _vCamera.distanceValue;
		playerData.playerStats.position = _player.transform.position;
		playerData.playerStats.quaternion = _player.transform.rotation;

		playerData.buildings = new PlayerData.BuildingStats[m_buildings.Length];
		for (int i = 0; i < m_buildings.Length; i++)
		{
			playerData.buildings[i] = m_buildings[i].GetSaveData();
		}

		playerData.storages = new PlayerData.StorageStats[m_storages.Length];
		for (int i = 0; i < m_storages.Length; i++)
		{
			playerData.storages[i] = m_storages[i].GetSaveData();
		}

		return playerData;
	}
	public bool SetSaveData(PlayerData data)
	{
		try
		{
			_player.SetOrientation(data.playerStats.position, data.playerStats.quaternion);

			_vCamera.distanceValue = data.playerStats.cameraZoom;
			_vCamera.SetFollowTarget(_player.CameraTarget);

			_player.SetSaveData(data.playerStats);

			for (int i = 0; i < m_buildings.Length; i++)
			{
				m_buildings[i].SetSaveData(data.buildings[i]);
			}

			for (int i = 0; i < m_storages.Length; i++)
			{
				m_storages[i].SetSaveData(data.storages[i]);
			}

			return true;
		}
		catch (System.Exception)
		{
			return false;
		}
	}
	private void SetNetworData()
	{
		_player.SetOrientation(_networkConfig.playerSpawnPosition, Quaternion.AngleAxis(_networkConfig.playerSpawnRotation, Vector3.up));

		_vCamera.distanceValue = _profile.Get<PlayerData>().Data.playerStats.cameraZoom;
	}

	private void NetworkBuildingListOnListChanged(NetworkListEvent<NetworkGameWorld.BuildingData> changeEvent)
	{
		switch (changeEvent.Type)
		{
			case NetworkListEvent<NetworkGameWorld.BuildingData>.EventType.Add:
				m_buildings[changeEvent.Value.index].NetworkClientUpdateData(changeEvent.Value);
				
				break;
		}
	}
}