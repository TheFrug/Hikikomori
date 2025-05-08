using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class spatialCameraManager : MonoBehaviour
{

    public int currentCamIndex = 1;

    [SerializeField]
    private Animator camAnimator;
    [SerializeField]
    private Transform cameraRig;

    [SerializeField]
    private Transform cam1Params;
    [SerializeField]
    private Transform cam2Params;
    [SerializeField]
    private Transform cam3Params;

    private bool isMoving = false;

    public void SwitchCamera(Transform activeCam)
    {
        if (isMoving) return;

        int targetIndex = GetCamIndex(activeCam);

        if (currentCamIndex == targetIndex) return;

        string animName = $"Cam{currentCamIndex}To{targetIndex}"; //plugs current+target cam into animation name convention
        camAnimator.Play(animName); // Use Play if they're direct animations
        StartCoroutine(WaitForAnimation(camAnimator, animName));
        currentCamIndex = targetIndex;
    }

    private int GetCamIndex(Transform cam)
    {
        if (cam == cam1Params) return 1;
        if (cam == cam2Params) return 2;
        if (cam == cam3Params) return 3;
        return currentCamIndex;
    }

    private IEnumerator WaitForAnimation(Animator animator, string animName)
    {
        isMoving = true;

        // Wait a frame to let the animator update its state
        yield return null;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float clipLength = stateInfo.length;

        yield return new WaitForSeconds(clipLength);
        isMoving = false;

        /*
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length); // Assumes normalized time
        isMoving = false;
        */
    }
}
