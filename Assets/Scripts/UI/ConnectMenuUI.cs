using UnityEngine;
using UnityEngine.UI;

public class ConnectMenuUI
	: MenuBase
{
	#region Variables

	[SerializeField] private TMPro.TMP_InputField codeInput;
	[SerializeField] private Button joinButton;
	[SerializeField] private Button quickJoinButton;
	[SerializeField] private Button backButton;

	#endregion

	#region UnityCallbacks

	protected override void Awake()
	{
		base.Awake();

		codeInput.onValueChanged.AddListener((x) =>
		{
			bool show = x.Length == 6;
			joinButton.interactable = show;
		});

		joinButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayButtonClick();
			string str = codeInput.text;

			if (!string.IsNullOrEmpty(str))
			{
				UnityLobbyService.Instance.JoinLobbyByCodeAsync(str);
			}
		});

		quickJoinButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayButtonClick();
			UnityLobbyService.Instance.QuickJoinAsync();
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
