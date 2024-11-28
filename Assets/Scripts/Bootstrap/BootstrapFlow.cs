using GameName.Data;
using GameName.PlayerProfile;
using System;
using UnityEngine;
using VContainer.Unity;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Cysharp.Threading.Tasks;

public class BootstrapFlow : IStartable
{
	private LoaderService _loaderService;
	private ScenesData _scenes;
	private Profile _profile;
	private UIMessageComponent _messagePrefab;

	public BootstrapFlow(
		LoaderService loaderService,
		ScenesData scenes,
		Profile profile,
		UIMessageComponent messagePrefab)
	{
		_loaderService = loaderService;
		_scenes = scenes;
		_profile = profile;
		_messagePrefab = messagePrefab;
	}

	public async void Start()
	{
		if (Application.internetReachability == NetworkReachability.NotReachable)
		{
			UIMessageComponent uiMessage = GameObject.Instantiate<UIMessageComponent>(_messagePrefab);
			uiMessage.Message("Not connected to internet.");

			Debug.LogException(new Exception("Not connected to internet."));

			return;
		}

		try
		{
			await UnityServices.InitializeAsync();
			await AuthenticationService.Instance.SignInAnonymouslyAsync();

			Debug.Log($"Sign in anonymously succeeded! <color=yellow>PlayerID:</color> <color=green>{AuthenticationService.Instance.PlayerId}</color>");
		}
		catch (Exception exception)
		{
			UIMessageComponent uiMessage = GameObject.Instantiate<UIMessageComponent>(_messagePrefab);
			uiMessage.Message(exception.Message);

			Debug.LogException(exception);

			return;
		}

		UniTask<AppData> taskAppData = _profile.Get<AppData>().LoadCloudAsync();
		UniTask<PlayerData> taskPlayerData = _profile.Get<PlayerData>().LoadCloudAsync();

		(AppData appData, PlayerData playerData) = await UniTask.WhenAll(taskAppData, taskPlayerData);

		appData.lastEntryDate = DateTime.UtcNow;

		ScenesData.Scene scene = _scenes.GetSceneDataAt(ScenesData.SceneId.CORE);
		_loaderService.LoadAddressableScene(scene.sceneAsset, scene.loadMode)
			.OnComplete(() =>
			{
				//Debug.Log($"Loading scene: {scene.id} - complete");
			});
	}
}