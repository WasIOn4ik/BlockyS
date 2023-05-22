using UnityEngine;
using UnityEngine.UI;

public class StartupMenuUI : MenuBase
{
	[SerializeField] private Button hostButton;
	[SerializeField] private Button connectButton;
	[SerializeField] private Button settingsButton;
	[SerializeField] private Button creditsButton;
	[SerializeField] private Button exitButton;
	[SerializeField] private Button customizationButton;

	#region UnityCallbacks

	protected override void Awake()
	{
		base.Awake();

		hostButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayButtonClick();
			OpenMenu(CREATE_LOBBY_MENU);
		});
		connectButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayButtonClick();
			OpenMenu(CONNECT_TO_LOBBY_MENU);
		});
		settingsButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayButtonClick();
			OpenMenu(SETTINGS_MENU);
		});
		creditsButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayButtonClick();
			OpenMenu(CREDITS_MENU);
		});
		exitButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayButtonClick();
			Application.Quit();
		});
		customizationButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayButtonClick();
			OpenMenu(CUSTOMIZATION_MENU);
		});
	}

	#endregion
}
