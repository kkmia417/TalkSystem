using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace kkmia.TalkSystem.Editor
{
    public sealed class DialogueGraphEditorWindow : EditorWindow
    {
        private TextAsset _csvFile;
        private DialogueGraphModel _model = new DialogueGraphModel();
        private DialogueGraphNode _selected;
        private Vector2 _graphScroll;
        private Vector2 _inspectorScroll;
        private string _search = string.Empty;
        private bool _showOnlyProblems;
        private float _zoom = 1f;

        [MenuItem("Tools/kkmia/Dialogue Graph Editor")]
        public static void Open()
        {
            GetWindow<DialogueGraphEditorWindow>("Dialogue Graph");
        }

        private void OnGUI()
        {
            DrawToolbar();

            EditorGUILayout.BeginHorizontal();
            DrawGraphPanel();
            DrawInspectorPanel();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _csvFile = (TextAsset)EditorGUILayout.ObjectField(_csvFile, typeof(TextAsset), false, GUILayout.Width(220));

            if (GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(60)))
                Load();

            GUI.enabled = _csvFile != null && _model.Nodes.Count > 0;
            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(60)))
                Save();
            GUI.enabled = true;

            if (GUILayout.Button("Add Node", EditorStyles.toolbarButton, GUILayout.Width(80)))
                AddNode();

            _search = GUILayout.TextField(_search, GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarTextField, GUILayout.MinWidth(160));
            _showOnlyProblems = GUILayout.Toggle(_showOnlyProblems, "Problems", EditorStyles.toolbarButton, GUILayout.Width(80));
            GUILayout.Label("Zoom", GUILayout.Width(36));
            _zoom = GUILayout.HorizontalSlider(_zoom, 0.6f, 1.4f, GUILayout.Width(90));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawGraphPanel()
        {
            var panelRect = GUILayoutUtility.GetRect(position.width * 0.68f, position.height - 24, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUI.Box(panelRect, GUIContent.none);

            var contentRect = new Rect(0, 0, 1400, 1400);
            _graphScroll = GUI.BeginScrollView(panelRect, _graphScroll, contentRect);

            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(_zoom, _zoom, 1f));

            DrawEdges();
            BeginWindows();
            foreach (var node in _model.Nodes.Where(IsVisible))
                node.rect = GUI.Window(node.id, node.rect, DrawNodeWindow, BuildNodeTitle(node));
            EndWindows();

            GUI.matrix = oldMatrix;
            GUI.EndScrollView();
        }

        private void DrawEdges()
        {
            Handles.BeginGUI();
            foreach (var edge in _model.Edges)
            {
                var from = _model.Find(edge.FromId);
                var to = _model.Find(edge.ToId);
                if (from == null) continue;

                var start = new Vector3(from.rect.xMax, from.rect.center.y, 0);
                var end = to != null ? new Vector3(to.rect.xMin, to.rect.center.y, 0) : new Vector3(from.rect.xMax + 120, from.rect.center.y + 60, 0);
                var color = edge.IsBroken ? Color.red : edge.IsChoice ? new Color(0.1f, 0.5f, 1f) : Color.white;
                Handles.DrawBezier(start, end, start + Vector3.right * 70, end + Vector3.left * 70, color, null, edge.IsChoice ? 3f : 2f);

                var labelPos = Vector3.Lerp(start, end, 0.5f);
                GUI.color = color;
                GUI.Label(new Rect(labelPos.x - 45, labelPos.y - 14, 120, 20), edge.Label);
                GUI.color = Color.white;
            }
            Handles.EndGUI();
        }

        private void DrawNodeWindow(int id)
        {
            var node = _model.Find(id);
            if (node == null) return;

            if (GUILayout.Button("Select"))
                _selected = node;

            EditorGUILayout.LabelField("Speaker", Trim(node.speaker), EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Text", Trim(node.text), EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("Next", node.nextId.ToString(), EditorStyles.miniLabel);
            if (!string.IsNullOrEmpty(node.choicesRaw))
                EditorGUILayout.LabelField("Choices", Trim(node.choicesRaw), EditorStyles.wordWrappedMiniLabel);

            GUI.DragWindow();
        }

        private void DrawInspectorPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(360), GUILayout.ExpandHeight(true));
            EditorGUILayout.LabelField("Inspector", EditorStyles.boldLabel);
            DrawDiagnostics();

            _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);
            if (_selected == null)
            {
                EditorGUILayout.HelpBox("Select a node to edit it.", MessageType.Info);
            }
            else
            {
                DrawSelectedNode();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawDiagnostics()
        {
            var errors = _model.Diagnostics.Messages.Count(m => m.Severity == DialogueValidationSeverity.Error);
            var warnings = _model.Diagnostics.Messages.Count(m => m.Severity == DialogueValidationSeverity.Warning);
            EditorGUILayout.HelpBox("Errors: " + errors + "  Warnings: " + warnings, errors > 0 ? MessageType.Error : MessageType.Info);
        }

        private void DrawSelectedNode()
        {
            EditorGUI.BeginChangeCheck();
            _selected.id = EditorGUILayout.IntField("Id", _selected.id);
            _selected.speaker = EditorGUILayout.TextField("Speaker", _selected.speaker);
            EditorGUILayout.LabelField("Text");
            _selected.text = EditorGUILayout.TextArea(_selected.text, GUILayout.MinHeight(72));
            _selected.nextId = EditorGUILayout.IntField("NextId", _selected.nextId);
            _selected.emotionKey = EditorGUILayout.TextField("EmotionKey", _selected.emotionKey);
            _selected.triggerKey = EditorGUILayout.TextField("TriggerKey", _selected.triggerKey);
            _selected.conditionKey = EditorGUILayout.TextField("ConditionKey", _selected.conditionKey);
            _selected.eventKey = EditorGUILayout.TextField("EventKey", _selected.eventKey);
            _selected.choicesRaw = EditorGUILayout.TextField("Choices", _selected.choicesRaw);
            _selected.autoNextSeconds = EditorGUILayout.FloatField("AutoNextSeconds", _selected.autoNextSeconds);

            if (EditorGUI.EndChangeCheck())
                DialogueGraphMapper.RebuildEdges(_model);

            EditorGUILayout.Space();
            if (GUILayout.Button("Delete Node"))
            {
                var nodes = (System.Collections.Generic.List<DialogueGraphNode>)_model.Nodes;
                nodes.Remove(_selected);
                _selected = null;
                DialogueGraphMapper.RebuildEdges(_model);
            }
        }

        private void Load()
        {
            if (_csvFile == null) return;
            _model = DialogueGraphMapper.FromCsv(_csvFile.text);
            _selected = _model.Nodes.FirstOrDefault();
        }

        private void Save()
        {
            if (_csvFile == null) return;
            var path = AssetDatabase.GetAssetPath(_csvFile);
            File.WriteAllText(path, DialogueGraphMapper.ToCsv(_model), System.Text.Encoding.UTF8);
            AssetDatabase.Refresh();
        }

        private void AddNode()
        {
            var node = new DialogueGraphNode
            {
                id = _model.NextAvailableId(),
                speaker = "Speaker",
                text = "New dialogue line",
                nextId = -1,
                rect = new Rect(80, 80, 260, 170)
            };

            ((System.Collections.Generic.List<DialogueGraphNode>)_model.Nodes).Add(node);
            _selected = node;
            DialogueGraphMapper.RebuildEdges(_model);
        }

        private bool IsVisible(DialogueGraphNode node)
        {
            if (_showOnlyProblems)
            {
                var hasProblem = _model.Edges.Any(e => e.IsBroken && e.FromId == node.id) ||
                                 _model.Diagnostics.Messages.Any(m => m.Message.Contains(node.id.ToString()));
                if (!hasProblem) return false;
            }

            if (string.IsNullOrWhiteSpace(_search))
                return true;

            var haystack = (node.id + " " + node.speaker + " " + node.text + " " + node.triggerKey + " " + node.conditionKey + " " + node.eventKey).ToLowerInvariant();
            return haystack.Contains(_search.ToLowerInvariant());
        }

        private string BuildNodeTitle(DialogueGraphNode node)
        {
            var marker = node == _selected ? "* " : string.Empty;
            return marker + "#" + node.id + " " + node.speaker;
        }

        private static string Trim(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= 48 ? value : value.Substring(0, 45) + "...";
        }
    }
}
