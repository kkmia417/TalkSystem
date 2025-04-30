using UnityEngine;

namespace kkmia.TalkSystem
{
    /// <summary>
    /// 会話進行のロジックを司るプレゼンター（MVPのP）
    /// </summary>
    public class DialoguePresenter
    {
        private readonly IDialogueRepository _repository;
        private readonly DialogueView _view;

        private DialogueData _currentData;
        private bool _isBusy = false;

        public DialoguePresenter(IDialogueRepository repository, DialogueView view)
        {
            _repository = repository;
            _view = view;

            _view.OnNextRequested += HandleNextRequested;
        }

        /// <summary>
        /// 指定されたIDの会話を開始する
        /// </summary>
        public void Start(int id)
        {
            if (_isBusy) return;
            _isBusy = true;

            _currentData = _repository.Get(id);
            if (_currentData == null)
            {
                Debug.LogWarning($"[DialoguePresenter] ID {id} の会話データが見つかりません。");
                _view.Clear();
                _isBusy = false;
                return;
            }
            
            _view.Show(_currentData, () => _isBusy = false);
        }

        /// <summary>
        /// 「次へ」が要求されたときの処理
        /// </summary>
        private void HandleNextRequested()
        {
            if (_isBusy) return;

            if (_currentData == null)
            {
                return;
            }

            if (_currentData.NextId >= 0)
            {
                Start(_currentData.NextId);
            }
            else
            {
                _view.Clear();
            }
        }

        /// <summary>
        /// 会話状態を初期化する
        /// </summary>
        public void Reset()
        {
            _currentData = null;
            _isBusy = false;
        }
    }
}