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

    #endregion

    #region StaticVariables

    public static GameBase instance;
    public static ServerBase server;
    public static ClientBase client;
    public static GameStorage storage;

    #endregion

    #region Variables

    public bool bNetMode;

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

        Application.quitting += HandleQuit;

        storage.LoadPrefs();
        storage.LoadProgress();
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

    #endregion
}
