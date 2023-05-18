using System;

[Serializable]
public struct ServerPrefs
{
	public float reconnectionTime;
	public int maxPlayers;
	public int maxConnectPayloadSize;
}
