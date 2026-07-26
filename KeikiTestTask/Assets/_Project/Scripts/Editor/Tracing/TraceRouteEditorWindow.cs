using System.Collections.Generic;
using Runtime.Domain.Tracing;
using UnityEditor;
using UnityEngine;

namespace Editor.Tracing
{
    public sealed class TraceRouteEditorWindow : EditorWindow
    {
        private enum KnotEditMode
        {
            Linear,
            Bezier
        }

        private const float SceneScale = 10f;
        private const float TraceSurfaceAspect = 0.86f;
        private const float SceneFrameSize = SceneScale * 0.62f;
        private const float PathLineWidth = 4f;
        private const float DirectionArrowSize = 0.18f;
        private const float TangentHandleSize = 0.09f;
        private const int DirectionArrowCount = 8;
        private const int RoundCapSegmentCount = 12;

        private static readonly Color SurfaceBoundsColor = new(0.35f, 0.55f, 1f, 0.45f);
        private static readonly Color InactivePathColor = new(1f, 1f, 1f, 0.45f);

        [SerializeField] private TraceGeometryAsset _geometry;
        [SerializeField] private Sprite _previewSprite;
        [SerializeField, Range(0.05f, 1f)] private float _previewOpacity = 0.35f;
        [SerializeField] private KnotEditMode _knotEditMode = KnotEditMode.Linear;
        [SerializeField] private bool _showPaintedStroke = true;
        [SerializeField] private Color _paintedStrokeColor = new(0.95f, 0.12f, 0.12f, 1f);

        private readonly List<Vector3> _meshVertices = new();
        private readonly List<Vector2> _meshUv = new();
        private readonly List<int> _meshTriangles = new();

        private UnityEditor.Editor _assetEditor;
        private Material _maskedStrokeMaterial;
        private Mesh _paintedStrokeMesh;
        private IReadOnlyList<string> _validationMessages;
        private Vector2 _scrollPosition;
        private int _activeStrokeIndex;

        private bool HasStrokes => _geometry && _geometry.Strokes.Count > 0;

        private TraceStrokeDefinition ActiveStroke => HasStrokes
            ? _geometry.Strokes[Mathf.Clamp(_activeStrokeIndex, 0, _geometry.Strokes.Count - 1)]
            : null;

        [MenuItem("Window/Keiki/Trace Route Editor")]
        private static void Open()
        {
            GetWindow<TraceRouteEditorWindow>("Trace Route Editor");
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += DrawSceneHandles;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DrawSceneHandles;
            DestroyEditorResources();
        }

