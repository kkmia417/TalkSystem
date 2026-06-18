using System;
using System.Collections.Generic;
using UnityEngine;

namespace kkmia.TalkSystem
{
    [Serializable]
    public class DialogueData
    {
        [field: SerializeField] public int Id { get; internal set; }
        [field: SerializeField] public string Speaker { get; internal set; }
        [field: SerializeField] public string Text { get; internal set; }
        [field: SerializeField] public int NextId { get; internal set; } = -1;
        [field: SerializeField] public string EmotionKey { get; internal set; }
        [field: SerializeField] public string TriggerKey { get; internal set; }
        [field: SerializeField] public string ConditionKey { get; internal set; }
        [field: SerializeField] public string EventKey { get; internal set; }
        [field: SerializeField] public string ChoicesRaw { get; internal set; }
        [field: SerializeField] public float AutoNextSeconds { get; internal set; } = -1f;

        public int RowNumber { get; internal set; }

        public bool HasTriggerKey
        {
            get { return !string.IsNullOrEmpty(TriggerKey); }
        }

        public bool HasConditionKey
        {
            get { return !string.IsNullOrEmpty(ConditionKey); }
        }

        public bool HasEventKey
        {
            get { return !string.IsNullOrEmpty(EventKey); }
        }

        public IReadOnlyList<DialogueChoice> GetChoices()
        {
            return DialogueChoice.ParseList(ChoicesRaw);
        }

        public DialogueData WithResolvedText(string resolvedText)
        {
            return new DialogueData
            {
                Id = Id,
                Speaker = Speaker,
                Text = resolvedText,
                NextId = NextId,
                EmotionKey = EmotionKey,
                TriggerKey = TriggerKey,
                ConditionKey = ConditionKey,
                EventKey = EventKey,
                ChoicesRaw = ChoicesRaw,
                AutoNextSeconds = AutoNextSeconds,
                RowNumber = RowNumber
            };
        }

        public override string ToString()
        {
            return $"[Dialogue {Id}] {Speaker}: {Text} (Next: {NextId}, Emotion: {EmotionKey}, Trigger: {TriggerKey}, Condition: {ConditionKey}, Event: {EventKey})";
        }
    }
}
