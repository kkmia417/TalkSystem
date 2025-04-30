using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace kkmia.TalkSystem
{
    /// <summary>
    /// CSVから読み込まれた会話データを保持・提供するリポジトリ
    /// </summary>
    public class DialogueRepository : IDialogueRepository
    {
        private readonly Dictionary<int, DialogueData> _cache;

        public DialogueRepository(TextAsset csv)
        {
            if (csv == null)
            {
                Debug.LogError("[DialogueRepository] CSVファイルが設定されていません。空のリポジトリとして初期化されます。");
                _cache = new Dictionary<int, DialogueData>();
                return;
            }

            _cache = CsvLoader.Parse<DialogueData>(csv);
        }

        /// <summary>
        /// IDから1件の会話データを取得します
        /// </summary>
        public DialogueData Get(int id) =>
            _cache.TryGetValue(id, out var data) ? data : null;

        /// <summary>
        /// 全会話データを列挙します
        /// </summary>
        public IEnumerable<DialogueData> GetAll() =>
            _cache.Values;

        /// <summary>
        /// TriggerKey に一致する最初の会話データを取得します
        /// </summary>
        public DialogueData GetByTriggerKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            return _cache.Values.FirstOrDefault(d => d.TriggerKey == key);
        }
    }
}