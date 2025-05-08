using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraTransitionManager : MonoBehaviour
{
    //Fill with virtual cameras
    [SerializeField]
    private CinemachineVirtualCamera cam1;
    [SerializeField]
    private CinemachineVirtualCamera cam2;
    [SerializeField]
    private CinemachineVirtualCamera cam3;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            SwitchCamera(cam1);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            SwitchCamera(cam2);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            SwitchCamera(cam3);
        }
    }

    public void SwitchCamera(CinemachineVirtualCamera activeCam)
    {
        cam1.Priority = (activeCam == cam1) ? 10 : 0;
        cam2.Priority = (activeCam == cam2) ? 10 : 0;
        cam3.Priority = (activeCam == cam3) ? 10 : 0;
    }

    public void ShowPanel()
    {

    }

}
