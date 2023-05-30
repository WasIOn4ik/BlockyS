using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum MessageAction
{
	Close,
	LoadScene,
	OpenMenu
}

public class MessageUI : MonoBehaviour
{
	#region Variables

	[SerializeField] protected TMP_Text message;
	[SerializeField] protected Button messageButton;

	protected string actionParam;

	protected MessageAction buttonAction;

	#endregion

	#region UnityCallbacks

	private void Awake()
	{
		messageButton.onClick.AddListener(() =>
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
		});
	}

	#endregion

	#region Fuctions

	public void ShowMessage(string entry, MessageAction action, bool bLocalized = true, string param = "")
	{
		SoundManager.Instance.PlayBackButtonClick();
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

	#endregion
}
