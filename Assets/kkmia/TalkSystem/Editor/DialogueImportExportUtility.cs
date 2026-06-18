using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace kkmia.TalkSystem.Editor
{
    [Serializable]
    public sealed class DialogueJsonDatabase
    {
        public List<DialogueJsonRow> rows = new List<DialogueJsonRow>();
    }

    [Serializable]
    public sealed class DialogueJsonRow
    {
        public int id;
        public string speaker;
        public string text;
        public int nextId = -1;
        public string emotionKey;
        public string triggerKey;
        public string conditionKey;
        public string eventKey;
        public string choices;
        public float autoNextSeconds = -1f;
    }

    public static class DialogueImportExportUtility
    {
        public static string CsvToJson(string csvText, bool prettyPrint = true)
        {
            var database = new DialogueJsonDatabase();
            foreach (var row in CsvLoader.ParseText<DialogueData>(csvText).Values.OrderBy(d => d.Id))
            {
                database.rows.Add(new DialogueJsonRow
                {
                    id = row.Id,
                    speaker = row.Speaker,
                    text = row.Text,
                    nextId = row.NextId,
                    emotionKey = row.EmotionKey,
                    triggerKey = row.TriggerKey,
                    conditionKey = row.ConditionKey,
                    eventKey = row.EventKey,
                    choices = row.ChoicesRaw,
                    autoNextSeconds = row.AutoNextSeconds
                });
            }

            return JsonUtility.ToJson(database, prettyPrint);
        }

        public static string JsonToCsv(string jsonText)
        {
            var database = JsonUtility.FromJson<DialogueJsonDatabase>(jsonText);
            var rows = database != null && database.rows != null
                ? database.rows.OrderBy(r => r.id).Select(ToCsvRow)
                : Enumerable.Empty<IReadOnlyList<string>>();

            return DialogueCsvCodec.Write(DialogueSchema.ExtendedHeaders, rows);
        }

        public static string YarnLikeToCsv(string scriptText)
        {
            var rows = new List<IReadOnlyList<string>>();
            var id = 1;
            var currentId = 1;
            var nextId = -1;
            var pendingChoices = new List<string>();

            using (var reader = new StringReader(scriptText ?? string.Empty))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var trimmed = line.Trim();
                    if (trimmed.Length == 0 || trimmed.StartsWith("//")) continue;

                    if (trimmed.StartsWith("title:", StringComparison.OrdinalIgnoreCase))
                    {
                        int.TryParse(trimmed.Substring("title:".Length).Trim(), out currentId);
                        id = Math.Max(id, currentId);
                        continue;
                    }

                    if (trimmed.StartsWith("->"))
                    {
                        var target = trimmed.Substring(2).Trim();
                        pendingChoices.Add(target + "->" + ExtractTargetId(target));
                        continue;
                    }

                    var separator = trimmed.IndexOf(':');
                    if (separator <= 0) continue;

                    var speaker = trimmed.Substring(0, separator).Trim();
                    var text = trimmed.Substring(separator + 1).Trim();
                    nextId = pendingChoices.Count > 0 ? -1 : currentId + 1;
                    var choices = pendingChoices.Count > 0 ? string.Join("|", pendingChoices.ToArray()) : string.Empty;
                    rows.Add(new[]
                    {
                        currentId.ToString(),
                        speaker,
                        text,
                        nextId.ToString(),
                        string.Empty,
                        currentId == 1 ? "Start" : string.Empty,
                        string.Empty,
                        string.Empty,
                        choices,
                        string.Empty
                    });

                    pendingChoices.Clear();
                    currentId++;
                    id = Math.Max(id, currentId);
                }
            }

            return DialogueCsvCodec.Write(DialogueSchema.ExtendedHeaders, rows);
        }

        public static void WriteTextAsset(string path, string contents)
        {
            File.WriteAllText(path, contents ?? string.Empty, System.Text.Encoding.UTF8);
            AssetDatabase.Refresh();
        }

        private static IReadOnlyList<string> ToCsvRow(DialogueJsonRow row)
        {
            return new[]
            {
                row.id.ToString(),
                row.speaker ?? string.Empty,
                row.text ?? string.Empty,
                row.nextId >= 0 ? row.nextId.ToString() : "-1",
                row.emotionKey ?? string.Empty,
                row.triggerKey ?? string.Empty,
                row.conditionKey ?? string.Empty,
                row.eventKey ?? string.Empty,
                row.choices ?? string.Empty,
                row.autoNextSeconds >= 0f ? row.autoNextSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty
            };
        }

        private static int ExtractTargetId(string value)
        {
            var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
            int result;
            return int.TryParse(digits, out result) ? result : -1;
        }
    }
}
