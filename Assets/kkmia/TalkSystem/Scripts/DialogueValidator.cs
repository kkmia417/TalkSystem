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

                ValidateChoiceSyntax(row, report);
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

        private static void ValidateChoiceSyntax(DialogueData row, DialogueValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(row.ChoicesRaw))
                return;

            foreach (var entry in row.ChoicesRaw.Split('|'))
            {
                if (string.IsNullOrWhiteSpace(entry))
                    continue;

                DialogueChoice parsed;
                if (!DialogueChoice.TryParseEntry(entry, out parsed))
                    report.Add(DialogueValidationSeverity.Warning, row.RowNumber, DialogueSchema.Choices,
                        "Choice entry could not be parsed and was ignored: \"" + entry.Trim() + "\".");
            }
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

        private const int ColorVisiting = 1;
        private const int ColorDone = 2;

        // 反復DFS（三色マーキング）による循環検出。
        // 再帰だと長大なチェーンで StackOverflow になり得るため明示スタックで実装する。
        private static void DetectCycles(List<DialogueData> rows, Dictionary<int, DialogueData> byId, DialogueValidationReport report)
        {
            var color = new Dictionary<int, int>();
            var reported = new HashSet<int>();
            var stack = new Stack<int>();

            foreach (var startRow in rows)
            {
                var startId = startRow.Id;
                int startColor;
                if (color.TryGetValue(startId, out startColor) && startColor == ColorDone)
                    continue;

                stack.Push(startId);
                while (stack.Count > 0)
                {
                    var id = stack.Peek();
                    int state;
                    color.TryGetValue(id, out state);

                    if (state == ColorVisiting)
                    {
                        // 子の探索が完了したノード。完了（黒）にしてスタックから外す。
                        color[id] = ColorDone;
                        stack.Pop();
                        continue;
                    }

                    if (state == ColorDone)
                    {
                        stack.Pop();
                        continue;
                    }

                    // 未訪問（白）。訪問中（灰）にして後続を積む。
                    color[id] = ColorVisiting;

                    DialogueData row;
                    if (!byId.TryGetValue(id, out row))
                        continue;

                    PushSuccessor(stack, color, reported, byId, report, row.NextId);
                    foreach (var choice in row.GetChoices())
                        PushSuccessor(stack, color, reported, byId, report, choice.NextId);
                }
            }
        }

        private static void PushSuccessor(Stack<int> stack, Dictionary<int, int> color, HashSet<int> reported,
            Dictionary<int, DialogueData> byId, DialogueValidationReport report, int nextId)
        {
            if (nextId < 0) return;

            int nextColor;
            color.TryGetValue(nextId, out nextColor);

            if (nextColor == ColorVisiting)
            {
                // 訪問中ノードへの後退辺 = 循環。ノードごとに一度だけ報告する。
                if (reported.Add(nextId))
                {
                    DialogueData cycleRow;
                    if (byId.TryGetValue(nextId, out cycleRow))
                        report.Add(DialogueValidationSeverity.Info, cycleRow.RowNumber, DialogueSchema.NextId, "Cycle detected. Cycles are allowed but should be intentional.");
                }

                return;
            }

            if (nextColor == 0)
                stack.Push(nextId);
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
