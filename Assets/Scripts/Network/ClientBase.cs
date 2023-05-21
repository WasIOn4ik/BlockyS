using System.Collections;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientBase : MonoBehaviour
{
	#region Variables

	public string playerName;

	private NetworkManager networkManager;

	#endregion

	#region UnityCallbacks

	private void Start()
	{
		networkManager = NetworkManager.Singleton;
	}

	private void OnDestroy()
	{
		UnbindAll();
	}

	#endregion

	#region Callbacks

	private void NetworkManager_OnTransportFailure()
	{
		SpesLogger.Error("TransportFailure");
		networkManager.Shutdown();
		SceneManager.LoadScene("StartupScene");
	}

	private void NetworkManager_OnDisconnected(ulong clientID)
	{
		SpesLogger.Detail("Client " + clientID + " disconnected " + (networkManager.IsServer ? "{Server}" : "{Client}" + " localID: " + networkManager.LocalClientId));

		if (clientID == networkManager.LocalClientId || clientID == 0)
		{
			SceneManager.LoadScene("StartupScene");
		}
	}

	#endregion

	#region Functions

	public void ConnectToHost(string address, ushort port)
	{
		ClearAll();

		EnsureShutdown();

		ConnectInternal(address, port);
	}

	/// <summary>
	/// Reset bindings and shutdown networkManager
	/// </summary>
	public void ClearAll()
	{
		UnbindAll();
		networkManager.Shutdown(true);
	}

	private void EnsureShutdown()
	{
		float shutdownCheckDelay = 0.1f;

		while (NetworkManager.Singleton.ShutdownInProgress)
		{
			new WaitForSeconds(shutdownCheckDelay);
		}
	}

	private void ConnectInternal(string address, ushort port)
	{
		//SetAddress(address, port);

		UpdateConnectionPayload();

		SetupManagerCallbacks();

		networkManager.StartClient();
	}

	private void SetAddress(string address, ushort port)
	{
		UnityTransport net = networkManager.GetComponent<UnityTransport>();
		net.ConnectionData.Address = address;
		net.ConnectionData.Port = port;
	}

	private void UpdateConnectionPayload()
	{
		ConnectionPayload payload = new ConnectionPayload();
		payload.pawnSkinID = GameBase.Storage.CurrentPawnSkinID;
		payload.boardSkin = GameBase.Storage.CurrentBoardSkinID;
		payload.playerName = GameBase.Client.playerName;
		payload.playerToken = UnityEngine.Random.Range(0, 10000).ToString();

		NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(JsonUtility.ToJson(payload));
	}

	private void SetupManagerCallbacks()
	{
		networkManager.OnTransportFailure += NetworkManager_OnTransportFailure;
		networkManager.OnClientDisconnectCallback += NetworkManager_OnDisconnected;
	}

	private void UnbindAll()
	{
		if (networkManager)
		{
			networkManager.OnTransportFailure -= NetworkManager_OnTransportFailure;
			networkManager.OnClientDisconnectCallback -= NetworkManager_OnDisconnected;
		}
	}

	#endregion
}
