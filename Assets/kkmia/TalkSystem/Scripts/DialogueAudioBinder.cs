using UnityEngine;

namespace kkmia.TalkSystem
{
    /// <summary>
    /// <see cref="DialogueAudioPlayer"/> を <see cref="DialogueManager"/> のイベントに接続し、
    /// 各行の BGM・SE・ボイスを再生する補助コンポーネント。
    /// <see cref="DialogueViewBinder"/> / <see cref="DialogueStageBinder"/> と同様、置くだけで自動配線される。
    /// </summary>
    [RequireComponent(typeof(DialogueAudioPlayer))]
    public class DialogueAudioBinder : MonoBehaviour, IDialogueSaveContributor
    {
        private const string BgmKey = "audio.bgm";

        [Tooltip("会話終了時に BGM も停止するか（false ならボイスのみ停止）。")]
        [SerializeField] private bool stopBgmOnDialogueEnd = true;

        private DialogueAudioDirector _director;
        private DialogueAudioPlayer _player;
        private DialogueManager _boundManager;

        private void Awake()
        {
            _player = GetComponent<DialogueAudioPlayer>();
            _director = new DialogueAudioDirector(_player);
        }

        private void OnEnable()
        {
            Bind(DialogueManager.Instance);
        }

        private void Start()
        {
            if (_boundManager == null)
                Bind(DialogueManager.Instance);
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void Bind(DialogueManager manager)
        {
            if (manager == null || _boundManager == manager) return;

            Unbind();
            _boundManager = manager;
            _boundManager.LineStarted += HandleLineStarted;
            _boundManager.DialogueEnded += HandleDialogueEnded;
        }

        private void Unbind()
        {
            if (_boundManager == null) return;
            _boundManager.LineStarted -= HandleLineStarted;
            _boundManager.DialogueEnded -= HandleDialogueEnded;
            _boundManager = null;
        }

        private void HandleLineStarted(DialogueEventContext context)
        {
            if (context != null)
                _director.Apply(context.Data);
        }

        private void HandleDialogueEnded(DialogueEventContext context)
        {
            if (stopBgmOnDialogueEnd)
                _director.StopAll();
            else
                _player.StopVoice();
        }

        // --- IDialogueSaveContributor（セーブの完全復元）---

        void IDialogueSaveContributor.Capture(DialogueSaveData data)
        {
            if (data == null || _director == null) return;
            data.SetExtra(BgmKey, _director.CurrentBgmKey);
        }

        void IDialogueSaveContributor.Restore(DialogueSaveData data)
        {
            if (data == null || _player == null) return;

            string key;
            if (!data.TryGetExtra(BgmKey, out key))
                return;

            if (string.IsNullOrEmpty(key))
                _player.PlayBgm(string.Empty, true, string.Empty, 0f);
            else
                _player.PlayBgm(key, false, string.Empty, 0f);

            if (_director != null)
                _director.SetCurrentBgmKey(key);
        }
    }
}
