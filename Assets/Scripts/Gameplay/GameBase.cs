using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.Localization.Settings;

[RequireComponent(typeof(ServerBase), typeof(ClientBase), typeof(GameStorage))]
public class GameBase : MonoBehaviour
{
    #region Variables

    [SerializeField] private MenusLibrary menusLibrary;
    public SkinsLibrary skins;
    public GameRules gameRules;
    public MessageScript messageMenuPrefab;

    public bool bNetMode;

    protected MessageScript currentMessage;

    #endregion

    #region StaticVariables

    public static GameBase instance;
    public static ServerBase server;
    public static ClientBase client;
    public static GameStorage storage;

    #endregion

    #region UnityCallbacks

    public void Awake()
    {
        if (instance)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(this);
        MenuBase.SetLibrary(menusLibrary);

        server = GetComponent<ServerBase>();
        client = GetComponent<ClientBase>();
        storage = GetComponent<GameStorage>();

        var net = GetComponent<NetworkManager>();

        server.networkManager = net;
        client.networkManager = net;

        Application.targetFrameRate = 60;
        Application.quitting += HandleQuit;

        storage.LoadPrefs();
        storage.LoadProgress();

        if (storage.CurrentBoardSkin != 0 && !storage.CheckBoard(storage.CurrentBoardSkin))
        {
            storage.CurrentBoardSkin = 0;
        }

        if (storage.CurrentPawnSkin != 0 && !storage.CheckPawn(storage.CurrentPawnSkin))
        {
            storage.CurrentPawnSkin = 0;
        }
    }

    #endregion

    #region Callbacks

    private void HandleQuit()
    {
        storage.SaveProgress();
        storage.SavePrefs();
    }

    #endregion

    #region Functions

    public void ApplyLanguage(string code)
    {
        var settings = LocalizationSettings.Instance;
        var locale = settings.GetAvailableLocales().GetLocale(code);
        settings.SetSelectedLocale(locale);
        SpesLogger.Detail("язык установлен на " + code);
    }

    public void GameplayFinished()
    {
        server.ClearAll();
        client.ClearAll();
    }

    public void ShowMessage(string entry, MessageAction action, bool bLocalized, string param = "")
    {
        if (!currentMessage)
        {
            currentMessage = Instantiate(messageMenuPrefab);
        }

        currentMessage.ShowMessage(entry, action, bLocalized, param);
    }

    #endregion
}
