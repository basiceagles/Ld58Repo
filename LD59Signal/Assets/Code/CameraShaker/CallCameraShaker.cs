using UnityEngine;

public class CallCameraShaker : MonoBehaviour
{
    private GameObject cameraTarget;
    private CameraShake cameraShake;
    public bool onStart;
    public bool onEnd;
    void Start()
    {
        cameraTarget = GameObject.Find("Main Camera");
        cameraShake = cameraTarget.GetComponent<CameraShake>();
    }

    public void CallCameraShakerMethod()
    {
        if (onStart)
        {
            cameraShake.ConstantlyShakeStart();
        }
        
        if (onEnd)
        {
            cameraShake.ConstantlyShakeStop();
        }
    }
}
