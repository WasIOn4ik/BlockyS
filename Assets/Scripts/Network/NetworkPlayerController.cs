using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkPlayerController : NetworkBehaviour, IPlayerController
{
    #region Variables

    protected NetworkVariable<PlayerNetworkedInfo> playerInfo = new();

    [SerializeField] InGameHUD hudPrefab;

    protected InputComponent inputComp;

    protected InGameHUD hud;

    #endregion

    #region UnityCallbacks

    public void Awake()
    {
        inputComp = GetComponent<InputComponent>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer)
        {
            hud = Instantiate(hudPrefab);
            hud.inputComp = inputComp;
        }

        if (IsOwner)
        {
            var cam = Camera.main;
            cam.transform.SetParent(transform);
            cam.transform.localPosition = Vector3.zero;
            cam.transform.localRotation = Quaternion.identity;
        }
    }

    #endregion

    #region IPlayerController

    /// <summary>
    /// В NetworkController'e вызывается на клиенте, а обрабатывается внутри ServerRpc
    /// </summary>
    /// <param name="turn"></param>
    public void EndTurn(Turn turn)
    {
        GetPlayerInfo().state = EPlayerState.Waiting;
        EndTurnServerRpc(turn);
    }

    public MonoBehaviour GetMono()
    {
        return this;
    }

    public PlayerInGameInfo GetPlayerInfo()
    {
        return playerInfo.Value;
    }

    /// <summary>
    /// В NetworkController'e вызывается на сервере, а обрабатывается внутри RPC
    /// </summary>
    public void StartTurn()
    {
        StartTurnClientRpc();
    }

    #endregion

    #region RPCs

    [ServerRpc(RequireOwnership = true, Delivery = RpcDelivery.Reliable)]
    public void EndTurnServerRpc(Turn turn)
    {
        GameplayBase.instance.S_EndTurn(this, turn);
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    public void StartTurnClientRpc()
    {
        GetPlayerInfo().state = EPlayerState.ActivePlayer;
    }

    public void SetPlayerInfo(PlayerInGameInfo inf)
    {
        playerInfo.Value = inf;
    }

    #endregion
}
