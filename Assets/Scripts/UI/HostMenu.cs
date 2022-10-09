using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HostMenu : MenuBase
{
    #region Variables

    [Header("SetupSubmenu")]
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

    #region Functions

    protected void WriteServerData()
    {
        var serv = GameBase.server;
        serv.prefs.boardHalfExtent = int.Parse(cellsCountText.text);
        serv.prefs.maxPlayers = (int)playersCountSlider.value;
        serv.localPlayers = GameBase.instance.bNetMode ? (int)localPlayersSlider.value : serv.prefs.maxPlayers;
    }

    #endregion

    #region UIFunctions

    public void OnPlayersCountChanged()
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

    public void OnLocalPlayrsCountChanged()
    {
        localPlayersText.text = localPlayersSlider.value.ToString() + " / " + (playersCountSlider.value - 1);
    }

    public void OnConfirmHostClicked()
    {
        WriteServerData();
        if (bNetMode)
        {
            SpesLogger.Warning("Game created in netMode");
            GameBase.server.HostGame(portInput.text.Length == 0 ? (ushort)2545 : ushort.Parse(portInput.text));
        }
        else
        {
            GameBase.server.SetupSingleDevice();
        }
    }

    public void OnBoardSizeChanged()
    {
        cellsCountText.text = (2 + (int)cellsCountSlider.value * 2 + 1).ToString();
    }

    public void OnBackToSelectNetModeClicked()
    {
        setupSubmenu.SetActive(false);
        startSubmenu.SetActive(true);
    }

    public void SetupSingleMode()
    {
        portInput.gameObject.SetActive(false);
        startSubmenu.SetActive(false);
        setupSubmenu.SetActive(true);
        GameBase.instance.bNetMode = false;
        bNetMode = false;
    }

    public void SetupNetMode()
    {
        portInput.gameObject.SetActive(true);
        startSubmenu.SetActive(false);
        setupSubmenu.SetActive(true);
        GameBase.instance.bNetMode = true;
        bNetMode = true;
    }

    #endregion
}
