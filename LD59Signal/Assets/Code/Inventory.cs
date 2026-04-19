using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    public Image[] slotIcons;
    public RectTransform[] slotRects;
    public RectTransform layoutGroupParent;
    public Color filledColor = new Color(1, 1, 1, 0.8f);
    public float sizeTransitionSpeed = 10f;
    public float defaultSlotSize = 70f;
    public float activeSlotSize = 90f;
    public Transform holdPoint;

    private GameObject[] slots = new GameObject[3];
    private ItemData[] slotData = new ItemData[3];
    public int currentSlot { get; private set; } = 0;
    public bool isHidden { get; private set; } = false;

    private void Start()
    {
        InitializeSlotSizes();
        UpdateUI();
    }

    private void InitializeSlotSizes()
    {
        if (slotRects == null) return;
        
        for (int i = 0; i < slotRects.Length; i++)
        {
            if (slotRects[i] != null)
            {
                slotRects[i].sizeDelta = new Vector2(defaultSlotSize, defaultSlotSize);
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        
        UpdateUISizes();
    }

    private void UpdateUISizes()
    {
        if (slotRects == null) return;

        bool anySizeChanged = false;
        for (int i = 0; i < 3; i++)
        {
            if (i >= slotRects.Length || slotRects[i] == null) continue;

            bool isActive = currentSlot == i && !isHidden && slots[i] != null;
            float targetSize = isActive ? activeSlotSize : defaultSlotSize;
            Vector2 targetSizeDelta = new Vector2(targetSize, targetSize);
            
            Vector2 previousSize = slotRects[i].sizeDelta;
            slotRects[i].sizeDelta = Vector2.Lerp(previousSize, targetSizeDelta, Time.deltaTime * sizeTransitionSpeed);
            
            if (Vector2.Distance(previousSize, slotRects[i].sizeDelta) > 0.01f)
            {
                anySizeChanged = true;
            }
        }

        if (anySizeChanged && layoutGroupParent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroupParent);
        }
    }

    public void SelectSlot(int index)
    {
        if (currentSlot == index && slots[index] != null)
        {
            isHidden = !isHidden;
            slots[index].SetActive(!isHidden);
            UpdateUI();
            return;
        }

        if (slots[currentSlot] != null) 
        {
            slots[currentSlot].SetActive(false);
        }

        currentSlot = index;
        isHidden = false;

        if (slots[currentSlot] != null) 
        {
            slots[currentSlot].SetActive(true);
        }

        UpdateUI();
    }

    public int GetEmptySlot()
    {
        for (int i = 0; i < 3; i++)
        {
            if (slots[i] == null && slotData[i] == null) return i;
        }
        return -1;
    }

    public bool IsSlotOccupied(int index)
    {
        if (index < 0 || index >= 3) return true;
        return slots[index] != null || slotData[index] != null;
    }

    public bool AddItem(GameObject obj, int index)
    {
        if (index < 0 || index >= 3) return false;
        if (IsSlotOccupied(index)) return false;

        ItemData data = obj.GetComponent<ItemData>();
        if (data == null)
        {
            Debug.LogWarning($"Item {obj.name} has no ItemData component!");
            return false;
        }

        slots[index] = obj;
        slotData[index] = data;
        
        obj.transform.SetParent(holdPoint);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        if (currentSlot != index && slots[currentSlot] != null)
        {
            slots[currentSlot].SetActive(false);
        }

        currentSlot = index;
        isHidden = false;
        obj.SetActive(true);
        
        UpdateUI();
        return true;
    }

    public GameObject RemoveCurrentItem()
    {
        if (slots[currentSlot] == null || isHidden) 
        {
            return null;
        }
        
        GameObject obj = slots[currentSlot];
        slots[currentSlot] = null;
        slotData[currentSlot] = null;
        
        obj.transform.SetParent(null, true);
        
        UpdateUI();
        SelectSlot(currentSlot);
        return obj;
    }

    public ItemData GetItemData(int index)
    {
        if (index < 0 || index >= 3) return null;
        return slotData[index];
    }

    public ItemData GetCurrentItemData()
    {
        return slotData[currentSlot];
    }

    public GameObject GetCurrentItem()
    {
        if (isHidden) 
        {
            return null;
        }
        return slots[currentSlot];
    }

    public void ForceUpdateUI()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        for (int i = 0; i < 3; i++)
        {
            if (slotIcons == null || i >= slotIcons.Length || slotIcons[i] == null)
                continue;

            Image iconImage = slotIcons[i];
            bool hasItem = slots[i] != null && slotData[i] != null;

            if (hasItem && slotData[i].icon != null)
            {
                iconImage.sprite = slotData[i].icon;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
                continue;
            }

            bool isActive = currentSlot == i && !isHidden;
            
            if (isActive)
            {
                iconImage.color = Color.white;
            }
            else
            {
                iconImage.color = filledColor;
            }
        }
    }
}
