using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private float shakeMagnitude = 0.1f;
    [SerializeField] private float dampingSpeed = 1.0f;
    [SerializeField] private float multiplier = 2.0f;
    
    private Vector3 initialPosition;
    private Coroutine shakeCoroutine;
    private Coroutine constantShakeCoroutine;
    private Coroutine smoothConstantShakeCoroutine;
    private bool isShaking = false;
    private bool isConstantShaking = false;
    private bool isSmoothConstantShaking = false;

    void Start()
    {
        initialPosition = transform.localPosition;
    }

    public void ConstantlyShakeStartSmoothly(float rampUpTime = 1.0f)
    {
        if (isSmoothConstantShaking) return;
        
        if (smoothConstantShakeCoroutine != null)
        {
            StopCoroutine(smoothConstantShakeCoroutine);
        }
        
        smoothConstantShakeCoroutine = StartCoroutine(SmoothConstantShakeCoroutine(rampUpTime));
        isSmoothConstantShaking = true;
    }

    private IEnumerator SmoothConstantShakeCoroutine(float rampUpTime)
    {
        isSmoothConstantShaking = true;
        initialPosition = transform.localPosition;
        
        float elapsed = 0f;
        float startMagnitude = 0.1f;
        float targetMagnitude = 1.0f;
        
        while (elapsed < rampUpTime)
        {
            float currentMagnitude = Mathf.Lerp(startMagnitude, targetMagnitude, elapsed / rampUpTime);
            transform.localPosition = initialPosition + Random.insideUnitSphere * currentMagnitude;
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        while (true)
        {
            transform.localPosition = initialPosition + Random.insideUnitSphere * targetMagnitude;
            yield return null;
        }
    }

    public void ConstantlyShakeStopSmoothly()
    {
        if (smoothConstantShakeCoroutine != null)
        {
            StopCoroutine(smoothConstantShakeCoroutine);
            transform.localPosition = initialPosition;
            isSmoothConstantShaking = false;
            smoothConstantShakeCoroutine = null;
        }
    }

    public void ConstantlyShakeStart()
    {
        if (isConstantShaking) return;
        
        if (constantShakeCoroutine != null)
        {
            StopCoroutine(constantShakeCoroutine);
        }
        
        constantShakeCoroutine = StartCoroutine(ConstantShakeCoroutine());
        isConstantShaking = true;
    }

    public void ConstantlyShakeStart(float magnitude)
    {
        if (isConstantShaking) return;
        
        if (constantShakeCoroutine != null)
        {
            StopCoroutine(constantShakeCoroutine);
        }
        
        constantShakeCoroutine = StartCoroutine(ConstantShakeCoroutine(magnitude));
        isConstantShaking = true;
    }

    public void ConstantlyShakeStop()
    {
        if (constantShakeCoroutine != null)
        {
            StopCoroutine(constantShakeCoroutine);
            transform.localPosition = initialPosition;
            isConstantShaking = false;
            constantShakeCoroutine = null;
        }
    }

    private IEnumerator ConstantShakeCoroutine(float? customMagnitude = null)
    {
        isConstantShaking = true;
        initialPosition = transform.localPosition;
        
        float magnitude = customMagnitude ?? shakeMagnitude;
        
        while (true)
        {
            transform.localPosition = initialPosition + Random.insideUnitSphere * magnitude;
            yield return null;
        }
    }

    public void Shake()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        
        shakeCoroutine = StartCoroutine(ShakeCoroutine(shakeDuration, shakeMagnitude));
    }

    public void HeavyShake()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        
        shakeCoroutine = StartCoroutine(ShakeCoroutine(shakeDuration, shakeMagnitude * multiplier));
    }

    public void Shake(float duration, float magnitude)
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        
        shakeCoroutine = StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        isShaking = true;
        initialPosition = transform.localPosition;
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float currentMagnitude = magnitude * (1f - (elapsed / duration));
            transform.localPosition = initialPosition + Random.insideUnitSphere * currentMagnitude;
            elapsed += Time.deltaTime * dampingSpeed;
            yield return null;
        }
        
        transform.localPosition = initialPosition;
        isShaking = false;
        shakeCoroutine = null;
    }

    public void StopShake()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            transform.localPosition = initialPosition;
            isShaking = false;
            shakeCoroutine = null;
        }
        
        ConstantlyShakeStop();
        ConstantlyShakeStopSmoothly();
    }

    public bool IsShaking()
    {
        return isShaking || isConstantShaking || isSmoothConstantShaking;
    }

    public bool IsConstantShaking()
    {
        return isConstantShaking || isSmoothConstantShaking;
    }

    public bool IsSmoothConstantShaking()
    {
        return isSmoothConstantShaking;
    }

    public void ShakeSmooth(float duration, float magnitude, AnimationCurve falloffCurve = null)
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        
        shakeCoroutine = StartCoroutine(SmoothShakeCoroutine(duration, magnitude, falloffCurve));
    }

    private IEnumerator SmoothShakeCoroutine(float duration, float magnitude, AnimationCurve falloffCurve)
    {
        isShaking = true;
        initialPosition = transform.localPosition;
        
        float elapsed = 0f;
        
        if (falloffCurve == null)
        {
            falloffCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        }
        
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            float currentMagnitude = magnitude * falloffCurve.Evaluate(progress);
            transform.localPosition = initialPosition + Random.insideUnitSphere * currentMagnitude;
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localPosition = initialPosition;
        isShaking = false;
        shakeCoroutine = null;
    }
}