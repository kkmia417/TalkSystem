using UnityEngine;

namespace kkmia.TalkSystem
{
    [RequireComponent(typeof(DialogueView))]
    public sealed class DialogueInputRouter : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour inputSourceComponent;
        [SerializeField] private DialogueBacklogView backlog;

        private DialogueView _view;
        private IDialogueInputSource _inputSource;

        private void Awake()
        {
            _view = GetComponent<DialogueView>();
            _inputSource = inputSourceComponent as IDialogueInputSource;

            if (_inputSource != null)
                _inputSource.InputReceived += HandleInput;
        }

        private void OnDestroy()
        {
            if (_inputSource != null)
                _inputSource.InputReceived -= HandleInput;
        }

        private void HandleInput(DialogueInputAction action)
        {
            if (_view == null) return;

            if (action == DialogueInputAction.Next || action == DialogueInputAction.Confirm)
                _view.RequestNext();
            else if (action == DialogueInputAction.Rollback && DialogueManager.Instance != null)
                DialogueManager.Instance.Rollback();
            else if (action == DialogueInputAction.Backlog && backlog != null)
                backlog.Toggle();
        }
    }
}
