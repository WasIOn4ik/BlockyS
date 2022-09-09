
using System;

[Serializable]
public struct ServerPrefs
{
    public string password;
    public int boardHalfExtent;
    public int maxPlayers;
    public float turnTime;
    public float reconnectionTime;
}
