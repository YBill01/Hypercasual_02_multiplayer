using Cysharp.Threading.Tasks;
using GameName.PlayerProfile;
using GameName.Data;
using UnityEngine;
using VContainer;
using System;

public class Game : MonoBehaviour, IPausable
{
	private bool _isPlayGame = false;
	public bool IsPlayGame => _isPlayGame;

	private bool _isPaused = false;
	public bool IsPaused => _isPaused;

	private Profile _profile;
	private GameConfigData _gameConfig;
	private GameWorld _gameWorld;
	private NetworkService _networkService;

	[Inject]
	public void Construct(
		Profile profile,
		GameConfigData gameConfig,
		NetworkService networkService,
		GameWorld gameWorld)
	{
		_profile = profile;
		_gameConfig = gameConfig;
		_gameWorld = gameWorld;
		_networkService = networkService;
	}

	private void OnEnable()
	{
		_networkService.OnPlayerSpawn += OnPlayerSpawn;
		_networkService.OnPlayerRemove += OnPlayerRemove;
	}

	

	private void OnDisable()
	{
		_networkService.OnPlayerSpawn -= OnPlayerSpawn;
		_networkService.OnPlayerRemove -= OnPlayerRemove;
	}

	private void Update()
	{
		if (!_isPlayGame || _isPaused)
		{
			return;
		}
		
		float deltaTime = Time.deltaTime;

		_gameWorld.OnUpdate(deltaTime);
	}

	public async UniTask StartGame()
	{
		await UniTask.NextFrame();

		//_gameWorld.SetSaveData(_profile.Get<PlayerData>().Data);
		
		await UniTask.NextFrame();

		_isPlayGame = true;
	}
	public void SetPause(bool pause)
	{
		_isPaused = pause;

		//_gameWorld.SetPause(pause);
	}
	public void EndGame()
	{
		_isPlayGame = false;
	}

	private void OnPlayerSpawn(NPlayer player)
	{
		_gameWorld.RegisterPlayer(player);

		if (_gameWorld.IsPlayerSpawned)
		{
			SetPause(false);
		}
	}
	private void OnPlayerRemove(NPlayer player)
	{
		_gameWorld.UnregisterPlayer(player);
	}
}