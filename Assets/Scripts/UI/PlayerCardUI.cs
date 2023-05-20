using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCardUI : MonoBehaviour
{
	#region Variables

	[SerializeField] private TMP_Text playerNameText;
	[SerializeField] private TMP_Text pawnSkinText;
	[SerializeField] private TMP_Text boardSkinText;
	[SerializeField] private GameObject hostOnlyContainer;
	[SerializeField] private Button kickButton;

	private NetworkManager networkManager;

	#endregion

	#region UnityCallbacks

	private void Awake()
	{
		networkManager = NetworkManager.Singleton;

		hostOnlyContainer.gameObject.SetActive(networkManager.IsServer);
	}

	#endregion

	#region Functions

	public void UpdateDisplay(LobbyPlayerDescriptor descriptor)
	{
		var skins = GameBase.Instance.skins;
		var board = skins.GetBoard(descriptor.boardSkin);
		var pawn = skins.GetPawn(descriptor.pawnSkin);

		playerNameText.text = descriptor.playerName.ToString();

		pawn.localizedTitle.GetLocalizedStringAsync().Completed += x =>
		{
			pawnSkinText.text = x.Result;
		};

		board.localizedTitle.GetLocalizedStringAsync().Completed += x =>
		{
			boardSkinText.text = x.Result;
		};
	}

	#endregion
}
