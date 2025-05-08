using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class spatialCameraManager : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("Index of the currently active camera (1, 2, or 3)")]
    public int currentCamIndex = 1;

    [Header("Camera References")]
    [Tooltip("Animator that controls the camera transition animations")]
    [SerializeField]
    private Animator camAnimator;

    [Tooltip("Parent transform controlling the camera rig's movement")]
    [SerializeField]
    private Transform cameraRig;

    [Header("Camera Positions")]
    [Tooltip("Transform reference for Camera Position 1")]
    [SerializeField]
    private Transform cam1Params;

    [Tooltip("Transform reference for Camera Position 2")]
    [SerializeField]
    private Transform cam2Params;

    [Tooltip("Transform reference for Camera Position 3")]
    [SerializeField]
    private Transform cam3Params;

    [Space(10)]
    [Tooltip("If true, camera is currently in motion and inputs are ignored")]
    [SerializeField]
    protected bool isMoving = false;

    // Called to switch the camera to a new predefined position using an animation.
    public void SwitchCamera(Transform activeCam)
    {
        if (isMoving) return; //If camera is already animating, do nothing

        int targetIndex = GetCamIndex(activeCam); // Set temp variable targetIndex, plugging in the Transform argument

        if (currentCamIndex == targetIndex) return; // Avoid redundant switches

        string animName = $"Cam{currentCamIndex}To{targetIndex}"; //plugs current+target cam into animation name convention
        camAnimator.Play(animName); // Play the transition animation
        StartCoroutine(WaitForAnimation(camAnimator, animName)); // Start coroutine to wait for animation to finish before accepting new input
        currentCamIndex = targetIndex; // Update the current camera index
    }

    // Maps a given camera Transform to its corresponding index (1–3).
    private int GetCamIndex(Transform cam)
    {
        if (cam == cam1Params) return 1;
        if (cam == cam2Params) return 2;
        if (cam == cam3Params) return 3;
        return currentCamIndex; // Fallback: return current index if no match
    }

    //Coroutine that waits for the current animation to finish before re-enabling camera input.
    private IEnumerator WaitForAnimation(Animator animator, string animName)
    {
        isMoving = true; // Lock camera control during animation

        // Wait one frame to ensure the Animator starts playing the correct state
        yield return null;

        // Retrieve the current state's info (this will now be the just-played animation)
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float clipLength = stateInfo.length;

        // Wait for the duration of the animation clip
        yield return new WaitForSeconds(clipLength);

        // Re-enable camera switching
        isMoving = false;

    }
}
