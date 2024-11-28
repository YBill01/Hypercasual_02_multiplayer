using GameName.Data;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

public class NetworkObjectBehaviour : NetworkBehaviour
{
	[SerializeField]
	private MeshRenderer m_meshRenderer;


	public NetworkVariable<ulong> playerId = new(0);
	public NetworkVariable<Color> playerColor = new(Color.white);

	private int _playerId = 0;

	private NetworkConfigData _networkConfig;

	[Inject]
	public void Construct(
		NetworkConfigData networkConfig)
	{
		_networkConfig = networkConfig;
	}

	public void Init()
	{
		//m_meshRenderer.material.SetColor("_BaseColor", _networkConfig.playerColors[playerId.Value]);
	}

	[ServerRpc]
	private void InitServerRpc(int playerId)
	{
		Debug.Log($"Server <color=green>{playerId}</color>");
	}

	[ClientRpc]
	private void InitClientRpc(int playerId)
	{
		Debug.Log($"Client <color=green>{playerId}</color>");
	}

	/*private void OnNetworkInstantiate()
	{
		Debug.Log($"OnNetworkInstantiate");
	}*/

	public override void OnNetworkSpawn()
	{
		//__initializeVariables();
		m_meshRenderer.material.SetColor("_BaseColor", playerColor.Value);
		
		//m_meshRenderer.material.color = _networkConfig.playerColors[_playerId];
		//m_meshRenderer.material.SetColor("_BaseColor", _networkConfig.playerColors[_playerId]);
		//Debug.Log($"..............");
		//Debug.Log(NetworkObjectId);
		//Debug.Log(OwnerClientId);
		//Debug.Log(OwnerClientId);
		//Debug.Log(NetworkManager.Singleton.SpawnManager.PlayerObjects.Count);

		Debug.Log($"OnNetworkSpawn {_playerId}");
	}
	public override void OnNetworkDespawn()
	{


		Debug.Log($"OnNetworkDespawn");
	}

	protected override void OnNetworkPostSpawn()
	{
		Debug.Log($"..............");

		Debug.Log($"NetworkObjectId {NetworkObjectId}");
		Debug.Log($"Count {NetworkManager.Singleton.SpawnManager.PlayerObjects.Count}");

		Debug.Log($"OnNetworkPostSpawn");
	}


	private void Update()
	{
		if (IsServer)
		{
			if (Keyboard.current[Key.Space].wasPressedThisFrame)
			{
				Debug.Log($"IsServer is 'Space' pressed.");
			}
		}

		if (IsSessionOwner)
		{
			if (Keyboard.current[Key.Space].wasPressedThisFrame)
			{
				Debug.Log($"IsSessionOwner is 'Space' pressed.");
			}
		}

		if (IsHost)
		{
			if (Keyboard.current[Key.Space].wasPressedThisFrame)
			{
				Debug.Log($"IsHost is 'Space' pressed.");
			}
		}

		if (IsClient)
		{
			if (Keyboard.current[Key.Space].wasPressedThisFrame)
			{
				Debug.Log($"IsClient is 'Space' pressed.");
			}
		}

		if (IsOwner)
		{
			if (Keyboard.current[Key.Space].wasPressedThisFrame)
			{
				Debug.Log($"IsOwner is 'Space' pressed.");
			}
		}


		Vector3 v = Vector3.zero;
		if (Keyboard.current[Key.W].isPressed)
		{
			v.y += 1.0f;
		}
		if (Keyboard.current[Key.S].isPressed)
		{
			v.y -= 1.0f;
		}
		if (Keyboard.current[Key.A].isPressed)
		{
			v.x -= 1.0f;
		}
		if (Keyboard.current[Key.D].isPressed)
		{
			v.x += 1.0f;
		}
		v = v.normalized;
		
		transform.position += v * 1.0f * Time.deltaTime;


	}

	/*public void SetPlayerId(int index)
	{
		_playerId = index;
	}*/


}