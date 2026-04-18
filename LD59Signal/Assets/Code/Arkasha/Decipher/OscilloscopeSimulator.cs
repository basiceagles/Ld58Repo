using UnityEngine;

public static class OscilloscopeSimulator
{
    public const int SAMPLE_COUNT = 256;
    private const float MATCH_TOLERANCE = 0.3f;

    // Вычисляет Y точки синусоиды в позиции x (0..1)
    public static float ComputeWave(float amplitude, float frequency, float phase, float x)
    {
        return amplitude * Mathf.Sin(frequency * x * Mathf.PI * 2f + phase);
    }

    // Считает насколько волна игрока совпадает с целевой. Результат 0..1
    public static float ComputeMatchScore(
        float targetAmp, float targetFreq, float targetPhase, float[] targetNoise,
        float playerAmp, float playerFreq, float playerPhase)
    {
        float score = 0f;

        for (int i = 0; i < SAMPLE_COUNT; i++)
        {
            float x = (float)i / SAMPLE_COUNT;

            float targetY = ComputeWave(targetAmp, targetFreq, targetPhase, x);
            if (targetNoise != null && i < targetNoise.Length)
                targetY += targetNoise[i];

            float playerY = ComputeWave(playerAmp, playerFreq, playerPhase, x);

            score += 1f - Mathf.Clamp01(Mathf.Abs(targetY - playerY) / MATCH_TOLERANCE);
        }

        return score / SAMPLE_COUNT;
    }

    // Заполняет массив точек для LineRenderer
    public static void FillPoints(
        float amplitude, float frequency, float phase, float[] noiseOffsets,
        float centerX, float centerY,
        float halfWidth, float halfHeight,
        float zDepth,
        Vector3[] outPoints)
    {
        int count = outPoints.Length;
        for (int i = 0; i < count; i++)
        {
            float x = (float)i / count;
            float y = ComputeWave(amplitude, frequency, phase, x);

            if (noiseOffsets != null && i < noiseOffsets.Length)
                y += noiseOffsets[i];

            outPoints[i] = new Vector3(
                centerX + Mathf.Lerp(-halfWidth, halfWidth, x),
                centerY + y * halfHeight,
                zDepth
            );
        }
    }
}