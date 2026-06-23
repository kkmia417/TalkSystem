using System;
using System.Linq;
using UnityEngine;

namespace kkmia.TalkSystem
{
    [DisallowMultipleComponent]
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("CSVファイル")]
        [Tooltip("会話データを含むCSVファイル (TextAsset)")]
        [SerializeField] private TextAsset csvFile;

        [Header("DialogueView (任意)")]
        [Tooltip("シーン内のViewが自動登録される場合は設定不要")]
        [SerializeField] private DialogueView view;

        [Header("Localization")]
        [SerializeField] private string languageKey = string.Empty;

        private DialoguePresenter _presenter;
        private IDialogueRepository _repository;
        private IDialogueConditionEvaluator _conditionEvaluator = new AllowAllDialogueConditionEvaluator();
        private IDialogueVariableResolver _variableResolver = new EmptyDialogueVariableResolver();
        private IDialogueTextResolver _textResolver = new DefaultDialogueTextResolver();
        private IDialogueEventDispatcher _eventDispatcher;

        public event Action<DialogueEventContext> LineStarted;
        public event Action<DialogueEventContext> LineCompleted;
        public event Action<DialogueEventContext> DialogueEnded;
        public event Action<DialogueEventContext> DialogueEventTriggered;
        public event Action<string> ErrorRaised;

        public IDialogueRepository Repository
        {
            get { return _repository; }
        }

        public DialogueSessionState State
        {
            get { return _presenter != null ? _presenter.State : DialogueSessionState.Idle; }
        }

        public System.Collections.Generic.IReadOnlyList<DialogueHistoryEntry> History
        {
            get
            {
                return _presenter != null
                    ? _presenter.Session.History
                    : new System.Collections.Generic.List<DialogueHistoryEntry>();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("DialogueManager: 重複インスタンスが存在したため削除します。");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (csvFile == null)
            {
                Debug.LogError("DialogueManager: csvFile が設定されていません。");
                return;
            }

            _repository = new DialogueRepository(csvFile);
            _eventDispatcher = new DelegateDialogueEventDispatcher(context =>
            {
                if (DialogueEventTriggered != null)
                    DialogueEventTriggered(context);
            });

            if (view != null)
                SetView(view);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            DisposePresenter();
        }

        public void SetConditionEvaluator(IDialogueConditionEvaluator evaluator)
        {
            _conditionEvaluator = evaluator ?? new AllowAllDialogueConditionEvaluator();
            ApplyPresenterConfiguration();
        }

        public void SetVariableResolver(IDialogueVariableResolver resolver)
        {
            _variableResolver = resolver ?? new EmptyDialogueVariableResolver();
            ApplyPresenterConfiguration();
        }

        public void SetTextResolver(IDialogueTextResolver resolver)
        {
            _textResolver = resolver ?? new DefaultDialogueTextResolver();
            ApplyPresenterConfiguration();
        }

        public void SetLanguage(string newLanguageKey)
        {
            languageKey = newLanguageKey ?? string.Empty;
            ApplyPresenterConfiguration();
        }

        public void SetEventDispatcher(IDialogueEventDispatcher dispatcher)
        {
            _eventDispatcher = dispatcher;
            ApplyPresenterConfiguration();
        }

        public void SetView(DialogueView newView)
        {
            view = newView ?? throw new ArgumentNullException(nameof(newView));

            if (_repository == null)
            {
                Debug.LogError("DialogueManager: Repository が初期化されていません。");
                return;
            }

            DisposePresenter();
            _presenter = new DialoguePresenter(_repository, view);
            _presenter.LineStarted += RaiseLineStarted;
            _presenter.LineCompleted += RaiseLineCompleted;
            _presenter.DialogueEnded += RaiseDialogueEnded;
            _presenter.ErrorRaised += RaiseError;
            ApplyPresenterConfiguration();

            view.Clear();
            view.gameObject.SetActive(false);
            Debug.Log("[DialogueManager] View がセットされました。");
        }

        public void LoadRepository(IDialogueRepositoryLoader loader)
        {
            if (loader == null)
            {
                Debug.LogError("DialogueManager: loader is null.");
                return;
            }

            StartCoroutine(loader.Load(repository =>
            {
                _repository = repository;
                if (view != null)
                    SetView(view);
            }, error =>
            {
                Debug.LogError("DialogueManager: " + error);
                RaiseError(error);
            }));
        }

        public void StartDialogue(int id)
        {
            if (!EnsureReady()) return;

            view.ForceStop();
            view.Clear();
            view.gameObject.SetActive(true);

            _presenter.Start(id);
        }

        public void StartDialogue(Func<DialogueData, bool> predicate)
        {
            if (!EnsureReady()) return;

            var data = _repository.GetAll().FirstOrDefault(predicate);
            if (data != null)
                StartDialogue(data.Id);
            else
                Debug.LogWarning("DialogueManager: 該当する会話データが見つかりません。");
        }

        public void StartDialogueForState(string triggerKey)
        {
            if (!EnsureReady()) return;

            if (string.IsNullOrEmpty(triggerKey))
            {
                Debug.LogWarning("DialogueManager: triggerKey が null または空です。");
                return;
            }

            var data = _repository.GetByTriggerKey(triggerKey);
            if (data != null)
                StartDialogue(data.Id, triggerKey);
            else
                Debug.LogWarning($"DialogueManager: TriggerKey \"{triggerKey}\" に該当するデータがありません。");
        }

        public void EndDialogue()
        {
            if (!EnsureReady()) return;

            _presenter.End();
            view.gameObject.SetActive(false);
        }

        /// <summary>タイプライターの 1 文字あたり間隔（秒）を設定する。コンフィグの文字速度から呼ぶ。</summary>
        public void SetTypewriterSpeed(float interval)
        {
            if (view != null)
                view.SetTypewriterSpeed(interval);
        }

        /// <summary>オート/スキップ進行のための自動送りオーバーライドを設定する。</summary>
        public void SetAutoAdvanceOverride(bool active, float seconds)
        {
            if (view != null)
                view.SetAutoAdvanceOverride(active, seconds);
        }

        /// <summary>現在行の送り（または文字送り完了）を要求する。オート/スキップ進行から呼ぶ。</summary>
        public void RequestNext()
        {
            if (view != null)
                view.RequestNext();
        }

        public DialogueSaveData CaptureState()
        {
            return _presenter != null ? _presenter.CaptureState() : new DialogueSaveData();
        }

        public bool RestoreState(DialogueSaveData saveData)
        {
            if (!EnsureReady()) return false;
            view.gameObject.SetActive(true);
            return _presenter.RestoreState(saveData);
        }

        private void StartDialogue(int id, string triggerKey)
        {
            if (!EnsureReady()) return;

            view.ForceStop();
            view.Clear();
            view.gameObject.SetActive(true);

            _presenter.Start(id, triggerKey);
        }

        private void ApplyPresenterConfiguration()
        {
            if (_presenter == null) return;
            _presenter.SetConditionEvaluator(_conditionEvaluator);
            _presenter.SetVariableResolver(_variableResolver);
            _presenter.SetTextResolver(_textResolver);
            _presenter.SetLanguage(languageKey);
            _presenter.SetEventDispatcher(_eventDispatcher);
        }

        private void DisposePresenter()
        {
            if (_presenter == null) return;
            _presenter.LineStarted -= RaiseLineStarted;
            _presenter.LineCompleted -= RaiseLineCompleted;
            _presenter.DialogueEnded -= RaiseDialogueEnded;
            _presenter.ErrorRaised -= RaiseError;
            _presenter.Dispose();
            _presenter = null;
        }

        private bool EnsureReady()
        {
            if (_repository == null)
            {
                Debug.LogError("DialogueManager: Repository が初期化されていません。");
                return false;
            }

            if (view == null)
            {
                Debug.LogError("DialogueManager: DialogueView がセットされていません。");
                return false;
            }

            if (_presenter == null)
            {
                Debug.LogError("DialogueManager: Presenter が初期化されていません。");
                return false;
            }

            return true;
        }

        private void RaiseLineStarted(DialogueEventContext context)
        {
            if (LineStarted != null) LineStarted(context);
        }

        private void RaiseLineCompleted(DialogueEventContext context)
        {
            if (LineCompleted != null) LineCompleted(context);
        }

        private void RaiseDialogueEnded(DialogueEventContext context)
        {
            if (DialogueEnded != null) DialogueEnded(context);
        }

        private void RaiseError(string message)
        {
            if (ErrorRaised != null) ErrorRaised(message);
        }
    }
}
