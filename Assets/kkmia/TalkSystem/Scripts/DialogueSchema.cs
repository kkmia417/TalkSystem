using System;
using System.Collections.Generic;

namespace kkmia.TalkSystem
{
    public static class DialogueSchema
    {
        public const string Id = "Id";
        public const string Speaker = "Speaker";
        public const string Text = "Text";
        public const string NextId = "NextId";
        public const string EmotionKey = "EmotionKey";
        public const string TriggerKey = "TriggerKey";
        public const string ConditionKey = "ConditionKey";
        public const string EventKey = "EventKey";
        public const string Choices = "Choices";
        public const string AutoNextSeconds = "AutoNextSeconds";

        public static readonly string[] DefaultHeaders =
        {
            Id,
            Speaker,
            Text,
            NextId,
            EmotionKey,
            TriggerKey,
            ConditionKey
        };

        public static readonly string[] ExtendedHeaders =
        {
            Id,
            Speaker,
            Text,
            NextId,
            EmotionKey,
            TriggerKey,
            ConditionKey,
            EventKey,
            Choices,
            AutoNextSeconds
        };

        public static Dictionary<string, int> BuildHeaderMap(IReadOnlyList<string> headers)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (headers == null) return map;

            for (var i = 0; i < headers.Count; i++)
            {
                var header = headers[i];
                if (!string.IsNullOrWhiteSpace(header) && !map.ContainsKey(header.Trim()))
                    map.Add(header.Trim(), i);
            }

            return map;
        }
    }
}
