using System;
using TMPro;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UISessionItemComponent : MonoBehaviour
{
	public event Action<ISessionInfo> OnJoinSession;

	[SerializeField]
	private TMP_Text m_sessionNameText;

	[SerializeField]
	private TMP_Text m_sessionPlayersText;

	private ISessionInfo _sessionInfo;

	private Button _joinButton;

	private void Awake()
	{
		_joinButton = GetComponent<Button>();
	}

	private void OnEnable()
	{
		_joinButton.onClick.AddListener(OnJoinButtonOnClick);
	}
	private void OnDisable()
	{
		_joinButton.onClick.RemoveListener(OnJoinButtonOnClick);
	}

	public void SetSession(ISessionInfo sessionInfo)
	{
		_sessionInfo = sessionInfo;

		SetSessionName(_sessionInfo.Name);

		int currentPlayers = _sessionInfo.MaxPlayers - _sessionInfo.AvailableSlots;
		SetPlayers(currentPlayers, _sessionInfo.MaxPlayers);

		_joinButton.interactable = _sessionInfo.AvailableSlots > 0;
	}

	private void SetSessionName(string sessionName)
	{
		m_sessionNameText.text = sessionName;
	}

	private void SetPlayers(int currentPlayers, int maxPlayers)
	{
		m_sessionPlayersText.text = $"{currentPlayers}/{maxPlayers}";
	}

	private void OnJoinButtonOnClick()
	{
		OnJoinSession?.Invoke(_sessionInfo);
	}
}