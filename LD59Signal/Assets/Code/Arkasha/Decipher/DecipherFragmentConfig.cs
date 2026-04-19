using System;
using UnityEngine;

[Serializable]
public struct DecipherFragmentConfig
{
    [Header("Целевая волна")]
    [Range(0.1f, 2f)]   public float targetAmplitude;
    [Range(1f, 20f)]    public float targetFrequency;
    [Range(0f, 6.28f)]  public float targetPhase;

    [Header("Скорости управления")]
    [Range(0.1f, 3f)]   public float ampAdjustSpeed;
    [Range(0.1f, 5f)]   public float freqAdjustSpeed;
    [Range(0.1f, 5f)]   public float phaseAdjustSpeed;
    public bool hasPhaseControl;

    [Header("Дрейф волны-цели")]
    public bool         hasDrift;
    [Range(0f, 2f)]     public float driftSpeed;

    [Header("Помехи")]
    [Range(0f, 0.5f)]   public float noiseStrength;
    [Range(0.1f, 5f)]   public float noiseInterval;

    [Header("Инверсия")]
    public bool         hasInversion;
    [Range(1f, 10f)]    public float inversionInterval;

    [Header("Текст фрагмента")]
    [TextArea(2, 4)]    public string revealedText;
}