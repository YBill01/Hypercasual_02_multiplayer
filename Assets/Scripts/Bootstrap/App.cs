using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace GameName.PlayerProfile
{
	public class App : MonoBehaviour
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		public static void Initialize()
		{
			Application.targetFrameRate = 60;
			Application.runInBackground = true;
			Screen.sleepTimeout = SleepTimeout.NeverSleep;
		}

		private Profile _profile;

		[Inject]
		public void Construct(Profile profile)
		{
			_profile = profile;
		}

		private void Awake()
		{
			DontDestroyOnLoad(this);
		}

		public async UniTask ProfileSave()
		{
			UniTask<AppData> taskAppData = _profile.Get<AppData>().SaveCloudAsync();
			UniTask<PlayerData> taskPlayerData = _profile.Get<PlayerData>().SaveCloudAsync();

			await UniTask.WhenAll(taskAppData, taskPlayerData);

			//_profile.Get<AppData>().Save();
			//_profile.Get<PlayerData>().Save();
		}

		private void OnDestroy()
		{
#if UNITY_EDITOR
			ProfileSave().Forget();
#endif
		}

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
		private void OnApplicationFocus(bool focus)
		{
			if (!focus)
			{
				ProfileSave().Forget();
			}
		}
#endif

		public async UniTaskVoid Quit()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;

			await UniTask.Yield();
#else
			await ProfileSave();

			Application.Quit();
#endif
		}
	}
}