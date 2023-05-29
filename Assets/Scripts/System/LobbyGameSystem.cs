using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public struct LobbyPlayerDescriptor : IEquatable<LobbyPlayerDescriptor>, INetworkSerializeByMemcpy
{
	public ulong clientID;
	public FixedString64Bytes playerName;
	public FixedString64Bytes playerID;
	public int boardSkin;
	public int pawnSkin;

	public bool Equals(LobbyPlayerDescriptor other)
	{
		return playerName == other.playerName && boardSkin == other.boardSkin && pawnSkin == other.pawnSkin && clientID == other.clientID && playerID == other.playerID;
	}
}

public class LobbyGameSystem : NetworkBehaviour
{
	#region HelperClasses

	public class ConnectedPlayersEventArgs : EventArgs
	{
		public List<LobbyPlayerDescriptor> value;
	}

	#endregion

	#region Events

	public event EventHandler<ConnectedPlayersEventArgs> onPlayersListChanged;

	#endregion

	#region StaticVariables

	public static LobbyGameSystem Instance { get; private set; }

	#endregion

	#region Variables

	private NetworkList<LobbyPlayerDescriptor> connectedPlayers;

	#endregion

	#region UnityCallbacks

	private void Awake()
	{
		Instance = this;

		connectedPlayers = new NetworkList<LobbyPlayerDescriptor>();
	}

	#endregion

	#region Overrides

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();

		connectedPlayers.OnListChanged += ConnectedPlayers_OnListChanged;

		if (IsServer)
		{
			NetworkManager.OnClientConnectedCallback += SERVER_NetworkManager_OnClientConnectedCallback;
			NetworkManager.OnClientDisconnectCallback += SERVER_NetworkManager_OnClientDisconnectCallback;

			foreach (var clientID in NetworkManager.ConnectedClientsIds)
			{
				connectedPlayers.Add(GetDescriptor(clientID));
			}
		}
	}

	public override void OnDestroy()
	{
		connectedPlayers.OnListChanged -= ConnectedPlayers_OnListChanged;

		if (IsServer)
		{
			NetworkManager.OnClientConnectedCallback -= SERVER_NetworkManager_OnClientConnectedCallback;
			NetworkManager.OnClientDisconnectCallback -= SERVER_NetworkManager_OnClientDisconnectCallback;
		}

		base.OnDestroy();
	}

	#endregion

	#region Callbacks

	private void SERVER_NetworkManager_OnClientDisconnectCallback(ulong clientID)
	{
		for (int i = 0; i < connectedPlayers.Count; i++)
		{
			if (connectedPlayers[i].clientID == clientID)
			{
				connectedPlayers.RemoveAt(i);
			}
		}
	}

	private void SERVER_NetworkManager_OnClientConnectedCallback(ulong clientID)
	{
		for (int i = 0; i < connectedPlayers.Count; i++)
		{
			if (connectedPlayers[i].clientID == clientID)
			{
				connectedPlayers[i] = GetDescriptor(clientID);
				return;
			}
		}

		connectedPlayers.Add(GetDescriptor(clientID));
	}

	private void ConnectedPlayers_OnListChanged(NetworkListEvent<LobbyPlayerDescriptor> changeEvent)
	{
		onPlayersListChanged?.Invoke(this, new ConnectedPlayersEventArgs() { value = GetPlayers() });
	}

	#endregion

	#region Functions

	public List<LobbyPlayerDescriptor> GetPlayers()
	{
		List<LobbyPlayerDescriptor> list = new List<LobbyPlayerDescriptor>();
		foreach (var player in connectedPlayers)
		{
			list.Add(player);
		}

		return list;
	}

	private LobbyPlayerDescriptor GetDescriptor(ulong clientID)
	{
		var player = GameBase.Server.GetRemotePlayerByClientID(clientID);

		return new LobbyPlayerDescriptor { clientID = clientID, playerName = player.playerName, pawnSkin = player.pawnSkinID, boardSkin = player.boardSkinID, playerID = player.playerID };
	}

	#endregion



}
