using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HostMenu : MenuBase
{
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
			BackToPreviousMenu();
		});

		backButtonSetup.onClick.AddListener(() =>
		{
			setupSubmenu.SetActive(false);
			startSubmenu.SetActive(true);
		});

		playLocalButton.onClick.AddListener(() =>
		{
			bNetMode = false;
			ShowSetupSubMenu();
		});

		playOnlineButton.onClick.AddListener(() =>
		{
			bNetMode = true;
			ShowSetupSubMenu();
		});

		confirmButton.onClick.AddListener(() =>
		{
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
		serv.prefs.boardHalfExtent = int.Parse(cellsCountText.text);
		serv.prefs.maxPlayers = (int)playersCountSlider.value;
		serv.localPlayers = GameBase.Instance.bNetMode ? (int)localPlayersSlider.value : serv.prefs.maxPlayers;
	}

	private void ShowSetupSubMenu()
	{
		portInput.gameObject.SetActive(bNetMode);
		startSubmenu.SetActive(false);
		setupSubmenu.SetActive(true);
		GameBase.Instance.bNetMode = bNetMode;
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
		if (bNetMode)
		{
			SpesLogger.Warning("Game created in netMode");
			GameBase.Server.HostGame(portInput.text.Length == 0 ? (ushort)GameBase.Server.defaultPort : ushort.Parse(portInput.text));
		}
		else
		{
			GameBase.Server.SetupSingleDevice();
		}
	}

	#endregion
}
