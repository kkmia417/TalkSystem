using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace kkmia.TalkSystem
{
    public static class DialogueValidator
    {
        public static DialogueValidationReport ValidateCsv(string csvText, IEnumerable<int> entryIds = null)
        {
            var document = DialogueCsvCodec.Parse(csvText);
            var report = new DialogueValidationReport();
            report.AddRange(document.Diagnostics.Messages);

            ValidateHeaders(document.Headers, report);

            var rows = CsvLoader.ParseRows<DialogueData>(document, report);
            ValidateData(rows, report, entryIds);
            return report;
        }

        public static DialogueValidationReport ValidateData(IEnumerable<DialogueData> data, IEnumerable<int> entryIds = null)
        {
            var report = new DialogueValidationReport();
            ValidateData(data, report, entryIds);
            return report;
        }

        private static void ValidateHeaders(IReadOnlyList<string> headers, DialogueValidationReport report)
        {
            if (headers == null || headers.Count == 0)
            {
                report.Add(DialogueValidationSeverity.Error, 1, string.Empty, "CSV header row is missing.");
                return;
            }

            foreach (var required in DialogueSchema.DefaultHeaders)
            {
                if (!headers.Any(h => h == required))
                    report.Add(DialogueValidationSeverity.Error, 1, required, "Required header is missing.");
            }
        }

        private static void ValidateData(IEnumerable<DialogueData> data, DialogueValidationReport report, IEnumerable<int> entryIds)
        {
            var rows = data != null ? data.ToList() : new List<DialogueData>();
            var byId = new Dictionary<int, DialogueData>();
            var seen = new Dictionary<int, int>();
            var triggerKeys = new Dictionary<string, int>();

            foreach (var row in rows)
            {
                if (row.Id <= 0)
                    report.Add(DialogueValidationSeverity.Error, row.RowNumber, DialogueSchema.Id, "Id must be a positive integer.");

                if (seen.ContainsKey(row.Id))
                    report.Add(DialogueValidationSeverity.Error, row.RowNumber, DialogueSchema.Id, "Duplicate Id also appears at row " + seen[row.Id] + ".");
                else
                    seen[row.Id] = row.RowNumber;

                if (!byId.ContainsKey(row.Id))
                    byId.Add(row.Id, row);

                if (string.IsNullOrWhiteSpace(row.Speaker))
                    report.Add(DialogueValidationSeverity.Warning, row.RowNumber, DialogueSchema.Speaker, "Speaker is empty.");

                if (string.IsNullOrWhiteSpace(row.Text))
                    report.Add(DialogueValidationSeverity.Warning, row.RowNumber, DialogueSchema.Text, "Text is empty.");

                if (!string.IsNullOrEmpty(row.TriggerKey))
                {
                    if (triggerKeys.ContainsKey(row.TriggerKey))
                        report.Add(DialogueValidationSeverity.Warning, row.RowNumber, DialogueSchema.TriggerKey, "TriggerKey is duplicated with row " + triggerKeys[row.TriggerKey] + ".");
                    else
                        triggerKeys.Add(row.TriggerKey, row.RowNumber);
                }
            }

            foreach (var row in rows)
            {
                if (row.NextId >= 0 && !byId.ContainsKey(row.NextId))
                    report.Add(DialogueValidationSeverity.Error, row.RowNumber, DialogueSchema.NextId, "NextId " + row.NextId + " does not exist.");

                var choices = row.GetChoices();
                foreach (var choice in choices)
                {
                    if (choice.NextId >= 0 && !byId.ContainsKey(choice.NextId))
                        report.Add(DialogueValidationSeverity.Error, row.RowNumber, DialogueSchema.Choices, "Choice target " + choice.NextId + " does not exist.");
                }
            }

            ValidateReachability(rows, byId, report, entryIds);
            DetectCycles(rows, byId, report);
        }

        private static void ValidateReachability(List<DialogueData> rows, Dictionary<int, DialogueData> byId, DialogueValidationReport report, IEnumerable<int> entryIds)
        {
            var starts = entryIds != null ? entryIds.Where(byId.ContainsKey).ToList() : rows.Where(r => r.HasTriggerKey).Select(r => r.Id).ToList();
            if (starts.Count == 0 && rows.Count > 0)
                starts.Add(rows[0].Id);

            var reachable = new HashSet<int>();
            var stack = new Stack<int>(starts);
            while (stack.Count > 0)
            {
                var id = stack.Pop();
                if (!reachable.Add(id)) continue;

                DialogueData row;
                if (!byId.TryGetValue(id, out row)) continue;

                if (row.NextId >= 0)
                    stack.Push(row.NextId);

                foreach (var choice in row.GetChoices())
                    if (choice.NextId >= 0)
                        stack.Push(choice.NextId);
            }

            foreach (var row in rows)
            {
                if (!reachable.Contains(row.Id))
                    report.Add(DialogueValidationSeverity.Warning, row.RowNumber, DialogueSchema.Id, "Row is unreachable from configured entry points.");
            }
        }

        private static void DetectCycles(List<DialogueData> rows, Dictionary<int, DialogueData> byId, DialogueValidationReport report)
        {
            var visiting = new HashSet<int>();
            var visited = new HashSet<int>();

            foreach (var row in rows)
                Visit(row.Id, byId, visiting, visited, report);
        }

        private static void Visit(int id, Dictionary<int, DialogueData> byId, HashSet<int> visiting, HashSet<int> visited, DialogueValidationReport report)
        {
            if (visited.Contains(id)) return;
            if (visiting.Contains(id))
            {
                DialogueData cycleRow;
                if (byId.TryGetValue(id, out cycleRow))
                    report.Add(DialogueValidationSeverity.Info, cycleRow.RowNumber, DialogueSchema.NextId, "Cycle detected. Cycles are allowed but should be intentional.");
                return;
            }

            DialogueData row;
            if (!byId.TryGetValue(id, out row)) return;

            visiting.Add(id);
            if (row.NextId >= 0)
                Visit(row.NextId, byId, visiting, visited, report);

            foreach (var choice in row.GetChoices())
                if (choice.NextId >= 0)
                    Visit(choice.NextId, byId, visiting, visited, report);

            visiting.Remove(id);
            visited.Add(id);
        }

        public static DialogueValidationReport ValidateCharacters(IEnumerable<DialogueData> data, CharacterExpressionDatabase database)
        {
            var report = new DialogueValidationReport();
            if (database == null || data == null) return report;

            foreach (var row in data)
            {
                CharacterDefinition character;
                if (!database.TryGetCharacter(row.Speaker, out character))
                {
                    report.Add(DialogueValidationSeverity.Warning, row.RowNumber, DialogueSchema.Speaker, "Speaker is not found in the character expression database.");
                    continue;
                }

                Sprite sprite;
                if (!string.IsNullOrEmpty(row.EmotionKey) && !character.TryGetSprite(row.EmotionKey, out sprite))
                    report.Add(DialogueValidationSeverity.Warning, row.RowNumber, DialogueSchema.EmotionKey, "EmotionKey is not found in the character expression database.");
            }

            return report;
        }
    }
}
