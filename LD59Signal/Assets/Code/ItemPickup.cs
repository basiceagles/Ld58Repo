using UnityEngine;
using TMPro;
using System.Collections;

public class ItemPickup : MonoBehaviour
{
    public float pickupRange = 3f;
    public LayerMask interactableLayer;
    public LayerMask placementLayer;
    public LayerMask obstacleLayer; 
    public Transform cam;
    public TextMeshProUGUI popupText;
    public float anglePlace;

    private Inventory inventory;
    private Outline lastOutline;
    private GameObject currentGhost;
    private GameObject currentInvalidGhost;
    private bool isPlacing;
    private Coroutine placeRoutine;
    private Vector2 popupOriginalPos;

    private void Awake()
    {
        inventory = GetComponent<Inventory>();
    }

    private void Start()
    {
        StartCoroutine(PrewarmOutlines());
        if (popupText != null)
        {
            popupOriginalPos = popupText.rectTransform.anchoredPosition;
            popupText.gameObject.SetActive(false);
        }
    }

    private IEnumerator PrewarmOutlines()
    {
        Outline[] allOutlines = FindObjectsOfType<Outline>(true);
        foreach(var o in allOutlines) o.enabled = true;
        yield return null; 
        foreach(var o in allOutlines) o.enabled = false;
    }

    private void Update()
    {
        HandleHighlightAndPickup();

        if (inventory.GetCurrentItem() != null)
        {
            if (Input.GetKeyDown(KeyCode.Q)) DropObject();
            HandlePlacement();
        }
        else
        {
            ClearGhost();
        }
    }

