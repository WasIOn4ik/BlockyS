using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;

public class CustomizationMenu : MenuBase
{
	#region Variables
	[Header("Preferences")]
	[SerializeField] private LocalizedString selectString;
	[SerializeField] private TMP_Text coinsCountText;

	[Header("Components")]
	[SerializeField] private Button backButton;

	[Header("Board skin")]
	[SerializeField] private Button boardSelectButton;
	[SerializeField] private Button boardLeftButton;
	[SerializeField] private Button boardRightButton;
	[SerializeField] private TMP_Text boardSelectText;
	[SerializeField] private Image boardFrame;
	[SerializeField] private MeshFilter boardMesh;

	[Header("Pawn skin")]
	[SerializeField] private Button pawnSelectButton;
	[SerializeField] private Button pawnLeftButton;
	[SerializeField] private Button pawnRightButton;
	[SerializeField] private TMP_Text pawnSelectText;
	[SerializeField] private Image pawnFrame;
	[SerializeField] private MeshFilter pawnMesh;

	private Canvas canvas;

	private SkinsLibrary skins;

	private int selectedBoard;
	private int selectedPawn;

	private GameStorage storage;

	private bool bPreviousOrtho;

	#endregion

	#region UnityCallbacks

	protected override void Awake()
	{
		base.Awake();

		storage = GameBase.storage;
		skins = GameBase.instance.skins;

		canvas = GetComponent<Canvas>();
		canvas.worldCamera = Camera.main;

		backButton.onClick.AddListener(() =>
		{
			BackToPreviousMenu();
		});

		boardLeftButton.onClick.AddListener(() =>
		{
			selectedBoard--;

			if (selectedBoard < 0)
				selectedBoard = skins.GetBoardSkinsCount() - 1;

			SelectBoardSkin(selectedBoard);
		});

		boardRightButton.onClick.AddListener(() =>
		{
			selectedBoard++;

			if (selectedBoard >= skins.GetBoardSkinsCount())
				selectedBoard = 0;

			SelectBoardSkin(selectedBoard);
		});

		boardSelectButton.onClick.AddListener(() =>
		{
			if (storage.TryBuyOrEquipBoard(selectedBoard))
			{
				storage.CurrentBoardSkin = selectedBoard;
				UpdateStats();
			}
		});

		pawnLeftButton.onClick.AddListener(() =>
		{
			selectedPawn--;

			if (selectedPawn < 0)
				selectedPawn = skins.GetPawnSkinsCount() - 1;

			SelectPawnSkin(selectedPawn);
		});

		pawnRightButton.onClick.AddListener(() =>
		{
			selectedPawn++;

			if (selectedPawn >= skins.GetPawnSkinsCount())
				selectedPawn = 0;

			SelectPawnSkin(selectedPawn);
		});

		pawnSelectButton.onClick.AddListener(() =>
		{
			if (storage.TryBuyOrEquipPawn(selectedPawn))
			{
				storage.CurrentPawnSkin = selectedPawn;
				UpdateStats();
			}
		});
	}

	private void OnEnable()
	{
		bPreviousOrtho = canvas.worldCamera.orthographic;
		canvas.worldCamera.orthographic = true;

		selectedPawn = storage.CurrentPawnSkin;
		selectedBoard = storage.CurrentBoardSkin;

		UpdateStats();
	}

	private void OnDisable()
	{
		if (canvas && canvas.worldCamera)
			canvas.worldCamera.orthographic = bPreviousOrtho;
		SpesLogger.Detail("Skins selected: " + storage.CurrentBoardSkin + " " + storage.CurrentPawnSkin);
	}

	#endregion

	#region Functions

	private void SelectBoardSkin(int skinNumber)
	{
		var skin = skins.GetUncheckedBoardSkin(skinNumber);
		boardMesh.mesh = skin.variation1Mesh;
		boardMesh.GetComponent<MeshRenderer>().material = skin.mat;

		//���� ���� ���������� ��� ������
		if (skin.cost == 0 || storage.CheckBoard(skinNumber))
		{
			boardSelectText.text = selectString.GetLocalizedStringAsync().Result;
			boardSelectButton.interactable = storage.CurrentBoardSkin != skinNumber;
		}
		else
		{
			boardSelectText.text = "<color=#FFD700>" + skin.cost;
			boardSelectButton.interactable = storage.GetCoins() >= skin.cost || skin.cost == 0;
		}
	}

	private void SelectPawnSkin(int skinNumber)
	{
		var skin = skins.GetUncheckedPawnSkin(skinNumber);
		pawnMesh.mesh = skin.mesh;
		pawnMesh.GetComponent<MeshRenderer>().material = skin.mat;
		pawnMesh.transform.localScale = Vector3.one * skin.scale;
		pawnMesh.transform.localRotation = Quaternion.Euler(skin.rotation);
		pawnMesh.transform.localPosition = skin.position;

		if (skin.cost == 0 || storage.CheckPawn(skinNumber))
		{
			pawnSelectText.text = selectString.GetLocalizedStringAsync().Result;
			pawnSelectButton.interactable = storage.CurrentPawnSkin != skinNumber;
		}
		else
		{
			pawnSelectText.text = "<color=#FFD700>" + skin.cost;
			pawnSelectButton.interactable = storage.GetCoins() >= skin.cost || skin.cost == 0;
		}
	}

	private void UpdateStats()
	{
		SelectBoardSkin(selectedBoard);
		SelectPawnSkin(selectedPawn);

		coinsCountText.text = storage.GetCoins().ToString();
	}

	#endregion
}
