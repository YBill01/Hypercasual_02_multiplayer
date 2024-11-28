using UnityEngine;

namespace GameName.Data
{
	[CreateAssetMenu(menuName = "GameName/Core/NetworkConfigData", fileName = "NetworkConfig", order = 7)]

	public class NetworkConfigData : ScriptableObject
	{
		public int MaxPlayers = 4;

		[Space]
		public GameObject playerPrefab;

		public Vector3 playerSpawnPosition;
		public float playerSpawnRotation;

		[Space]
		public Color[] playerColors;
	}
}