using System.Collections;
using UnityEngine;
public class CameraAnimator
{
	static public float animationTime = 2f;

	public static void AnimateCamera()
	{
		GameplayBase.Instance.StartCoroutine(HandleAnimation(Camera.main));
	}

	protected static IEnumerator HandleAnimation(Camera cam)
	{
		float time = Time.deltaTime;
		while (cam.transform.localPosition.magnitude > 0.005f)
		{
			cam.transform.localPosition = Vector3.Lerp(cam.transform.localPosition, Vector3.zero, time / animationTime);
			cam.transform.localRotation = Quaternion.Slerp(cam.transform.localRotation, Quaternion.identity, time / animationTime);

			time += Time.deltaTime;

			yield return null;
		}
	}
}
