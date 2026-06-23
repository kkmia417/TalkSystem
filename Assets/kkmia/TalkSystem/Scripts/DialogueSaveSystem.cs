using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace kkmia.TalkSystem
{
    /// <summary>
    /// 複数スロットのセーブ/ロードを束ねるエントリポイント。会話本体は <see cref="DialogueManager"/> から
    /// capture/restore し、演出系の完全復元は <see cref="IDialogueSaveContributor"/> に委ねる。
    /// 既定では <see cref="FileDialogueSaveStorage"/>（persistentDataPath）を使う。
    /// </summary>
    public class DialogueSaveSystem : MonoBehaviour
    {
        /// <summary>オートセーブ専用スロット番号。</summary>
        public const int AutosaveSlot = 0;

        [Tooltip("未設定なら DialogueManager.Instance を使う。")]
        [SerializeField] private DialogueManager manager;

        [Tooltip("IDialogueSaveContributor を実装したコンポーネント（ステージ/音声の完全復元用）。")]
        [SerializeField] private List<MonoBehaviour> contributors = new List<MonoBehaviour>();

        private DialogueSaveService _service;
        private IDialogueSaveStorage _storage;

        public DialogueSaveService Service
        {
            get { return _service; }
        }

        private void Awake()
        {
            _storage = new FileDialogueSaveStorage();
            _service = new DialogueSaveService(_storage);

            foreach (var component in contributors)
            {
                var contributor = component as IDialogueSaveContributor;
                if (contributor != null)
                    _service.AddContributor(contributor);
            }
        }

        public void RegisterContributor(IDialogueSaveContributor contributor)
        {
            if (_service != null)
                _service.AddContributor(contributor);
        }

        private DialogueManager Manager
        {
            get { return manager != null ? manager : DialogueManager.Instance; }
        }

        /// <summary>指定スロットへ保存する（サムネイル無し）。</summary>
        public DialogueSaveSlot Save(int slot, bool isAutosave = false, string title = null)
        {
            var mgr = Manager;
            if (mgr == null)
            {
                Debug.LogError("[DialogueSaveSystem] DialogueManager が見つかりません。");
                return null;
            }

            var data = mgr.CaptureState();
            var resolvedTitle = string.IsNullOrEmpty(title) ? BuildTitle(mgr) : title;
            return _service.Save(slot, data, resolvedTitle, isAutosave, NowUnix());
        }

        /// <summary>画面キャプチャ付きで保存する（フレーム終端まで待つためコルーチン）。</summary>
        public void SaveWithThumbnail(int slot, bool isAutosave = false, string title = null)
        {
            Save(slot, isAutosave, title);
            StartCoroutine(CaptureThumbnail(slot));
        }

        /// <summary>指定スロットを読み込み、会話本体と演出系を復元する。</summary>
        public bool Load(int slot)
        {
            var mgr = Manager;
            if (mgr == null) return false;

            var saveSlot = _service.Load(slot);
            if (saveSlot == null || saveSlot.Data == null)
                return false;

            if (!mgr.RestoreState(saveSlot.Data))
                return false;

            _service.ApplyRestore(saveSlot.Data);
            return true;
        }

        public bool Exists(int slot)
        {
            return _service != null && _service.Exists(slot);
        }

        public void Delete(int slot)
        {
            if (_service != null)
                _service.Delete(slot);
        }

        public List<int> ListSlots()
        {
            return _service != null ? _service.ListSlots() : new List<int>();
        }

        public DialogueSaveSlot Peek(int slot)
        {
            return _service != null ? _service.Load(slot) : null;
        }

        /// <summary>スロットのサムネイルを Texture2D として取得する。無ければ null。</summary>
        public Texture2D LoadThumbnail(int slot)
        {
            var bytes = _service != null ? _service.LoadThumbnail(slot) : null;
            if (bytes == null || bytes.Length == 0) return null;

            var texture = new Texture2D(2, 2);
            return texture.LoadImage(bytes) ? texture : null;
        }

        private IEnumerator CaptureThumbnail(int slot)
        {
            yield return new WaitForEndOfFrame();

            Texture2D screenshot = null;
            try
            {
                screenshot = ScreenCapture.CaptureScreenshotAsTexture();
                var png = screenshot.EncodeToPNG();
                _service.SaveThumbnail(slot, png);
            }
            finally
            {
                if (screenshot != null)
                    Destroy(screenshot);
            }
        }

        private static string BuildTitle(DialogueManager mgr)
        {
            var history = mgr.History;
            if (history != null && history.Count > 0)
            {
                var last = history[history.Count - 1];
                var text = last != null ? last.Text : string.Empty;
                if (!string.IsNullOrEmpty(text))
                    return text.Length > 40 ? text.Substring(0, 40) + "…" : text;
            }

            return "Save";
        }

        private static long NowUnix()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
