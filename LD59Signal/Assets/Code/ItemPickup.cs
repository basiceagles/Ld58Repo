using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ItemPickup : MonoBehaviour
{
    [SerializeField] private float pickupRange = 5f;
    [SerializeField] private float anglePlace = 15f;
    [SerializeField] private float placementDuration = 2f; 
    [SerializeField] private LayerMask interactableLayer, placementLayer, obstacleLayer;
    [SerializeField] private Transform cam;
    [SerializeField] private TextMeshProUGUI popupText;
    public Image progressImage;

    private Inventory inventory;
    private Outline lastOutline;
    private GameObject currentGhost;
    private GameObject currentInvalidGhost;
    private bool isPlacing;
    private Coroutine placeRoutine;
    private Vector2 popupOriginalPos;
    private float ghostRotationY;

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
        foreach (var o in allOutlines) 
        {
            o.enabled = true;
        }
        yield return null; 
        foreach (var o in allOutlines) 
        {
            o.enabled = false;
        }
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
        if (cam == null || isPlacing) 
        {
            return;
        }

        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, pickupRange, interactableLayer))
        {
            ItemSpawner spawner = hit.collider.GetComponentInParent<ItemSpawner>();
            if (spawner != null)
            {
                UpdateOutline(spawner.gameObject);
                if (Input.GetKeyDown(KeyCode.E) && inventory.GetEmptySlot() != -1 && spawner.itemPrefab != null)
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
                        AntennaController antenna = data.GetComponentInChildren<AntennaController>();
                        if (!data.isActivated)
                        {
                            data.ToggleActivation();
                            data.placementAudioSource.Play();
                        }
                        else if (antenna != null)
                        {
                            antenna.OnInteract(hit.collider.gameObject);
                        }
                        else
                        {
                            data.ToggleActivation();
                        }
                    }
                    else if (inventory.GetEmptySlot() != -1)
                    {
                        PickupObject(data.gameObject, inventory.GetEmptySlot(), data);
                        ClearOutline();
                    }
                }
                return;
            }
            ClearOutline();
        }
        else ClearOutline();
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
        if (obj.GetComponent<Rigidbody>()) 
        {
            obj.GetComponent<Rigidbody>().isKinematic = true;
        }

        inventory.AddItem(obj, slotIndex);

        ItemData d = data ?? obj.GetComponent<ItemData>();
        if (d != null)
        {
            d.isPlaced = false;
            d.StopAnimation();
            
            if (!string.IsNullOrEmpty(d.itemName)) 
            {
                StartCoroutine(ShowPopupTextRoutine(d.itemName));
            }
        }
    }

    private void DropObject()
    {
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
    }

    private void HandlePlacement()
    {
        GameObject currentItem = inventory.GetCurrentItem();
        if (currentItem == null) 
        {
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

            if (currentInvalidGhost == null && data.invalidGhostPrefab != null)
            {
                currentInvalidGhost = Instantiate(data.invalidGhostPrefab);
            }

            Vector3 pos = hit.point + Vector3.up * data.placementYOffset;

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                ghostRotationY += scroll > 0 ? 15f : -15f;
            }

            Quaternion rot = Quaternion.identity;
            Vector3 look = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            if (look != Vector3.zero) 
            {
                rot = Quaternion.LookRotation(look, Vector3.up);
            }
            rot *= Quaternion.Euler(0, ghostRotationY, 0);

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

            GameObject activeGhost = canPlace ? currentGhost : currentInvalidGhost;
            GameObject inactiveGhost = canPlace ? currentInvalidGhost : currentGhost;

            if (inactiveGhost != null) 
            {
                inactiveGhost.SetActive(false);
            }

            if (activeGhost != null)
            {
                activeGhost.SetActive(true);
                activeGhost.transform.position = pos;
                activeGhost.transform.rotation = rot;
            }

            if (canPlace && Input.GetMouseButtonDown(0) && !isPlacing)
            {
                placeRoutine = StartCoroutine(PlaceHoldRoutine(pos, rot, data));
            }
        }
        else 
        {
            ClearGhost();
        }

        if (Input.GetMouseButtonUp(0) && isPlacing)
        {
            StopCoroutine(placeRoutine);
            isPlacing = false;
            if (progressImage != null) 
            {
                progressImage.fillAmount = 0;
            }
            //if (data.placementAudioSource != null) 
            //{
            //    data.placementAudioSource.Stop();
            //}
        }
    }

    private IEnumerator PlaceHoldRoutine(Vector3 pos, Quaternion rot, ItemData data)
    {
        isPlacing = true;
        //if (data.placementAudioSource) 
        //{
        //    data.placementAudioSource.Play();
        //}

        float timer = 0f;
        while (timer < placementDuration) 
        {
            timer += Time.deltaTime; 
            if (progressImage != null)
            {
                progressImage.fillAmount = timer / placementDuration;
            }
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

        if (progressImage != null)
        {
            progressImage.fillAmount = 0;
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

        ghostRotationY = 0;
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
