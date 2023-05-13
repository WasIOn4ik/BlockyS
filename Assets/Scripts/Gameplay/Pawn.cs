using System.Collections;
using UnityEngine;
using Unity.Netcode;

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

	protected PawnSkinDescription skin;

	#endregion

	#region UnityCallbacks

	private void Awake()
	{
		block.OnValueChanged += OnMoved;
		playerOrder.OnValueChanged += OnPlayerOrderAssigned;
	}

	#endregion

	#region Overrides

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();

		if (!IsOwner || IsServer)
			OnAnimated += GameplayBase.instance.cameraAnimator.AnimateCamera;
	}

	#endregion

	#region Callbacks

	private void OnPlayerOrderAssigned(int previousValue, int newValue)
	{
		UpdateColor();
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
			HandleAnimation(newBlock);
		}
		else
		{
			SpesLogger.Error("Out of map while updating block in Pawn: " + name);
		}
	}

	#endregion

	#region Functions

	public void HandleAnimation(BoardBlock newBlock)
	{
		StartCoroutine(Animate(newBlock));
	}

	public void UpdateColor()
	{
		//Standard pawns coloring. Local pawns - blue, remote - red
		if (skin.name == "Default")
		{
			Color col = playerOrder.Value == GameplayBase.instance.ActivePlayer.Value ? Color.blue : Color.red;
			mesh.material.color = col;
		}
	}

	private IEnumerator Animate(BoardBlock point)
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
					string winnerName = playerInfo;
					//If it's local player, adding suffix with playerOrder
					if (IsOwner)
					{
						winnerName = winnerName + "_" + playerOrder.Value;
					}
					GameplayBase.instance.GameFinishedClientRpc(winnerName);
				}
			}
		}

		if (OnAnimated != null)
			OnAnimated();
	}

	#endregion

	#region RPCs

	[ClientRpc(Delivery = RpcDelivery.Reliable)]
	public void SetSkinClientRpc(int ind)
	{
		skin = GameBase.instance.skins.GetUncheckedPawnSkin(ind);

		filter.mesh = skin.mesh;
		mesh.material = skin.mat;

		UpdateColor();
	}

	#endregion
}
