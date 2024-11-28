using Cysharp.Threading.Tasks;
using GameName.Data;
using GameName.PlayerProfile;
using R3;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using UnityEngine;

public class SessionService
{
	public readonly Subject<Unit> OnCreate = new();
	public readonly Subject<Unit> OnJoin = new();
	public readonly Subject<Unit> OnLeave = new();

	private QuerySessionsResults _sessionQueryResults;

	private ISession _activeSession;

	public ISession ActiveSession
	{
		get => _activeSession;
		private set
		{
			if (value != null)
			{
				_activeSession = value;

				RegisterSessionEvents();

				Debug.Log($"Joined Session {_activeSession.Id}");

				OnJoin.OnNext(Unit.Default);
			}
			else if (_activeSession != null)
			{
				Debug.Log($"Leave Session {_activeSession.Id}");

				_activeSession = null;

				OnLeave.OnNext(Unit.Default);
			}
		}
	}

	public enum SessionAction
	{
		Invalid,
		Create,
		StartMatchmaking,
		QuickJoin,
		JoinByCode,
		JoinById
	}

	public struct AdditionalOptions
	{
		public MatchmakerOptions MatchmakerOptions;
		public bool AutoCreateSession;
	}

	public struct SessionData
	{
		public NetworkConfigData NetworkConfig;

		public SessionAction SessionAction;

		public string SessionName;
		public string JoinCode;
		public string Id;

		public AdditionalOptions AdditionalOptions;
	}

	private Profile _profile;

	public SessionService(Profile profile)
	{
		_profile = profile;
	}

	public async UniTask<IList<ISessionInfo>> QuerySessions()
	{
		QuerySessionsOptions sessionQueryOptions = new QuerySessionsOptions();
		_sessionQueryResults = await MultiplayerService.Instance.QuerySessionsAsync(sessionQueryOptions);

		return _sessionQueryResults.Sessions;
	}

	public async UniTask EnterSession(SessionData sessionData)
	{
		try
		{
			if (_activeSession != null)
			{
				await LeaveSession();
			}

			Dictionary<string, PlayerProperty> playerProperties = GetPlayerProperties();

			Debug.Log("Joining Session...");

			JoinSessionOptions joinSessionOptions = new JoinSessionOptions
			{
				PlayerProperties = playerProperties
			};

			SessionOptions sessionOptions = new SessionOptions
			{
				MaxPlayers = sessionData.NetworkConfig.MaxPlayers,
				IsLocked = false,
				IsPrivate = false,
				PlayerProperties = playerProperties,
				Name = $"{sessionData.Id} {(sessionData.SessionAction == SessionAction.Create ? sessionData.SessionName : Guid.NewGuid().ToString())}"
			};

			sessionOptions.WithRelayNetwork();

			switch (sessionData.SessionAction)
			{
				case SessionAction.Create:
					ActiveSession = await MultiplayerService.Instance.CreateSessionAsync(sessionOptions);

					OnCreate.OnNext(Unit.Default);

					break;
				case SessionAction.StartMatchmaking:
					ActiveSession = await MultiplayerService.Instance.MatchmakeSessionAsync(sessionData.AdditionalOptions.MatchmakerOptions, sessionOptions);
					break;
				case SessionAction.QuickJoin:
					var quickJoinOptions = new QuickJoinOptions
					{
						CreateSession = sessionData.AdditionalOptions.AutoCreateSession
					};
					ActiveSession = await MultiplayerService.Instance.MatchmakeSessionAsync(quickJoinOptions, sessionOptions);
					break;
				case SessionAction.JoinByCode:
					ActiveSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(sessionData.JoinCode, joinSessionOptions);
					break;
				case SessionAction.JoinById:
					ActiveSession = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionData.Id, joinSessionOptions);
					break;
				case SessionAction.Invalid:
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
		catch (SessionException sessionException)
		{
			HandleSessionException(sessionException);
		}
		catch (AggregateException aggregateException)
		{
			aggregateException.Handle(ex =>
			{
				if (ex is SessionException sessionException)
				{
					HandleSessionException(sessionException);

					return true;
				}

				return false;
			});
		}
	}

	private void HandleSessionException(SessionException sessionException)
	{
		Debug.LogException(sessionException);

		ActiveSession = null;
	}

	private Dictionary<string, PlayerProperty> GetPlayerProperties()
	{
		//var playerName = await AuthenticationService.Instance.GetPlayerNameAsync();
		var playerNameProperty = new PlayerProperty(_profile.Get<AppData>().Data.Name, VisibilityPropertyOptions.Member);
		var playerProperties = new Dictionary<string, PlayerProperty> { { "w_PlayerName", playerNameProperty } };
		
		return playerProperties;
	}

	public async UniTask LeaveSession()
	{
		if (ActiveSession != null)
		{
			UnregisterPlayerEvents();

			try
			{
				await ActiveSession.LeaveAsync();
			}
			catch
			{
				// Ignored as we are exiting the game
			}
			finally
			{
				ActiveSession = null;
			}
		}
	}

	public async void KickPlayer(string playerId)
	{
		if (!ActiveSession.IsHost)
			return;

		await ActiveSession.AsHost().RemovePlayerAsync(playerId);
	}

	private void RegisterSessionEvents()
	{
		ActiveSession.PlayerJoined += OnPlayerJoinedSession;

		/*ActiveSession.Changed += m_WidgetEventDispatcher.OnSessionChanged;
		ActiveSession.StateChanged += m_WidgetEventDispatcher.OnSessionStateChanged;
		ActiveSession.PlayerJoined += m_WidgetEventDispatcher.OnPlayerJoinedSession;
		ActiveSession.PlayerLeft += m_WidgetEventDispatcher.OnPlayerLeftSession;
		ActiveSession.SessionPropertiesChanged += m_WidgetEventDispatcher.OnSessionPropertiesChanged;
		ActiveSession.PlayerPropertiesChanged += m_WidgetEventDispatcher.OnPlayerPropertiesChanged;
		ActiveSession.RemovedFromSession += m_WidgetEventDispatcher.OnRemovedFromSession;
		ActiveSession.Deleted += m_WidgetEventDispatcher.OnSessionDeleted;*/

		ActiveSession.RemovedFromSession += OnRemovedFromSession;
	}

	private void UnregisterPlayerEvents()
	{
		ActiveSession.PlayerJoined -= OnPlayerJoinedSession;

		/*ActiveSession.Changed -= m_WidgetEventDispatcher.OnSessionChanged;
		ActiveSession.StateChanged -= m_WidgetEventDispatcher.OnSessionStateChanged;
		ActiveSession.PlayerJoined -= m_WidgetEventDispatcher.OnPlayerJoinedSession;
		ActiveSession.PlayerLeft -= m_WidgetEventDispatcher.OnPlayerLeftSession;
		ActiveSession.SessionPropertiesChanged -= m_WidgetEventDispatcher.OnSessionPropertiesChanged;
		ActiveSession.PlayerPropertiesChanged -= m_WidgetEventDispatcher.OnPlayerPropertiesChanged;
		ActiveSession.RemovedFromSession -= m_WidgetEventDispatcher.OnRemovedFromSession;
		ActiveSession.Deleted -= m_WidgetEventDispatcher.OnSessionDeleted;*/

		ActiveSession.RemovedFromSession -= OnRemovedFromSession;
	}

	private void OnPlayerJoinedSession(string value)
	{
		Debug.Log(value);
	}

	private async void OnRemovedFromSession()
	{
		await LeaveSession();
	}

	/*private static void SetConnection(ref SessionOptions options)
	{
		options.WithRelayNetwork();
	}*/
}