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
    protected static SinglePlayerController previousLocalController;

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

    public void StartTurn()
    {
        SpesLogger.Deb("Локальный игрок " + GetPlayerInfo().playerOrder + " начал ход");

        if (previousLocalController)
            previousLocalController.GetPlayerInfo().state = EPlayerState.Waiting;

        GetPlayerInfo().state = EPlayerState.ActivePlayer;

        var cam = Camera.main;

        cam.transform.SetParent(transform);
        transform.SetPositionAndRotation(cameraPosition, cameraRotation);
        cam.transform.localPosition = Vector3.zero;
        cam.transform.localRotation = Quaternion.identity;

        hud.SetInputComponent(inputComp);
        inputComp.turnValid += hud.OnTurnValidationChanged;
        inputComp.UpdateTurnValid(false);
    }

    public void EndTurn(Turn turn)
    {
        SpesLogger.Deb("Локальный игрок " + GetPlayerInfo().playerOrder + " завершил ход");

        previousLocalController = this;
        GetPlayerInfo().state = EPlayerState.Operator;

        cameraPosition = transform.position;
        cameraRotation = transform.rotation;

        GameplayBase.instance.S_EndTurn(this, turn);

        inputComp.turnValid -= hud.OnTurnValidationChanged;
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
