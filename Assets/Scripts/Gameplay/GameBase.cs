using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System;
using UnityEngine.SceneManagement;

public class GameBase : MonoBehaviour
{
    #region Variables

    [SerializeField] private MenusLibrary menusLibrary;

    #endregion

    #region StaticVariables

    public static GameBase instance;
    public static ServerBase server;
    public static ClientBase client;

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
        var net = GetComponent<NetworkManager>();

        server.networkManager = net;
        client.networkManager = net;
    }

    #endregion
}
