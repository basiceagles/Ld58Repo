using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    public float pickupRange = 3f;
    public LayerMask interactableLayer; 

    [Header("References")]
    public Transform holdPoint; 
    public Transform cam; 

    private GameObject holdObject;
    private Outline lastOutline;

    private void Start()
    {
        Outline[] allOutlines = FindObjectsOfType<Outline>();
        for (int i = 0; i < allOutlines.Length; i++)
        {
            allOutlines[i].enabled = false;
        }
    }

    private void Update()
    {
        if (holdObject == null)
        {
            HandleHighlightAndPickup();
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                DropObject();
            }
        }
    }

    private void HandleHighlightAndPickup()
    {
        if (cam == null) return;

        Ray ray = new Ray(cam.position, cam.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, interactableLayer))
        {
            Outline outline = hit.collider.GetComponent<Outline>();
            if (outline == null)
            {
                outline = hit.collider.GetComponentInParent<Outline>();
            }

            if (outline != null)
            {
                if (lastOutline != null && lastOutline != outline)
                {
                    lastOutline.enabled = false;
                }
                outline.enabled = true;
                lastOutline = outline;
            }
            else
            {
                ClearOutline();
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                PickupObject(hit.collider.gameObject);
                ClearOutline(); 
            }
        }
        else
        {
            ClearOutline();
        }
    }

    private void ClearOutline()
    {
        if (lastOutline != null)
        {
            lastOutline.enabled = false;
            lastOutline = null;
        }
    }

    private void PickupObject(GameObject obj)
    {
        holdObject = obj;
        
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        obj.transform.SetParent(holdPoint);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
    }

    private void DropObject()
    {
        if (holdObject == null) return;

        Rigidbody rb = holdObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        holdObject.transform.SetParent(null, true);
        holdObject = null;
    }
}
