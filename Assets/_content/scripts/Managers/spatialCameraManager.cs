// spatialCameraManager.cs
using System.Collections;
using UnityEngine;

public class spatialCameraManager : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("Index of the currently active camera (1, 2, or 3)")]
    public int currentCamIndex = 1;

    [Header("Camera References")]
    [SerializeField] private Animator camAnimator;
    [SerializeField] private Transform cameraRig;

    [Header("Camera Positions")]
    [SerializeField] private Transform cam1Params;
    [SerializeField] private Transform cam2Params;
    [SerializeField] private Transform cam3Params;

    [Space(10)]
    [Tooltip("If true, camera is currently in motion and inputs are ignored")]
    [SerializeField]
    protected bool isMoving = false;

    public System.Action<int> OnCameraChanged;

    // Called to switch the camera to a new predefined position using an animation.
    public void SwitchCamera(Transform activeCam)
    {
        // NEW: consult UIStateController — prevent camera switching when UI disallows it
        if (UIStateController.Instance != null && !UIStateController.Instance.CanClickRoomButtons)
            return;

        if (isMoving) return; // If camera is already animating, do nothing

        int targetIndex = GetCamIndex(activeCam);

        if (currentCamIndex == targetIndex) return; // Avoid redundant switches

        FindObjectOfType<BehaviorIconRoomController>()?.FadeAllIconsOut();

        string animName = $"Cam{currentCamIndex}To{targetIndex}";
        camAnimator.Play(animName);
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

        yield return null;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float clipLength = stateInfo.length;

        yield return new WaitForSeconds(clipLength);

        isMoving = false;
        OnCameraChanged?.Invoke(currentCamIndex);
    }
}
