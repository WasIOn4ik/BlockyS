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
			OpenMenu(CREATE_LOBBY_MENU);
		});
		connectButton.onClick.AddListener(() =>
		{
			OpenMenu(CONNECT_TO_LOBBY_MENU);
		});
		settingsButton.onClick.AddListener(() =>
		{
			OpenMenu(SETTINGS_MENU);
		});
		creditsButton.onClick.AddListener(() =>
		{
			OpenMenu(CREDITS_MENU);
		});
		exitButton.onClick.AddListener(() =>
		{
			Application.Quit();
		});
		customizationButton.onClick.AddListener(() =>
		{
			OpenMenu(CUSTOMIZATION_MENU);
		});
	}

	#endregion
}
