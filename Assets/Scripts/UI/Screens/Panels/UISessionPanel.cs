using Cysharp.Threading.Tasks;
using GameName.Data;
using System.Collections.Generic;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

public class UISessionPanel : MonoBehaviour
{
	[SerializeField]
	private GameObject m_contentParent;

	[Space]
	[SerializeField]
	private UISessionItemComponent m_sessionItemPrefab;

	[Space]
	[SerializeField]
	private Button m_refreshButton;

	private IList<ISessionInfo> _sessions;

	private CoreFlow _coreFlow;
	private SessionService _sessionService;
	private NetworkConfigData _networkConfig;

	private IList<UISessionItemComponent> _itemsList = new List<UISessionItemComponent>();

	public void Init(CoreFlow coreFlow, SessionService sessionService, NetworkConfigData networkConfig)
	{
		_coreFlow = coreFlow;
		_sessionService = sessionService;
		_networkConfig = networkConfig;

		RefreshSessionList();
	}

	private void OnEnable()
	{
		m_refreshButton.onClick.AddListener(RefreshButtonOnClick);
	}
	private void OnDisable()
	{
		m_refreshButton.onClick.RemoveListener(RefreshButtonOnClick);
	}

	public async void RefreshSessionList()
	{
		try
		{
			await UpdateSessions();
		}
		catch (System.Exception)
		{
			return;
		}

		foreach (UISessionItemComponent item in _itemsList)
		{
			Destroy(item.gameObject);
		}

		_itemsList.Clear();

		if (_sessions == null)
		{
			return;
		}

		foreach (ISessionInfo session in _sessions)
		{
			UISessionItemComponent item = Instantiate(m_sessionItemPrefab, m_contentParent.transform);

			item.SetSession(session);
			item.OnJoinSession += JoinSession;

			_itemsList.Add(item);
		}
	}

	private async UniTask UpdateSessions()
	{
		_sessions = await _sessionService.QuerySessions();
	}

	public void JoinSession(ISessionInfo session)
	{
		_coreFlow.JoinGame(session);
	}

	private void RefreshButtonOnClick()
	{
		RefreshSessionList();
	}
}