        private void OnGUI()
        {
            DrawGeometrySelector();
            DrawPreviewSettings();

            if (!_geometry)
            {
                EditorGUILayout.HelpBox(
                    "Select or create a TraceGeometryAsset.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            DrawStrokeSettings();
            DrawToolbar();
            DrawValidationMessages();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Use Scene View handles to move knots and tangents. " +
                "The outlined rectangle represents normalized TraceSurface coordinates " +
                $"with a {TraceSurfaceAspect:0.##}:1 aspect ratio.",
                MessageType.None);

            DrawGeometryInspector();
        }

        private void DrawGeometrySelector()
        {
            EditorGUI.BeginChangeCheck();
            TraceGeometryAsset selected = (TraceGeometryAsset)EditorGUILayout.ObjectField(
                "Geometry",
                _geometry,
                typeof(TraceGeometryAsset),
                false);

            if (!EditorGUI.EndChangeCheck())
                return;

            _geometry = selected;
            _activeStrokeIndex = 0;
            _validationMessages = null;
            DestroyImmediate(_assetEditor);
            _assetEditor = null;
            SceneView.RepaintAll();
        }

        private void DrawPreviewSettings()
        {
            EditorGUI.BeginChangeCheck();
            _previewSprite = (Sprite)EditorGUILayout.ObjectField(
                "Preview silhouette",
                _previewSprite,
                typeof(Sprite),
                false);

            if (_previewSprite)
            {
                _previewOpacity = EditorGUILayout.Slider(
                    "Preview opacity",
                    _previewOpacity,
                    0.05f,
                    1f);
            }

            if (EditorGUI.EndChangeCheck())
                SceneView.RepaintAll();
        }

        private void DrawStrokeSettings()
        {
            if (!HasStrokes)
                return;

            string[] strokeNames = new string[_geometry.Strokes.Count];

            for (int i = 0; i < strokeNames.Length; i++)
                strokeNames[i] = $"{i + 1}. {_geometry.Strokes[i].Id}";

            _activeStrokeIndex = Mathf.Clamp(
                EditorGUILayout.Popup(
                    "Active stroke",
                    _activeStrokeIndex,
                    strokeNames),
                0,
                strokeNames.Length - 1);

            DrawActiveStrokeInsets(ActiveStroke);

            EditorGUILayout.Space();
            _knotEditMode = (KnotEditMode)EditorGUILayout.EnumPopup(
                "Knot edit mode",
                _knotEditMode);

            if (_knotEditMode == KnotEditMode.Linear)
            {
                EditorGUILayout.HelpBox(
                    "Linear mode is active. Moving any knot keeps the active stroke " +
                    "made of straight segments. Select Bezier to edit tangent handles.",
                    MessageType.None);
            }

            _showPaintedStroke = EditorGUILayout.Toggle(
                "Show painted stroke",
                _showPaintedStroke);

            if (_showPaintedStroke)
            {
                _paintedStrokeColor = EditorGUILayout.ColorField(
                    "Painted stroke color",
                    _paintedStrokeColor);
            }
        }

        private void DrawActiveStrokeInsets(TraceStrokeDefinition stroke)
        {
            EditorGUI.BeginChangeCheck();

            bool overrideInsets = EditorGUILayout.Toggle(
                "Override stroke insets",
                stroke.OverrideRouteInsets);
            float firstPointInset = stroke.FirstRoutePointInsetNormalized;
            float starEndInset = stroke.StarEndInsetNormalized;

            using (new EditorGUI.DisabledScope(!overrideInsets))
            {
                firstPointInset = EditorGUILayout.Slider(
                    "Stroke start / mascot inset",
                    firstPointInset,
                    0f,
                    0.5f);
                starEndInset = EditorGUILayout.Slider(
                    "Stroke star end inset",
                    starEndInset,
                    0f,
                    0.5f);
            }

            if (!overrideInsets)
            {
                EditorGUILayout.HelpBox(
                    "This stroke uses the geometry-level route insets.",
                    MessageType.None);
            }

            if (!EditorGUI.EndChangeCheck())
                return;

            Undo.RecordObject(_geometry, "Change trace stroke insets");
            stroke.ConfigureRouteInsets(
                overrideInsets,
                firstPointInset,
                starEndInset);
            MarkGeometryChanged();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Frame Route"))
                    FrameRouteInSceneView();

                if (GUILayout.Button("Bake All"))
                    BakeAll();

                using (new EditorGUI.DisabledScope(!HasStrokes))
                {
                    if (GUILayout.Button("Reverse Active"))
                        ReverseActiveStroke();

                    if (GUILayout.Button("Make Active Linear"))
                        MakeActiveStrokeLinear();
                }

                if (GUILayout.Button("Validate"))
                    _validationMessages = TraceGeometryValidator.Validate(_geometry);
            }
        }

        private void BakeAll()
        {
            TraceGeometryBaker.BakeAsset(_geometry);
            _validationMessages = TraceGeometryValidator.Validate(_geometry);
            AssetDatabase.SaveAssets();
            SceneView.RepaintAll();
        }

        private void ReverseActiveStroke()
        {
            TraceStrokeDefinition stroke = ActiveStroke;

            if (stroke == null)
                return;

            Undo.RecordObject(_geometry, "Reverse trace stroke");
            stroke.Reverse();
            TraceGeometryBaker.BakeStroke(stroke);
            MarkGeometryChanged();
        }

