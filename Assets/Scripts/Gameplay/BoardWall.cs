using System.Collections;
using UnityEngine;
using Unity.Netcode;
using System;

public class BoardWall : NetworkBehaviour
{
	#region Variables

	[SerializeField] private MeshRenderer meshRenderer;
	[SerializeField] private MeshFilter meshFilter;

	public NetworkVariable<Turn> coords = new();

	public delegate void MovedDelegate();
	public event MovedDelegate OnAnimated;

	#endregion

	#region UnityCallbacks

	private void Awake()
	{
		coords.OnValueChanged += OnPlaced;
	}

	#endregion

	#region Callbacks

	private void OnSkinChanged(int newValue)
	{
		var skin = GameBase.Instance.skins.GetBoard(newValue);
		if (skin.TryGetWall(out var wallMesh))
		{
			meshFilter.mesh = wallMesh;
		}
	}

	private void OnPlaced(Turn previousValue, Turn newValue)
	{
		SpesLogger.Detail(newValue.type.ToString() + " " + newValue.pos.x + " x " + newValue.pos.y);
		var blocks = GameplayBase.Instance.gameboard.blocks;

		if (newValue.type == ETurnType.PlaceXForward)
		{
			try
			{
				blocks[newValue.pos.x, newValue.pos.y].zDir = null;
				blocks[newValue.pos.x + 1, newValue.pos.y].zDir = null;
				blocks[newValue.pos.x, newValue.pos.y + 1].mzDir = null;
				blocks[newValue.pos.x + 1, newValue.pos.y + 1].mzDir = null;
			}
			catch (Exception ex)
			{
				SpesLogger.Exception(ex, "Can't break connections while X placing");
			}
		}
		else if (newValue.type == ETurnType.PlaceZForward)
		{
			try
			{
				blocks[newValue.pos.x, newValue.pos.y].xDir = null;
				blocks[newValue.pos.x + 1, newValue.pos.y].mxDir = null;
				blocks[newValue.pos.x, newValue.pos.y + 1].xDir = null;
				blocks[newValue.pos.x + 1, newValue.pos.y + 1].mxDir = null;
			}
			catch (Exception ex)
			{
				SpesLogger.Exception(ex, "Can't break connections while Z placing");
			}
		}
		GameplayBase.Instance.gameboard.wallsPlaces[newValue.pos.x, newValue.pos.y].bEmpty = false;

		OnSkinChanged(blocks[newValue.pos.x, newValue.pos.y].skinID);
		Animate();
	}

	#endregion

	#region Functions

	private void Animate()
	{
		StartCoroutine(HandleAnimation());
	}

	private IEnumerator HandleAnimation()
	{
		float time = Time.deltaTime;
		float animationTime = 2f;

		Vector3 targetPos = transform.position;

		transform.position = targetPos - Vector3.up;

		float multiplier;

		while ((transform.position - targetPos).magnitude > 0.01f)
		{
			time += Time.deltaTime;
			multiplier = time / animationTime;

			transform.position = Vector3.Lerp(transform.position, targetPos, multiplier);

			yield return null;
		}

		if (OnAnimated != null)
		{
			OnAnimated();
			foreach (MovedDelegate d in OnAnimated.GetInvocationList())
			{
				OnAnimated -= d;
			}
		}
	}

	#endregion
}
