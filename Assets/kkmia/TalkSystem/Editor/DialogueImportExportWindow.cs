using System.IO;
using System.Net;
using UnityEditor;
using UnityEngine;

namespace kkmia.TalkSystem.Editor
{
    public sealed class DialogueImportExportWindow : EditorWindow
    {
        private TextAsset _sourceFile;
        private string _publishedCsvUrl = string.Empty;
        private string _outputPath = "Assets/dialogue_converted.csv";
        private Vector2 _scroll;
        private string _preview = string.Empty;

        [MenuItem("Tools/kkmia/Dialogue Import Export")]
        public static void Open()
        {
            GetWindow<DialogueImportExportWindow>("Dialogue Import Export");
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            _sourceFile = (TextAsset)EditorGUILayout.ObjectField("Source File", _sourceFile, typeof(TextAsset), false);
            _outputPath = EditorGUILayout.TextField("Output Path", _outputPath);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Google Sheets / Published CSV", EditorStyles.boldLabel);
            _publishedCsvUrl = EditorGUILayout.TextField("Published CSV URL", _publishedCsvUrl);
            if (GUILayout.Button("Download CSV URL To Output"))
                DownloadCsv();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Conversions", EditorStyles.boldLabel);
            GUI.enabled = _sourceFile != null;
            if (GUILayout.Button("CSV -> JSON Preview"))
                _preview = DialogueImportExportUtility.CsvToJson(_sourceFile.text);
            if (GUILayout.Button("JSON -> CSV Preview"))
                _preview = DialogueImportExportUtility.JsonToCsv(_sourceFile.text);
            if (GUILayout.Button("Yarn-like Text -> CSV Preview"))
                _preview = DialogueImportExportUtility.YarnLikeToCsv(_sourceFile.text);
            if (GUILayout.Button("Write Preview To Output"))
                DialogueImportExportUtility.WriteTextAsset(_outputPath, _preview);
            GUI.enabled = true;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            _preview = EditorGUILayout.TextArea(_preview, GUILayout.MinHeight(260));
            EditorGUILayout.EndScrollView();
        }

        private void DownloadCsv()
        {
            if (string.IsNullOrWhiteSpace(_publishedCsvUrl)) return;

            using (var client = new WebClient())
            {
                var csv = client.DownloadString(_publishedCsvUrl);
                var report = DialogueValidator.ValidateCsv(csv);
                if (report.HasErrors && !EditorUtility.DisplayDialog("Validation errors", "Downloaded CSV has validation errors. Save anyway?", "Save", "Cancel"))
                    return;

                var outputDirectory = Path.GetDirectoryName(_outputPath);
                if (!string.IsNullOrEmpty(outputDirectory))
                    Directory.CreateDirectory(outputDirectory);

                DialogueImportExportUtility.WriteTextAsset(_outputPath, csv);
            }
        }
    }
}
