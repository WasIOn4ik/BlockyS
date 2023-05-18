using UnityEngine;

public interface IPlayerController
{
	public void StartTurn();

	public void EndTurn(Turn turn);

	public MonoBehaviour GetMono();

	public PlayerInGameInfo GetPlayerInfo();

	public void SetPlayerInfo(PlayerInGameInfo inf);

	public PlayerCosmetic GetCosmetic();

	public void UpdateTurn(int active);

	public void TurnTimeout();
}
