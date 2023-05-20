using System;

[Serializable]
public struct ServerPrefs
{
	public float reconnectionTime;
	public int maxRemotePlayers;
	public int maxConnectPayloadSize;
}
