using UnityEngine;

public class InputComponent : MonoBehaviour
{
	#region Variables

	public delegate void Function(bool b);
	public event Function turnValid;

	public IPlayerController controller;

	[Header("Components")]
	[SerializeField] private MonoBehaviour controllerComponent;
	[SerializeField] private Transform ghostWallVIsualPrefab;
	[SerializeField] private BoardWall wallPrefab;

	[Header("Preferences")]
	[SerializeField] private LayerMask moveLayer;
	[SerializeField] private LayerMask placeLayer;
	[SerializeField] private float dragThreshold = 5f;

	[SerializeField] float displace = 2f;

	private bool bMoveMode = true;

	private Vector3 mouseDragStartPos;

	/// <summary>
	/// its false, when drag
	/// </summary>
	private bool bClick = false;

	private Vector3 cameraForwardMovementDirection;
	private Vector3 cameraRightMovementDirection;

	private Transform ghostWallVisual;

	private Turn currentTurnCache;

	private ETurnType currentPlaceType = ETurnType.PlaceXForward;
	private WallPlaceholder previousClickedPlaceholder;

	#endregion

	#region UnityCallbacks

	private void Awake()
	{
		controller = controllerComponent as IPlayerController;

		ghostWallVisual = Instantiate(ghostWallVIsualPrefab);
		ghostWallVisual.gameObject.SetActive(false);
	}

	private void Update()
	{
		if (controller.GetPlayerInfo().state == EPlayerState.Waiting)
			return;

#if UNITY_ANDROID

		if (Input.touchCount > 0)
		{
			var touch = Input.GetTouch(0);

			if (touch.phase == TouchPhase.Began)
			{
				mouseDragStartPos = new Vector3(touch.position.x, touch.position.y);
				bClick = true;
			}
			else if (touch.phase == TouchPhase.Moved)
			{
				bClick = false;
			}
			else if (bClick && touch.phase == TouchPhase.Ended && controller.GetPlayerInfo().state == EPlayerState.ActivePlayer)
			{
				//Pawn moving
				if (bMoveMode)
				{
					TryMovePawn();
				}
				//Wall placing
				else
				{
					TryPlaceWall();
				}
			}

			//Camera movement
			if (touch.phase == TouchPhase.Moved)
			{
				var delta = new Vector3(touch.position.x, touch.position.y) - mouseDragStartPos;
				mouseDragStartPos = new Vector3(touch.position.x, touch.position.y);
				int halfExtent = GameplayBase.Instance.gameboard.halfExtention;
				var temp = transform.position - (cameraForwardMovementDirection * delta.y + cameraRightMovementDirection * delta.x) / 100;

				temp.x = Mathf.Clamp(temp.x, -halfExtent - displace * cameraForwardMovementDirection.x, halfExtent - displace * cameraForwardMovementDirection.x);
				temp.z = Mathf.Clamp(temp.z, -halfExtent - displace * cameraForwardMovementDirection.z, halfExtent - displace * cameraForwardMovementDirection.z);

				transform.position = temp;
			}
		}
#endif

#if UNITY_EDITOR || UNITY_STANDALONE

		if (Input.GetMouseButtonDown(0))
		{
			mouseDragStartPos = Input.mousePosition;
			bClick = true;
		}
		else if ((Input.mousePosition - mouseDragStartPos).magnitude > dragThreshold)
		{
			bClick = false;
		}

		else if (bClick && Input.GetMouseButtonUp(0) && controller.GetPlayerInfo().state == EPlayerState.ActivePlayer)
		{
			if (bMoveMode)
			{
				TryMovePawn();
			}
			else
			{
				TryPlaceWall();
			}
		}

		if (Input.GetMouseButton(0))
		{
			var delta = Input.mousePosition - mouseDragStartPos;
			mouseDragStartPos = Input.mousePosition;

			int halfExtent = GameplayBase.Instance.gameboard.halfExtention;
			var temp = transform.position - (cameraForwardMovementDirection * delta.y + cameraRightMovementDirection * delta.x) / 100;

			temp.x = Mathf.Clamp(temp.x, -halfExtent - displace * cameraForwardMovementDirection.x, halfExtent - displace * cameraForwardMovementDirection.x);
			temp.z = Mathf.Clamp(temp.z, -halfExtent - displace * cameraForwardMovementDirection.z, halfExtent - displace * cameraForwardMovementDirection.z);

			transform.position = temp;
		}
#endif
	}

	#endregion

	#region Functions 

	public void SetVectors(Vector3 forward, Vector3 right)
	{
		cameraForwardMovementDirection = forward;
		cameraForwardMovementDirection.y = 0;
		cameraForwardMovementDirection.Normalize();

		cameraRightMovementDirection = right;
		cameraRightMovementDirection.y = 0;
		cameraRightMovementDirection.Normalize();
	}

	public void ConfirmTurn()
	{
		controller.EndTurn(currentTurnCache);
		currentPlaceType = ETurnType.PlaceXForward;
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

		ghostWallVisual.gameObject.SetActive(false);

		BoardBlock.ClearCurrentSelection();
	}

	public bool GetMoveMode()
	{
		return bMoveMode;
	}

	private bool TryPlaceWall()
	{
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

		if (Physics.Raycast(ray, out RaycastHit hit, 1000f, placeLayer))
		{
			var wph = hit.collider.gameObject.GetComponentInParent<WallPlaceholder>();

			if (wph)
			{
				//Wall rotation handle
				if (previousClickedPlaceholder == wph)
				{
					currentPlaceType = RotateWall(currentPlaceType);
				}
				previousClickedPlaceholder = wph;

				currentTurnCache = new(currentPlaceType, wph.coords);

				//Wall position check
				if (!GameplayBase.Instance.CheckPlace(currentTurnCache))
				{
					currentTurnCache.type = RotateWall(currentTurnCache.type);
					currentPlaceType = currentTurnCache.type;

					//Check if wall can be placed in only one rotation
					if (!GameplayBase.Instance.CheckPlace(currentTurnCache))
					{
						SpesLogger.Detail("Wall cannot be placed with selected rotation");
						currentTurnCache = new();
						UpdateTurnValid(false);
						ghostWallVisual.gameObject.SetActive(false);
						return false;
					}
				}
				UpdateTurnValid(true);

				ghostWallVisual.gameObject.SetActive(true);
				ghostWallVisual.position = wph.transform.position;
				ghostWallVisual.rotation = currentTurnCache.type == ETurnType.PlaceXForward ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, 90, 0);
			}
		}
		return true;
	}

	private bool TryMovePawn()
	{
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

		if (Physics.Raycast(ray, out RaycastHit hit, 1000f, moveLayer))
		{
			var bb = hit.collider.gameObject.GetComponentInParent<BoardBlock>();

			if (bb)
			{
				SpesLogger.Deb("Clicking on block: " + bb.name);

				//Starting pawn move
				if (controller.GetPlayerInfo().pawn.Block == bb.coords)
				{
					if (bb.IsSelectedBlock())
						bb.UnHighlightAround();
					else
						bb.HighlightAround();
				}
				//Ending pawn move
				else if (bb.bHighlighted)
				{
					currentTurnCache = new(ETurnType.Move, bb.coords);
					ConfirmTurn();
					return true;
				}
			}
		}
		return false;
	}

	private ETurnType RotateWall(ETurnType wt)
	{
		return (wt == ETurnType.PlaceXForward ? ETurnType.PlaceZForward : ETurnType.PlaceXForward);
	}

	#endregion
}
