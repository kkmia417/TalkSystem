using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;


namespace kkmia.TalkSystem.Editor
{
    /// <summary>
    /// CSVベースの会話データをGUIで編集できるUnityエディター拡張ウィンドウ。
    /// ソート・フィルタ・Undo/Redo・保存機能を提供。
    /// </summary>
    public class DialogueCSVEditorWindow : EditorWindow
    {
        private TextAsset _csvFile;
        private List<string[]> _csvData = new List<string[]>();
        private Stack<List<string[]>> _undoStack = new Stack<List<string[]>>();
        private Stack<List<string[]>> _redoStack = new Stack<List<string[]>>();
        private Vector2 _scrollPos;

        private string[] _headers;
        private int _sortColumn = 0;
        private bool _ascending = true;
        private string _speakerFilter = "";

        private const float BaseColumnWidth = 80f;
        private int _selectedRow = -1;

        /// <summary>
        /// メニューからウィンドウを開く
        /// </summary>
        [MenuItem("Tools/kkmia/Dialogue CSV Editor")]
        private static void Open()
        {
            GetWindow<DialogueCSVEditorWindow>("Dialogue CSV Editor");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            _csvFile = (TextAsset)EditorGUILayout.ObjectField("CSV File", _csvFile, typeof(TextAsset), false);

            if (_csvFile != null)
            {
                if (GUILayout.Button("Load CSV"))
                {
                    LoadCSV();
                }

                if (_csvData.Count > 0)
                {
                    EditorGUILayout.Space();
                    DrawControls();

                    _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

                    EditorGUILayout.BeginVertical();
                    DrawHeaders();
                    DrawRows();
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.EndScrollView();

                    DrawRowControlButtons();
                    DrawUndoRedoButtons();

                    if (GUILayout.Button("Save CSV"))
                    {
                        SaveCSV();
                    }
                }
            }

            HandleKeyboardShortcuts();
        }

