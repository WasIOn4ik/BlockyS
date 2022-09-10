using System;
using Unity.Netcode;

[Serializable]
public class PlayerInGameInfo
{
    public int playerOrder;
    public Pawn pawn;
    public EPlayerState state;

    public static implicit operator PlayerNetworkedInfo(PlayerInGameInfo inGame)
    {
        PlayerNetworkedInfo netInfo = new();
        netInfo.playerOrder = inGame.playerOrder;
        netInfo.pawn = inGame.pawn;
        netInfo.state = inGame.state;

        return netInfo;
    }
}

[Serializable]
public struct PlayerNetworkedInfo : INetworkSerializeByMemcpy
{
    public int playerOrder;
    public NetworkBehaviourReference pawn;
    public EPlayerState state;

    public static implicit operator PlayerInGameInfo(PlayerNetworkedInfo net)
    {
        PlayerInGameInfo inf = new();
        inf.playerOrder = net.playerOrder;
        net.pawn.TryGet(out inf.pawn);
        inf.state = net.state;

        return inf;
    }
}
