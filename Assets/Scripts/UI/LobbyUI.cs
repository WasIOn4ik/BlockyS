using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
	#region Variables

	[SerializeField] private AssetReference playerCardPrefab;
	[SerializeField] private Button startGameButton;
	[SerializeField] private Button backButton;
	[SerializeField] private Transform cardsHolder;

	private List<PlayerCardUI> players = new List<PlayerCardUI>();

	#endregion

	#region UnityCallbacks

	private void Awake()
	{
		if (!NetworkManager.Singleton.IsServer)
		{
			startGameButton.gameObject.SetActive(false);
		}

		LobbyGameSystem.Instance.onPlayersListChanged += LobbySystem_onPlayersListChanged;

		playerCardPrefab.LoadAssetAsync<GameObject>().Completed += go =>
		{
			if (go.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
			{
				var card = go.Result.GetComponent<PlayerCardUI>();
				for (int i = 0; i < 4; i++)
				{
					var newCard = Instantiate(card, cardsHolder);
					newCard.gameObject.SetActive(false);
					players.Add(newCard);
				}
				UpdateInternal(LobbyGameSystem.Instance.GetPlayers());
			}
		};

		startGameButton.onClick.AddListener(() =>
		{
			GameBase.Server.SetMaxRemotePlayersCount(LobbyGameSystem.Instance.GetPlayers().Count - 1);
			SceneLoader.LoadNetwork(Scenes.GameScene);
		});

		backButton.onClick.AddListener(() =>
		{
			if (NetworkManager.Singleton.IsServer)
				GameBase.Server.ClearAll();
			else
				GameBase.Client.ClearAll();

			SceneLoader.LoadScene(Scenes.StartupScene);
		});
	}

	private void LobbySystem_onPlayersListChanged(object sender, LobbyGameSystem.ConnectedPlayersEventArgs e)
	{
		for (int i = 0; i < players.Count; i++)
		{
			players[i].gameObject.SetActive(false);
		}

		UpdateInternal(e.value);
	}

	private void UpdateInternal(List<LobbyPlayerDescriptor> list)
	{
		for (int i = 0; i < players.Count && i < list.Count; i++)
		{
			players[i].gameObject.SetActive(true);
			players[i].UpdateDisplay(list[i]);
		}

		startGameButton.interactable = list.Count > 1;
	}

	#endregion

	#region Functions

	#endregion
}
