using System;
using Unity.Netcode;

[Serializable]
public struct Turn : INetworkSerializeByMemcpy
{
    public ETurnType type;
    public Point pos;

    public Turn(ETurnType nType, Point nPos)
    {
        type = nType;
        pos = nPos;
    }
}

