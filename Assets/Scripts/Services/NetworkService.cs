using Cysharp.Threading.Tasks;
using GameName.Data;
using R3;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class NetworkService : IPostStartable, IDisposable
{
	public event Action<NPlayer> OnPlayerSpawn;
	public event Action<NPlayer> OnPlayerRemove;

	private Dictionary<ulong, int> _playerColors;

	private CompositeDisposable _compositeDisposable;

	private IObjectResolver _resolver;

	private SessionService _sessionService;
	private NetworkConfigData _networkConfig;

	public NetworkService(
		IObjectResolver resolver,
		SessionService sessionService,
		NetworkConfigData networkConfig)
	{
		_resolver = resolver;
		_sessionService = sessionService;
		_networkConfig = networkConfig;
	}

	public void PostStart()
	{
		_playerColors = new Dictionary<ulong, int>();

		_compositeDisposable = new CompositeDisposable();

		_sessionService.OnCreate
			.Subscribe(_ => SessionOnCreate())
			.AddTo(_compositeDisposable);

		_sessionService.OnJoin
			.Subscribe(_ => SessionOnJoin())
			.AddTo(_compositeDisposable);

		_sessionService.OnLeave
			.Subscribe(_ => SessionOnLeave())
			.AddTo(_compositeDisposable);

		NetworkManager.Singleton.OnConnectionEvent += OnConnectionEvent;
	}

	private void OnConnectionEvent(NetworkManager networkManager, ConnectionEventData connectionEventData)
	{
		if (networkManager.IsServer)
		{
			if (connectionEventData.EventType == ConnectionEvent.ClientConnected)
			{
				PlayerSpawn(connectionEventData.ClientId);
			}
			else if (connectionEventData.EventType == ConnectionEvent.ClientDisconnected)
			{
				if (connectionEventData.ClientId != networkManager.LocalClientId)
				{
					PlayerDespawned(connectionEventData.ClientId);
				}
			}
		}
		else
		{
			if (connectionEventData.EventType == ConnectionEvent.ClientConnected || connectionEventData.EventType == ConnectionEvent.PeerConnected)
			{
				PlayerSpawned(connectionEventData.ClientId);
			}
			else if (networkManager.IsConnectedClient && (connectionEventData.EventType == ConnectionEvent.ClientDisconnected || connectionEventData.EventType == ConnectionEvent.PeerDisconnected))
			{
				if (connectionEventData.ClientId == networkManager.LocalClientId)
				{
					_sessionService.LeaveSession().Forget();
				}
				else
				{
					PlayerDespawned(connectionEventData.ClientId);
				}
			}
		}
	}

	public void PlayerSpawn(ulong clientId)
	{
		if (NetworkManager.Singleton.IsServer)
		{
			GameObject gameObject = GameObject.Instantiate(_networkConfig.playerPrefab);
			
			NPlayer player = gameObject.GetComponent<NPlayer>();

			player.ClientId.Value = clientId;
			player.ClientColor.Value = GetPlayerColor(clientId);
			
			gameObject.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

			PlayerSpawned(clientId);
		}
	}

	public async void PlayerSpawned(ulong clientId)
	{
		await UniTask.NextFrame();

		foreach (NetworkObject networkObject in NetworkManager.Singleton.SpawnManager.PlayerObjects)
		{
			NPlayer player = networkObject.GetComponent<NPlayer>();

			if (!player.IsRegistered)
			{
				OnPlayerSpawn?.Invoke(player);
			}
		}
	}

	public void PlayerDespawned(ulong clientId)
	{
		if (NetworkManager.Singleton.IsServer)
		{
			ClearPlayerColor(clientId);
		}
	}

	private void SessionOnCreate()
	{
		Debug.Log($"SessionOnCreate: players - {NetworkManager.Singleton.ConnectedClientsList.Count}");
	}
	private void SessionOnJoin()
	{
		Debug.Log($"SessionOnJoin: players - {NetworkManager.Singleton.ConnectedClientsList.Count}");
	}
	private void SessionOnLeave()
	{
		ClearPlayerColor();

		Debug.Log($"SessionOnLeave: players - {NetworkManager.Singleton.ConnectedClientsList.Count}");
	}

	private Color GetPlayerColor(ulong clientId)
	{
		for (int i = 0; i < _networkConfig.playerColors.Length; i++)
		{
			if (!_playerColors.ContainsValue(i))
			{
				_playerColors.Add(clientId, i);

				return _networkConfig.playerColors[i];
			}
		}

		int colorIndex = UnityEngine.Random.Range(0, _networkConfig.playerColors.Length);

		_playerColors.Add(clientId, colorIndex);

		return _networkConfig.playerColors[colorIndex];
	}
	private void ClearPlayerColor(ulong clientId)
	{
		if (_playerColors.ContainsKey(clientId))
		{
			_playerColors.Remove(clientId);
		}
	}
	private void ClearPlayerColor()
	{
		_playerColors.Clear();
	}

	public void Dispose()
	{
		_compositeDisposable.Dispose();
	}
}