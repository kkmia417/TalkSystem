using System;
using UnityEngine;

namespace kkmia.TalkSystem
{
    /// <summary>
    /// 単一の会話データを表すクラス。
    /// CSV から動的に生成され、Presenter によって使用される。
    /// </summary>
    [Serializable]
    public class DialogueData
    {
        [field: SerializeField] public int    Id           { get; internal set; }
        [field: SerializeField] public string Speaker      { get; internal set; }
        [field: SerializeField] public string Text         { get; internal set; }
        [field: SerializeField] public int    NextId       { get; internal set; } = -1;
        [field: SerializeField] public string EmotionKey   { get; internal set; }
        [field: SerializeField] public string TriggerKey   { get; internal set; }
        [field: SerializeField] public string ConditionKey { get; internal set; }

        /// <summary>
        /// TriggerKey が存在するかどうか
        /// </summary>
        public bool HasTriggerKey => !string.IsNullOrEmpty(TriggerKey);

        /// <summary>
        /// 条件キーが存在するかどうか
        /// </summary>
        public bool HasConditionKey => !string.IsNullOrEmpty(ConditionKey);

        public override string ToString()
        {
            return $"[Dialogue {Id}] {Speaker}: {Text} (Next: {NextId}, Emotion: {EmotionKey}, Trigger: {TriggerKey}, Condition: {ConditionKey})";
        }
    }
}