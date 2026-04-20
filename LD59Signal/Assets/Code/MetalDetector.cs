using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MetalDetector : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float maxDetectionRange = 10f;
    [SerializeField] private float closeDetectionRange = 1f;
    [SerializeField] private LayerMask scrapLayer;
    [SerializeField] private Transform detectionOrigin;

    [Header("Audio")]
    [SerializeField] private AudioClip beepClip;
    [SerializeField] private float minBeepInterval = 0.1f;
    [SerializeField] private float maxBeepInterval = 2f;

    [Header("Interaction")]
    [SerializeField] private float liftHeight = 0.2f;
    [SerializeField] private float liftDuration = 0.5f;
    [SerializeField] private float activationDelay = 1f;

    private AudioSource audioSource;
    private float beepTimer;
    private float currentBeepInterval = float.MaxValue;
    private Collider closestScrap;
    private Coroutine activationCoroutine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (detectionOrigin == null)
        {
            detectionOrigin = transform;
        }
    }

    private void Update()
    {
        DetectScrap();
        HandleBeeping();
        HandleActivation();
    }

    private void DetectScrap()
    {
        closestScrap = null;
        float closestDistance = float.MaxValue;

        Collider[] hits = Physics.OverlapSphere(detectionOrigin.position, maxDetectionRange, scrapLayer);

        foreach (Collider hit in hits)
        {
            float distance = Vector3.Distance(detectionOrigin.position, hit.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestScrap = hit;
            }
        }

        if (closestScrap != null)
        {
            if (closestDistance <= closeDetectionRange)
            {
                currentBeepInterval = minBeepInterval;
            }
            else
            {
                float t = Mathf.InverseLerp(maxDetectionRange, closeDetectionRange, closestDistance);
                currentBeepInterval = Mathf.Lerp(maxBeepInterval, minBeepInterval, t);
            }
        }
        else
        {
            currentBeepInterval = float.MaxValue;
        }
    }

    private void HandleBeeping()
    {
        if (currentBeepInterval >= float.MaxValue - 1f)
        {
            beepTimer = 0f;
            return;
        }

        beepTimer -= Time.deltaTime;
        if (beepTimer <= 0f)
        {
            PlayBeep();
            beepTimer = currentBeepInterval;
        }
    }

    private void PlayBeep()
    {
        if (beepClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(beepClip);
        }
    }

    private void HandleActivation()
    {
        if (closestScrap == null)
        {
            CancelActivation();
            return;
        }

        float distance = Vector3.Distance(detectionOrigin.position, closestScrap.transform.position);

        if (distance > closeDetectionRange)
        {
            CancelActivation();
            return;
        }

        if (activationCoroutine == null)
        {
            activationCoroutine = StartCoroutine(ActivateScrap(closestScrap));
        }
    }

    private void CancelActivation()
    {
        if (activationCoroutine != null)
        {
            StopCoroutine(activationCoroutine);
            activationCoroutine = null;
        }
    }

    private IEnumerator ActivateScrap(Collider scrapCollider)
    {
        yield return new WaitForSeconds(activationDelay);

        if (scrapCollider == null)
        {
            activationCoroutine = null;
            yield break;
        }

        Rigidbody rb = scrapCollider.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.None;
        }

        Vector3 startPos = scrapCollider.transform.position;
        Vector3 targetPos = startPos + Vector3.up * liftHeight;

        float elapsed = 0f;
        while (elapsed < liftDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / liftDuration;
            scrapCollider.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        scrapCollider.transform.position = targetPos;
        
        // Mark item as no longer buried so player can interact with it
        ItemData itemData = scrapCollider.GetComponentInParent<ItemData>();
        if (itemData != null) itemData.isBuried = false;
        
        activationCoroutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (detectionOrigin == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(detectionOrigin.position, maxDetectionRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(detectionOrigin.position, closeDetectionRange);
    }
}
