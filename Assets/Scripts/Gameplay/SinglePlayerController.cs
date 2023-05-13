using System.Collections;
using UnityEngine;

public class SinglePlayerController : MonoBehaviour, IPlayerController
{
    #region Variables

    [SerializeField] private InGameHUD hudPrefab;

    [SerializeField] private PlayerInGameInfo playerInfo = new();

    [SerializeField] private PlayerCosmetic cosmetic;

	private Vector3 cameraPosition;
	private Quaternion cameraRotation;

	private InputComponent inputComp;

	private Camera cam;

	#endregion

	#region StaticVariables

	private static InGameHUD hud;
	private static SinglePlayerController previousLocalController;

	private static bool bTurnDisplayUpToDate = false;

    #endregion

    #region UnityCallbacks

    private void Awake()
    {
        cameraPosition = transform.position;
        cameraRotation = transform.rotation;

        inputComp = GetComponent<InputComponent>();

        if (!hud)
            hud = Instantiate(hudPrefab);

        cosmetic = new PlayerCosmetic() { boardSkinID = GameBase.storage.CurrentBoardSkin, pawnSkinID = GameBase.storage.CurrentPawnSkin };
    }

    #endregion

    #region IPlayerController

    public void StartTurn()
    {
        SpesLogger.Deb($"Turn of local player {GetPlayerInfo().playerOrder} started");

        if (previousLocalController)
        {
            var linfo = previousLocalController.GetPlayerInfo();
            linfo.state = EPlayerState.Operator;
            previousLocalController.SetPlayerInfo(linfo);
        }

        var info = GetPlayerInfo();
        info.state = EPlayerState.ActivePlayer;
        SetPlayerInfo(info);

        cam = Camera.main;
        var tp = cam.transform.position;
        var tr = cam.transform.rotation;

        cam.transform.SetParent(transform);

        //new camera position
        Vector3 pos = GetPlayerInfo().pawn.transform.position + GetPlayerInfo().pawn.transform.forward * GameBase.instance.gameRules.cameraBackwardOffset + Vector3.up * GameBase.instance.gameRules.cameraHeight;
        transform.SetPositionAndRotation(pos, cameraRotation);

        hud.SetInputComponent(inputComp);
        hud.ToDefault();
        hud.SetWallsCount(GetPlayerInfo().WallCount);
        inputComp.turnValid += hud.OnTurnValidationChanged;
        inputComp.UpdateTurnValid(false);

        //Initialize camera on start and remove "camera jitter effect" on turn transfer
        if (GameplayBase.instance.bGameActive)
        {
            cam.transform.position = tp;
            cam.transform.rotation = tr;
        }
        else
        {
            cam.transform.localPosition = Vector3.zero;
            cam.transform.localRotation = Quaternion.identity;
        }
    }

    public void EndTurn(Turn turn)
    {
        if (turn.type == ETurnType.PlaceXForward || turn.type == ETurnType.PlaceZForward)
        {
            if (GetPlayerInfo().WallCount <= 0)
            {
                SpesLogger.Detail($"Player {GetPlayerInfo().playerOrder} can't build anymore");
                return;
            }
        }
        SpesLogger.Deb($"Local player {GetPlayerInfo().playerOrder} ended turn");

        previousLocalController = this;

        var info = GetPlayerInfo();
        info.state = EPlayerState.Operator;
        SetPlayerInfo(info);

        cameraPosition = transform.position;

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

    public PlayerCosmetic GetCosmetic()
    {
        return cosmetic;
    }

    public void UpdateTurn(int active)
    {
        if (bTurnDisplayUpToDate)
            return;

        bTurnDisplayUpToDate = true;
        StartCoroutine(UpdateDisplayTurn(active));
    }

	#endregion

	#region Functions

	private IEnumerator UpdateDisplayTurn(int active)
	{
		yield return null;

		hud.SetPlayerTurn(active);

		bTurnDisplayUpToDate = false;
	}

	#endregion
}

