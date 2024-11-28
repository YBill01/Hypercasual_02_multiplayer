using TMPro;
using UnityEngine;

public class UIMessageComponent : MonoBehaviour
{
	[SerializeField]
	private TMP_Text m_mesageText;

	public void Message(string text)
	{
		m_mesageText.text = text;
	}
}