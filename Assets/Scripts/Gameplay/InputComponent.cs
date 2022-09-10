using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputComponent : MonoBehaviour
{
    #region Variables

    public delegate void Function(bool b);
    public event Function turnValid;

    [Header("Components")]
    [SerializeField] protected MonoBehaviour controllerComponent;
    [SerializeField] protected Transform wallPRedictPrefab;
    [SerializeField] protected BoardWall wallPrefab;

    [Header("Preferences")]
    [SerializeField] protected LayerMask moveLayer;
    [SerializeField] protected LayerMask placeLayer;

    protected bool bMoveMode = true;

    protected IPlayerController controller;

    protected Vector3 startPos;

    /// <summary>
    /// Вектор движения камеры "Вперед"
    /// </summary>
    protected Vector3 forwardDir;
    /// <summary>
    /// Вектор движения камеры "Вправо"
    /// </summary>
    protected Vector3 rightDir;

    /// <summary>
    /// Визуальное представление постройки стенки до подтверждения хода
    /// </summary>
    protected Transform wallPredict;
    /// <summary>
    /// Кэш хода
    /// </summary>
    protected Turn turn;

    /// <summary>
    /// Текущее расположение стенки, нужно для реализации поворота
    /// </summary>
    protected ETurnType placeType = ETurnType.PlaceXForward;
    /// <summary>
    /// Предыдущий плейсхолдер, нужен для реализации поворота по повторному нажатию
    /// </summary>
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
        if (controller.GetPlayerInfo().state == EPlayerState.Waiting)
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
                        SpesLogger.Deb("Нажатие на блок: " + bb.name);

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
                            ConfirmTurn();
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

                        if (!GameplayBase.instance.CheckPlace(turn))
                        {
                            SpesLogger.Detail("Ход не является допустимым");
                            turn = new();
                            UpdateTurnValid(false);
                            wallPredict.gameObject.SetActive(false);
                            return;
                        }
                        UpdateTurnValid(true);

                        wallPredict.gameObject.SetActive(true);
                        wallPredict.position = wph.transform.position;
                        wallPredict.rotation = turn.type == ETurnType.PlaceXForward ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, 90, 0);
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
        SetMoveMode(true);
    }

    public void UpdateTurnValid(bool state)
    {
        if (turnValid != null)
            turnValid(state);
    }

    public void SetMoveMode(bool newMoveMode)
    {
        bMoveMode = newMoveMode;

        wallPredict.gameObject.SetActive(false);

        if (BoardBlock.selectedBlock)
        {
            BoardBlock.selectedBlock.UnHighlightAround();
        }
    }

    public bool GetMoveMode()
    {
        return bMoveMode;
    }

    #endregion
}
