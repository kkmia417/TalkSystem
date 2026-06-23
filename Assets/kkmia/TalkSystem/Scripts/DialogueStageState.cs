using System.Collections.Generic;

namespace kkmia.TalkSystem
{
    public enum DialogueStageOperationKind
    {
        Show,
        Hide,
        ClearAll
    }

    /// <summary>
    /// ステージ状態の差分として算出された単一の演出操作。
    /// View はこの操作列を順番に適用するだけでよい。
    /// </summary>
    public readonly struct DialogueStageOperation
    {
        public DialogueStageOperation(DialogueStageOperationKind kind, string slot, string characterKey, string expression, string animation)
        {
            Kind = kind;
            Slot = slot ?? string.Empty;
            CharacterKey = characterKey ?? string.Empty;
            Expression = expression ?? string.Empty;
            Animation = animation ?? string.Empty;
        }

        public DialogueStageOperationKind Kind { get; }
        public string Slot { get; }
        public string CharacterKey { get; }
        public string Expression { get; }
        public string Animation { get; }

        public static DialogueStageOperation Show(string slot, string characterKey, string expression, string animation)
        {
            return new DialogueStageOperation(DialogueStageOperationKind.Show, slot, characterKey, expression, animation);
        }

        public static DialogueStageOperation Hide(string slot, string characterKey, string animation)
        {
            return new DialogueStageOperation(DialogueStageOperationKind.Hide, slot, characterKey, string.Empty, animation);
        }

        public static DialogueStageOperation ClearAll()
        {
            return new DialogueStageOperation(DialogueStageOperationKind.ClearAll, string.Empty, string.Empty, string.Empty, string.Empty);
        }
    }

    /// <summary>
    /// どのスロットに誰が立っているかを追跡する純ロジック。
    /// 立ち絵指示（<see cref="DialogueStageDirective"/>）を、View が適用すべき操作列へ変換する。
    /// Unity 型に依存しないためユニットテスト可能。
    /// </summary>
    public sealed class DialogueStageState
    {
        // slot -> characterKey。挿入順を保つため List ではなく Dictionary + 補助探索を使う。
        private readonly Dictionary<string, string> _occupancy = new Dictionary<string, string>();
        private readonly string _defaultSlot;

        public DialogueStageState(string defaultSlot = DialogueStageSlot.Center)
        {
            _defaultSlot = string.IsNullOrEmpty(defaultSlot) ? DialogueStageSlot.Center : defaultSlot;
        }

        /// <summary>現在の slot -> characterKey の対応（読み取り専用）。セーブ等で参照する。</summary>
        public IReadOnlyDictionary<string, string> Occupancy
        {
            get { return _occupancy; }
        }

        public void Reset()
        {
            _occupancy.Clear();
        }

        /// <summary>
        /// 立ち絵指示列を適用し、発生する操作列を返します。状態（占有スロット）も更新します。
        /// </summary>
        public IReadOnlyList<DialogueStageOperation> Apply(IReadOnlyList<DialogueStageDirective> directives)
        {
            var operations = new List<DialogueStageOperation>();
            if (directives == null) return operations;

            foreach (var directive in directives)
            {
                if (directive == null) continue;

                if (directive.IsClearAll)
                {
                    _occupancy.Clear();
                    operations.Add(DialogueStageOperation.ClearAll());
                    continue;
                }

                if (directive.IsExit)
                {
                    var slot = directive.HasSlot ? directive.Slot : FindSlotOf(directive.CharacterKey);
                    if (!string.IsNullOrEmpty(slot))
                        _occupancy.Remove(slot);

                    operations.Add(DialogueStageOperation.Hide(slot, directive.CharacterKey, directive.Animation));
                    continue;
                }

                operations.Add(ApplyShow(directive));
            }

            return operations;
        }

        private DialogueStageOperation ApplyShow(DialogueStageDirective directive)
        {
            // スロット解決順: 明示指定 > 既に立っている位置 > 既定スロット。
            var slot = directive.Slot;
            if (string.IsNullOrEmpty(slot))
                slot = FindSlotOf(directive.CharacterKey);
            if (string.IsNullOrEmpty(slot))
                slot = _defaultSlot;

            // 同じキャラが別スロットから移動する場合は元スロットを空ける。
            var previousSlot = FindSlotOf(directive.CharacterKey);
            if (!string.IsNullOrEmpty(previousSlot) && previousSlot != slot)
                _occupancy.Remove(previousSlot);

            _occupancy[slot] = directive.CharacterKey;
            return DialogueStageOperation.Show(slot, directive.CharacterKey, directive.Expression, directive.Animation);
        }

        private string FindSlotOf(string characterKey)
        {
            if (string.IsNullOrEmpty(characterKey)) return string.Empty;

            foreach (var pair in _occupancy)
            {
                if (pair.Value == characterKey)
                    return pair.Key;
            }

            return string.Empty;
        }
    }
}
