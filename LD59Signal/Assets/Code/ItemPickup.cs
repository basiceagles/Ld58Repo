using UnityEngine;
using TMPro;
using System.Collections;

public class ItemPickup : MonoBehaviour
{
    public float pickupRange = 3f;
<<<<<<< Updated upstream
    public LayerMask interactableLayer;
    public LayerMask placementLayer;
    public LayerMask obstacleLayer; 
=======
    public float anglePlace = 15f;
    public LayerMask interactableLayer, placementLayer, obstacleLayer;
>>>>>>> Stashed changes
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

    private void Awake() => inventory = GetComponent<Inventory>();

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
        foreach (var o in allOutlines) o.enabled = true;
        yield return null; 
        foreach (var o in allOutlines) o.enabled = false;
    }

    private void Update()
    {
        HandleHighlightAndPickup();
        if (inventory.GetCurrentItem() != null)
        {
            if (Input.GetKeyDown(KeyCode.Q)) 
            {
                DropObject();
            }
            HandlePlacement();
        }
        else ClearGhost();
    }

    private void HandleHighlightAndPickup()
    {
<<<<<<< Updated upstream
        if (cam == null || isPlacing)
=======
        if (cam == null || isPlacing) 
>>>>>>> Stashed changes
        {
            return;
        }

        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, pickupRange, interactableLayer))
        {
<<<<<<< Updated upstream
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
=======
            ItemSpawner spawner = hit.collider.GetComponentInParent<ItemSpawner>();
            if (spawner != null)
            {
                UpdateOutline(spawner.gameObject);
                if (Input.GetKeyDown(KeyCode.E) && inventory.GetEmptySlot() != -1 && spawner.itemPrefab != null)
>>>>>>> Stashed changes
                {
                    PickupObject(Instantiate(spawner.itemPrefab), inventory.GetEmptySlot(), null);
                    ClearOutline();
                }
                return;
            }

            ItemData data = hit.collider.GetComponentInParent<ItemData>();
            if (data != null)
            {
                UpdateOutline(data.gameObject);
                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (data.isPlaced)
                    {
                        data.ToggleActivation();
                    }
                    else if (inventory.GetEmptySlot() != -1)
                    {
                        PickupObject(data.gameObject, inventory.GetEmptySlot(), data);
                        ClearOutline();
                    }
                }
                return;
            }
        }
        ClearOutline();
    }

    private void UpdateOutline(GameObject obj)
    {
        Outline outline = obj.GetComponent<Outline>() ?? obj.GetComponentInChildren<Outline>();
        if (outline != null && lastOutline != outline)
        {
            if (lastOutline != null) lastOutline.enabled = false;
            outline.enabled = true;
            lastOutline = outline;
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

    private void PickupObject(GameObject obj, int slotIndex, ItemData data)
    {
<<<<<<< Updated upstream
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
=======
        if (obj.GetComponent<Rigidbody>()) 
        {
            obj.GetComponent<Rigidbody>().isKinematic = true;
>>>>>>> Stashed changes
        }

        inventory.AddItem(obj, slotIndex);

<<<<<<< Updated upstream
        if (data != null && !string.IsNullOrEmpty(data.itemName))
        {
            StartCoroutine(ShowPopupTextRoutine(data.itemName));
=======
        ItemData d = data ?? obj.GetComponent<ItemData>();
        if (d != null)
        {
            d.isPlaced = false;
            d.StopAnimation();
            
            if (!string.IsNullOrEmpty(d.itemName)) 
            {
                StartCoroutine(ShowPopupTextRoutine(d.itemName));
            }
>>>>>>> Stashed changes
        }
    }

    private void DropObject()
    {
<<<<<<< Updated upstream
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
=======
        if (isPlacing) 
        {
            return;
        }
        GameObject obj = inventory.RemoveCurrentItem();
        if (obj != null) 
        { 
            if (obj.GetComponent<Rigidbody>()) 
            {
                obj.GetComponent<Rigidbody>().isKinematic = false; 
            }
            ClearGhost(); 
        }
>>>>>>> Stashed changes
    }

    private void HandlePlacement()
    {
        GameObject currentItem = inventory.GetCurrentItem();
<<<<<<< Updated upstream
        if (currentItem == null)
        {
            ClearGhost();
=======
        if (currentItem == null) 
        {
>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
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
            
=======
            bool canPlace = Vector3.Angle(hit.normal, Vector3.up) < anglePlace;

            if (currentGhost == null && data.ghostPrefab != null)
            {
                currentGhost = Instantiate(data.ghostPrefab);
                Renderer[] rs = currentGhost.GetComponentsInChildren<Renderer>();
                if (rs.Length > 0)
                {
                    Bounds b = rs[0].bounds;
                    for (int i = 1; i < rs.Length; i++) { b.Encapsulate(rs[i].bounds); }
                    data.placementYOffset = currentGhost.transform.position.y - b.min.y;
                }
            }

>>>>>>> Stashed changes
            if (currentInvalidGhost == null && data.invalidGhostPrefab != null)
            {
                currentInvalidGhost = Instantiate(data.invalidGhostPrefab);
            }

<<<<<<< Updated upstream
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
=======
            Vector3 pos = hit.point + Vector3.up * data.placementYOffset;
            Quaternion rot = Quaternion.identity;
            
            Vector3 look = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            if (look != Vector3.zero) 
            {
                rot = Quaternion.LookRotation(look, Vector3.up);
            }

            // Obstacle check
            if (canPlace)
            {
                GameObject checkObj = currentGhost ?? currentInvalidGhost;
                if (checkObj != null)
                {
                    Renderer r = checkObj.GetComponentInChildren<Renderer>();
                    Vector3 extents = r != null ? r.bounds.extents * 0.95f : Vector3.one * 0.5f;
                    if (Physics.CheckBox(pos + Vector3.up * (extents.y + 0.05f), extents, rot, obstacleLayer))
                    {
                        canPlace = false;
                    }
                }
            }

            // Toggle ghosts
            GameObject activeGhost = canPlace ? currentGhost : currentInvalidGhost;
            GameObject inactiveGhost = canPlace ? currentInvalidGhost : currentGhost;

            if (inactiveGhost != null) 
>>>>>>> Stashed changes
            {
                inactiveGhost.SetActive(false);
            }

            if (activeGhost != null)
            {
                activeGhost.SetActive(true);
<<<<<<< Updated upstream
                activeGhost.transform.position = placementPos;
                Vector3 lookDir = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
                if (lookDir != Vector3.zero)
                    activeGhost.transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
=======
                activeGhost.transform.position = pos;
                activeGhost.transform.rotation = rot;
>>>>>>> Stashed changes
            }

            if (canPlace && Input.GetMouseButtonDown(0) && !isPlacing)
            {
<<<<<<< Updated upstream
                placeRoutine = StartCoroutine(PlaceHoldRoutine(placementPos, activeGhost != null ? activeGhost.transform.rotation : Quaternion.identity, data));
            }
=======
                placeRoutine = StartCoroutine(PlaceHoldRoutine(pos, rot, data));
            }
        }
        else 
        {
            ClearGhost();
>>>>>>> Stashed changes
        }

        if (Input.GetMouseButtonUp(0) && isPlacing)
        {
            StopCoroutine(placeRoutine);
            isPlacing = false;
<<<<<<< Updated upstream
            if (data != null && data.placementAudioSource != null)
=======
            if (data.placementAudioSource != null) 
>>>>>>> Stashed changes
            {
                data.placementAudioSource.Stop();
            }
        }
    }

    private IEnumerator PlaceHoldRoutine(Vector3 pos, Quaternion rot, ItemData data)
    {
        isPlacing = true;
<<<<<<< Updated upstream
        
        if (data.placementAudioSource != null) 
        {
            data.placementAudioSource.Play();
        }

        float holdTimer = 0f;
        while (holdTimer < 2f)
=======
        if (data.placementAudioSource) 
>>>>>>> Stashed changes
        {
            data.placementAudioSource.Play();
        }

        float timer = 0f;
        while (timer < 2f) 
        {
            timer += Time.deltaTime; 
            yield return null; 
        }

        GameObject obj = inventory.RemoveCurrentItem();
        if (obj != null)
        {
            obj.transform.position = pos; 
            obj.transform.rotation = rot;
            if (obj.GetComponent<Rigidbody>()) 
            {
                obj.GetComponent<Rigidbody>().isKinematic = true;
            }
            ItemData id = obj.GetComponent<ItemData>();
            if (id != null) 
            {
                id.isPlaced = true;
            }
        }
        isPlacing = false; ClearGhost();
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
        if (currentInvalidGhost != null)
        {
            Destroy(currentInvalidGhost);
            currentInvalidGhost = null;
        }
    }
    
    private IEnumerator ShowPopupTextRoutine(string text)
    {
<<<<<<< Updated upstream
        if (popupText == null)
        {
            yield break;
        }
        
        popupText.text = text;
=======
        if (popupText == null) 
        {
            yield break;
        }
        popupText.text = text; 
>>>>>>> Stashed changes
        popupText.gameObject.SetActive(true);
        RectTransform rt = popupText.rectTransform;
        float timer = 0f, duration = 2.5f;

        while (timer < duration)
        {
            timer += Time.deltaTime; 
            float t = timer / duration;
            rt.anchoredPosition = Vector2.Lerp(popupOriginalPos, popupOriginalPos + new Vector2(0, 100f), t * t); 
            Color c = popupText.color; 
            c.a = t > 0.5f ? Mathf.Lerp(1f, 0f, (t - 0.5f) * 2f) : 1f;
            popupText.color = c;
            yield return null;
        }
        popupText.gameObject.SetActive(false);
    }
}
