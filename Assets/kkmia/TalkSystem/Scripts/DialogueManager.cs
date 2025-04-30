using UnityEngine;
using System;
using System.Linq;

namespace kkmia.TalkSystem
{
    /// <summary>
    /// 会話システムの制御を担当するマネージャクラス。  
    /// CSVファイルを基に会話を開始・終了し、View に情報を流し込む役割を持つ。
    /// </summary>
    [DisallowMultipleComponent]
    public class DialogueManager : MonoBehaviour
    {
        /// <summary>
        /// シングルトンインスタンス
        /// </summary>
        public static DialogueManager Instance { get; private set; }

        [Header("CSVファイル")]
        [Tooltip("会話データを含むCSVファイル (TextAsset)")]
        [SerializeField] private TextAsset csvFile;

        [Header("DialogueView (任意)")]
        [Tooltip("シーン内のViewが自動登録される場合は設定不要")]
        [SerializeField] private DialogueView view;

        private DialoguePresenter _presenter;
        private IDialogueRepository _repository;

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

            if (view != null)
            {
                _presenter = new DialoguePresenter(_repository, view);
                view.Clear();
                view.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 外部から DialogueView を登録する。シーンごとに呼び出すことで View を切り替え可能。
        /// </summary>
        public void SetView(DialogueView newView)
        {
            view = newView ?? throw new ArgumentNullException(nameof(newView));
            _presenter = new DialoguePresenter(_repository, view);
            
            Debug.Log("[DialogueManager] View がセットされました。");
        }

        /// <summary>
        /// 指定したIDの会話を開始します。
        /// </summary>
        public void StartDialogue(int id)
        {
            if (!EnsureReady()) return;

            view.ForceStop();
            view.Clear();
            view.gameObject.SetActive(true);

            _presenter.Start(id);
        }

        /// <summary>
        /// 条件に一致する最初の会話データを取得して開始します。
        /// </summary>
        public void StartDialogue(Func<DialogueData, bool> predicate)
        {
            if (!EnsureReady()) return;

            var data = _repository.GetAll().FirstOrDefault(predicate);
            if (data != null)
            {
                StartDialogue(data.Id);
            }
            else
            {
                Debug.LogWarning("DialogueManager: 該当する会話データが見つかりません。");
            }
        }

        /// <summary>
        /// 指定されたTriggerKeyをもとに会話を開始します。
        /// </summary>
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
            {
                StartDialogue(data.Id);
            }
            else
            {
                Debug.LogWarning($"DialogueManager: TriggerKey \"{triggerKey}\" に該当するデータがありません。");
            }
        }

        /// <summary>
        /// 表示中の会話を強制終了します。
        /// </summary>
        public void EndDialogue()
        {
            if (!EnsureReady()) return;

            view.ForceStop();
            view.Clear();
            view.gameObject.SetActive(false);
        }

        /// <summary>
        /// Repository や View が有効かをチェック
        /// </summary>
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
    }
}
