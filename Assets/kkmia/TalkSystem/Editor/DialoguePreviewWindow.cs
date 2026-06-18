using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace kkmia.TalkSystem.Editor
{
    public sealed class DialoguePreviewWindow : EditorWindow
    {
        private TextAsset _csvFile;
        private int _startId = 1;
        private DialogueRepository _repository;
        private DialogueSession _session;
        private Vector2 _scroll;

        [MenuItem("Tools/kkmia/Dialogue Preview")]
        public static void Open()
        {
            GetWindow<DialoguePreviewWindow>("Dialogue Preview");
        }

        public static void Open(TextAsset csvFile)
        {
            var window = GetWindow<DialoguePreviewWindow>("Dialogue Preview");
            window._csvFile = csvFile;
            window.Reload();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            _csvFile = (TextAsset)EditorGUILayout.ObjectField("CSV File", _csvFile, typeof(TextAsset), false);
            if (EditorGUI.EndChangeCheck())
                Reload();

            EditorGUILayout.BeginHorizontal();
            _startId = EditorGUILayout.IntField("Start ID", _startId);
            if (GUILayout.Button("Reload"))
                Reload();
            if (GUILayout.Button("Start"))
                StartPreview();
            EditorGUILayout.EndHorizontal();

            if (_session == null)
                return;

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawCurrentLine();
            EditorGUILayout.EndScrollView();
        }

        private void Reload()
        {
            _repository = _csvFile != null ? new DialogueRepository(_csvFile) : null;
            _session = _repository != null ? new DialogueSession(_repository) : null;
            Repaint();
        }

        private void StartPreview()
        {
            if (_session == null)
                Reload();

            if (_session != null)
            {
                _session.Start(_startId);
                _session.MarkLineReady();
            }
        }

        private void DrawCurrentLine()
        {
            var data = _session.CurrentData;
            if (data == null)
            {
                EditorGUILayout.HelpBox("No active dialogue line.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("State", _session.State.ToString());
            EditorGUILayout.LabelField("ID", data.Id.ToString());
            EditorGUILayout.LabelField("Speaker", data.Speaker);
            EditorGUILayout.LabelField("Text", data.Text, EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("NextId", data.NextId.ToString());

            var choices = _session.CurrentChoices.ToList();
            if (choices.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Choices", EditorStyles.boldLabel);
                for (var i = 0; i < choices.Count; i++)
                {
                    if (GUILayout.Button(choices[i].ToString()))
                    {
                        _session.SelectChoice(i);
                        _session.MarkLineReady();
                    }
                }
            }
            else if (GUILayout.Button(data.NextId >= 0 ? "Next" : "End"))
            {
                _session.Advance();
                _session.MarkLineReady();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Seen", string.Join(", ", _session.SeenLineIds.Select(id => id.ToString()).ToArray()));
        }
    }
}
