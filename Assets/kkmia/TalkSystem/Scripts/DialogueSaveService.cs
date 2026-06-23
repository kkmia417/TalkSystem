using System.Collections.Generic;

namespace kkmia.TalkSystem
{
    /// <summary>
    /// セーブ/ロードのオーケストレーション（純ロジック）。スロットのメタ付与・contributor 実行・
    /// ストレージ呼び出しを担う。会話本体の capture/restore（DialogueManager）とは分離しているためテスト可能。
    /// </summary>
    public sealed class DialogueSaveService
    {
        private readonly IDialogueSaveStorage _storage;
        private readonly List<IDialogueSaveContributor> _contributors = new List<IDialogueSaveContributor>();

        public DialogueSaveService(IDialogueSaveStorage storage, IEnumerable<IDialogueSaveContributor> contributors = null)
        {
            _storage = storage;
            if (contributors != null)
            {
                foreach (var contributor in contributors)
                {
                    if (contributor != null)
                        _contributors.Add(contributor);
                }
            }
        }

        public void AddContributor(IDialogueSaveContributor contributor)
        {
            if (contributor != null && !_contributors.Contains(contributor))
                _contributors.Add(contributor);
        }

        public void RemoveContributor(IDialogueSaveContributor contributor)
        {
            _contributors.Remove(contributor);
        }

        /// <summary>
        /// すでに capture 済みの本体データを、contributor で補強してスロットへ保存する。
        /// </summary>
        public DialogueSaveSlot Save(int slot, DialogueSaveData data, string title, bool isAutosave, long timestampUnix)
        {
            if (data == null) data = new DialogueSaveData();

            for (var i = 0; i < _contributors.Count; i++)
                _contributors[i].Capture(data);

            var saveSlot = new DialogueSaveSlot
            {
                SlotIndex = slot,
                Title = title ?? string.Empty,
                SavedAtUnix = timestampUnix,
                IsAutosave = isAutosave,
                Data = data
            };

            _storage.Save(saveSlot);
            return saveSlot;
        }

        /// <summary>スロットを読み込む（contributor の復元は <see cref="ApplyRestore"/> で別途行う）。</summary>
        public DialogueSaveSlot Load(int slot)
        {
            DialogueSaveSlot data;
            return _storage.TryLoad(slot, out data) ? data : null;
        }

        /// <summary>本体復元後に呼び、contributor へサブシステム状態を反映させる。</summary>
        public void ApplyRestore(DialogueSaveData data)
        {
            if (data == null) return;
            for (var i = 0; i < _contributors.Count; i++)
                _contributors[i].Restore(data);
        }

        public bool Exists(int slot)
        {
            return _storage.Exists(slot);
        }

        public void Delete(int slot)
        {
            _storage.Delete(slot);
        }

        public List<int> ListSlots()
        {
            var result = new List<int>();
            foreach (var slot in _storage.ListSlots())
                result.Add(slot);
            result.Sort();
            return result;
        }

        public byte[] LoadThumbnail(int slot)
        {
            return _storage.LoadThumbnail(slot);
        }

        public void SaveThumbnail(int slot, byte[] pngBytes)
        {
            if (pngBytes != null && pngBytes.Length > 0)
                _storage.SaveThumbnail(slot, pngBytes);
        }
    }
}
