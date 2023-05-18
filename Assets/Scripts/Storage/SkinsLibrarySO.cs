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

	public int GetPawnSkinsCount()
	{
		return pawnSkins.Count;
	}

	public BoardSkinSO GetBoard(int id)
	{
		return boardSkins[id];
	}

	public PawnSkinSO GetPawn(int id)
	{
		return pawnSkins[id];
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
			tasks.Add(boardSkins[sk].LoadAll());
		}

		Task t = Task.Run(async () =>
		{
			await Task.WhenAll(tasks);
		});

		t.GetAwaiter().OnCompleted(() =>
		{
			if (t.IsCompleted)
			{
				onLoaded?.Invoke();
			}
		});
	}
	#endregion
}
