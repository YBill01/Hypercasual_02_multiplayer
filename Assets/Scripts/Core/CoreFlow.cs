using GameName.Data;
using GameName.PlayerProfile;
using VContainer.Unity;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;
using System;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using Unity.Services.Multiplayer;
using R3;

public class CoreFlow : IStartable
{
	public event Action<CoreState> OnStatePreChange;
	public event Action<CoreState> OnStateChanged;
	public event Action<bool> OnPauseChanged;
	public event Action OnEndGame;

	private App _app;
	private LoaderService _loaderService;
	private ScenesData _scenes;
	private Profile _profile;
	private SessionService _sessionService;
	private NetworkService _networkService;
	private GameConfigData _gameConfig;
	private NetworkConfigData _networkConfig;

	private SceneInstance _sceneGame;

	public CoreState State { get; private set; }

	public enum CoreState
	{
		Transition,
		Meta,
		Game
	}

	private CompositeDisposable _compositeDisposable;

	public CoreFlow(
		App app,
		LoaderService loaderService,
		ScenesData scenes,
		Profile profile,
		SessionService sessionService,
		NetworkService networkService,
		GameConfigData gameConfig,
		NetworkConfigData networkConfig)
	{
		_app = app;
		_loaderService = loaderService;
		_scenes = scenes;
		_profile = profile;
		_sessionService = sessionService;
		_networkService = networkService;
		_gameConfig = gameConfig;
		_networkConfig = networkConfig;
	}

	public void Start()
	{
		_gameConfig.viewData.HashingData();

		State = CoreState.Meta;

		_compositeDisposable = new CompositeDisposable();

		_sessionService.OnLeave
			.Subscribe(_ => OnLeaveSession())
			.AddTo(_compositeDisposable);
	}

	public void InitConfig(bool reset)
	{
		AppData appData = _profile.Get<AppData>().Data;
		appData.lastEntryDate = DateTime.UtcNow;
		
		if (appData.firstPlay || reset)
		{
			_profile.Get<PlayerData>().Clear();

			PlayerData playerData = _profile.Get<PlayerData>().Data;

			playerData.playerStats.position = _gameConfig.startPlayerPosition;
			playerData.playerStats.quaternion = Quaternion.AngleAxis(_gameConfig.startPlayerRotation, Vector3.up);
			playerData.playerStats.cameraZoom = _gameConfig.startPlayerCameraZoom;

			appData.firstPlay = false;
		}
	}

	public async UniTask NetworkCreateSession()
	{
		if (_sessionService.ActiveSession == null)
		{
			await _sessionService.EnterSession(new SessionService.SessionData
			{
				NetworkConfig = _networkConfig,
				SessionAction = SessionService.SessionAction.Create,
				SessionName = _profile.Get<AppData>().Data.Name,
				AdditionalOptions = new SessionService.AdditionalOptions
				{
					AutoCreateSession = true
				}
			});
		}
	}
	public async UniTask NetworkJoinSession(ISessionInfo session)
	{
		if (_sessionService.ActiveSession == null)
		{
			await _sessionService.EnterSession(new SessionService.SessionData
			{
				NetworkConfig = _networkConfig,
				SessionAction = SessionService.SessionAction.JoinById,
				SessionName = session.Name,
				Id = session.Id
			});
		}
	}


	public void StartGame()
	{
		StartGame(async () =>
		{
			await NetworkCreateSession();

			OnStateChanged?.Invoke(State);
		});
	}
	public void JoinGame(ISessionInfo session)
	{
		StartGame(async () =>
		{
			await NetworkJoinSession(session);

			OnStateChanged?.Invoke(State);
		});
	}

	private void StartGame(Action action = null)
	{
		OnStatePreChange?.Invoke(State);

		State = CoreState.Transition;

		OnStateChanged?.Invoke(State);

		ScenesData.Scene scene = _scenes.GetSceneDataAt(ScenesData.SceneId.GAME);
		_loaderService.LoadAddressableScene(scene.sceneAsset, scene.loadMode)
			.OnCompleteAsync((s) =>
			{
				_sceneGame = s;
				SceneManager.SetActiveScene(_sceneGame.Scene);

				State = CoreState.Game;

				action?.Invoke();

				//OnStateChanged?.Invoke(State);
			});
	}

	public async void EndGame()
	{
		OnEndGame?.Invoke();

		await _sessionService.LeaveSession();
	}

	private void OnLeaveSession()
	{
		OnStatePreChange?.Invoke(State);

		State = CoreState.Meta;

		_loaderService.UnloadAddressableScene(_sceneGame);

		OnStateChanged?.Invoke(State);
	}

	public void SetPauseValue(bool value)
	{
		OnPauseChanged?.Invoke(value);
	}
}