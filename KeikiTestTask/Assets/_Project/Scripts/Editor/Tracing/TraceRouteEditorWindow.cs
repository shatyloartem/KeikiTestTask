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

        [SerializeField] private TraceGeometryAsset _geometry;
        [SerializeField] private Sprite _previewSprite;
        [SerializeField, Range(0.05f, 1f)] private float _previewOpacity = 0.35f;
        [SerializeField] private KnotEditMode _knotEditMode = KnotEditMode.Linear;
        [SerializeField] private bool _showPaintedStroke = true;
        [SerializeField] private Color _paintedStrokeColor = new(0.95f, 0.12f, 0.12f, 1f);
        private UnityEditor.Editor _assetEditor;
        private Material _maskedStrokeMaterial;
        private int _activeStrokeIndex;
        private Vector2 _scrollPosition;
        private IReadOnlyList<string> _validationMessages;

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
            DestroyImmediate(_assetEditor);
            DestroyImmediate(_maskedStrokeMaterial);
        }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            TraceGeometryAsset selected = (TraceGeometryAsset)EditorGUILayout.ObjectField(
                "Geometry",
                _geometry,
                typeof(TraceGeometryAsset),
                false);

            if (EditorGUI.EndChangeCheck())
            {
                _geometry = selected;
                _activeStrokeIndex = 0;
                _validationMessages = null;
                DestroyImmediate(_assetEditor);
                _assetEditor = null;
                SceneView.RepaintAll();
            }

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

            if (!_geometry)
            {
                EditorGUILayout.HelpBox(
                    "Select or create a TraceGeometryAsset.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.Space();

            if (_geometry.Strokes.Count > 0)
            {
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

                DrawActiveStrokeInsets(
                    _geometry.Strokes[_activeStrokeIndex]);

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

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Frame Route"))
                    FrameRouteInSceneView();

                if (GUILayout.Button("Bake All"))
                {
                    TraceGeometryBaker.BakeAsset(_geometry);
                    _validationMessages = TraceGeometryValidator.Validate(_geometry);
                    AssetDatabase.SaveAssets();
                    SceneView.RepaintAll();
                }

                using (new EditorGUI.DisabledScope(_geometry.Strokes.Count == 0))
                {
                    if (GUILayout.Button("Reverse Active"))
                    {
                        Undo.RecordObject(_geometry, "Reverse trace stroke");
                        _geometry.Strokes[_activeStrokeIndex].Reverse();
                        TraceGeometryBaker.BakeStroke(_geometry.Strokes[_activeStrokeIndex]);
                        EditorUtility.SetDirty(_geometry);
                        SceneView.RepaintAll();
                    }

                    if (GUILayout.Button("Make Active Linear"))
                        MakeActiveStrokeLinear();
                }

                if (GUILayout.Button("Validate"))
                    _validationMessages = TraceGeometryValidator.Validate(_geometry);
            }

            DrawValidationMessages();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Use Scene View handles to move knots and tangents. " +
                "The outlined rectangle represents normalized TraceSurface coordinates " +
                $"with a {TraceSurfaceAspect:0.##}:1 aspect ratio.",
                MessageType.None);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            UnityEditor.Editor.CreateCachedEditor(
                _geometry,
                null,
                ref _assetEditor);
            _assetEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();

            if (GUI.changed)
                SceneView.RepaintAll();
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
            EditorUtility.SetDirty(_geometry);
            _validationMessages = null;
            SceneView.RepaintAll();
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

        private void DrawSceneHandles(SceneView sceneView)
        {
            if (!_geometry)
                return;

            DrawPreviewSprite();

            Handles.color = new Color(0.35f, 0.55f, 1f, 0.45f);
            Handles.DrawWireCube(
                Vector3.zero,
                new Vector3(SceneScale * TraceSurfaceAspect, SceneScale, 0f));

            for (int strokeIndex = 0;
                 strokeIndex < _geometry.Strokes.Count;
                 strokeIndex++)
            {
                TraceStrokeDefinition stroke = _geometry.Strokes[strokeIndex];
                bool isActive = strokeIndex == _activeStrokeIndex;

                if (isActive && _showPaintedStroke)
                    DrawPaintedStroke(stroke);

                Handles.color = strokeIndex == _activeStrokeIndex
                    ? Color.yellow
                    : new Color(1f, 1f, 1f, 0.45f);

                DrawBakedPath(stroke);

                if (isActive)
                    DrawKnotHandles(stroke);
            }
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
            GUI.color = new Color(1f, 1f, 1f, _previewOpacity);
            GUI.DrawTextureWithTexCoords(
                previewRect,
                texture,
                textureCoordinates,
                true);
            GUI.color = previousColor;
            Handles.EndGUI();
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
                SceneScale * 0.62f,
                true,
                true);
            sceneView.Repaint();
        }

        private void DrawBakedPath(TraceStrokeDefinition stroke)
        {
            if (stroke.BakedPoints.Count < 2)
                return;

            Vector3[] points = new Vector3[stroke.BakedPoints.Count];

            for (int i = 0; i < points.Length; i++)
                points[i] = ToScene(stroke.BakedPoints[i]);

            Handles.DrawAAPolyLine(4f, points);

            int arrowStep = Mathf.Max(1, points.Length / 8);

            for (int i = arrowStep; i < points.Length; i += arrowStep)
            {
                Vector3 tangent = (points[i] - points[i - 1]).normalized;
                Handles.ConeHandleCap(
                    0,
                    points[i],
                    Quaternion.LookRotation(Vector3.forward, tangent),
                    0.18f,
                    EventType.Repaint);
            }
        }

        private void DrawPaintedStroke(TraceStrokeDefinition stroke)
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

            Mesh mesh = BuildPaintedStrokeMesh(stroke, halfWidth);
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

            bool passSet = _maskedStrokeMaterial.SetPass(0);

            if (passSet)
                Graphics.DrawMeshNow(mesh, Matrix4x4.identity);

            DestroyImmediate(mesh);
            return passSet;
        }

        private Mesh BuildPaintedStrokeMesh(
            TraceStrokeDefinition stroke,
            float halfWidth)
        {
            IReadOnlyList<Vector2> points = stroke.BakedPoints;
            List<Vector3> vertices = new(points.Count * 2 + 28);
            List<Vector2> textureCoordinates = new(points.Count * 2 + 28);
            List<int> triangles = new((points.Count - 1) * 6 + 72);

            for (int i = 0; i < points.Count; i++)
            {
                Vector3 current = ToScene(points[i]);
                Vector3 previous = ToScene(points[Mathf.Max(0, i - 1)]);
                Vector3 next = ToScene(points[Mathf.Min(points.Count - 1, i + 1)]);
                Vector3 tangent = (next - previous).normalized;
                Vector3 normal = new(-tangent.y, tangent.x, 0f);

                AddMaskedVertex(
                    vertices,
                    textureCoordinates,
                    current + normal * halfWidth);
                AddMaskedVertex(
                    vertices,
                    textureCoordinates,
                    current - normal * halfWidth);

                if (i == 0)
                    continue;

                int currentLeft = i * 2;
                int currentRight = currentLeft + 1;
                int previousLeft = currentLeft - 2;
                int previousRight = currentLeft - 1;
                triangles.Add(previousLeft);
                triangles.Add(currentLeft);
                triangles.Add(currentRight);
                triangles.Add(previousLeft);
                triangles.Add(currentRight);
                triangles.Add(previousRight);
            }

            AddRoundCap(
                vertices,
                textureCoordinates,
                triangles,
                ToScene(points[0]),
                halfWidth);
            AddRoundCap(
                vertices,
                textureCoordinates,
                triangles,
                ToScene(points[^1]),
                halfWidth);

            Mesh mesh = new()
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, textureCoordinates);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private void AddRoundCap(
            List<Vector3> vertices,
            List<Vector2> textureCoordinates,
            List<int> triangles,
            Vector3 center,
            float radius)
        {
            const int segmentCount = 12;
            int centerIndex = vertices.Count;
            AddMaskedVertex(vertices, textureCoordinates, center);

            for (int i = 0; i <= segmentCount; i++)
            {
                float angle = Mathf.PI * 2f * i / segmentCount;
                Vector3 position = center +
                                   new Vector3(
                                       Mathf.Cos(angle),
                                       Mathf.Sin(angle),
                                       0f) *
                                   radius;
                AddMaskedVertex(vertices, textureCoordinates, position);

                if (i == 0)
                    continue;

                triangles.Add(centerIndex);
                triangles.Add(centerIndex + i);
                triangles.Add(centerIndex + i + 1);
            }
        }

        private void AddMaskedVertex(
            List<Vector3> vertices,
            List<Vector2> textureCoordinates,
            Vector3 position)
        {
            vertices.Add(position);
            textureCoordinates.Add(
                SurfaceToSpriteUv(ToNormalized(position)));
        }

        private Vector2 SurfaceToSpriteUv(Vector2 surfacePosition)
        {
            Rect spriteRect = _previewSprite.rect;
            float spriteAspect = spriteRect.width / spriteRect.height;

            if (spriteAspect > TraceSurfaceAspect)
            {
                float fittedHeight = TraceSurfaceAspect / spriteAspect;
                float inset = (1f - fittedHeight) * 0.5f;

                return new Vector2(
                    surfacePosition.x,
                    (surfacePosition.y - inset) / fittedHeight);
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
            {
                Vector3 position = ToScene(knot.Position);
                EditorGUI.BeginChangeCheck();
                Vector3 newPosition = Handles.PositionHandle(position, Quaternion.identity);
                bool positionChanged = EditorGUI.EndChangeCheck();

                Vector3 newInPosition = default;
                Vector3 newOutPosition = default;
                bool inTangentChanged = false;
                bool outTangentChanged = false;

                if (_knotEditMode == KnotEditMode.Bezier)
                {
                    Vector3 inPosition = ToScene(knot.Position + knot.InTangent);
                    Vector3 outPosition = ToScene(knot.Position + knot.OutTangent);

                    Handles.color = Color.cyan;
                    Handles.DrawLine(position, inPosition);
                    Handles.DrawLine(position, outPosition);

                    EditorGUI.BeginChangeCheck();
                    newInPosition = Handles.FreeMoveHandle(
                        inPosition,
                        0.09f,
                        Vector3.zero,
                        Handles.DotHandleCap);
                    inTangentChanged = EditorGUI.EndChangeCheck();

                    EditorGUI.BeginChangeCheck();
                    newOutPosition = Handles.FreeMoveHandle(
                        outPosition,
                        0.09f,
                        Vector3.zero,
                        Handles.DotHandleCap);
                    outTangentChanged = EditorGUI.EndChangeCheck();
                }

                if (!positionChanged &&
                    !inTangentChanged &&
                    !outTangentChanged)
                {
                    continue;
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
                    {
                        knot.SetInTangent(
                            ToNormalized(newInPosition) - knot.Position);
                    }

                    if (outTangentChanged)
                    {
                        knot.SetOutTangent(
                            ToNormalized(newOutPosition) - knot.Position);
                    }
                }

                TraceGeometryBaker.BakeStroke(stroke);
                EditorUtility.SetDirty(_geometry);
            }
        }

        private void MakeActiveStrokeLinear()
        {
            if (!_geometry ||
                _geometry.Strokes.Count == 0)
            {
                return;
            }

            Undo.RecordObject(_geometry, "Make trace stroke linear");
            TraceStrokeDefinition stroke = _geometry.Strokes[_activeStrokeIndex];
            SetLinearTangents(stroke);
            TraceGeometryBaker.BakeStroke(stroke);
            EditorUtility.SetDirty(_geometry);
            SceneView.RepaintAll();
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
