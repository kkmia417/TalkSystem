using System;
using System.Collections.Generic;

namespace kkmia.TalkSystem
{
    [Serializable]
    public sealed class DialogueChoice
    {
        public DialogueChoice(string text, int nextId, string conditionKey)
        {
            Text = text ?? string.Empty;
            NextId = nextId;
            ConditionKey = conditionKey ?? string.Empty;
        }

        public string Text { get; private set; }
        public int NextId { get; private set; }
        public string ConditionKey { get; private set; }

        public bool HasConditionKey
        {
            get { return !string.IsNullOrEmpty(ConditionKey); }
        }

        public static IReadOnlyList<DialogueChoice> ParseList(string raw)
        {
            var result = new List<DialogueChoice>();
            if (string.IsNullOrWhiteSpace(raw)) return result;

            var entries = raw.Split('|');
            foreach (var entry in entries)
            {
                var value = entry.Trim();
                if (value.Length == 0) continue;

                var condition = string.Empty;
                var conditionIndex = value.IndexOf('?');
                if (conditionIndex >= 0)
                {
                    condition = value.Substring(conditionIndex + 1).Trim();
                    value = value.Substring(0, conditionIndex).Trim();
                }

                var separator = value.IndexOf("->", StringComparison.Ordinal);
                if (separator < 0)
                    separator = value.LastIndexOf(':');

                if (separator < 0)
                    continue;

                var text = value.Substring(0, separator).Trim();
                var targetText = value.Substring(separator + (value[separator] == ':' ? 1 : 2)).Trim();

                int target;
                if (int.TryParse(targetText, out target))
                    result.Add(new DialogueChoice(text, target, condition));
            }

            return result;
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(ConditionKey)
                ? Text + " -> " + NextId
                : Text + " -> " + NextId + " ?" + ConditionKey;
        }
    }
}
