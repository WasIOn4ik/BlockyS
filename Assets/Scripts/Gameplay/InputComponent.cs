using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputComponent : MonoBehaviour
{
    #region Variables

    [SerializeField] protected MonoBehaviour controllerComponent;
    [SerializeField] protected Transform wallPRedictPrefab;
    [SerializeField] protected BoardWall wallPrefab;

    [SerializeField] protected LayerMask moveLayer;
    [SerializeField] protected LayerMask placeLayer;

    public IPlayerController controller;

    public bool bMoveMode = true;

    protected Vector3 startPos;
    protected float holdDuration = 0.3f;

    protected Vector3 forwardDir;
    protected Vector3 rightDir;

    protected Transform wallPredict;
    protected ETurnType placeType = ETurnType.PlaceXForward;
    protected Turn turn;
    protected WallPlaceholder previousClickedPlaceholder;

    #endregion

    #region UnityCallbacks

    public void Awake()
    {
        controller = controllerComponent as IPlayerController;

        wallPredict = Instantiate(wallPRedictPrefab);
        wallPredict.gameObject.SetActive(false);

        forwardDir = transform.forward;
        forwardDir.y = 0;
        forwardDir.Normalize();

        rightDir = transform.right;
        rightDir.y = 0;
        rightDir.Normalize();
    }

    public void Update()
    {
        if (controller.GetPlayerInfo().state != EPlayerState.ActivePlayer)
            return;

#if UNITY_ANDROID

        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            transform.position = new Vector3(transform.position.x - touch.deltaPosition.x, 0, transform.position.z - touch.deltaPosition.y);
        }
#endif

#if UNITY_EDITOR || UNITY_STANDALONE

        if (Input.GetMouseButtonDown(0))
        {
            startPos = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            //Режим перемещения пешки
            if (bMoveMode)
            {
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f, moveLayer))
                {
                    var bb = hit.collider.gameObject.GetComponentInParent<BoardBlock>();

                    if (bb)
                    {
                        SpesLogger.Warning(bb.name);

                        //Начало хода за пешку
                        if (controller.GetPlayerInfo().pawn.block.Value == bb.coords)
                        {
                            if (bb.bSelected)
                                bb.UnHighlightAround();
                            else
                                bb.HighlightAround();
                        }
                        //Подтверждение хода за пешку
                        else if (bb.bHighlighted)
                        {
                            turn = new();
                            turn.type = ETurnType.Move;
                            turn.pos = bb.coords;

                            controller.EndTurn(turn);
                        }
                    }
                }
            }
            //Режим строительства стенок
            else
            {
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f, placeLayer))
                {
                    var wph = hit.collider.gameObject.GetComponentInParent<WallPlaceholder>();

                    if (wph)
                    {
                        turn = new();

                        if (previousClickedPlaceholder == wph)
                        {
                            placeType = (placeType == ETurnType.PlaceXForward ? ETurnType.PlaceZForward : ETurnType.PlaceXForward);
                        }
                        previousClickedPlaceholder = wph;

                        turn.type = placeType;
                        turn.pos = wph.coords;

                        wallPredict.gameObject.SetActive(true);
                        wallPredict.position = wph.transform.position;
                        wallPredict.rotation = turn.type == ETurnType.PlaceXForward ? Quaternion.Euler(0, 90f, 0) : Quaternion.identity;
                    }
                }
            }
        }
        else if (Input.GetMouseButton(0))
        {
            var mouse = Input.mousePosition - startPos;
            startPos = Input.mousePosition;
            transform.position = transform.position - (forwardDir * mouse.y + rightDir * mouse.x) / 100;//new Vector3(transform.position.x + mouse.x / 100, transform.position.y, transform.position.z + mouse.y / 100);
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            Turn turn = new();
            controller.EndTurn(turn);
        }

#endif
    }

    #endregion

    #region Functions 

    public void ConfirmTurn()
    {
        controller.EndTurn(turn);
        wallPredict.gameObject.SetActive(false);
        bMoveMode = true;

        if (BoardBlock.selectedBlock)
        {
            BoardBlock.selectedBlock.UnHighlightAround();
        }
    }

    #endregion
}
