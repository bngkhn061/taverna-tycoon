using UnityEngine;
using UnityEngine.UIElements;

namespace Taverna
{
    /// <summary>Sabır / üretim halkası: 0..1 arası dairesel ilerleme (Painter2D ile çizilir).</summary>
    public class RingElement : VisualElement
    {
        float _value = 1f;
        Color _color = Color.green;
        public float lineWidth = 15f;

        public RingElement()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        public void Set(float value01, Color color)
        {
            value01 = Mathf.Clamp01(value01);
            if (Mathf.Abs(value01 - _value) < 0.002f && color == _color) return;
            _value = value01;
            _color = color;
            MarkDirtyRepaint();
        }

        void Draw(MeshGenerationContext ctx)
        {
            var r = contentRect;
            if (r.width <= 0) return;
            float rad = Mathf.Min(r.width, r.height) * 0.5f - lineWidth * 0.5f;
            var p = ctx.painter2D;
            p.lineWidth = lineWidth;
            p.lineCap = LineCap.Round;
            p.strokeColor = new Color32(239, 226, 198, 255); // kâğıt tonu iz
            p.BeginPath();
            p.Arc(r.center, rad, Angle.Degrees(0f), Angle.Degrees(360f));
            p.Stroke();
            if (_value <= 0.001f) return;
            p.strokeColor = _color;
            p.BeginPath();
            p.Arc(r.center, rad, Angle.Degrees(-90f), Angle.Degrees(-90f + 360f * _value));
            p.Stroke();
        }

        /// <summary>Sabır renkleri (UI v3 paleti): çimen → hardal → tuğla.</summary>
        public static Color PatienceColor(float v) =>
            v > 0.5f ? new Color32(106, 168, 79, 255) : v > 0.25f ? new Color32(242, 182, 49, 255) : new Color32(212, 85, 58, 255);
    }
}
