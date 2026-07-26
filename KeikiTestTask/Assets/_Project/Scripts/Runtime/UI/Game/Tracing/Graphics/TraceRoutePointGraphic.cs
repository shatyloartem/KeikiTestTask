using UnityEngine;
using UnityEngine.UI;

namespace Runtime.UI.Game.Tracing.Graphics
{
    public sealed class TraceRoutePointGraphic : MaskableGraphic
    {
        private const int SegmentCount = 24;

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            Rect rect = rectTransform.rect;
            Vector2 center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f;

            AddVertex(vertexHelper, center);

            for (int i = 0; i <= SegmentCount; i++)
            {
                float angle = Mathf.PI * 2f * i / SegmentCount;
                Vector2 position = center +
                                   new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) *
                                   radius;
                AddVertex(vertexHelper, position);

                if (i > 0)
                {
                    vertexHelper.AddTriangle(
                        0,
                        i,
                        i + 1);
                }
            }
        }

        private void AddVertex(VertexHelper vertexHelper, Vector2 position)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = color;
            vertex.uv0 = Vector2.zero;
            vertexHelper.AddVert(vertex);
        }
    }
}
