using System;

namespace kkmia.TalkSystem
{
    /// <summary>
    /// 1 行分の <see cref="DialogueData"/> の演出フィールドを解釈し、ステージ View へ反映する指揮役。
    /// <see cref="DialoguePresenter.LineStarted"/> / <see cref="DialogueManager.LineStarted"/> から
    /// <see cref="Apply(DialogueData)"/> を呼ぶ想定。Unity 型に依存しないためテスト可能。
    /// 音声（BGM/SE/ボイス）は別系統（Phase 3）が担当し、ここでは扱わない。
    /// </summary>
    public sealed class DialogueStageDirector
    {
        private readonly IDialogueStageView _view;
        private readonly DialogueStageState _state;

        public DialogueStageDirector(IDialogueStageView view, DialogueStageState state = null)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _state = state ?? new DialogueStageState();
        }

        public DialogueStageState State
        {
            get { return _state; }
        }

        /// <summary>この行の背景・立ち絵指示をステージへ適用する。</summary>
        public void Apply(DialogueData data)
        {
            if (data == null) return;

            ApplyBackground(data);
            ApplyCharacters(data);
        }

        /// <summary>会話終了時などにステージを初期状態へ戻す。</summary>
        public void Clear(bool clearBackground = true)
        {
            _state.Reset();
            _view.ClearCharacters();
            if (clearBackground)
                _view.SetBackground(string.Empty, true, string.Empty, 0f);
        }

        private void ApplyBackground(DialogueData data)
        {
            var cue = data.GetBackgroundCue();
            if (!cue.HasValue) return;

            var duration = cue.HasDuration ? Math.Max(0f, cue.Duration) : 0f;
            _view.SetBackground(cue.Key, cue.IsClear, cue.Transition, duration);
        }

        private void ApplyCharacters(DialogueData data)
        {
            var operations = _state.Apply(data.GetStageDirectives());
            for (var i = 0; i < operations.Count; i++)
            {
                var op = operations[i];
                switch (op.Kind)
                {
                    case DialogueStageOperationKind.Show:
                        _view.SetCharacter(op.Slot, op.CharacterKey, op.Expression, op.Animation);
                        break;
                    case DialogueStageOperationKind.Hide:
                        _view.RemoveCharacter(op.Slot, op.CharacterKey, op.Animation);
                        break;
                    case DialogueStageOperationKind.ClearAll:
                        _view.ClearCharacters();
                        break;
                }
            }
        }
    }
}
