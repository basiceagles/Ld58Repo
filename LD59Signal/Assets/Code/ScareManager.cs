using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ScareManager : MonoBehaviour
{
    public GameObject[] scarePrefabs;
    public AudioClip scareSpawnSound;
    [Range(0f, 1f)] public float spawnSoundVolume = 0.5f;
    public Transform playerTransform;
    public Camera playerCamera;
    
    [Range(1f, 50f)] public float minSpawnDistance = 10f;
    [Range(5f, 50f)] public float maxSpawnDistance = 25f;
    public float stormDistanceMultiplier = 0.5f;
    
    public float minTimeBetweenScares = 30f;
    public float maxTimeBetweenScares = 90f;
    public float stormScaresMultiplier = 0.4f;
    
    public float lookAtThreshold = 0.85f;
    public LayerMask groundLayer;
    public LayerMask obstacleLayer;
    public float obstacleCheckRadius = 0.8f;
    public float obstacleCheckHeight = 1.5f;

    private GameObject currentScare;
    private GamePhaseManager phaseManager;
    private bool lastStormState = false;

    private void Start()
    {
        playerTransform = Camera.main.transform;
        playerCamera = Camera.main;
        phaseManager = FindObjectOfType<GamePhaseManager>();
        
        SpawnSpecificScare(1, -90f);
        
        StartCoroutine(ScareLoop());
    }

    public void SpawnSpecificScare(int index, float xRotation)
    {
        if (scarePrefabs == null || index < 0 || index >= scarePrefabs.Length) return;
        if (playerTransform == null) playerTransform = Camera.main.transform;

        Vector3 spawnPos = playerTransform.position + playerTransform.forward * 5f;
        if (Physics.Raycast(spawnPos + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f, groundLayer))
        {
            spawnPos = hit.point;
        }

        if (currentScare != null) Destroy(currentScare);
        currentScare = Instantiate(scarePrefabs[index], spawnPos, Quaternion.Euler(xRotation, 0, 0));
        
        if (scareSpawnSound != null)
        {
            AudioSource.PlayClipAtPoint(scareSpawnSound, spawnPos, spawnSoundVolume);
        }
    }

    private IEnumerator ScareLoop()
    {
        while (true)
        {
            if (scarePrefabs == null || scarePrefabs.Length == 0)
            {
                yield return new WaitForSeconds(5f);
                continue;
            }

            float waitTime = Random.Range(minTimeBetweenScares, maxTimeBetweenScares);
            if (phaseManager != null && phaseManager.isStormActive)
            {
                waitTime *= stormScaresMultiplier;
            }
            
            float timer = 0f;
            while (timer < waitTime)
            {
                if (phaseManager != null && phaseManager.isStormActive && !lastStormState)
                {
                    lastStormState = true;
                    break; 
                }
                if (phaseManager != null && !phaseManager.isStormActive) lastStormState = false;

                timer += 1f;
                yield return new WaitForSeconds(1f);
            }

            if (currentScare == null)
            {
                TrySpawnScare();
            }
        }
    }

    private void Update()
    {
        if (currentScare != null)
        {
            CheckIfPlayerLooking();
        }
    }

    private void TrySpawnScare()
    {
        if (playerTransform == null)
        {
            return;
        }

        float side = Random.value > 0.5f ? 1f : -1f;
        
        float minAngle = 45f;
        float maxAngle = 85f;
        if (phaseManager != null && phaseManager.isStormActive)
        {
            minAngle = 35f;
            maxAngle = 65f;
        }

        float angle = side * Random.Range(minAngle, maxAngle);
        
        float dist = Random.Range(minSpawnDistance, maxSpawnDistance);
        if (phaseManager != null && phaseManager.isStormActive)
        {
            dist *= stormDistanceMultiplier;
        }

        Vector3 direction = Quaternion.Euler(0, angle, 0) * playerTransform.forward;
        Vector3 spawnPos = playerTransform.position + direction * dist;

        if (Physics.Raycast(spawnPos + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f, groundLayer))
        {
            spawnPos = hit.point;
            
            if (Physics.CheckSphere(spawnPos + Vector3.up * obstacleCheckHeight, obstacleCheckRadius, obstacleLayer))
            {
                return;
            }

            int prefabIndex = Random.Range(0, scarePrefabs.Length);
            GameObject prefab = scarePrefabs[prefabIndex];
            
            // Element 1 is spawned with x-90 rotation
            Quaternion spawnRot = (prefabIndex == 1) ? Quaternion.Euler(-90, 0, 0) : Quaternion.identity;
            currentScare = Instantiate(prefab, spawnPos, spawnRot);
            
            if (scareSpawnSound != null)
            {
                AudioSource.PlayClipAtPoint(scareSpawnSound, spawnPos, spawnSoundVolume);
            }
            
            // Only look at player if not element 1 to preserve x-90 rotation
            if (prefabIndex != 1)
            {
                Vector3 lookPos = playerTransform.position;
                lookPos.y = currentScare.transform.position.y;
                currentScare.transform.LookAt(lookPos);
            }
        }
    }

    private void CheckIfPlayerLooking()
    {
        if (currentScare == null)
        {
            return;
        }

        Vector3 dirToScare = (currentScare.transform.position - playerCamera.transform.position).normalized;
        float dot = Vector3.Dot(playerCamera.transform.forward, dirToScare);

        if (dot > lookAtThreshold)
        {
            Destroy(currentScare);
            currentScare = null;
        }
        
        if (currentScare != null && Vector3.Distance(playerTransform.position, currentScare.transform.position) > maxSpawnDistance * 2f)
        {
            Destroy(currentScare);
            currentScare = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (playerTransform == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(playerTransform.position, minSpawnDistance);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(playerTransform.position, maxSpawnDistance);

        Vector3 rightBound = Quaternion.Euler(0, 45, 0) * playerTransform.forward;
        Vector3 leftBound = Quaternion.Euler(0, -45, 0) * playerTransform.forward;
        Vector3 farRight = Quaternion.Euler(0, 85, 0) * playerTransform.forward;
        Vector3 farLeft = Quaternion.Euler(0, -85, 0) * playerTransform.forward;

        Gizmos.color = new Color(1, 0, 0, 0.2f);
        DrawSector(playerTransform.position, rightBound, farRight, maxSpawnDistance);
        DrawSector(playerTransform.position, leftBound, farLeft, maxSpawnDistance);
    }

    private void DrawSector(Vector3 center, Vector3 start, Vector3 end, float radius)
    {
        Gizmos.DrawLine(center, center + start * radius);
        Gizmos.DrawLine(center, center + end * radius);
    }
}
