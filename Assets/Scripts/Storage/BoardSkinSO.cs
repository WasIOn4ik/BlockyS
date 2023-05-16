using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[CreateAssetMenu(menuName = "BlockyS/BoardSkin")]
public class BoardSkinSO : ScriptableObject
{
	#region Variables

	public const string BOARD_LABEL = "Board";
	public string title;

	public int cost;

	[SerializeField] private AssetReference materialAsset;

	[SerializeField] private AssetReference decorMesh1;
	[SerializeField] private AssetReference decorMesh2;
	[SerializeField] private AssetReference decorMesh3;

	[SerializeField] private AssetReference wallMesh;

	[SerializeField] private List<AssetReference> blocks;

	#endregion

	#region Getters

	public bool TryGetDecorMesh(int i, out Mesh mesh)
	{
		mesh = null;
		switch (i)
		{
			case 0:
				if (decorMesh1.IsValid())
				{
					mesh = decorMesh1.Asset as Mesh;
					return true;
				}
				return false;
			case 1:
				if (decorMesh2.IsValid())
				{
					mesh = decorMesh2.Asset as Mesh;
					return true;
				}
				return false;
			case 2:
				if (decorMesh3.IsValid())
				{
					mesh = decorMesh3.Asset as Mesh;
					return true;
				}
				return false;
			case 3:
				if (decorMesh1.IsValid())
				{
					mesh = decorMesh1.Asset as Mesh;
					return true;
				}
				return false;
		}
		return false;
	}

	public bool TryGetWall(out Mesh wall)
	{
		if (wallMesh.IsValid())
		{
			wall = wallMesh.OperationHandle.Result as Mesh;
			return true;
		}
		wall = null;
		return false;
	}

	public bool TryGetMaterial(out Material mat)
	{
		if (materialAsset.IsValid())
		{
			mat = materialAsset.OperationHandle.Result as Material;
			return true;
		}
		mat = null;
		return false;
	}

	public bool TryGetBlock(out Mesh blockMesh)
	{
		AssetReference blockAssetReference = blocks[UnityEngine.Random.Range(0, blocks.Count)];
		if (blockAssetReference.IsValid())
		{
			blockMesh = blockAssetReference.OperationHandle.Result as Mesh;
			return true;
		}
		blockMesh = null;
		return false;
	}

	#endregion

	#region Functions

	public void LoadCustomizationDisplay(Action<BoardSkinSO> onLoaded)
	{
		List<Task> tasks = new List<Task>();

		if (!decorMesh1.IsValid())
			tasks.Add(decorMesh1.LoadAssetAsync<Mesh>().Task);

		if (!materialAsset.IsValid())
			tasks.Add(materialAsset.LoadAssetAsync<Material>().Task);

		//Waiting for all task to complete
		Task t = Task.Run(async () =>
		{
			await Task.WhenAll(tasks);
		});

		//Executing in main thread when all tasks completed
		t.GetAwaiter().OnCompleted(() =>
		{
			if (t.IsCompleted)
			{
				onLoaded(this);
			}
		});
	}

	public Task LoadAll()
	{
		List<Task> tasks = new List<Task>();

		if (!decorMesh1.IsValid())
			tasks.Add(decorMesh1.LoadAssetAsync<Mesh>().Task);
		if (!decorMesh2.IsValid())
			tasks.Add(decorMesh2.LoadAssetAsync<Mesh>().Task);
		if (!decorMesh3.IsValid())
			tasks.Add(decorMesh3.LoadAssetAsync<Mesh>().Task);

		if (!wallMesh.IsValid())
			tasks.Add(wallMesh.LoadAssetAsync<Mesh>().Task);

		if (!materialAsset.IsValid())
			tasks.Add(materialAsset.LoadAssetAsync<Material>().Task);

		foreach (var b in blocks)
		{
			if (!b.IsValid())
			{
				tasks.Add(b.LoadAssetAsync<Mesh>().Task);
			}
		}

		//Waiting for all task to complete
		return Task.Run(async () =>
		{
			await Task.WhenAll(tasks);
		});
	}

	public void UnloadAll()
	{
		if (decorMesh1.IsValid())
			decorMesh1.ReleaseAsset();
		if (decorMesh2.IsValid())
			decorMesh2.ReleaseAsset();
		if (decorMesh3.IsValid())
			decorMesh3.ReleaseAsset();
		if (wallMesh.IsValid())
			wallMesh.ReleaseAsset();

		foreach (var b in blocks)
		{
			if (b.IsValid())
				b.ReleaseAsset();
		}
	}

	#endregion
}
