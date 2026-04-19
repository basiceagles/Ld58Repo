using UnityEngine;
using UnityEngine.UI;

// Рисует волну прямо в UI через Graphic — работает с Overlay Canvas
[RequireComponent(typeof(CanvasRenderer))]
public class WaveRenderer : MaskableGraphic
{
    private float _amplitude;
    private float _frequency;
    private float _phase;
    private float[] _noiseOffsets;

    public void UpdateWave(float amplitude, float frequency, float phase, float[] noiseOffsets = null)
    {
        _amplitude   = amplitude;
        _frequency   = frequency;
        _phase       = phase;
        _noiseOffsets = noiseOffsets;

        // Говорим Unity что нужно перерисовать
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect r         = rectTransform.rect;
        float halfW    = r.width  * 0.5f;
        float halfH    = r.height * 0.5f;
        float lineWidth = 2f;

        for (int i = 0; i < OscilloscopeSimulator.SAMPLE_COUNT - 1; i++)
        {
            float x0 = (float)i       / OscilloscopeSimulator.SAMPLE_COUNT;
            float x1 = (float)(i + 1) / OscilloscopeSimulator.SAMPLE_COUNT;

            float y0 = OscilloscopeSimulator.ComputeWave(_amplitude, _frequency, _phase, x0);
            float y1 = OscilloscopeSimulator.ComputeWave(_amplitude, _frequency, _phase, x1);

            if (_noiseOffsets != null)
            {
                if (i     < _noiseOffsets.Length) y0 += _noiseOffsets[i];
                if (i + 1 < _noiseOffsets.Length) y1 += _noiseOffsets[i + 1];
            }

            Vector2 p0 = new Vector2(Mathf.Lerp(-halfW, halfW, x0), y0 * halfH);
            Vector2 p1 = new Vector2(Mathf.Lerp(-halfW, halfW, x1), y1 * halfH);

            // Вектор перпендикуляра для толщины линии
            Vector2 dir    = (p1 - p0).normalized;
            Vector2 normal = new Vector2(-dir.y, dir.x) * lineWidth * 0.5f;

            int idx = vh.currentVertCount;

            vh.AddVert(p0 - normal, color, Vector2.zero);
            vh.AddVert(p0 + normal, color, Vector2.zero);
            vh.AddVert(p1 + normal, color, Vector2.zero);
            vh.AddVert(p1 - normal, color, Vector2.zero);

            vh.AddTriangle(idx,     idx + 1, idx + 2);
            vh.AddTriangle(idx,     idx + 2, idx + 3);
        }
    }
}