        /// <summary>
        /// Ctrl+Z / Ctrl+Y による Undo/Redo をサポート
        /// </summary>
        private void HandleKeyboardShortcuts()
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.control)
            {
                if (e.keyCode == KeyCode.Z && _undoStack.Count > 0)
                {
                    SaveRedo();
                    _csvData = _undoStack.Pop();
                    _selectedRow = -1;
                    Repaint();
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Y && _redoStack.Count > 0)
                {
                    SaveUndo();
                    _csvData = _redoStack.Pop();
                    _selectedRow = -1;
                    Repaint();
                    e.Use();
                }
            }
        }

        /// <summary>
        /// ソートやフィルタのUI
        /// </summary>
        private void DrawControls()
        {
            EditorGUILayout.BeginHorizontal();
            _sortColumn = EditorGUILayout.Popup("Sort by", _sortColumn, _headers);
            _ascending = EditorGUILayout.Toggle("Ascending", _ascending);
            if (GUILayout.Button("Sort"))
            {
                SaveUndo();
                SortData();
            }
            EditorGUILayout.EndHorizontal();

            _speakerFilter = EditorGUILayout.TextField("Filter Speaker", _speakerFilter);
        }

        /// <summary>
        /// ヘッダー行の描画
        /// </summary>
        private void DrawHeaders()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(60);
            foreach (var header in _headers)
            {
                EditorGUILayout.LabelField(header, EditorStyles.boldLabel, GUILayout.Width(GetColumnWidth(header)));
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// CSVデータの各行を描画・編集
        /// </summary>
        private void DrawRows()
        {
            for (int rowIndex = 0; rowIndex < _csvData.Count; rowIndex++)
            {
                var row = _csvData[rowIndex];

                if (!string.IsNullOrEmpty(_speakerFilter) && (row.Length < 2 || !row[1].Contains(_speakerFilter)))
                    continue;

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Toggle(_selectedRow == rowIndex, "Select", GUILayout.Width(60)))
                {
                    _selectedRow = rowIndex;
                }

                for (int i = 0; i < _headers.Length; i++)
                {
                    string current = i < row.Length ? row[i] : "";
                    EditorGUI.BeginChangeCheck();
                    string newValue = EditorGUILayout.TextField(current, GUILayout.Width(GetColumnWidth(_headers[i])));
                    if (EditorGUI.EndChangeCheck())
                    {
                        SaveUndo();
                        if (i >= row.Length)
                        {
                            Array.Resize(ref row, _headers.Length);
                            _csvData[rowIndex] = row;
                        }
                        row[i] = newValue;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// 行の追加・削除ボタン
        /// </summary>
        private void DrawRowControlButtons()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Row"))
            {
                SaveUndo();
                _csvData.Add(new string[_headers.Length]);
            }
            if (GUILayout.Button("Remove Selected Row") && _selectedRow >= 0 && _selectedRow < _csvData.Count)
            {
                SaveUndo();
                _csvData.RemoveAt(_selectedRow);
                _selectedRow = -1;
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Undo / Redo ボタン群
        /// </summary>
        private void DrawUndoRedoButtons()
        {
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = _undoStack.Count > 0;
            if (GUILayout.Button("Undo"))
            {
                SaveRedo();
                _csvData = _undoStack.Pop();
                _selectedRow = -1;
            }

            GUI.enabled = _redoStack.Count > 0;
            if (GUILayout.Button("Redo"))
            {
                SaveUndo();
                _csvData = _redoStack.Pop();
                _selectedRow = -1;
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// CSVを読み込み、内部データとして保持
        /// </summary>
        private void LoadCSV()
        {
            _csvData.Clear();

            var lines = _csvFile.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0) return;

            _headers = lines[0].Split(',');

            for (int i = 1; i < lines.Length; i++)
            {
                var cols = lines[i].Split(',');
                if (cols.Length != _headers.Length)
                {
                    Debug.LogWarning($"[CSV Editor] 行 {i + 1} の列数が一致しません。スキップします。");
                    continue;
                }
                _csvData.Add(cols);
            }
        }

        /// <summary>
        /// 選択中の列で昇順または降順に並び替え
        /// </summary>
        private void SortData()
        {
            if (_sortColumn < 0 || _sortColumn >= _headers.Length) return;

            _csvData = _ascending
                ? _csvData.OrderBy(row => row[_sortColumn]).ToList()
                : _csvData.OrderByDescending(row => row[_sortColumn]).ToList();
        }

        /// <summary>
        /// 編集内容をCSVファイルに保存
        /// </summary>
        private void SaveCSV()
        {
            if (_csvFile == null) return;

            var path = AssetDatabase.GetAssetPath(_csvFile);
            using (var writer = new StreamWriter(path, false))
            {
                writer.WriteLine(string.Join(",", _headers));
                foreach (var row in _csvData)
                {
                    var paddedRow = row.Length == _headers.Length
                        ? row
                        : row.Concat(Enumerable.Repeat("", _headers.Length - row.Length)).ToArray();

                    writer.WriteLine(string.Join(",", paddedRow));
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[DialogueCSVEditor] CSVファイルを保存しました: {path}");
        }

        /// <summary>
        /// 現在の状態をUndoスタックに保存
        /// </summary>
        private void SaveUndo()
        {
            _undoStack.Push(Clone(_csvData));
        }

        /// <summary>
        /// 現在の状態をRedoスタックに保存
        /// </summary>
        private void SaveRedo()
        {
            _redoStack.Push(Clone(_csvData));
        }

        /// <summary>
        /// 文字列配列のListをディープコピー
        /// </summary>
        private List<string[]> Clone(List<string[]> source)
        {
            var copy = new List<string[]>();
            foreach (var row in source)
            {
                copy.Add((string[])row.Clone());
            }
            return copy;
        }

        /// <summary>
        /// 各列に対する適切な幅を算出
        /// </summary>
        private float GetColumnWidth(string header)
        {
            int maxLen = header.Length;

            foreach (var row in _csvData)
            {
                int colIndex = System.Array.IndexOf(_headers, header);
                if (colIndex >= 0 && colIndex < row.Length)
                {
                    int cellLen = row[colIndex]?.Length ?? 0;
                    if (cellLen > maxLen) maxLen = cellLen;
                }
            }

            return Mathf.Max(BaseColumnWidth, maxLen * 10f);
        }
    }
}
