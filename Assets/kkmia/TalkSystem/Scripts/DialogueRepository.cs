using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace kkmia.TalkSystem
{
    public class DialogueRepository : IDialogueRepository
    {
        private readonly Dictionary<int, DialogueData> _cache;

        public DialogueRepository(TextAsset csv)
        {
            if (csv == null)
            {
                Debug.LogError("[DialogueRepository] CSVファイルが設定されていません。空のリポジトリとして初期化されます。");
                _cache = new Dictionary<int, DialogueData>();
                ValidationReport = new DialogueValidationReport();
                ValidationReport.Add(DialogueValidationSeverity.Error, 0, string.Empty, "CSV file is not assigned.");
                return;
            }

            _cache = CsvLoader.Parse<DialogueData>(csv);
            ValidationReport = DialogueValidator.ValidateData(_cache.Values);
        }

        public DialogueRepository(IEnumerable<DialogueData> data)
        {
            _cache = new Dictionary<int, DialogueData>();
            if (data != null)
            {
                foreach (var item in data)
                {
                    if (item != null && !_cache.ContainsKey(item.Id))
                        _cache.Add(item.Id, item);
                }
            }

            ValidationReport = DialogueValidator.ValidateData(_cache.Values);
        }

        public DialogueValidationReport ValidationReport { get; private set; }

        public DialogueData Get(int id)
        {
            DialogueData data;
            return _cache.TryGetValue(id, out data) ? data : null;
        }

        public IEnumerable<DialogueData> GetAll()
        {
            return _cache.Values;
        }

        public DialogueData GetByTriggerKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            return _cache.Values.FirstOrDefault(d => d.TriggerKey == key);
        }
    }
}
