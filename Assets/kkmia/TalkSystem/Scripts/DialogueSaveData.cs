using System;
using System.Collections.Generic;

namespace kkmia.TalkSystem
{
    [Serializable]
    public sealed class DialogueSaveData
    {
        public int CurrentDialogueId = -1;
        public string TriggerKey = string.Empty;
        public DialogueSessionState State = DialogueSessionState.Idle;
        public List<int> SeenLineIds = new List<int>();
        public List<int> ChoiceHistory = new List<int>();
    }
}
