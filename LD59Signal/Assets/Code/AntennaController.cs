using UnityEngine;

public class AntennaController : MonoBehaviour
{
    public MeshRenderer baseRenderer;
    public Material bulbOnMaterial;
    public AudioSource humSource;
    public GameObject buttonPart;
    public GameObject monitorPart;
    public Camera antennaCamera;
    [SerializeField] private float rotSpeed = 30f;
    [SerializeField] private float rotLimit = 45f;

    public GameObject dishPart;
    public Transform linePoint;
    public Transform lineTarget;
    public LineRenderer lineRenderer;
    public LayerMask signalLayer;

    private bool isPowered;
    private bool isViewing;
    private bool skipE;
    private bool signalConfirmed;
    private Camera mainCam;
    private float pan;
    private float tilt;
    private Quaternion startRot;
    private AntennaController targetAntenna;

    private void Start()
    {
        if (antennaCamera != null)
        {
            startRot = antennaCamera.transform.localRotation;
            antennaCamera.enabled = false;

            AudioListener al = antennaCamera.GetComponent<AudioListener>();
            if (al)
            {
                al.enabled = false;
            }
        }

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    public void OnInteract(GameObject hit)
    {
        if (IsPartOf(hit, buttonPart) && !isPowered)
        {
            isPowered = true;
            if (humSource)
            {
                humSource.Play();
            }
            if (baseRenderer && baseRenderer.materials.Length > 1)
            {
                Material[] mats = baseRenderer.materials;
                mats[1] = bulbOnMaterial;
                baseRenderer.materials = mats;
            }
        }
        else if (IsPartOf(hit, monitorPart) && isPowered && !isViewing)
        {
            EnterView();
        }
    }

    private bool IsPartOf(GameObject hit, GameObject part)
    {
        if (part == null)
        {
            return false;
        }
        return hit == part || hit.transform.IsChildOf(part.transform);
    }

    private void EnterView()
    {
        if (antennaCamera == null)
        {
            return;
        }

        isViewing = true;
        skipE = true;
        pan = 0;
        tilt = 0;

        mainCam = Camera.main;
        if (mainCam)
        {
            mainCam.enabled = false;
        }

        PlayerController pc = FindObjectOfType<PlayerController>();
        CameraController cc = FindObjectOfType<CameraController>();
        if (pc)
        {
            pc.enabled = false;
        }
        if (cc)
        {
            cc.enabled = false;
        }

        antennaCamera.enabled = true;
    }

    private void ExitView()
    {
        isViewing = false;

        if (antennaCamera != null)
        {
            antennaCamera.enabled = false;
            antennaCamera.transform.localRotation = startRot;
        }

        if (mainCam)
        {
            mainCam.enabled = true;
        }

        PlayerController pc = FindObjectOfType<PlayerController>();
        CameraController cc = FindObjectOfType<CameraController>();
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

        if (Input.GetMouseButtonDown(0) && !signalConfirmed)
        {
            TrySignal();
        }

        pan = Mathf.Clamp(pan + Input.GetAxis("Horizontal") * rotSpeed * Time.deltaTime, -rotLimit, rotLimit);
        tilt = Mathf.Clamp(tilt - Input.GetAxis("Vertical") * rotSpeed * Time.deltaTime, -rotLimit, rotLimit);

        if (antennaCamera)
        {
            antennaCamera.transform.localRotation = startRot * Quaternion.Euler(tilt, pan, 0);
        }

        if (lineRenderer && lineRenderer.enabled && targetAntenna != null && targetAntenna.lineTarget != null)
        {
            DrawSignalLine(linePoint.position, targetAntenna.lineTarget.position);
        }
    }

    private void TrySignal()
    {
        if (antennaCamera == null)
        {
            return;
        }

        Ray ray = new Ray(antennaCamera.transform.position, antennaCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 500f, signalLayer))
        {
            AntennaController other = hit.collider.GetComponentInParent<AntennaController>();
            if (other != null && other != this && IsPartOf(hit.collider.gameObject, other.dishPart))
            {
                signalConfirmed = true;
                targetAntenna = other;
                Debug.Log("сигнал установлен");

                lineRenderer.enabled = true;
                lineRenderer.startWidth = 0.03f;
                lineRenderer.endWidth = 0.03f;
                DrawSignalLine(linePoint.position, other.lineTarget.position);
            }
        }
    }

    private void DrawSignalLine(Vector3 from, Vector3 to)
    {
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, from);
        lineRenderer.SetPosition(1, to);
    }
}
