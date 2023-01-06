using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

public class CustomizationMenu : MenuBase
{
    #region Variables
    [Header("Preferences")]
    [SerializeField] protected LocalizedString selectString;
    [SerializeField] protected TMP_Text coinsCountText;

    [Header("Board skin")]
    [SerializeField] protected Button boardSelectButton;
    [SerializeField] protected TMP_Text boardSelectText;
    [SerializeField] protected Image boardFrame;
    [SerializeField] protected MeshFilter boardMesh;

    [Header("Pawn skin")]
    [SerializeField] protected Button pawnSelectButton;
    [SerializeField] protected TMP_Text pawnSelectText;
    [SerializeField] protected Image pawnFrame;
    [SerializeField] protected MeshFilter pawnMesh;

    protected Canvas canvas;

    protected SkinsLibrary skins;

    protected int selectedBoard;
    protected int selectedPawn;

    protected GameStorage storage;

    private bool bPreviousOrtho;

    #endregion

    #region UnityCallbacks

    public override void Awake()
    {
        base.Awake();

        storage = GameBase.storage;
        skins = GameBase.instance.skins;

        canvas = GetComponent<Canvas>();
        canvas.worldCamera = Camera.main;
    }

    public void OnEnable()
    {
        bPreviousOrtho = canvas.worldCamera.orthographic;
        canvas.worldCamera.orthographic = true;

        selectedPawn = storage.CurrentPawnSkin;
        selectedBoard = storage.CurrentBoardSkin;

        UpdateStats();
    }

    public void OnDisable()
    {
        if (canvas && canvas.worldCamera)
            canvas.worldCamera.orthographic = bPreviousOrtho;
        SpesLogger.Detail("Выбраны скины: " + storage.CurrentBoardSkin + " " + storage.CurrentPawnSkin);
    }

    #endregion

    #region Functions

    private void UpdateStats()
    {
        SelectBoardSkin(selectedBoard);
        SelectPawnSkin(selectedPawn);

        coinsCountText.text = storage.GetCoins().ToString();
    }

    protected void SelectBoardSkin(int skinNumber)
    {
        var skin = skins.boardSkins[skinNumber];
        boardMesh.mesh = skin.variation1Mesh;
        boardMesh.GetComponent<MeshRenderer>().material = skin.mat;

        //Если скин бесплтаный или куплен
        if (skin.cost == 0 || storage.CheckBoard(skinNumber))
        {
            boardSelectText.text = selectString.GetLocalizedString();
            boardSelectButton.interactable = storage.CurrentBoardSkin != skinNumber;
        }
        else
        {
            boardSelectText.text = "<color=#FFD700>" + skin.cost;
            boardSelectButton.interactable = storage.GetCoins() >= skin.cost || skin.cost == 0;
        }
    }

    protected void SelectPawnSkin(int skinNumber)
    {
        var skin = skins.pawnSkins[skinNumber];
        pawnMesh.mesh = skin.mesh;
        pawnMesh.GetComponent<MeshRenderer>().material = skin.mat;
        pawnMesh.transform.localScale = Vector3.one * skin.scale;
        pawnMesh.transform.localRotation = Quaternion.Euler(skin.rotation);
        pawnMesh.transform.localPosition = skin.position;

        if (skin.cost == 0 || storage.CheckPawn(skinNumber))
        {
            pawnSelectText.text = selectString.GetLocalizedString();
            pawnSelectButton.interactable = storage.CurrentPawnSkin != skinNumber;
        }
        else
        {
            pawnSelectText.text = "<color=#FFD700>" + skin.cost;
            pawnSelectButton.interactable = storage.GetCoins() >= skin.cost || skin.cost == 0;
        }
    }

    #endregion

    #region UIFunctions

    public void ConfirmSelectBoard()
    {
        if (storage.TryBuyOrEquipBoard(selectedBoard))
        {
            storage.CurrentBoardSkin = selectedBoard;
            UpdateStats();
        }
    }

    public void ConfirmSelectPawn()
    {
        if (storage.TryBuyOrEquipPawn(selectedPawn))
        {
            storage.CurrentPawnSkin = selectedPawn;
            UpdateStats();
        }
    }

    public void NextBoard()
    {
        selectedBoard++;

        if (selectedBoard >= skins.boardSkins.Count)
            selectedBoard = 0;

        SelectBoardSkin(selectedBoard);
    }

    public void PrevBoard()
    {
        selectedBoard--;

        if (selectedBoard < 0)
            selectedBoard = skins.boardSkins.Count - 1;

        SelectBoardSkin(selectedBoard);
    }

    public void NextPawn()
    {
        selectedPawn++;

        if (selectedPawn >= skins.pawnSkins.Count)
            selectedPawn = 0;

        SelectPawnSkin(selectedPawn);
    }

    public void PrevPawn()
    {
        selectedPawn--;

        if (selectedPawn < 0)
            selectedPawn = skins.pawnSkins.Count - 1;

        SelectPawnSkin(selectedPawn);
    }

    #endregion
}
