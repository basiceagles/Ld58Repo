using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    public Image[] slotIcons;
    public Color emptyColor = new Color(1, 1, 1, 0.3f);
    public Color filledColor = new Color(1, 1, 1, 0.8f);
    public Color activeColor = Color.green;
    public Transform holdPoint;

    private GameObject[] slots = new GameObject[3];
    public int currentSlot { get; private set; } = 0;
    public bool isHidden { get; private set; } = false;

    private void Start()
    {
        UpdateUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
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
            if (slots[i] == null) return i;
        }
        return -1;
    }

    public void AddItem(GameObject obj, int index)
    {
        slots[index] = obj;
        
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
    }

    public GameObject RemoveCurrentItem()
    {
        if (slots[currentSlot] == null || isHidden) 
        {
            return null;
        }
        
        GameObject obj = slots[currentSlot];
        slots[currentSlot] = null;
        
        obj.transform.SetParent(null, true);
        
        UpdateUI();
        SelectSlot(currentSlot);
        return obj;
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
            if (slotIcons != null && i < slotIcons.Length && slotIcons[i] != null)
            {
                if (currentSlot == i && !isHidden && slots[i] != null)
                {
                    slotIcons[i].color = activeColor;
                }
                else if (slots[i] != null)
                {
                    slotIcons[i].color = filledColor;
                }
                else
                {
                    slotIcons[i].color = emptyColor;
                }
            }
        }
    }
}
