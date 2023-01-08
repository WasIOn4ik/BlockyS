using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class Pawn : NetworkBehaviour
{
    #region Variables

    [Header("Preferences")]
    [SerializeField] protected MeshFilter filter;
    [SerializeField] protected MeshRenderer mesh;
    [SerializeField] protected float jumpHeight;
    [SerializeField] public float animationTime;

    [Header("InGame data")]
    public NetworkVariable<int> playerOrder = new NetworkVariable<int>();
    public NetworkVariable<Point> block = new NetworkVariable<Point>();

    public delegate void MovedDelegate();
    public event MovedDelegate OnAnimated;

    protected PawnDescription skin;

    #endregion

    #region UnityCallbacks

    public void Awake()
    {
        block.OnValueChanged += OnMoved;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner || IsServer)
            OnAnimated += GameplayBase.instance.cameraAnimator.AnimateCamera;
    }

    private void OnMoved(Point previousValue, Point newValue)
    {
        var arr = GameplayBase.instance.gameboard.blocks;
        if (previousValue != null)
        {
            var prevBlock = arr[previousValue.x, previousValue.y];
            if (prevBlock)
            {
                prevBlock.bEmpty = true;
            }
        }
        if (newValue.x < arr.GetLength(0) && newValue.y < arr.GetLength(1))
        {
            var newBlock = arr[newValue.x, newValue.y];
            newBlock.bEmpty = false;
            StartCoroutine(Animate(newBlock));
            //transform.position = newBlock.transform.position;
        }
        else
        {
            SpesLogger.Error("Выход за пределы карты при обновлении block в Pawn: " + name);
        }
    }

    #endregion 

    #region Functions

    public IEnumerator Animate(BoardBlock point)
    {
        float time = Time.deltaTime;

        Vector3 targetPos = point.transform.position;

        float distance = (transform.position - targetPos).magnitude;

        float sinus = 0.0f;

        float multiplier;

        while ((transform.position - targetPos).magnitude > 0.01f)
        {
            time += Time.deltaTime;
            multiplier = time / animationTime;

            Vector3 zeroedYCurrent = transform.position;
            zeroedYCurrent.y = targetPos.y;

            sinus = Mathf.Lerp(sinus, 1, multiplier * distance);

            transform.position = Vector3.Lerp(zeroedYCurrent, targetPos, multiplier)
                + (Vector3.up * (jumpHeight * Mathf.Sin(Mathf.PI * sinus)));

            yield return null;
        }

        if (IsServer)
        {
            if (block.Value.x == GameBase.server.prefs.boardHalfExtent && block.Value.y == GameBase.server.prefs.boardHalfExtent)
            {
                if (GameBase.server.Clients.TryGetValue(OwnerClientId, out var playerInfo))
                {
                    string res = playerInfo;
                    if (IsOwner)
                    {
                        res = res + "_" + playerOrder.Value;
                    }
                    GameplayBase.instance.GameFinishedClientRpc(res);
                }
            }
        }

        if (OnAnimated != null)
            OnAnimated();
    }

    public void UpdateColor()
    {
        //Стандартные пешки красятся в разные цвета. Синий - локальный игрок, Красный - враг
        if (skin.name == "Default")
        {
            mesh.material.color = (IsServer || IsOwner) && playerOrder.Value == GameplayBase.instance.ActivePlayer.Value ? Color.blue : Color.red;
        }
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    public void SetSkinClientRpc(int ind)
    {
        skin = GameBase.instance.skins.pawnSkins[ind];

        filter.mesh = skin.mesh;
        mesh.material = skin.mat;

        UpdateColor();
    }

    #endregion
}
