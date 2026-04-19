using UnityEngine;

public class AntennaController : MonoBehaviour
{
    public MeshRenderer baseRenderer;
    public Material bulbOnMaterial;
    public AudioSource humSource;
    public GameObject buttonPart;
    public GameObject monitorPart;
    public Camera[] antennaCameras;
    public float rotSpeed = 30f;
    public float rotLimit = 45f;

    private bool isPowered, isViewing, skipE;
    private int camIndex;
    private Camera mainCam;
    private float pan, tilt;
    private Quaternion[] startRot;

    private void Start()
    {
        startRot = new Quaternion[antennaCameras.Length];
        for (int i = 0; i < antennaCameras.Length; i++)
        {
            if (antennaCameras[i] == null) 
            {
                continue;
            }
            startRot[i] = antennaCameras[i].transform.localRotation;
            antennaCameras[i].enabled = false;
            var al = antennaCameras[i].GetComponent<AudioListener>();
            if (al)
            {
                al.enabled = false;
            }
        }
    }

    public void OnInteract(GameObject hit)
    {
        if (Match(hit, buttonPart) && !isPowered) 
        {
            PowerOn();
        }
        else if (Match(hit, monitorPart) && isPowered && !isViewing) 
        {
            EnterView();
        }
    }

    private bool Match(GameObject hit, GameObject part)
    {
        return part != null && (hit == part || hit.transform.IsChildOf(part.transform));
    }

    private void PowerOn()
    {
        isPowered = true;
        if (humSource) 
        {
            humSource.Play();
        }
        if (baseRenderer && baseRenderer.materials.Length > 1)
        {
            var mats = baseRenderer.materials;
            mats[1] = bulbOnMaterial;
            baseRenderer.materials = mats;
        }
    }

    private void EnterView()
    {
        if (antennaCameras.Length == 0) 
        {
            return;
        }
        isViewing = true;
        skipE = true;
        pan = tilt = 0;
        camIndex = 0;

        mainCam = Camera.main;
        if (mainCam) 
        {
            mainCam.enabled = false;
        }

        var pc = FindObjectOfType<PlayerController>();
        var cc = FindObjectOfType<CameraController>();
        if (pc) 
        {
            pc.enabled = false;
        }
        if (cc) 
        {
            cc.enabled = false;
        }

        SetCam(0);
    }

    private void ExitView()
    {
        isViewing = false;
        for (int i = 0; i < antennaCameras.Length; i++)
        {
            if (antennaCameras[i] == null) continue;
            antennaCameras[i].enabled = false;
            antennaCameras[i].transform.localRotation = startRot[i];
        }

        if (mainCam)
        {
            mainCam.enabled = true;
        }

        var pc = FindObjectOfType<PlayerController>();
        var cc = FindObjectOfType<CameraController>();
        if (pc) 
        {
            pc.enabled = true;
        }
        if (cc) 
        {
            cc.enabled = true;
        }
    }

    private void Update()
    {
        if (!isViewing) 
        {
            return;
        }

        if (skipE) 
        {
            if (Input.GetKeyDown(KeyCode.E)) 
            {
                skipE = false;
            }
        }
        else if (Input.GetKeyDown(KeyCode.E)) 
        {
            ExitView(); 
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            camIndex = (camIndex + 1) % antennaCameras.Length;
            pan = tilt = 0;
            SetCam(camIndex);
        }

        pan = Mathf.Clamp(pan + Input.GetAxis("Horizontal") * rotSpeed * Time.deltaTime, -rotLimit, rotLimit);
        tilt = Mathf.Clamp(tilt - Input.GetAxis("Vertical") * rotSpeed * Time.deltaTime, -rotLimit, rotLimit);

        if (antennaCameras[camIndex])
        {
            antennaCameras[camIndex].transform.localRotation = startRot[camIndex] * Quaternion.Euler(tilt, pan, 0);
        }
    }

    private void SetCam(int index)
    {
        for (int i = 0; i < antennaCameras.Length; i++)
        {
            if (antennaCameras[i]) antennaCameras[i].enabled = (i == index);
        }
    }
}
