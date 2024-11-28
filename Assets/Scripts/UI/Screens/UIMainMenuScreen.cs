using Cysharp.Threading.Tasks;
using GameName.Data;
using GameName.PlayerProfile;
using System;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class UIMainMenuScreen : UIScreen
{
	[Space]
	[SerializeField]
	private GameObject m_menuPanel;
	
	[Space]
	[SerializeField]
	private UISessionPanel m_sessionPanel;

	[Space]
	[SerializeField]
	private Button m_homeButton;
	[SerializeField]
	private Button m_exitButton;

	[Space]
	[SerializeField]
	private UIToggleButton m_pauseToggleButton;

	[Space]
	[SerializeField]
	private Button m_newGameButton;
	[SerializeField]
	private Button m_continueGameButton;

	private App _app;
	private Profile _profile;
	private SessionService _sessionService;
	private NetworkConfigData _networkConfig;
	private CoreFlow _coreFlow;

	[Inject]
	public void Construct(
		App app,
		Profile profile,
		SessionService sessionService,
		NetworkConfigData networkConfig,
		CoreFlow coreFlow)
	{
		_app = app;
		_profile = profile;
		_sessionService = sessionService;
		_networkConfig = networkConfig;
		_coreFlow = coreFlow;
	}

	private void Start()
	{
		ApplyState();
	}

	private void OnEnable()
	{
		m_homeButton.onClick.AddListener(HomeButtonOnClick);
		m_exitButton.onClick.AddListener(ExitButtonOnClick);

		m_pauseToggleButton.onValueChanged += PauseToggleButtonOnValueChanged;

		m_newGameButton.onClick.AddListener(NewGameButtonOnClick);
		m_continueGameButton.onClick.AddListener(ContinueGameButtonOnClick);
	}
	private void OnDisable()
	{
		m_homeButton.onClick.RemoveListener(HomeButtonOnClick);
		m_exitButton.onClick.RemoveListener(ExitButtonOnClick);

		m_pauseToggleButton.onValueChanged += PauseToggleButtonOnValueChanged;

		m_newGameButton.onClick.RemoveListener(NewGameButtonOnClick);
		m_continueGameButton.onClick.RemoveListener(ContinueGameButtonOnClick);
	}

	public void ApplyState()
	{
		switch (_coreFlow.State)
		{
			case CoreFlow.CoreState.Transition:
				m_menuPanel.SetActive(false);
				m_sessionPanel.gameObject.SetActive(false);

				m_homeButton.gameObject.SetActive(false);
				m_exitButton.gameObject.SetActive(false);

				m_pauseToggleButton.gameObject.SetActive(false);
				m_pauseToggleButton.Value = false;

				break;
			case CoreFlow.CoreState.Meta:
				m_menuPanel.SetActive(true);
				m_sessionPanel.gameObject.SetActive(true);
				m_sessionPanel.Init(_coreFlow, _sessionService, _networkConfig);

				m_homeButton.gameObject.SetActive(false);
				m_exitButton.gameObject.SetActive(true);

				m_pauseToggleButton.gameObject.SetActive(false);

				break;
			case CoreFlow.CoreState.Game:
				m_menuPanel.SetActive(false);
				m_sessionPanel.gameObject.SetActive(false);

				m_homeButton.gameObject.SetActive(true);
				m_exitButton.gameObject.SetActive(false);

				m_pauseToggleButton.gameObject.SetActive(true);
				m_pauseToggleButton.Value = false;

				break;
			default:
				break;
		}

		m_continueGameButton.interactable = !_profile.Get<AppData>().Data.firstPlay;

		// dev...
		//m_menuPanel.SetActive(false);

		//m_homeButton.gameObject.SetActive(true);
		//m_exitButton.gameObject.SetActive(true);

		m_pauseToggleButton.gameObject.SetActive(false);
	}

	private void HomeButtonOnClick()
	{
		_coreFlow.EndGame();
	}
	private void ExitButtonOnClick()
	{
		_app.Quit().Forget();
	}

	private void PauseToggleButtonOnValueChanged(bool value)
	{
		_coreFlow.SetPauseValue(value);
	}

	private void NewGameButtonOnClick()
	{
		_coreFlow.InitConfig(true);
		_coreFlow.StartGame();

		m_menuPanel.SetActive(false);
		m_sessionPanel.gameObject.SetActive(false);
	}
	private void ContinueGameButtonOnClick()
	{
		_coreFlow.StartGame();

		m_menuPanel.SetActive(false);
		m_sessionPanel.gameObject.SetActive(false);
	}
}