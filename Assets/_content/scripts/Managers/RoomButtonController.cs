using UnityEngine;
using UnityEngine.UI;

public class RoomButtonController : MonoBehaviour
{
    public int roomIndex = 1;
    [SerializeField] private Image buttonImage;

    [SerializeField] public Color normalColor = Color.white;
    [SerializeField] public Color activeColor = Color.green;

    private Color targetColor;
    private bool isLerping = false;
    private float lerpSpeed = 5f;

    private void Start()
    {
        var cam = spatialCameraManagerInstance;

        if (cam != null)
        {
            cam.OnCameraSwitchStarted += HandleCameraSwitchStarted;
            cam.OnCameraChanged += HandleCameraChanged;
        }

        // Force initial state
        HandleCameraChanged(spatialCameraManagerInstance.currentCamIndex);
    }

    private void OnDestroy()
    {
        var cam = spatialCameraManagerInstance;

        if (cam != null)
        {
            cam.OnCameraSwitchStarted -= HandleCameraSwitchStarted;
            cam.OnCameraChanged -= HandleCameraChanged;
        }
    }

    // Fired immediately when a camera switch begins
    private void HandleCameraSwitchStarted(int newIndex)
    {
        targetColor = (newIndex == roomIndex) ? activeColor : normalColor;
        isLerping = true;
    }

    // Fired after the camera animation finishes
    private void HandleCameraChanged(int newIndex)
    {
        // Snap cleanly to the correct final color
        targetColor = (newIndex == roomIndex) ? activeColor : normalColor;
        isLerping = true;
    }

    private void Update()
    {
        if (!isLerping) return;

        buttonImage.color = Color.Lerp(buttonImage.color, targetColor, Time.deltaTime * lerpSpeed);

        // Stop lerping when close
        if (Vector4.Distance(buttonImage.color, targetColor) < 0.01f)
        {
            buttonImage.color = targetColor;
            isLerping = false;
        }
    }

    private spatialCameraManager _cached;
    private spatialCameraManager spatialCameraManagerInstance
    {
        get
        {
            if (_cached == null)
                _cached = FindObjectOfType<spatialCameraManager>();
            return _cached;
        }
    }
}
