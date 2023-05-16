using System.Collections;
using UnityEngine;
public class SpesAnimator
{
    [SerializeField] protected float cameraMoveDuration = 2f;

    public void AnimateCamera()
    {
        GameplayBase.Instance.StartCoroutine(HandleAnimation(Camera.main));
    }

    protected IEnumerator HandleAnimation(Camera cam)
    {
        float time = Time.deltaTime;
        while (cam.transform.localPosition.magnitude > 0.005f)
        {
            cam.transform.localPosition = Vector3.Lerp(cam.transform.localPosition, Vector3.zero, time / cameraMoveDuration);
            cam.transform.localRotation = Quaternion.Lerp(cam.transform.localRotation, Quaternion.identity, time / cameraMoveDuration);

            time += Time.deltaTime;

            yield return null;
        }
        GameplayBase.Instance.UpdateDefaultSkinColorForPawns();
    }
}
