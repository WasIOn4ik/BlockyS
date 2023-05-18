using UnityEngine;

[CreateAssetMenu(menuName = "Spes/GameRules")]
public class GameRules : ScriptableObject
{
	[Header("Timers")]
	public float turnTime;
	public float cameraAnimationTime;

	[Header("Walls per size")]
	public int x5Count;
	public int x7Count;
	public int x9Count;

	[Header("Camera settings")]
	public float cameraHeight;
	public float cameraBackwardOffset;
}
