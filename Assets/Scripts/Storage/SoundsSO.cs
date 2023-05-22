using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "BlockyS/Sounds")]
public class SoundsSO : ScriptableObject
{
	[Header("Effects")]
	public AudioClip buttonClip;
	public AudioClip backClip;
	public AudioClip pawnMoveClip;
	public AudioClip wallPlaceClip;
	public AudioClip winClip;
	public AudioClip loseClip;

	[Header("Music")]
	public AudioClip menuMusic;
	public AudioClip[] gameMusic;
}