        private void MakeActiveStrokeLinear()
        {
            TraceStrokeDefinition stroke = ActiveStroke;

            if (stroke == null)
                return;

            Undo.RecordObject(_geometry, "Make trace stroke linear");
            SetLinearTangents(stroke);
            TraceGeometryBaker.BakeStroke(stroke);
            MarkGeometryChanged();
        }

        private void DrawValidationMessages()
        {
            if (_validationMessages == null)
                return;

            if (_validationMessages.Count == 0)
            {
                EditorGUILayout.HelpBox("Geometry is valid.", MessageType.Info);
                return;
            }

            foreach (string message in _validationMessages)
                EditorGUILayout.HelpBox(message, MessageType.Warning);
        }

        private void DrawGeometryInspector()
        {
            using EditorGUILayout.ScrollViewScope scroll =
                new(_scrollPosition);
            _scrollPosition = scroll.scrollPosition;

            UnityEditor.Editor.CreateCachedEditor(
                _geometry,
                null,
                ref _assetEditor);

            EditorGUI.BeginChangeCheck();
            _assetEditor.OnInspectorGUI();

            if (!EditorGUI.EndChangeCheck())
                return;

            _validationMessages = null;
            SceneView.RepaintAll();
        }

        private void DrawSceneHandles(SceneView _)
        {
            if (!_geometry)
                return;

            DrawPreviewSprite();
            DrawSurfaceBounds();

            int activeStrokeIndex = Mathf.Clamp(
                _activeStrokeIndex,
                0,
                _geometry.Strokes.Count - 1);

            for (int strokeIndex = 0; strokeIndex < _geometry.Strokes.Count; strokeIndex++)
            {
                TraceStrokeDefinition stroke = _geometry.Strokes[strokeIndex];
                bool isActive = strokeIndex == activeStrokeIndex;

                if (isActive && _showPaintedStroke)
                    DrawPaintedStroke(stroke);

                Handles.color = isActive ? Color.yellow : InactivePathColor;
                DrawBakedPath(stroke);

                if (isActive)
                    DrawKnotHandles(stroke);
            }
        }

        private static void DrawSurfaceBounds()
        {
            Handles.color = SurfaceBoundsColor;
            Handles.DrawWireCube(
                Vector3.zero,
                new Vector3(SceneScale * TraceSurfaceAspect, SceneScale, 0f));
        }

        private void DrawPreviewSprite()
        {
            if (!_previewSprite || !_previewSprite.texture)
                return;

            Vector2 topLeft = HandleUtility.WorldToGUIPoint(
                new Vector3(
                    -SceneScale * TraceSurfaceAspect * 0.5f,
                    SceneScale * 0.5f,
                    0f));
            Vector2 bottomRight = HandleUtility.WorldToGUIPoint(
                new Vector3(
                    SceneScale * TraceSurfaceAspect * 0.5f,
                    -SceneScale * 0.5f,
                    0f));
            Rect previewRect = Rect.MinMaxRect(
                Mathf.Min(topLeft.x, bottomRight.x),
                Mathf.Min(topLeft.y, bottomRight.y),
                Mathf.Max(topLeft.x, bottomRight.x),
                Mathf.Max(topLeft.y, bottomRight.y));

            Rect spriteRect = _previewSprite.rect;
            Texture2D texture = _previewSprite.texture;
            previewRect = FitAspect(
                previewRect,
                spriteRect.width / spriteRect.height);
            Rect textureCoordinates = new(
                spriteRect.x / texture.width,
                spriteRect.y / texture.height,
                spriteRect.width / texture.width,
                spriteRect.height / texture.height);
            Color previousColor = GUI.color;

            Handles.BeginGUI();

            try
            {
                GUI.color = new Color(1f, 1f, 1f, _previewOpacity);
                GUI.DrawTextureWithTexCoords(
                    previewRect,
                    texture,
                    textureCoordinates,
                    true);
            }
            finally
            {
                GUI.color = previousColor;
                Handles.EndGUI();
            }
        }

