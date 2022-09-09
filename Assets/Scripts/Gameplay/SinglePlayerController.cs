using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SinglePlayerController : MonoBehaviour, IPlayerController
{
    #region Variables

    [SerializeField] InGameHUD hudPrefab;

    protected Vector3 cameraPosition;
    protected Quaternion cameraRotation;

    protected InputComponent inputComp;

    protected static InGameHUD hud;

    [SerializeField] protected PlayerInGameInfo playerInfo = new();

    #endregion

    #region UnityCallbacks

    public void Awake()
    {
        cameraPosition = transform.position;
        cameraRotation = transform.rotation;

        inputComp = GetComponent<InputComponent>();

        if (!hud)
            hud = Instantiate(hudPrefab);
    }

    #endregion

    #region IPlayerController

    public void EndTurn(Turn turn)
    {
        SpesLogger.Deb("EndTurn");
        GetPlayerInfo().state = EPlayerState.Waiting;
        cameraPosition = transform.position;
        cameraRotation = transform.rotation;
        GameplayBase.instance.S_EndTurn(this, turn);
    }

    public void StartTurn()
    {
        SpesLogger.Deb("StartTurn");
        GetPlayerInfo().state = EPlayerState.ActivePlayer;
        var cam = Camera.main;
        cam.transform.SetParent(transform);
        transform.SetPositionAndRotation(cameraPosition, cameraRotation);
        cam.transform.localPosition = Vector3.zero;
        cam.transform.localRotation = Quaternion.identity;

        hud.inputComp = inputComp;
    }

    public MonoBehaviour GetMono()
    {
        return this;
    }

    public PlayerInGameInfo GetPlayerInfo()
    {
        return playerInfo;
    }

    public void SetPlayerInfo(PlayerInGameInfo inf)
    {
        playerInfo = inf;
    }

    #endregion
}
