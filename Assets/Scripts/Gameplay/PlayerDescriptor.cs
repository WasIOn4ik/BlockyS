using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDescriptor
{
	public bool bLocal;
	public string playerID;
	public string playerName;
	public ulong clientID;
	public int playerOrder;
	public IPlayerController playerController;
	public Pawn playerPawn;
	public int boardSkinID;
	public int pawnSkinID;
}
