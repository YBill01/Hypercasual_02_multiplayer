using UnityEngine;
using GameName.Data;
using Unity.Netcode;
using System;
using GameName.PlayerProfile;
using System.Collections.Generic;
using VContainer;
using VContainer.Unity;
using UnityEditor;

public class NPlayer : NetworkBehaviour, IUpdatable, ISaveable<PlayerData.PlayerStats>, IStocked
{
	[SerializeField]
	private PlayerConfigData m_config;

	[field: SerializeField, Space]
	public VCameraTarget CameraTarget { get; private set; }

	[Header("Stock")]
	[SerializeField]
	private Stock m_stock;

	[Header("Kick")]
	[SerializeField]
	private float m_kickStrength = 1.0f;
	[SerializeField]
	private float m_kickRadius = 1.0f;

	[SerializeField]
	private LayerMask m_kickInteractLayerMask;

	[Space]
	[SerializeField]
	private SkinnedMeshRenderer m_meshRenderer;

	[NonSerialized]
	public NetworkVariable<ulong> ClientId = new(0);
	[NonSerialized]
	public NetworkVariable<Color> ClientColor = new(Color.white);

	public Stock Stock => m_stock;

	public bool IsRegistered { get; private set; }

	private GameWorld _gameWorld;

	private NPlayerController _controller;

	private List<StockLink> _stockLinks;

	public bool IsCollecting => _stockLinks.Count > 0;
	public bool IsCollectingCooldown => _collectingCooldown > 0.0f;

	private float _collectingCooldown = 0.0f;

	[NonSerialized]
	public NetworkVariable<bool> IsKick = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	public bool IsKickCooldown => _kickCooldown > 0.0f;

	private float _kickCooldown = 0.0f;

	private IObjectResolver _resolver;

	private StockEvents _stockEvents;

	[Inject]
	public void Construct(
		IObjectResolver resolver,
		StockEvents stockEvents)
	{
		_resolver = resolver;
		_stockEvents = stockEvents;

		_resolver.InjectGameObject(m_stock.gameObject);
	}

	private void Awake()
	{
		_controller = GetComponent<NPlayerController>();

		_stockLinks = new List<StockLink>();
	}

	public override void OnNetworkSpawn()
	{
		m_meshRenderer.material.SetColor("_BaseColor", ClientColor.Value);

		if (!IsOwner)
		{
			Destroy(GetComponent<NAvatar>());
			Destroy(GetComponent<NPlayerController>());
		}
		
		Debug.Log($"OnNetworkSpawn {ClientId.Value} {ClientColor.Value}");
	}
	public override void OnNetworkDespawn()
	{
		_gameWorld.UnregisterPlayer(this);
	}


	public void SetOrientation(Vector3 position, Quaternion rotation)
	{
		if (IsOwner)
		{
			_controller.SetOrientation(position, rotation);
		}
	}

	public void OnUpdate(float deltaTime)
	{
		m_stock.OnUpdate(deltaTime);

		if (IsServer)
		{
			if (IsCollectingCooldown)
			{
				_collectingCooldown -= deltaTime;
			}

			if (IsCollecting && !IsCollectingCooldown)
			{
				if (TrySendCollecting())
				{
					_collectingCooldown = m_config.collectingDuration;

					_controller.Collect();
				}
			}

			if (IsKickCooldown)
			{
				_kickCooldown -= deltaTime;
			}

			if (IsKick.Value && !IsKickCooldown)
			{
				if (TryKick())
				{
					_kickCooldown = m_config.kickDelay;
				}
			}
		}

		if (IsOwner)
		{
			_controller.OnUpdate(deltaTime);
		}
	}

	public bool TryStockLink(StockLink link)
	{
		if (_stockLinks.Contains(link))
		{
			return false;
		}

		_stockLinks.Add(link);

		return true;
	}
	public bool TryStockUnlink(StockLink link)
	{
		if (_stockLinks.Contains(link))
		{
			return _stockLinks.Remove(link);
		}

		return false;
	}

	private bool TrySendCollecting()
	{
		for (int i = 0; i < _stockLinks.Count; i++)
		{
			foreach (StockItem item in _stockLinks[i].stockIn)
			{
				if (!item.inactive && _stockLinks[i].stockOut.HasEmpty(item))
				{
					_stockEvents.OnTransfer.OnNext(new StockTransfer
					{
						item = item,
						stockIn = _stockLinks[i].stockIn,
						stockOut = _stockLinks[i].stockOut
					});

					return true;
				}
			}
		}

		return false;
	}

	private bool TryKick()
	{
		Collider[] colliders = Physics.OverlapSphere(transform.position, m_kickRadius, m_kickInteractLayerMask);

		bool kicked = false;

		for (int i = 0; i < colliders.Length; i++)
		{
			if (colliders[i].TryGetComponent(out NPlayer playerComponent))
			{
				if (playerComponent != this)
				{
					Vector3 position1 = playerComponent.transform.position;
					position1.y = 0.0f;
					Vector3 position2 = transform.position;
					position2.y = 0.0f;

					playerComponent.KickClientRpc((position1 - position2).normalized, m_kickStrength);

					kicked = true;
				}
			}
		}

		return kicked;
	}

	[Rpc(SendTo.Owner)]
	public void KickClientRpc(Vector3 direction, float strength)
	{
		_controller.Impact(direction, strength);
	}

	public void Register(GameWorld gameWorld)
	{
		_gameWorld = gameWorld;

		IsRegistered = true;
	}
	public void Unregister()
	{
		_gameWorld = null;

		IsRegistered = false;
	}

	public PlayerData.PlayerStats GetSaveData()
	{
		PlayerData.PlayerStats stats = new PlayerData.PlayerStats();

		stats.stock = m_stock.GetSaveData();

		return stats;
	}
	public bool SetSaveData(PlayerData.PlayerStats data)
	{
		try
		{
			if (!m_stock.SetSaveData(data.stock))
			{
				return false;
			}

			return true;
		}
		catch (System.Exception)
		{
			return false;
		}
	}

#if UNITY_EDITOR
	private void OnDrawGizmosSelected()
	{
		if (Application.isPlaying)
		{
			Handles.color = IsKick.Value ? Color.red : Color.gray;
			Handles.DrawWireDisc(transform.position, Vector3.up, m_kickRadius, 2.0f);
			Handles.color = Color.white;
		}
	}
#endif
}