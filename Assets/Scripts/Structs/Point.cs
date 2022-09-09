using System;
using Unity.Netcode;

[Serializable]
public struct Point : INetworkSerializeByMemcpy
{
    public int x;
    public int y;

    public Point(int nx, int ny)
    {
        x = nx;
        y = ny;
    }

    public static bool operator ==(Point a, Point b)
    {
        return a.x == b.x && a.y == b.y;
    }
    public static bool operator !=(Point a, Point b)
    {
        return a.x != b.x || a.y != b.y;
    }
}
