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
            hud.SetInputComponent(inputComp);
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
    /// � NetworkController'e ���������� �� �������, � �������������� ������ ServerRpc
    /// </summary>
    /// <param name="turn"></param>
    public void EndTurn(Turn turn)
    {
        if (GetPlayerInfo().state != EPlayerState.ActivePlayer)
            return;

        GetPlayerInfo().state = EPlayerState.Waiting;
        SpesLogger.Deb("��������� ������� ����� " + GetPlayerInfo().playerOrder + " �������� ���");
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
    /// � NetworkController'e ���������� �� �������, � �������������� ������ RPC
    /// </summary>
    public void StartTurn()
    {
        SpesLogger.Deb("������ ���� �������� ������ " + GetPlayerInfo().playerOrder);
        StartTurnClientRpc();
    }

    #endregion

    #region RPCs

    [ServerRpc(RequireOwnership = true, Delivery = RpcDelivery.Reliable)]
    public void EndTurnServerRpc(Turn turn)
    {
        SpesLogger.Deb("������� ����� �������� ��� " + GetPlayerInfo().playerOrder);
        GetPlayerInfo().state = EPlayerState.Operator;
        GameplayBase.instance.S_EndTurn(this, turn);
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    public void StartTurnClientRpc()
    {
        SpesLogger.Deb("��������� ������� ����� " + GetPlayerInfo().playerOrder + " ����� ���");
        GetPlayerInfo().state = EPlayerState.ActivePlayer;
        inputComp.UpdateTurnValid(false);
    }

    public void SetPlayerInfo(PlayerInGameInfo inf)
    {
        playerInfo.Value = inf;
    }

    #endregion
}
