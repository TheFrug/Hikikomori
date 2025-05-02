using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class cinemachineStateSwitcher : MonoBehaviour
{
    public CinemachineVirtualCamera cam1;
    public CinemachineVirtualCamera cam2;
    public CinemachineVirtualCamera cam3;

    private CinemachineVirtualCamera currentCam;

    private Animator animator;

    public void SwitchToCamera(int camIndex)
    {
        StartCoroutine(SwitchCameraRoutine(camIndex));
    }

    private IEnumerator SwitchCameraRoutine(int camIndex)
    {
        // Play transition out animation
        if (animator != null)
        {
            animator.SetTrigger("FadeOut");
            yield return new WaitForSeconds(0.5f); // Match your fade duration
        }

        // Reset priorities
        cam1.Priority = 0;
        cam2.Priority = 0;
        cam3.Priority = 0;

        switch (camIndex)
        {
            case 1:
                cam1.Priority = 10;
                currentCam = cam1;
                break;
            case 2:
                cam2.Priority = 10;
                currentCam = cam2;
                break;
            case 3:
                cam3.Priority = 10;
                currentCam = cam3;
                break;
        }

        // Play transition in animation
        if (animator != null)
        {
            animator.SetTrigger("FadeIn");
        }
    }
}
