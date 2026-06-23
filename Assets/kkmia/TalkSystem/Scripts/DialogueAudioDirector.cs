using System;

namespace kkmia.TalkSystem
{
    /// <summary>
    /// 1 行分の <see cref="DialogueData"/> の音声フィールド（Bgm / Se / Voice）を解釈し、
    /// 音声プレイヤーへ反映する指揮役。<see cref="DialogueManager.LineStarted"/> から
    /// <see cref="Apply(DialogueData)"/> を呼ぶ想定。Unity 型に依存しないためテスト可能。
    /// 背景・立ち絵は別系統（<see cref="DialogueStageDirector"/>）が担当する。
    /// </summary>
    public sealed class DialogueAudioDirector
    {
        private readonly IDialogueAudioPlayer _player;

        public DialogueAudioDirector(IDialogueAudioPlayer player)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
        }

        /// <summary>この行の BGM・SE・ボイスを再生する。</summary>
        public void Apply(DialogueData data)
        {
            if (data == null) return;

            ApplyBgm(data);
            ApplySe(data);
            ApplyVoice(data);
        }

        /// <summary>会話終了時などに全ての音声を停止する。</summary>
        public void StopAll()
        {
            _player.StopAll();
        }

        private void ApplyBgm(DialogueData data)
        {
            var cue = data.GetBgmCue();
            if (!cue.HasValue) return;

            var duration = cue.HasDuration ? Math.Max(0f, cue.Duration) : 0f;
            _player.PlayBgm(cue.Key, cue.IsClear, cue.Transition, duration);
        }

        private void ApplySe(DialogueData data)
        {
            var keys = data.GetSeKeys();
            for (var i = 0; i < keys.Count; i++)
                _player.PlaySe(keys[i]);
        }

        private void ApplyVoice(DialogueData data)
        {
            if (data.HasVoice)
                _player.PlayVoice(data.Voice);
        }
    }
}
