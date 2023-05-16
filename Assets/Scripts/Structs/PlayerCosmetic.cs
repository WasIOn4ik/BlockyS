
using System;
using Unity.Netcode;

[Serializable]
public struct PlayerCosmetic : IEquatable<PlayerCosmetic>, INetworkSerializable
{
	public int pawnSkinID;
	public int boardSkinID;

	public bool Equals(PlayerCosmetic other)
	{
		return pawnSkinID == other.pawnSkinID && boardSkinID == other.boardSkinID;
	}

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		serializer.SerializeValue(ref pawnSkinID);
		serializer.SerializeValue(ref boardSkinID);
	}
}
