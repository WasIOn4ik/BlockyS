using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HostMenuUI : MenuBase
{
	#region Constants

	private const int MIN_REMOTE_PLAYERS_FOR_MULTIPLAYER = 2;
	private const int MAX_REMOTE_PLAYERS_FOR_MULTIPLAYER = 4;

	#endregion

	#region Variables

	[Header("StartSubmenu")]
	[SerializeField] private Button backButtonStart;
	[SerializeField] private Button playLocalButton;
	[SerializeField] private Button playOnlineButton;

	[Header("SetupSubmenu")]
	[SerializeField] private Button confirmButton;
	[SerializeField] private Button backButtonSetup;
	[SerializeField] private TMP_InputField portInput;

	[SerializeField] private Slider playersCountSlider;
	[SerializeField] private TMP_Text playersCountText;

	[SerializeField] private Slider cellsCountSlider;
	[SerializeField] private TMP_Text cellsCountText;
	[SerializeField] private RectTransform localPlayersTab;
	[SerializeField] private Slider localPlayersSlider;
	[SerializeField] private TMP_Text localPlayersText;

	[Header("Tabs")]
	[SerializeField] private GameObject startSubmenu;
	[SerializeField] private GameObject setupSubmenu;

	protected bool bNetMode;

	#endregion

	#region UnityCallbacks

	protected override void Awake()
	{
		base.Awake();

		backButtonStart.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayBackButtonClick();
			BackToPreviousMenu();
		});

		backButtonSetup.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayBackButtonClick();
			setupSubmenu.SetActive(false);
			startSubmenu.SetActive(true);
		});

		playLocalButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayButtonClick();
			bNetMode = false;
			ShowSetupSubMenu();
		});

		playOnlineButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayButtonClick();
			bNetMode = true;
			ShowSetupSubMenu();
		});

		confirmButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayButtonClick();
			OnConfirmHostClicked();
		});

		playersCountSlider.onValueChanged.AddListener((x) =>
		{
			OnPlayersCountChanged();
		});

		localPlayersSlider.onValueChanged.AddListener((x) =>
		{
			OnLocalPlayrsCountChanged();
		});

		cellsCountSlider.onValueChanged.AddListener((x) =>
		{
			cellsCountText.text = (2 + (int)cellsCountSlider.value * 2 + 1).ToString();
		});
	}

	#endregion

	#region Functions

	private void WriteServerData()
	{
		var serv = GameBase.Server;

		GamePrefs prefs = new GamePrefs();
		prefs.boardHalfExtent = int.Parse(cellsCountText.text);
		serv.SetGamePrefs(prefs);
	}

	private void ShowSetupSubMenu()
	{
		portInput.gameObject.SetActive(bNetMode);
		startSubmenu.SetActive(false);
		setupSubmenu.SetActive(true);
	}

	#endregion

	#region UIFunctions

	private void OnPlayersCountChanged()
	{
		int playersCount = (int)playersCountSlider.value;
		playersCountText.text = playersCount.ToString() + " / 4";

		if (!bNetMode || playersCount == 2)
		{
			localPlayersTab.gameObject.SetActive(false);
		}
		else
		{
			localPlayersTab.gameObject.SetActive(true);
			localPlayersSlider.maxValue = playersCountSlider.value - 1;
			OnLocalPlayrsCountChanged();
		}
	}

	private void OnLocalPlayrsCountChanged()
	{
		localPlayersText.text = localPlayersSlider.value.ToString() + " / " + (playersCountSlider.value - 1);
	}

	private void OnConfirmHostClicked()
	{
		WriteServerData();
		var server = GameBase.Server;

		if (bNetMode)
		{
			SpesLogger.Warning("Game created in netMode");
			server.SetLocalPlayersCount((int)localPlayersSlider.value);
			server.SetMaxRemotePlayersCount((int)(playersCountSlider.value - localPlayersSlider.value));
			UnityLobbyService.Instance.CreateLobbyAsync("TestLobby", false);
			//server.HostGame(portInput.text.Length == 0 ? (ushort)ServerBase.defaultPort : ushort.Parse(portInput.text));
		}
		else
		{
			server.SetLocalPlayersCount((int)playersCountSlider.value);
			server.SetupSingleDevice();
		}
	}

	#endregion
}