        private static Rect FitAspect(Rect container, float contentAspect)
        {
            if (container.width <= Mathf.Epsilon ||
                container.height <= Mathf.Epsilon ||
                contentAspect <= Mathf.Epsilon)
            {
                return container;
            }

            float containerAspect = container.width / container.height;

            if (contentAspect > containerAspect)
            {
                float fittedHeight = container.width / contentAspect;
                container.y += (container.height - fittedHeight) * 0.5f;
                container.height = fittedHeight;
            }
            else
            {
                float fittedWidth = container.height * contentAspect;
                container.x += (container.width - fittedWidth) * 0.5f;
                container.width = fittedWidth;
            }

            return container;
        }

        private static void FrameRouteInSceneView()
        {
            SceneView sceneView = SceneView.lastActiveSceneView;

            if (!sceneView)
                return;

            sceneView.in2DMode = true;
            sceneView.LookAt(
                Vector3.zero,
                Quaternion.identity,
                SceneFrameSize,
                true,
                true);
            sceneView.Repaint();
        }

        private static void DrawBakedPath(TraceStrokeDefinition stroke)
        {
            if (stroke.BakedPoints.Count < 2)
                return;

            Vector3[] points = new Vector3[stroke.BakedPoints.Count];

            for (int i = 0; i < points.Length; i++)
                points[i] = ToScene(stroke.BakedPoints[i]);

            Handles.DrawAAPolyLine(PathLineWidth, points);

            int arrowStep = Mathf.Max(1, points.Length / DirectionArrowCount);

            for (int i = arrowStep; i < points.Length; i += arrowStep)
            {
                Vector3 tangent = (points[i] - points[i - 1]).normalized;
                Handles.ConeHandleCap(
                    0,
                    points[i],
                    Quaternion.LookRotation(Vector3.forward, tangent),
                    DirectionArrowSize,
                    EventType.Repaint);
            }
        }

        private void DrawPaintedStroke(
            TraceStrokeDefinition stroke)
        {
            if (stroke.BakedPoints.Count < 2)
                return;

            float halfWidth = SceneScale *
                              TraceSurfaceAspect *
                              _geometry.TrailWidthNormalized *
                              0.5f;

            if (_previewSprite &&
                Event.current.type == EventType.Repaint &&
                DrawMaskedPaintedStroke(stroke, halfWidth))
            {
                return;
            }

            DrawUnmaskedPaintedStroke(stroke, halfWidth);
        }

        private bool DrawMaskedPaintedStroke(
            TraceStrokeDefinition stroke,
            float halfWidth)
        {
            if (!EnsureMaskedStrokeResources())
                return false;

            UpdatePaintedStrokeMesh(stroke, halfWidth);

            Rect spriteRect = _previewSprite.rect;
            Texture2D texture = _previewSprite.texture;
            _maskedStrokeMaterial.SetTexture("_MainTex", texture);
            _maskedStrokeMaterial.SetColor("_Color", _paintedStrokeColor);
            _maskedStrokeMaterial.SetVector(
                "_SpriteRect",
                new Vector4(
                    spriteRect.x / texture.width,
                    spriteRect.y / texture.height,
                    spriteRect.width / texture.width,
                    spriteRect.height / texture.height));

            if (!_maskedStrokeMaterial.SetPass(0))
                return false;

            Graphics.DrawMeshNow(_paintedStrokeMesh, Matrix4x4.identity);
            return true;
        }

