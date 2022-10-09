using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientBase : MonoBehaviour
{
    #region Varaibles

    public PlayerInfo clientInfo;

    public NetworkManager networkManager;

    #endregion

    #region Callbacks

    private void OnInstanceLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        SpesLogger.Deb("Клиент " + clientId + " загрузил карту " + sceneName);
    }

    private void OnLoadStarted(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
    {
        SpesLogger.Deb("Клиент " + clientId + " начал загруку карты " + sceneName);
    }

    private void OnTransportFailure()
    {
        SpesLogger.Error("TransportFailure");
        networkManager.Shutdown();
        SceneManager.LoadScene("StartupScene");
    }

    private void OnDisconnected(ulong clientID)
    {
        SpesLogger.Detail("Клиент " + clientID + " отключился " + (networkManager.IsServer ? "{Сервер}" : "{Клиент}"));
    }

    private void OnConnected(ulong clientID)
    {
        SpesLogger.Detail("Клиент " + clientID + " подключился " + (networkManager.IsServer ? "{Сервер}" : "{Клиент}"));
    }

    #endregion

    #region Functions

    public void ConnectToGame(string address, ushort port)
    {
        ClearAll();
        StartCoroutine(ConnectCoroutine(address, port));
    }

    public void ClearAll()
    {
        clientInfo = new();
        networkManager.Shutdown(true);
    }

    protected IEnumerator ConnectCoroutine(string address, ushort port)
    {
        while (networkManager.ShutdownInProgress)
        {
            yield return null;
        }
        UnityTransport net = networkManager.GetComponent<UnityTransport>();
        net.ConnectionData.Address = address;
        net.ConnectionData.Port = port;

        ConnectionPayload payload = new ConnectionPayload() { client = clientInfo, password = "" };
        string jsonData = JsonUtility.ToJson(payload);
        networkManager.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(jsonData);

        SetupManagerCallbacks();
        SetupSceneCallbacks();

        SpesLogger.Detail("Подключение к узлу: " + net.ConnectionData.Address + ":" + net.ConnectionData.Port);
        networkManager.StartClient();
    }

    protected void SetupManagerCallbacks()
    {
        networkManager.OnClientConnectedCallback += OnConnected;
        networkManager.OnClientDisconnectCallback += OnDisconnected;
        networkManager.OnTransportFailure += OnTransportFailure;
    }

    protected void SetupSceneCallbacks()
    {
        if (networkManager.SceneManager != null)
        {
            networkManager.SceneManager.OnLoad += OnLoadStarted;
            networkManager.SceneManager.OnLoadComplete += OnInstanceLoadComplete;
        }
        else
        {
            SpesLogger.Warning("Попытка привязки к SceneManager, который не существует");
        }
    }

    #endregion

    #region UnityCallbacks

    public void OnDestroy()
    {
        if (networkManager)
        {
            networkManager.OnClientConnectedCallback -= OnConnected;
            networkManager.OnClientDisconnectCallback -= OnDisconnected;
            networkManager.OnTransportFailure -= OnTransportFailure;

            if (networkManager.SceneManager != null)
            {
                networkManager.SceneManager.OnLoad -= OnLoadStarted;
                networkManager.SceneManager.OnLoadComplete -= OnInstanceLoadComplete;
            }
        }
    }

    #endregion

}
