using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.AddressableAssets;
using System.Threading.Tasks;
using JetBrains.Annotations;

[CreateAssetMenu(menuName = "Spes/Skins")]
public class SkinsLibrarySO : ScriptableObject
{
	#region Variables

	[SerializeField] private string labelToLoadBoardSkins;
	[SerializeField] private string labelToLoadPawnSkins;

	[SerializeField] private List<BoardSkinSO> boardSkins = new();
	[SerializeField] private List<PawnSkinSO> pawnSkins = new();

	#endregion

	#region Getters

	public bool TryGetBoard(string skinName, out BoardSkinSO skin)
	{
		foreach (var sk in boardSkins)
		{
			if (sk.title == skinName)
			{
				skin = sk;
				return true;
			}
		}
		skin = null;
		return false;
	}

	public bool TryGetPawn(string skinName, out PawnSkinSO skin)
	{
		foreach (var sk in pawnSkins)
		{
			if (sk.title == skinName)
			{
				skin = sk;
				return true;
			}
		}
		skin = null;
		return false;
	}

	public int GetBoardSkinsCount()
	{
		return boardSkins.Count;
	}

	public int GetBoardIndexInList(int id)
	{
		for (int i = 0; i < boardSkins.Count; i++)
		{
			if (boardSkins[i].id == id)
				return i;
		}

		return -1;
	}

	public int GetPawnIndexInList(int id)
	{
		for (int i = 0; i < pawnSkins.Count; i++)
		{
			if (pawnSkins[i].id == id)
				return i;
		}

		return -1;
	}

	public int GetPawnSkinsCount()
	{
		return pawnSkins.Count;
	}

	public BoardSkinSO GetBoard(int id)
	{
		return boardSkins.Find(x =>
		{
			return x.id == id;
		});
	}

	public PawnSkinSO GetPawn(int id)
	{
		return pawnSkins.Find(x =>
		{
			return x.id == id;
		});
	}

	public BoardSkinSO GetBoardByListIndex(int index)
	{
		return boardSkins[index];
	}

	public PawnSkinSO GetPawnByListIndex(int index)
	{
		return pawnSkins[index];
	}

	#endregion

	#region Functions

	public void Initialize()
	{
		boardSkins.Clear();
		Addressables.LoadAssetsAsync<BoardSkinSO>(new List<string> { labelToLoadBoardSkins }, x => { boardSkins.Add(x); }, Addressables.MergeMode.Union, true);

		pawnSkins.Clear();
		Addressables.LoadAssetsAsync<PawnSkinSO>(new List<string> { labelToLoadPawnSkins }, x => { pawnSkins.Add(x); }, Addressables.MergeMode.Union, true);

		SpesLogger.Detail("Skins data loaded successfully");
	}

	public void PreloadBoardSkins(IEnumerable<int> skins, Action onLoaded)
	{
		List<Task> tasks = new List<Task>();

		foreach (var sk in skins)
		{
			tasks.Add(GetBoard(sk).LoadAll());
		}

		Task t = Task.Run(async () =>
		{
			await Task.WhenAll(tasks);
			Debug.Log("Preload ended");
		});

		t.GetAwaiter().OnCompleted(() =>
		{
			if (t.IsCompleted)
			{
				Debug.Log("PreCallback");
				onLoaded?.Invoke();
			}
		});
	}

	public Task ReleaseAll(Action onRelease = null)
	{
		return Task.Run(() =>
		{
			foreach (var bs in boardSkins)
			{
				bs.UnloadAll();
			}

			foreach (var ps in pawnSkins)
			{
				ps.Unload();
			}
		});
	}

	#endregion
}