    private void HandleHighlightAndPickup()
    {
        if (cam == null || isPlacing)
        {
            return;
        }

        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, pickupRange, interactableLayer))
        {
            Outline outline = null;
            ItemData data = hit.collider.GetComponent<ItemData>();
            if (data == null) 
            {
                data = hit.collider.GetComponentInParent<ItemData>();
            }

            if (data != null)
            {
                outline = data.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = data.GetComponentInChildren<Outline>();
                }
            }
            else outline = hit.collider.GetComponent<Outline>();

            if (outline != null)
            {
                if (lastOutline != outline)
                {
                    if (lastOutline != null) lastOutline.enabled = false;
                    outline.enabled = true;
                    lastOutline = outline;
                }
            }
            else ClearOutline();

            if (Input.GetKeyDown(KeyCode.E))
            {
                int emptySlot = inventory.GetEmptySlot();
                if (emptySlot != -1)
                {
                    PickupObject(hit.collider.gameObject, emptySlot, data);
                    ClearOutline();
                }
            }
        }
        else ClearOutline();
    }

    private void ClearOutline()
    {
        if (lastOutline != null)
        {
            lastOutline.enabled = false;
            lastOutline = null;
        }
    }

    private void PickupObject(GameObject obj, int slotIndex, ItemData data)
    {
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        inventory.AddItem(obj, slotIndex);

        if (data != null && !string.IsNullOrEmpty(data.itemName))
        {
            StartCoroutine(ShowPopupTextRoutine(data.itemName));
        }
    }

    private void DropObject()
    {
        if (isPlacing)
        {
            return;
        }

        GameObject obj = inventory.RemoveCurrentItem();
        if (obj == null)
        {
            return;
        }

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        ClearGhost();
    }

    private void HandlePlacement()
    {
        GameObject currentItem = inventory.GetCurrentItem();
        if (currentItem == null)
        {
            ClearGhost();
            return;
        }

        ItemData data = currentItem.GetComponent<ItemData>();
        if (data == null || !data.isPlaceable) 
        {
            ClearGhost();
            return;
        }

        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, 5f, placementLayer))
        {
            if (currentGhost == null && data.ghostPrefab != null)
            {
                currentGhost = Instantiate(data.ghostPrefab);
                
                Renderer[] renderers = currentGhost.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds b = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                    {
                        b.Encapsulate(renderers[i].bounds);
                    }
                    data.placementYOffset = currentGhost.transform.position.y - b.min.y;
                }
            }
            
            if (currentInvalidGhost == null && data.invalidGhostPrefab != null)
            {
                currentInvalidGhost = Instantiate(data.invalidGhostPrefab);
            }

            Vector3 placementPos = hit.point + Vector3.up * data.placementYOffset;
            
            if (currentGhost != null)
            {
                currentGhost.transform.position = placementPos;
                Vector3 lookDir = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
                if (lookDir != Vector3.zero)
                {
                    currentGhost.transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
                }
            }

            bool canPlace = Vector3.Angle(hit.normal, Vector3.up) < anglePlace;

            if (canPlace && currentGhost != null)
            {
                Renderer r = currentGhost.GetComponentInChildren<Renderer>();
                Vector3 extents = r != null ? r.bounds.extents * 0.95f : Vector3.one * 0.5f;

                Vector3 checkCenter = placementPos + Vector3.up * (extents.y + 0.05f);

                if (Physics.CheckBox(checkCenter, extents, currentGhost.transform.rotation, obstacleLayer))
                {
                    canPlace = false; 
                }
            }

            GameObject activeGhost = canPlace ? currentGhost : currentInvalidGhost;
            GameObject inactiveGhost = canPlace ? currentInvalidGhost : currentGhost;

            if (inactiveGhost != null)
            {
                inactiveGhost.SetActive(false);
            }

            if (activeGhost != null)
            {
                activeGhost.SetActive(true);
                activeGhost.transform.position = placementPos;
                Vector3 lookDir = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
                if (lookDir != Vector3.zero)
                    activeGhost.transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
            }

            if (canPlace && Input.GetMouseButtonDown(0) && !isPlacing)
            {
                placeRoutine = StartCoroutine(PlaceHoldRoutine(placementPos, activeGhost != null ? activeGhost.transform.rotation : Quaternion.identity, data));
            }
        }
        else ClearGhost();

        if (Input.GetMouseButtonUp(0) && isPlacing)
        {
            StopCoroutine(placeRoutine);
            isPlacing = false;
            if (data != null && data.placementAudioSource != null)
            {
                data.placementAudioSource.Stop();
            }
        }
    }

    private IEnumerator PlaceHoldRoutine(Vector3 pos, Quaternion rot, ItemData data)
    {
        isPlacing = true;
        
        if (data.placementAudioSource != null) 
        {
            data.placementAudioSource.Play();
        }

        float holdTimer = 0f;
        while (holdTimer < 2f)
        {
            holdTimer += Time.deltaTime;
            yield return null;
        }

        GameObject obj = inventory.RemoveCurrentItem();
        if (obj != null)
        {
            obj.transform.position = pos;
            obj.transform.rotation = rot;
            
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
        }

        isPlacing = false;
        ClearGhost();
    }

    private void ClearGhost()
    {
        if (currentGhost != null)
        {
            Destroy(currentGhost);
            currentGhost = null;
        }
        if (currentInvalidGhost != null)
        {
            Destroy(currentInvalidGhost);
            currentInvalidGhost = null;
        }
    }
    
    private IEnumerator ShowPopupTextRoutine(string text)
    {
        if (popupText == null)
        {
            yield break;
        }
        
        popupText.text = text;
        popupText.gameObject.SetActive(true);

        RectTransform rt = popupText.rectTransform;
        Vector2 startPos = popupOriginalPos;
        Vector2 endPos = popupOriginalPos + new Vector2(0, 100f);
        
        float timer = 0f;
        float duration = 2.5f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t * t); 
            
            Color c = popupText.color;
            c.a = t > 0.5f ? Mathf.Lerp(1f, 0f, (t - 0.5f) * 2f) : 1f;
            popupText.color = c;

            yield return null;
        }
        
        popupText.gameObject.SetActive(false);
    }
}