        private bool EnsureMaskedStrokeResources()
        {
            if (!_maskedStrokeMaterial)
            {
                Shader shader = Shader.Find(
                    "Hidden/Keiki/Trace Stroke Preview Mask");

                if (!shader)
                    return false;

                _maskedStrokeMaterial = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            if (!_paintedStrokeMesh)
            {
                _paintedStrokeMesh = new Mesh
                {
                    name = "Trace Route Preview",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            return true;
        }

        private void UpdatePaintedStrokeMesh(
            TraceStrokeDefinition stroke,
            float halfWidth)
        {
            IReadOnlyList<Vector2> points = stroke.BakedPoints;
            _meshVertices.Clear();
            _meshUv.Clear();
            _meshTriangles.Clear();

            for (int i = 0; i < points.Count; i++)
            {
                Vector3 current = ToScene(points[i]);
                Vector3 previous = ToScene(points[Mathf.Max(0, i - 1)]);
                Vector3 next = ToScene(points[Mathf.Min(points.Count - 1, i + 1)]);
                Vector3 tangent = (next - previous).normalized;
                Vector3 normal = new(-tangent.y, tangent.x, 0f);

                AddMeshVertex(current + normal * halfWidth);
                AddMeshVertex(current - normal * halfWidth);

                if (i == 0)
                    continue;

                int currentLeft = i * 2;
                int currentRight = currentLeft + 1;
                int previousLeft = currentLeft - 2;
                int previousRight = currentLeft - 1;
                _meshTriangles.Add(previousLeft);
                _meshTriangles.Add(currentLeft);
                _meshTriangles.Add(currentRight);
                _meshTriangles.Add(previousLeft);
                _meshTriangles.Add(currentRight);
                _meshTriangles.Add(previousRight);
            }

            AddRoundCap(ToScene(points[0]), halfWidth);
            AddRoundCap(ToScene(points[^1]), halfWidth);

            _paintedStrokeMesh.Clear();
            _paintedStrokeMesh.SetVertices(_meshVertices);
            _paintedStrokeMesh.SetUVs(0, _meshUv);
            _paintedStrokeMesh.SetTriangles(_meshTriangles, 0);
            _paintedStrokeMesh.RecalculateBounds();
        }

        private void AddRoundCap(Vector3 center, float radius)
        {
            int centerIndex = _meshVertices.Count;
            AddMeshVertex(center);

            for (int i = 0; i <= RoundCapSegmentCount; i++)
            {
                float angle = Mathf.PI * 2f * i / RoundCapSegmentCount;
                Vector3 position = center +
                                   new Vector3(
                                       Mathf.Cos(angle),
                                       Mathf.Sin(angle),
                                       0f) *
                                   radius;
                AddMeshVertex(position);

                if (i == 0)
                    continue;

                _meshTriangles.Add(centerIndex);
                _meshTriangles.Add(centerIndex + i);
                _meshTriangles.Add(centerIndex + i + 1);
            }
        }

        private void AddMeshVertex(Vector3 position)
        {
            _meshVertices.Add(position);
            _meshUv.Add(SurfaceToSpriteUv(ToNormalized(position)));
        }

        private Vector2 SurfaceToSpriteUv(Vector2 surfacePosition)
        {
            Rect spriteRect = _previewSprite.rect;
            float spriteAspect = spriteRect.width / spriteRect.height;

            if (spriteAspect > TraceSurfaceAspect)
            {
                float fittedHeight = TraceSurfaceAspect / spriteAspect;
                float verticalInset = (1f - fittedHeight) * 0.5f;

                return new Vector2(
                    surfacePosition.x,
                    (surfacePosition.y - verticalInset) / fittedHeight);
            }

            float fittedWidth = spriteAspect / TraceSurfaceAspect;
            float horizontalInset = (1f - fittedWidth) * 0.5f;

            return new Vector2(
                (surfacePosition.x - horizontalInset) / fittedWidth,
                surfacePosition.y);
        }

        private void DrawUnmaskedPaintedStroke(
            TraceStrokeDefinition stroke,
            float halfWidth)
        {
            Handles.color = _paintedStrokeColor;

            for (int i = 1; i < stroke.BakedPoints.Count; i++)
            {
                Vector3 from = ToScene(stroke.BakedPoints[i - 1]);
                Vector3 to = ToScene(stroke.BakedPoints[i]);
                Vector3 tangent = (to - from).normalized;
                Vector3 normal = new(-tangent.y, tangent.x, 0f);

                Handles.DrawAAConvexPolygon(
                    from + normal * halfWidth,
                    to + normal * halfWidth,
                    to - normal * halfWidth,
                    from - normal * halfWidth);
            }

            foreach (Vector2 point in stroke.BakedPoints)
            {
                Handles.DrawSolidDisc(
                    ToScene(point),
                    Vector3.forward,
                    halfWidth);
            }
        }

        private void DrawKnotHandles(TraceStrokeDefinition stroke)
        {
            foreach (TraceBezierKnot knot in stroke.Knots)
                DrawKnotHandle(stroke, knot);
        }

        private void DrawKnotHandle(
            TraceStrokeDefinition stroke,
            TraceBezierKnot knot)
        {
            Vector3 position = ToScene(knot.Position);
            EditorGUI.BeginChangeCheck();
            Vector3 newPosition = Handles.PositionHandle(
                position,
                Quaternion.identity);
            bool positionChanged = EditorGUI.EndChangeCheck();

            Vector3 newInPosition = default;
            Vector3 newOutPosition = default;
            bool inTangentChanged = false;
            bool outTangentChanged = false;

            if (_knotEditMode == KnotEditMode.Bezier)
            {
                Vector3 inPosition = ToScene(knot.Position + knot.InTangent);
                Vector3 outPosition = ToScene(knot.Position + knot.OutTangent);
                Color previousColor = Handles.color;
                Handles.color = Color.cyan;
                Handles.DrawLine(position, inPosition);
                Handles.DrawLine(position, outPosition);

                EditorGUI.BeginChangeCheck();
                newInPosition = Handles.FreeMoveHandle(
                    inPosition,
                    TangentHandleSize,
                    Vector3.zero,
                    Handles.DotHandleCap);
                inTangentChanged = EditorGUI.EndChangeCheck();

                EditorGUI.BeginChangeCheck();
                newOutPosition = Handles.FreeMoveHandle(
                    outPosition,
                    TangentHandleSize,
                    Vector3.zero,
                    Handles.DotHandleCap);
                outTangentChanged = EditorGUI.EndChangeCheck();
                Handles.color = previousColor;
            }

            if (!positionChanged &&
                !inTangentChanged &&
                !outTangentChanged)
            {
                return;
            }

            Undo.RecordObject(_geometry, "Edit trace knot");

            if (positionChanged)
                knot.SetPosition(ToNormalized(newPosition));

            if (_knotEditMode == KnotEditMode.Linear)
            {
                SetLinearTangents(stroke);
            }
            else
            {
                if (inTangentChanged)
                    knot.SetInTangent(ToNormalized(newInPosition) - knot.Position);

                if (outTangentChanged)
                    knot.SetOutTangent(ToNormalized(newOutPosition) - knot.Position);
            }

            TraceGeometryBaker.BakeStroke(stroke);
            MarkGeometryChanged();
        }

        private void MarkGeometryChanged()
        {
            EditorUtility.SetDirty(_geometry);
            _validationMessages = null;
            SceneView.RepaintAll();
        }

        private void DestroyEditorResources()
        {
            DestroyImmediate(_assetEditor);
            DestroyImmediate(_maskedStrokeMaterial);
            DestroyImmediate(_paintedStrokeMesh);
            _assetEditor = null;
            _maskedStrokeMaterial = null;
            _paintedStrokeMesh = null;
        }

        private static void SetLinearTangents(TraceStrokeDefinition stroke)
        {
            foreach (TraceBezierKnot knot in stroke.Knots)
            {
                knot.SetInTangent(Vector2.zero);
                knot.SetOutTangent(Vector2.zero);
            }
        }

        private static Vector3 ToScene(Vector2 normalized)
        {
            return new Vector3(
                (normalized.x - 0.5f) * SceneScale * TraceSurfaceAspect,
                (normalized.y - 0.5f) * SceneScale,
                0f);
        }

        private static Vector2 ToNormalized(Vector3 scenePosition)
        {
            return new Vector2(
                scenePosition.x / (SceneScale * TraceSurfaceAspect) + 0.5f,
                scenePosition.y / SceneScale + 0.5f);
        }
    }
}
