using UnityEngine;

public class ItemData : MonoBehaviour
{
    public string itemName;
    public bool isPlaceable;
    public GameObject ghostPrefab;
    public GameObject invalidGhostPrefab;
    public float placementYOffset;
    public Vector3 heldRotationOffset;
    public Vector3 heldPositionOffset;
    public bool pickupOnBodyHoldPoint;
    public AudioSource placementAudioSource;
    public Sprite icon;

    public Animator itemAnimator;
    public bool isPlaced = false; 
    public bool isActivated = false;

    private void Start()
    {
        StopAnimation();
    }

    public void StopAnimation()
    {
        if (itemAnimator != null)
        {
            itemAnimator.enabled = false;
            itemAnimator.Rebind();
            itemAnimator.Update(0f);
        }
    }

    public void ToggleActivation()
    {
        if (itemAnimator == null)
        {
            return;
        }
        isActivated = !isActivated;
        itemAnimator.enabled = isActivated;
    }
}
