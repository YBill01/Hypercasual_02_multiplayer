using TMPro;
using Unity.Services.Authentication;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class UIVersionComponent : MonoBehaviour
{
	private void Start()
	{
		//GetComponent<TMP_Text>().text = $"v{Application.version}";
		GetComponent<TMP_Text>().text = $"<color=yellow>PlayerID:</color> <color=green>{AuthenticationService.Instance.PlayerId}</color> | v{Application.version}";
	}
}