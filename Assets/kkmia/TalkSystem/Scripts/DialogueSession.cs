using System.Collections.Generic;
using System.Linq;

namespace kkmia.TalkSystem
{
    public sealed class DialogueSession
    {
        private readonly IDialogueRepository _repository;
        private readonly List<int> _seenLineIds = new List<int>();
        private readonly List<int> _choiceHistory = new List<int>();
        private readonly HashSet<int> _skipGuard = new HashSet<int>();

        public DialogueSession(IDialogueRepository repository)
        {
            _repository = repository;
            ConditionEvaluator = new AllowAllDialogueConditionEvaluator();
            State = DialogueSessionState.Idle;
        }

        public DialogueSessionState State { get; private set; }
        public DialogueData CurrentData { get; private set; }
        public IReadOnlyList<DialogueChoice> CurrentChoices { get; private set; } = new List<DialogueChoice>();
        public IReadOnlyList<int> SeenLineIds { get { return _seenLineIds; } }
        public IReadOnlyList<int> ChoiceHistory { get { return _choiceHistory; } }
        public string TriggerKey { get; private set; }
        public IDialogueConditionEvaluator ConditionEvaluator { get; set; }

        public bool Start(int id, string triggerKey = null)
        {
            TriggerKey = triggerKey ?? string.Empty;
            _skipGuard.Clear();
            return LoadLine(id);
        }

        public void MarkTyping()
        {
            State = DialogueSessionState.Typing;
        }

        public void MarkLineReady()
        {
            if (CurrentData == null)
            {
                State = DialogueSessionState.Idle;
                return;
            }

            State = CurrentChoices.Count > 0 ? DialogueSessionState.ChoicePending : DialogueSessionState.WaitingForInput;
        }

        public bool Advance()
        {
            if (CurrentData == null)
            {
                End();
                return false;
            }

            if (CurrentData.NextId >= 0)
                return LoadLine(CurrentData.NextId);

            End();
            return false;
        }

        public bool SelectChoice(int index)
        {
            if (index < 0 || index >= CurrentChoices.Count)
                return false;

            _choiceHistory.Add(index);
            return LoadLine(CurrentChoices[index].NextId);
        }

        public void End()
        {
            CurrentData = null;
            CurrentChoices = new List<DialogueChoice>();
            State = DialogueSessionState.Ended;
        }

        public DialogueSaveData Capture()
        {
            return new DialogueSaveData
            {
                CurrentDialogueId = CurrentData != null ? CurrentData.Id : -1,
                TriggerKey = TriggerKey,
                State = State,
                SeenLineIds = new List<int>(_seenLineIds),
                ChoiceHistory = new List<int>(_choiceHistory)
            };
        }

        public bool Restore(DialogueSaveData saveData)
        {
            if (saveData == null)
                return false;

            _seenLineIds.Clear();
            if (saveData.SeenLineIds != null)
                _seenLineIds.AddRange(saveData.SeenLineIds);

            _choiceHistory.Clear();
            if (saveData.ChoiceHistory != null)
                _choiceHistory.AddRange(saveData.ChoiceHistory);

            TriggerKey = saveData.TriggerKey ?? string.Empty;
            State = saveData.State;

            if (saveData.CurrentDialogueId >= 0)
                return LoadLine(saveData.CurrentDialogueId);

            CurrentData = null;
            CurrentChoices = new List<DialogueChoice>();
            return true;
        }

        private bool LoadLine(int id)
        {
            var data = _repository.Get(id);
            if (data == null)
            {
                End();
                return false;
            }

            if (!PassesCondition(data))
            {
                if (!_skipGuard.Add(id))
                {
                    End();
                    return false;
                }

                if (data.NextId >= 0)
                    return LoadLine(data.NextId);

                End();
                return false;
            }

            CurrentData = data;
            if (!_seenLineIds.Contains(data.Id))
                _seenLineIds.Add(data.Id);

            CurrentChoices = data.GetChoices().Where(PassesCondition).ToList();
            State = DialogueSessionState.ShowingLine;
            return true;
        }

        private bool PassesCondition(DialogueData data)
        {
            if (data == null || !data.HasConditionKey)
                return true;

            return ConditionEvaluator == null || ConditionEvaluator.Evaluate(data.ConditionKey, data);
        }

        private bool PassesCondition(DialogueChoice choice)
        {
            if (choice == null || !choice.HasConditionKey)
                return true;

            return ConditionEvaluator == null || ConditionEvaluator.Evaluate(choice.ConditionKey, CurrentData);
        }
    }
}
