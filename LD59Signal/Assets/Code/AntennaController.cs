using UnityEngine;

public class AntennaController : MonoBehaviour
{
    public MeshRenderer baseRenderer;
    public Material bulbOnMaterial;
    public AudioSource humSource;
    public GameObject buttonPart;
    public GameObject monitorPart;
    public Camera antennaCamera;
    
    [Header("Antenna Parts Rotation")]
    public Transform yawPart;
    public Transform pitchPart;
    
    [SerializeField] private float yawSpeed = 30f;
    [SerializeField] private float yawLimit = 45f;
    
    [SerializeField] private float pitchSpeed = 30f;
    [SerializeField] private float pitchLimit = 45f;

    public GameObject dishPart;
    public Transform linePoint;
    public Transform lineTarget;
    public LineRenderer lineRenderer;
    public LayerMask signalLayer;
    
    [Header("Laser Beam")]
    public Material laserMaterial;
    public Color laserColor = Color.green;
    public float laserWidth = 0.05f;
    public Light laserPointLight;

    private bool isPowered;
    private bool isViewing;
    private bool skipE;
    private bool signalConfirmed;
    public bool IsSignalConfirmed => signalConfirmed;
    private Camera mainCam;
    private float pan;
    private float tilt;
    private Quaternion startRotYaw;
    private Quaternion startRotPitch;
    private AntennaController targetAntenna;
    private ItemData itemData;

    private void Start()
    {
        if (antennaCamera != null)
        {
            antennaCamera.enabled = false;

            AudioListener al = antennaCamera.GetComponent<AudioListener>();
            if (al)
            {
                al.enabled = false;
            }
        }
        
        if (yawPart != null)
        {
            startRotYaw = yawPart.localRotation;
        }
        
        if (pitchPart != null)
        {
            startRotPitch = pitchPart.localRotation;
        }

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
        
        if (laserPointLight != null)
        {
            laserPointLight.enabled = false;
        }

        itemData = GetComponentInParent<ItemData>();
    }

    public void OnInteract(GameObject hit)
    {
        if (IsPartOf(hit, buttonPart))
        {
            if (!isPowered)
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
            else
            {
                // Unpower and fold
                isPowered = false;
                if (humSource) humSource.Stop();
                if (baseRenderer && baseRenderer.materials.Length > 1)
                {
                    Material[] mats = baseRenderer.materials;
                    mats[1] = null; 
                    baseRenderer.materials = mats;
                }
                if (itemData) itemData.ToggleActivation(); 
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
        
        SyncPanTiltFromCurrentRotation();

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
        SaveCurrentRotation();
        isViewing = false;

        if (antennaCamera != null)
        {
            antennaCamera.enabled = false;
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

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        pan = Mathf.Clamp(pan + horizontal * yawSpeed * Time.deltaTime, -yawLimit, yawLimit);
        tilt = Mathf.Clamp(tilt + vertical * pitchSpeed * Time.deltaTime, -pitchLimit, pitchLimit);

        if (yawPart != null)
        {
            yawPart.localRotation = startRotYaw * Quaternion.Euler(0, 0, pan);
        }
        
        if (pitchPart != null)
        {
            pitchPart.localRotation = startRotPitch * Quaternion.Euler(tilt, 0, 0);
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
            Debug.LogWarning("TrySignal: antennaCamera is null");
            return;
        }

        Ray ray = new Ray(antennaCamera.transform.position, antennaCamera.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 2f);
        
        if (Physics.Raycast(ray, out RaycastHit hit, 500f, signalLayer))
        {
            Debug.Log($"TrySignal hit: {hit.collider.name} on layer {hit.collider.gameObject.layer}");
            AntennaController other = hit.collider.GetComponentInParent<AntennaController>();
            if (other != null && other != this && IsPartOf(hit.collider.gameObject, other.dishPart))
            {
                signalConfirmed = true;
                targetAntenna = other;
                Debug.Log("сигнал установлен");

                SetupLaserBeam();
                DrawSignalLine(linePoint.position, other.lineTarget.position);
            }
            else
            {
                Debug.Log($"TrySignal rejected: other={(other?.name ?? "null")}, isSelf={other == this}, isDishPart={IsPartOf(hit.collider.gameObject, other?.dishPart)}");
            }
        }
        else
        {
            Debug.Log("TrySignal: no hit on signalLayer");
        }
    }

    private void DrawSignalLine(Vector3 from, Vector3 to)
    {
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, from);
        lineRenderer.SetPosition(1, to);
        
        if (laserPointLight != null)
        {
            laserPointLight.transform.position = to;
        }
    }

    private void SetupLaserBeam()
    {
        if (lineRenderer == null) return;
        
        lineRenderer.enabled = true;
        lineRenderer.startWidth = laserWidth;
        lineRenderer.endWidth = laserWidth;
        
        if (laserMaterial != null)
        {
            lineRenderer.material = laserMaterial;
        }
        
        lineRenderer.startColor = laserColor;
        lineRenderer.endColor = laserColor;
        
        if (laserPointLight != null)
        {
            laserPointLight.enabled = true;
            laserPointLight.color = laserColor;
        }
    }

    public void RotateDishManual(float input)
    {
        if (signalConfirmed) return;

        pan = Mathf.Clamp(pan + input * yawSpeed * Time.deltaTime, -yawLimit, yawLimit);
        
        if (yawPart != null)
        {
            yawPart.localRotation = startRotYaw * Quaternion.Euler(0, 0, pan);
            Debug.Log($"RotateDishManual: yawPart={yawPart.name}, pan={pan:F2}");
        }
        else
        {
            Debug.LogWarning("RotateDishManual: yawPart is null!");
        }
    }

    private void SyncPanTiltFromCurrentRotation()
    {
        if (yawPart != null)
        {
            Quaternion yawOffset = Quaternion.Inverse(startRotYaw) * yawPart.localRotation;
            pan = yawOffset.eulerAngles.z;
            if (pan > 180f) pan -= 360f;
        }
        
        if (pitchPart != null)
        {
            Quaternion pitchOffset = Quaternion.Inverse(startRotPitch) * pitchPart.localRotation;
            tilt = pitchOffset.eulerAngles.x;
            if (tilt > 180f) tilt -= 360f;
        }
    }

    private void SaveCurrentRotation()
    {
        // pan and tilt are already stored, just clamp them
        pan = Mathf.Clamp(pan, -yawLimit, yawLimit);
        tilt = Mathf.Clamp(tilt, -pitchLimit, pitchLimit);
    }
}
