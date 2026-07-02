using System;
using System.Collections.Generic;

namespace kkmia.TalkSystem
{
    /// <summary>セーブに含める任意のサブシステム状態（ステージ占有・現在BGMなど）の 1 項目。</summary>
    [Serializable]
    public sealed class DialogueSaveValue
    {
        public string Key = string.Empty;
        public string Value = string.Empty;

        public DialogueSaveValue() { }

        public DialogueSaveValue(string key, string value)
        {
            Key = key ?? string.Empty;
            Value = value ?? string.Empty;
        }
    }

    [Serializable]
    public sealed class DialogueSaveData
    {
        public int SchemaVersion = DialogueSaveSchema.CurrentVersion;
        public string ContentVersion = string.Empty;
        public string ProductChannel = string.Empty;
        public int CurrentDialogueId = -1;
        public string TriggerKey = string.Empty;
        public DialogueSessionState State = DialogueSessionState.Idle;
        public List<int> SeenLineIds = new List<int>();
        public List<int> ChoiceHistory = new List<int>();
        public List<DialogueHistoryEntry> History = new List<DialogueHistoryEntry>();
        public DialogueProgressState Progress = new DialogueProgressState();

        /// <summary>
        /// 演出系（ステージ・音声など）が完全復元のために書き込む任意の追加状態。
        /// <see cref="IDialogueSaveContributor"/> 経由で各サブシステムが読み書きする。
        /// </summary>
        public List<DialogueSaveValue> ExtraState = new List<DialogueSaveValue>();

        /// <summary>追加状態を設定（既存キーは上書き）。</summary>
        public void SetExtra(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return;

            for (var i = 0; i < ExtraState.Count; i++)
            {
                if (ExtraState[i] != null && ExtraState[i].Key == key)
                {
                    ExtraState[i].Value = value ?? string.Empty;
                    return;
                }
            }

            ExtraState.Add(new DialogueSaveValue(key, value));
        }

        public bool TryGetExtra(string key, out string value)
        {
            for (var i = 0; i < ExtraState.Count; i++)
            {
                if (ExtraState[i] != null && ExtraState[i].Key == key)
                {
                    value = ExtraState[i].Value;
                    return true;
                }
            }

            value = null;
            return false;
        }
    }
}
