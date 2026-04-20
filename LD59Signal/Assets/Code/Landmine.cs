using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class Landmine : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Renderer materialRenderer;
    
    [Header("Materials")]
    [SerializeField] private Material idleMaterial;
    [SerializeField] private Material beepMaterial;
    
    [Header("Audio Clips")]
    [SerializeField] private AudioClip beepClip;
    [SerializeField] private AudioClip explosionClip;
    
    [Header("Settings")]
    [SerializeField] private float beepInterval = 1f;
    [SerializeField] private float explosionDelay = 0.5f;
    [SerializeField] private string sceneToLoad = "GameOver";
    [SerializeField] private LayerMask playerLayer;
    
    private bool hasExploded = false;
    private bool isBeepMaterialActive = false;
    
    private void Start()
    {
        if (materialRenderer != null && idleMaterial != null)
        {
            materialRenderer.material = idleMaterial;
        }
        
        StartCoroutine(BeepLoop());
    }
    
    private IEnumerator BeepLoop()
    {
        while (!hasExploded)
        {
            yield return new WaitForSeconds(beepInterval);
            
            if (hasExploded) yield break;
            
            // Play beep sound
            if (audioSource != null && beepClip != null)
            {
                audioSource.PlayOneShot(beepClip);
            }
            
            // Toggle material
            if (materialRenderer != null)
            {
                isBeepMaterialActive = !isBeepMaterialActive;
                materialRenderer.material = isBeepMaterialActive ? beepMaterial : idleMaterial;
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Landmine triggered by: {other.name}, layer: {other.gameObject.layer}");
        
        if (hasExploded) return;
        
        // Check if colliding object is on player layer
        bool isPlayer = ((1 << other.gameObject.layer) & playerLayer) != 0;
        Debug.Log($"Is player layer: {isPlayer}, playerLayer mask: {playerLayer.value}");
        
        if (isPlayer)
        {
            StartCoroutine(Explode());
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Landmine collided with: {collision.gameObject.name}");
        
        if (hasExploded) return;
        
        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            StartCoroutine(Explode());
        }
    }
    
    private IEnumerator Explode()
    {
        hasExploded = true;
        StopAllCoroutines();
        
        // Play explosion sound
        if (audioSource != null && explosionClip != null)
        {
            audioSource.PlayOneShot(explosionClip);
        }
        
        // Wait for explosion delay
        yield return new WaitForSeconds(explosionDelay);
        
        // Load scene
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}
