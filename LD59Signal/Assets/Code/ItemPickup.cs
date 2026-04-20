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
    [SerializeField] public LayerMask wrenchLayer;
    [SerializeField] private Transform cam;
    [SerializeField] private TextMeshProUGUI popupText;
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private TextMeshProUGUI promptText;
    public Image progressImage;

    private Inventory inventory;
    private Outline lastOutline;
    private GameObject currentGhost;
    private GameObject currentInvalidGhost;
    private bool isPlacing;
    private Coroutine placeRoutine;
    private Vector2 popupOriginalPos;
    private float ghostRotationY;
    private bool isRepairing;
    private Coroutine repairRoutine;

    private void Awake()
    {
        inventory = GetComponent<Inventory>();
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        if (promptText != null) promptText.gameObject.SetActive(false);
        if (popupText != null) popupText.gameObject.SetActive(false);

        foreach (var o in FindObjectsOfType<Outline>(true))
        {
            o.enabled = false;
        }
    }

    private void Start()
    {
        if (popupText != null)
        {
            popupOriginalPos = popupText.rectTransform.anchoredPosition;
            popupText.gameObject.SetActive(false);
        }
        if (interactionPrompt != null) interactionPrompt.SetActive(false);

        foreach (var o in FindObjectsOfType<Outline>(true)) o.enabled = false;
    }



    private void Update()
    {
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        if (promptText != null) promptText.gameObject.SetActive(false);

        HandleHighlightAndPickup();
        if (inventory.GetCurrentItem() != null)
        {
            if (Input.GetKeyDown(KeyCode.Q) && !IsHoveringDish() && !isRepairing) 
            {
                DropObject();
            }
            HandlePlacement();
        }
        else ClearGhost();
        
        HandleRepair();
        HandleDishRotation();
    }

    private void HandleRepair()
    {
        if (isPlacing) return;

        // Проверяем оба слоя: и интерактивный, и ключ
        LayerMask combinedMask = interactableLayer | wrenchLayer;
        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, pickupRange, combinedMask))
        {
            AntennaController antenna = hit.collider.GetComponentInParent<AntennaController>();
            GameObject currentItem = inventory.GetCurrentItem();

            if (antenna != null && antenna.IsBroken && currentItem != null && ((1 << currentItem.layer) & wrenchLayer) != 0)
            {
                if (interactionPrompt != null)
                {
                    interactionPrompt.SetActive(true);
                    if (promptText != null) promptText.text = "LMB - Чинить";
                }

                if (Input.GetMouseButtonDown(0) && !isRepairing)
                {
                    repairRoutine = StartCoroutine(RepairRoutine(antenna));
                }
            }
        }

        if (Input.GetMouseButtonUp(0) && isRepairing)
        {
            StopRepair();
        }
    }

    private IEnumerator RepairRoutine(AntennaController antenna)
    {
        isRepairing = true;
        float timer = 0f;
        float duration = 5f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            if (progressImage != null) progressImage.fillAmount = timer / duration;
            yield return null;
        }

        antenna.Repair();
        StopRepair();
    }

    private void StopRepair()
    {
        if (repairRoutine != null) StopCoroutine(repairRoutine);
        isRepairing = false;
        if (progressImage != null) progressImage.fillAmount = 0;
    }

    private bool IsHoveringDish()
    {
        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, pickupRange, interactableLayer))
        {
            AntennaController antenna = hit.collider.GetComponentInParent<AntennaController>();
            if (antenna != null && hit.collider.gameObject == antenna.dishPart)
            {
                return true;
            }
        }
        return false;
    }

    private void HandleDishRotation()
    {
        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, pickupRange, interactableLayer))
        {
            AntennaController antenna = hit.collider.GetComponentInParent<AntennaController>();
            if (antenna != null && hit.collider.gameObject == antenna.dishPart)
            {
                float dir = 0;
                if (Input.GetKey(KeyCode.Q)) dir = -1;
                if (Input.GetKey(KeyCode.E)) dir = 1;
                
                if (dir != 0)
                {
                    antenna.RotateDishManual(dir);
                }
            }
        }
    }

    private void HandleHighlightAndPickup()
    {
        if (cam == null || isPlacing || isRepairing) 
        {
            if (interactionPrompt != null) interactionPrompt.SetActive(false);
            return;
        }

        LayerMask combinedMask = interactableLayer | wrenchLayer;
        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, pickupRange, combinedMask))
        {
            ItemSpawner spawner = hit.collider.GetComponentInParent<ItemSpawner>();
            ItemData data = hit.collider.GetComponentInParent<ItemData>();

            if (spawner != null || data != null)
            {
                if (interactionPrompt != null) interactionPrompt.SetActive(true);
                if (promptText != null) promptText.gameObject.SetActive(true);

                if (spawner != null)
                {
                    UpdateOutline(spawner.gameObject);
                    if (promptText != null) promptText.text = "E - Подобрать";
                    if (Input.GetKeyDown(KeyCode.E) && inventory.GetEmptySlot() != -1 && spawner.itemPrefab != null)
                    {
                        PickupObject(Instantiate(spawner.itemPrefab), inventory.GetEmptySlot(), null);
                        ClearOutline();
                    }
                }
                else if (data != null)
                {
                AntennaController antenna = data.GetComponentInChildren<AntennaController>();
                
                // Всегда разрешаем подсвечивать конкретные детали (кнопки и т.д.)
                UpdateOutline(hit.collider.gameObject);

                if (antenna != null && hit.collider.gameObject == antenna.dishPart && !antenna.IsLocked)
                    {
                        if (promptText != null) promptText.text = "Q/E - Крутить";
                    }
                    else if (antenna != null && hit.collider.gameObject == antenna.dishPart && antenna.IsLocked)
                    {
                        if (interactionPrompt != null) interactionPrompt.SetActive(false);
                    }
                    else
                    {
                        if (promptText != null) promptText.text = "E - Взаимодействие";
                    }

                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        if (data.isPlaced)
                        {
                            if (!data.isActivated) data.ToggleActivation();
                            else if (antenna != null) antenna.OnInteract(hit.collider.gameObject);
                            else data.ToggleActivation();
                        }
                        else if (inventory.GetEmptySlot() != -1)
                        {
                            PickupObject(data.gameObject, inventory.GetEmptySlot(), data);
                            ClearOutline();
                        }
                    }
                }
            }
            else
            {
                ClearOutline();
            }
        }
        else
        {
            ClearOutline();
        }
    }

    private void DisableAllOutlinesOnObject(GameObject obj)
    {
        if (obj == null) return;
        foreach (var o in obj.GetComponentsInChildren<Outline>(true))
        {
            o.enabled = false;
        }
    }

    private void UpdateOutline(GameObject obj)
    {
        Outline outline = obj.GetComponent<Outline>() ?? obj.GetComponentInChildren<Outline>();
        if (outline != null && lastOutline != outline)
        {
            if (lastOutline != null) 
            {
                // Не выключаем контур, если это корневой контур сломанной антенны
                AntennaController lastAnt = lastOutline.GetComponent<AntennaController>();
                if (lastAnt == null || !lastAnt.IsBroken) 
                {
                    // А вот если это деталь - можно выключить
                    lastOutline.enabled = false;
                }
            }
            outline.enabled = true;
            lastOutline = outline;
        }
    }

    private void ClearOutline()
    {
        if (lastOutline != null)
        {
            AntennaController ant = lastOutline.GetComponentInParent<AntennaController>();
            // Выключаем контур только если это не красная подсветка поломки
            if (ant == null || !ant.IsBroken)
            {
                lastOutline.enabled = false; 
            }
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
            DisableAllOutlinesOnObject(obj);
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
            if (data.placementAudioSource != null) 
            {
                data.placementAudioSource.Stop();
            }
        }
    }

    private IEnumerator PlaceHoldRoutine(Vector3 pos, Quaternion rot, ItemData data)
    {
        isPlacing = true;
        if (data.placementAudioSource) 
        {
            data.placementAudioSource.Play();
        }

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
            DisableAllOutlinesOnObject(obj);
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
