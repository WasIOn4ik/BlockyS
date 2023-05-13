using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientBase : MonoBehaviour
{
    #region Variables

    public string playerName;

    public NetworkManager networkManager;

	#endregion

	#region UnityCallbacks

	private void OnDestroy()
    {
        UnbindAll();
    }

    #endregion

    #region Callbacks

    private void NetworkManager_OnLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        SpesLogger.Deb("Client " + clientId + " loaded map " + sceneName);
    }

    private void NetworkManager_OnLoad(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
    {
        SpesLogger.Deb("Client " + clientId + " started loading of map " + sceneName);
    }

    private void OnTransportFailure()
    {
        SpesLogger.Error("TransportFailure");
        networkManager.Shutdown();
        SceneManager.LoadScene("StartupScene");
    }

    private void OnDisconnected(ulong clientID)
    {
        SpesLogger.Detail("Client " + clientID + " disconnected " + (networkManager.IsServer ? "{Server}" : "{Client}" + " localID: " + networkManager.LocalClientId));

        if (clientID == networkManager.LocalClientId || clientID == 0)
        {
            SceneManager.LoadScene("StartupScene");
        }
    }

    private void OnConnected(ulong clientID)
    {
        SpesLogger.Detail("Client " + clientID + " connected " + (networkManager.IsServer ? "{Server}" : "{Client}"));
    }

    #endregion

    #region Functions

    public void ConnectToGame(string address, ushort port)
    {
        ClearAll();
        StartCoroutine(ConnectCoroutine(address, port));
    }

    /// <summary>
    /// Reset bindings and shutdown networkManager
    /// </summary>
    public void ClearAll()
    {
        UnbindAll();
        networkManager.Shutdown(true);
    }

	private IEnumerator ConnectCoroutine(string address, ushort port)
    {
        while (networkManager.ShutdownInProgress)
        {
            yield return null;
        }
        UnityTransport net = networkManager.GetComponent<UnityTransport>();
        net.ConnectionData.Address = address;
        net.ConnectionData.Port = port;

        ConnectionPayload payload = new ConnectionPayload() { playerName = playerName, password = "" };
        string jsonData = JsonUtility.ToJson(payload);
        networkManager.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(jsonData);

        SetupManagerCallbacks();
        SetupSceneCallbacks();

        SpesLogger.Detail("Connectiong to host: " + net.ConnectionData.Address + ":" + net.ConnectionData.Port);
        networkManager.StartClient();
    }

	private void SetupManagerCallbacks()
    {
        networkManager.OnClientConnectedCallback += OnConnected;
        networkManager.OnClientDisconnectCallback += OnDisconnected;
        networkManager.OnTransportFailure += OnTransportFailure;
    }

	private void SetupSceneCallbacks()
    {
        if (networkManager.SceneManager != null)
        {
            networkManager.SceneManager.OnLoad += NetworkManager_OnLoad;
            networkManager.SceneManager.OnLoadComplete += NetworkManager_OnLoadComplete;
        }
        else
        {
            SpesLogger.Warning("Tring to bind to not existing SceneManager");
        }
    }
    private void UnbindAll()
    {
        if (networkManager)
        {
            networkManager.OnClientConnectedCallback -= OnConnected;
            networkManager.OnClientDisconnectCallback -= OnDisconnected;
            networkManager.OnTransportFailure -= OnTransportFailure;

            if (networkManager.SceneManager != null)
            {
                networkManager.SceneManager.OnLoad -= NetworkManager_OnLoad;
                networkManager.SceneManager.OnLoadComplete -= NetworkManager_OnLoadComplete;
            }
        }
    }

    #endregion
}
