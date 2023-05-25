using System.Collections;
using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;

public class Pawn : NetworkBehaviour
{
	#region Constants

	private const string JUMP_ANIMATION = "PawnJump";

	#endregion

	#region Variables

	[Header("Preferences")]
	[SerializeField] protected float jumpHeight;
	[SerializeField] public float animationTime;

	[Header("InGame data")]
	public NetworkVariable<int> skinID = new NetworkVariable<int>();
	protected NetworkVariable<int> playerOrder = new NetworkVariable<int>();
	protected NetworkVariable<Point> block = new NetworkVariable<Point>();

	[Header("Components")]
	[SerializeField] private Transform skinSpawnParent;
	[SerializeField] private Animator animator;

	public Point Block { get { return block.Value; } set { block.Value = value; } }
	public int PlayerOrder { get { return playerOrder.Value; } set { playerOrder.Value = value; } }

	private PawnSkin pawnSkin;

	#endregion

	#region UnityCallbacks

	private void Awake()
	{
		block.OnValueChanged += Block_OnValueChanged;
		playerOrder.OnValueChanged += PlayerOrder_OnValueChanged;
	}

	#endregion

	#region Overrides

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();

		GameBase.Instance.skins.GetPawn(skinID.Value).InstantiateTo(skinSpawnParent, x =>
		{
			pawnSkin = x.GetComponent<PawnSkin>();

			ApplyAnimationFromSkin();

			UpdateAnimationDuration();
		});
	}

	#endregion

	#region Callbacks

	private void PlayerOrder_OnValueChanged(int previousValue, int newValue)
	{
		UpdateColor();
	}

	private void Block_OnValueChanged(Point previousValue, Point newValue)
	{
		if (!GameplayBase.Instance)
			return;

		var arr = GameplayBase.Instance.gameboard.blocks;
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
		SoundManager.Instance.PlayPawnMove();
		animator.Play(JUMP_ANIMATION);
		StartCoroutine(Animate(newBlock));
	}

	public void UpdateColor()
	{/*
		//Standard pawns coloring. Local pawns - blue, remote - red
		if (skin.name == "Default")
		{
			Color col = playerOrder.Value == GameplayBase.instance.ActivePlayer.Value ? Color.blue : Color.red;
			mesh.material.color = col;
		}*/
	}

	private void ApplyAnimationFromSkin()
	{
		AnimatorOverrideController aoc = new AnimatorOverrideController(animator.runtimeAnimatorController);
		var list = new List<KeyValuePair<AnimationClip, AnimationClip>>();
		aoc.GetOverrides(list);
		int index = list.FindIndex(x => { return x.Key.name == JUMP_ANIMATION; });
		KeyValuePair<AnimationClip, AnimationClip> current = new KeyValuePair<AnimationClip, AnimationClip>(list[index].Key, pawnSkin.clip);
		list[index] = current;
		aoc.ApplyOverrides(list);
		animator.runtimeAnimatorController = aoc;
	}

	private void UpdateAnimationDuration()
	{
		animationTime = pawnSkin.clip.length;
	}

	private IEnumerator Animate(BoardBlock point)
	{
		float time = Time.deltaTime;

		Vector3 targetPos = point.transform.position;

		float multiplier;

		while ((transform.position - targetPos).magnitude > 0.01f)
		{
			time += Time.deltaTime;
			multiplier = time / animationTime;

			Vector3 zeroedYCurrent = transform.position;
			zeroedYCurrent.y = targetPos.y;

			transform.position = Vector3.Lerp(zeroedYCurrent, targetPos, multiplier);

			yield return null;
		}
		var gamePrefs = GameBase.Server.GetGamePrefs();

		if (block.Value.x == gamePrefs.boardHalfExtent && block.Value.y == gamePrefs.boardHalfExtent)
		{
			GameplayBase.Instance.GameFinishedClientRpc(GameBase.Server.GetPlayerByOrder(playerOrder.Value).playerName, OwnerClientId);
		}
	}

	/// <summary>
	/// Called from end of animation clip
	/// </summary>
	private void OnAnimated()
	{
		Debug.Log("OnAnimated_____________________________________________________");
		CameraAnimator.AnimateCamera();
	}

	#endregion

	#region RPCs

	public void JumpOnSpot()
	{
		animator.Play(JUMP_ANIMATION);
	}

	#endregion
}
