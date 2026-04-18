using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class WaveRenderer : MonoBehaviour
{
    [SerializeField] private RectTransform _panelRect;

    private LineRenderer _lineRenderer;
    private readonly Vector3[] _points = new Vector3[OscilloscopeSimulator.SAMPLE_COUNT];

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = OscilloscopeSimulator.SAMPLE_COUNT;
        _lineRenderer.useWorldSpace = true;
    }

    // Обновляет позиции волны. Вызывать каждый кадр
    public void UpdateWave(float amplitude, float frequency, float phase, float[] noiseOffsets = null)
    {
        if (_panelRect == null) return;

        // Получаем мировые координаты углов панели
        // Порядок углов: [0]=BL, [1]=TL, [2]=TR, [3]=BR
        Vector3[] corners = new Vector3[4];
        _panelRect.GetWorldCorners(corners);

        Vector3 center   = (corners[0] + corners[2]) * 0.5f;
        float halfWidth  = (corners[2].x - corners[0].x) * 0.5f;
        float halfHeight = (corners[1].y - corners[0].y) * 0.5f;

        // Чуть перед плоскостью Canvas чтобы волна не ушла за фон
        float zDepth = corners[0].z - 0.01f;

        OscilloscopeSimulator.FillPoints(
            amplitude, frequency, phase, noiseOffsets,
            center.x, center.y,
            halfWidth, halfHeight,
            zDepth,
            _points
        );

        _lineRenderer.SetPositions(_points);
    }
}