using System;
using Unity.Netcode;

public class NetworkGameWorld : NetworkBehaviour
{
	public static event Action<NetworkGameWorld> OnSpawn;

	//public readonly Subject<BuildingData> OnBuildingDataUpdate = new();

	private GameWorld _gameWorld;

	//[NonSerialized]
	//public NetworkVariable<int> PlayerData = new(0);
	[NonSerialized]
	public NetworkList<BuildingData> NetworkBuildingList = new();
	
	//[NonSerialized]
	//public NetworkVariable<StockTransferData> NetworkStockTransfer = new();

	public struct StockItemData : INetworkSerializable, IEquatable<StockItemData>
	{
		public int clientId;

		public int stockIndex;
		public int stockSlotIndex;
		public int stockSlotItemIndex;

		public int type;
		public bool inactive;
		public float time;
		public float duration;

		public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
		{
			serializer.SerializeValue(ref clientId);

			serializer.SerializeValue(ref stockIndex);
			serializer.SerializeValue(ref stockSlotIndex);
			serializer.SerializeValue(ref stockSlotItemIndex);

			serializer.SerializeValue(ref type);
			serializer.SerializeValue(ref inactive);
			serializer.SerializeValue(ref time);
			serializer.SerializeValue(ref duration);
		}

		public bool Equals(StockItemData other)
		{
			return stockIndex == other.stockIndex
				&& stockSlotIndex == other.stockSlotIndex
				&& stockSlotItemIndex == other.stockSlotItemIndex
				&& type == other.type
				&& inactive == other.inactive;
		}
	}

	[ServerRpc]
	public void SetStockDataServerRpc(ulong clientId, StockItemData[] data)
	{
		ClientRpcParams rpcParams = default;
		rpcParams.Send.TargetClientIds = new ulong[1] { clientId };

		SetStockDataClientRpc(data, rpcParams);
	}

	[ClientRpc]
	public void SetStockDataClientRpc(StockItemData[] data, ClientRpcParams rpcParams = default)
	{
		_gameWorld.SetStockData(data);
	}

	public struct BuildingData : INetworkSerializable, IEquatable<BuildingData>
	{
		public int index;

		public float Progress;

		public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
		{
			serializer.SerializeValue(ref index);
			serializer.SerializeValue(ref Progress);
		}

		public bool Equals(BuildingData other)
		{
			throw new NotImplementedException();
		}
	}

	public struct StockTransferData : INetworkSerializable//, IEquatable<StockTransferData>
	{
		public int type;

		public int stockInIndex;
		public int stockOutIndex;
		
		public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
		{
			serializer.SerializeValue(ref type);

			serializer.SerializeValue(ref stockInIndex);
			serializer.SerializeValue(ref stockOutIndex);
		}

		/*public bool Equals(StockTransferData other)
		{
			return false;

			*//*return type == other.type
				&& stockInIndex == other.stockInIndex
				&& stockOutIndex == other.stockOutIndex;*//*
		}*/
	}

	public struct StockOperationData : INetworkSerializable, IEquatable<StockOperationData>
	{
		public int type;

		public int stockIndex;

		public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
		{
			serializer.SerializeValue(ref type);

			serializer.SerializeValue(ref stockIndex);
		}

		public bool Equals(StockOperationData other)
		{
			return type == other.type
				&& stockIndex == other.stockIndex;
		}
	}

	[ServerRpc]
	public void SetStockTransferDataServerRpc(StockTransferData[] data)
	{
		SetStockTransferDataClientRpc(data);
	}

	[Rpc(SendTo.NotServer)]
	public void SetStockTransferDataClientRpc(StockTransferData[] data)
	{
		_gameWorld.SetNetworkStockTransfer(data);
	}

	[ServerRpc]
	public void SetStockAddDataServerRpc(StockOperationData data)
	{
		SetStockAddDataClientRpc(data);
	}

	[Rpc(SendTo.NotServer)]
	public void SetStockAddDataClientRpc(StockOperationData data)
	{
		_gameWorld.SetNetworkStockAdd(data);
	}

	[ServerRpc]
	public void SetStockTakeDataServerRpc(StockOperationData data)
	{
		SetStockTakeDataClientRpc(data);
	}

	[Rpc(SendTo.NotServer)]
	public void SetStockTakeDataClientRpc(StockOperationData data)
	{
		_gameWorld.SetNetworkStockTake(data);
	}

	public override void OnNetworkSpawn()
	{
		OnSpawn?.Invoke(this);
	}
	public override void OnNetworkDespawn()
	{
		Unregister();
	}

	public void Register(GameWorld gameWorld)
	{
		_gameWorld = gameWorld;
	}
	public void Unregister()
	{
		_gameWorld = null;
	}

	public void UpdateBuildingData(Building[] buildings)
	{
		NetworkBuildingList.Clear();

		for (int i = 0; i < buildings.Length; i++)
		{
			NetworkBuildingList.Add(new BuildingData
			{
				index = i,
				Progress = buildings[i].Production.ProductProgress
			});
		}
	}
}