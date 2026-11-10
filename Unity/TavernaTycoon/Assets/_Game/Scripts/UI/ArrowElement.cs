using UnityEngine;
using UnityEngine.UIElements;

namespace Taverna
{
    /// <summary>Aşağı bakan, konturlu hedef oku (Painter2D). rotate stiliyle ekran kenarı göstergesi olur.</summary>
    public class ArrowElement : VisualElement
    {
        public Color fill = new(0.95f, 0.76f, 0.31f);
        public Color stroke = new(0.36f, 0.23f, 0.12f);

        public ArrowElement()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        void Draw(MeshGenerationContext ctx)
        {
            var r = contentRect;
            if (r.width <= 0) return;
            float w = r.width, h = r.height;
            var p = ctx.painter2D;
            p.fillColor = fill;
            p.strokeColor = stroke;
            p.lineWidth = 9f;
            p.lineJoin = LineJoin.Round;
            p.BeginPath();
            p.MoveTo(new Vector2(w * 0.3f, 0));
            p.LineTo(new Vector2(w * 0.7f, 0));
            p.LineTo(new Vector2(w * 0.7f, h * 0.45f));
            p.LineTo(new Vector2(w * 0.95f, h * 0.45f));
            p.LineTo(new Vector2(w * 0.5f, h * 0.95f));
            p.LineTo(new Vector2(w * 0.05f, h * 0.45f));
            p.LineTo(new Vector2(w * 0.3f, h * 0.45f));
            p.ClosePath();
            p.Fill();
            p.Stroke();
        }
    }
}
