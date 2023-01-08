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

    public override bool Equals(object obj)
    {
        try
        {
            return this == (Point)obj;
        }
        catch (Exception ex)
        {
            SpesLogger.Error("Не удалось преобразовать тип к Point: " + ex.Message + " " + ex.StackTrace);
            return false;
        }
    }

    public override int GetHashCode()
    {
        long a = x * int.MaxValue + y;
        return a.GetHashCode();
    }
}
