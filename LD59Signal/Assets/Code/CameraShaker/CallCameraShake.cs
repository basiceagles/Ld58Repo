using UnityEngine;

public class CallCameraShake : MonoBehaviour
{
    private GameObject mainCamera;
    private CameraShake cameraShake;


    void Start()
    {
        mainCamera = GameObject.Find("Main Camera");
        cameraShake = mainCamera.GetComponent<CameraShake>();
    }
    
    public void CallCameraShaker()
    {
        cameraShake.Shake();
    }

    public void CallHeavyCameraShaker()
    {
        cameraShake.HeavyShake();
    }
}
