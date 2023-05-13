using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using TMPro;
using UnityEngine.SceneManagement;

public enum MessageAction
{
	Close,
	LoadScene,
	OpenMenu
}

public class MessageScript : MonoBehaviour
{
	#region Variables

	[SerializeField] protected TMP_Text message;

	protected string actionParam;

	protected MessageAction buttonAction;

	#endregion

	#region Fuctions

	public void ShowMessage(string entry, MessageAction action, bool bLocalized = true, string param = "")
	{
		gameObject.SetActive(true);
		if (bLocalized)
		{
			LocalizedString str = new LocalizedString("Messages", entry);
			message.text = str.GetLocalizedString();
		}
		else
		{
			message.text = entry;
		}
		actionParam = param;
		buttonAction = action;
	}

	private void OnButtonPressed()
	{
		switch (buttonAction)
		{
			case MessageAction.Close:
				gameObject.SetActive(false);
				break;
			case MessageAction.LoadScene:
				SceneManager.LoadScene(actionParam);
				break;
			case MessageAction.OpenMenu:
				MenuBase.OpenMenu(actionParam);
				break;
		}
	}

	#endregion
}
