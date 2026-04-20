using UnityEngine;

public class AntennaController : MonoBehaviour
{
    public MeshRenderer baseRenderer;
    public Material bulbOnMaterial;
    public Material bulbOffMaterial;
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
    
    public ParticleSystem smokeEffect;
    public GameObject ashPrefab;
    public Color brokenOutlineColor = Color.red;

    private bool isPowered;
    private bool isViewing;
    private bool skipE;
    private bool signalConfirmed;
    private bool isReceivingSignal;
    public bool IsLocked => signalConfirmed || isReceivingSignal;
    public bool IsSignalConfirmed => signalConfirmed;
    private Camera mainCam;
    private float pan;
    private float tilt;
    private Quaternion startRot;
    private AntennaController targetAntenna;
    private ItemData itemData;
    private bool isBroken;
    private bool isDestroyed;
    private bool isSignalEstablished;

    public bool IsBroken => isBroken;
    public bool IsDestroyed => isDestroyed;
    public bool IsPowered => isPowered;

    private void Awake()
    {
        itemData = GetComponentInParent<ItemData>();
        
        foreach (var o in GetComponentsInChildren<Outline>(true))
        {
            o.enabled = false;
            o.OutlineColor = Color.white;
        }
    }

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
        if (isBroken || isDestroyed) return;

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
                isPowered = false;
                if (humSource)
                {
                    humSource.Stop();
                }
                if (baseRenderer && baseRenderer.materials.Length > 1)
                {
                    Material[] mats = baseRenderer.materials;
                    mats[1] = bulbOffMaterial; 
                    baseRenderer.materials = mats;
                }
                if (itemData) 
                {
                    itemData.ToggleActivation(); 
                }
                
                if (lineRenderer) lineRenderer.enabled = false;
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
        if (isSignalEstablished && lineRenderer != null)
        {
            if (targetAntenna == null)
            {
                lineRenderer.enabled = false;
            }
            else
            {
                bool canShowLine = isPowered && !isBroken && !isDestroyed && targetAntenna.IsPowered && !targetAntenna.IsBroken && !targetAntenna.IsDestroyed;
                
                lineRenderer.enabled = canShowLine;
                if (canShowLine)
                {
                    DrawSignalLine(linePoint.position, targetAntenna.lineTarget.position);
                }
                else
                {
                    lineRenderer.positionCount = 0; 
                }
            }
        }
        else if (lineRenderer != null && !isSignalEstablished)
        {
            lineRenderer.enabled = false;
            lineRenderer.positionCount = 0;
        }

        if (!isViewing || isBroken || isDestroyed)
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
                isSignalEstablished = true; 
                targetAntenna = other;
                other.SetReceivingSignal(true);
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

    public void RotateDishManual(float input)
    {
        if (IsLocked)
        {
            return;
        }

        if (dishPart)
        {
            dishPart.transform.Rotate(Vector3.up, input * rotSpeed * Time.deltaTime, Space.World);
        }
    }

    public void SetReceivingSignal(bool state)
    {
        isReceivingSignal = state;
    }

    public void Break()
    {
        isBroken = true;
        if (smokeEffect)
        {
            smokeEffect.Play();
        }
        
        foreach (var lr in GetComponentsInChildren<LineRenderer>(true))
        {
            lr.positionCount = 0;
            lr.enabled = false;
        }
        
        foreach (var o in GetComponentsInChildren<Outline>(true))
        {
            o.enabled = true;
            o.OutlineColor = brokenOutlineColor;
        }
    }

    public void Repair()
    {
        isBroken = false;
        if (smokeEffect) smokeEffect.Stop();
        
        foreach (var o in GetComponentsInChildren<Outline>(true))
        {
            o.enabled = false;
            o.OutlineColor = Color.white;
        }
    }

    public void DestroyToAsh()
    {
        if (isDestroyed) return;
        isDestroyed = true;
        
        if (targetAntenna != null) targetAntenna.SetReceivingSignal(false);

        if (ashPrefab) Instantiate(ashPrefab, transform.position, transform.rotation);
        Destroy(gameObject);
    }

    [ContextMenu("Debug/Break Antenna")]
    public void DebugBreak() => Break();
}
