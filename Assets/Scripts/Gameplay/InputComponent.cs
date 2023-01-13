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
    /// <summary>
    /// ����� ������������ "��������" ������ ������� � ��������
    /// </summary>
    public float clickThreshold = 5f;

    [SerializeField] float displace = 2f;

    protected bool bMoveMode = true;

    public IPlayerController controller;

    /// <summary>
    /// ����������, �� ������� ���� ���� ������ ��� �������� �������
    /// </summary>
    protected Vector3 startPos;

    /// <summary>
    /// ���������� false, ����� ��������� �������� ������ �����
    /// </summary>
    protected bool bClick = false;

    /// <summary>
    /// ������ �������� ������ "������"
    /// </summary>
    protected Vector3 forwardDir;
    /// <summary>
    /// ������ �������� ������ "������"
    /// </summary>
    protected Vector3 rightDir;

    /// <summary>
    /// ���������� ������������� ��������� ������ �� ������������� ����
    /// </summary>
    protected Transform wallPredict;
    /// <summary>
    /// ��� ����
    /// </summary>
    protected Turn turn;

    /// <summary>
    /// ������� ������������ ������, ����� ��� ���������� ��������
    /// </summary>
    protected ETurnType placeType = ETurnType.PlaceXForward;
    /// <summary>
    /// ���������� �����������, ����� ��� ���������� �������� �� ���������� �������
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

            if (touch.phase == TouchPhase.Began)
            {
                startPos = new Vector3(touch.position.x, touch.position.y);
                bClick = true;
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                bClick = false;
            }
            else if (bClick && touch.phase == TouchPhase.Ended && controller.GetPlayerInfo().state == EPlayerState.ActivePlayer)
            {
                //����� ����������� �����
                if (bMoveMode)
                {
                    TryMovePawn();
                }
                //����� ������������� ������
                else
                {
                    TryPlaceWall();
                }
            }

            //���������� �������
            if (touch.phase == TouchPhase.Moved)
            {
                var delta = new Vector3(touch.position.x, touch.position.y) - startPos;
                startPos = new Vector3(touch.position.x, touch.position.y);
                int halfExtent = GameplayBase.instance.gameboard.halfExtention;
                var temp = transform.position - (forwardDir * delta.y + rightDir * delta.x) / 100;

                temp.x = Mathf.Clamp(temp.x, -halfExtent - displace * forwardDir.x, halfExtent - displace * forwardDir.x);
                temp.z = Mathf.Clamp(temp.z, -halfExtent - displace * forwardDir.z, halfExtent - displace * forwardDir.z);

                transform.position = temp;
            }
        }
#endif

#if UNITY_EDITOR || UNITY_STANDALONE

        if (Input.GetMouseButtonDown(0))
        {
            startPos = Input.mousePosition;
            bClick = true;
        }
        else if ((Input.mousePosition - startPos).magnitude > clickThreshold)
        {
            bClick = false;
        }

        else if (bClick && Input.GetMouseButtonUp(0) && controller.GetPlayerInfo().state == EPlayerState.ActivePlayer)
        {
            //����� ����������� �����
            if (bMoveMode)
            {
                TryMovePawn();
            }
            //����� ������������� ������
            else
            {
                TryPlaceWall();
            }
        }

        //���������� �������
        if (Input.GetMouseButton(0))
        {
            var delta = Input.mousePosition - startPos;
            startPos = Input.mousePosition;

            int halfExtent = GameplayBase.instance.gameboard.halfExtention;
            var temp = transform.position - (forwardDir * delta.y + rightDir * delta.x) / 100;

            temp.x = Mathf.Clamp(temp.x, -halfExtent - displace * forwardDir.x, halfExtent - displace * forwardDir.x);
            temp.z = Mathf.Clamp(temp.z, -halfExtent - displace * forwardDir.z, halfExtent - displace * forwardDir.z);

            transform.position = temp;
        }
#endif
    }

    #endregion

    #region Functions 

    public void ConfirmTurn()
    {
        controller.EndTurn(turn);
        placeType = ETurnType.PlaceXForward;
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

    protected bool TryPlaceWall()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, placeLayer))
        {
            var wph = hit.collider.gameObject.GetComponentInParent<WallPlaceholder>();

            if (wph)
            {
                //��������� �������� ������
                if (previousClickedPlaceholder == wph)
                {
                    placeType = RotateWall(placeType);
                }
                previousClickedPlaceholder = wph;

                turn = new(placeType, wph.coords);

                //�������� ��������� �������������
                if (!GameplayBase.instance.CheckPlace(turn))
                {
                    turn.type = RotateWall(turn.type);
                    placeType = turn.type;

                    //���� ������ ����� ��������� ������ � ����� ���������
                    if (!GameplayBase.instance.CheckPlace(turn))
                    {
                        SpesLogger.Detail("��� �� �������� ����������");
                        turn = new();
                        UpdateTurnValid(false);
                        wallPredict.gameObject.SetActive(false);
                        return false;
                    }
                }
                UpdateTurnValid(true);

                wallPredict.gameObject.SetActive(true);
                wallPredict.position = wph.transform.position;
                wallPredict.rotation = turn.type == ETurnType.PlaceXForward ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, 90, 0);
            }
        }
        return true;
    }

    protected bool TryMovePawn()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, moveLayer))
        {
            var bb = hit.collider.gameObject.GetComponentInParent<BoardBlock>();

            if (bb)
            {
                SpesLogger.Deb("������� �� ����: " + bb.name);

                //������ ���� �� �����
                if (controller.GetPlayerInfo().pawn.block.Value == bb.coords)
                {
                    if (bb.bSelected)
                        bb.UnHighlightAround();
                    else
                        bb.HighlightAround();
                }
                //������������� ���� �� �����
                else if (bb.bHighlighted)
                {
                    turn = new(ETurnType.Move, bb.coords);
                    ConfirmTurn();
                    return true;
                }
            }
        }
        return false;
    }

    protected ETurnType RotateWall(ETurnType wt)
    {
        return (wt == ETurnType.PlaceXForward ? ETurnType.PlaceZForward : ETurnType.PlaceXForward);
    }

    #endregion
}
