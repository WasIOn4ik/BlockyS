using UnityEngine;
using UnityEngine.UI;

public class ConnectMenuUI
	: MenuBase
{
	#region Variables

	[SerializeField] private TMPro.TMP_InputField address;
	[SerializeField] private Button confirmButton;
	[SerializeField] private Button backButton;

	#endregion

	#region UnityCallbacks

	protected override void Awake()
	{
		base.Awake();
		confirmButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayButtonClick();
			string str = address.text;
			var add = str.Split(':');
			if (add.Length == 2)
			{
				if (ushort.TryParse(add[1], out ushort port))
				{
					UnityLobbyService.Instance.QuickJoinAsync();
					//GameBase.Client.ConnectToHost(add[0], port);
				}
				else
				{
					SpesLogger.Warning("Port is incorrect");
				}
			}
			else
			{
				SpesLogger.Warning("Write address:port");
			}
		});

		backButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayBackButtonClick();
			GameBase.Client.ClearAll();
			BackToPreviousMenu();
		});
	}

	#endregion
}
