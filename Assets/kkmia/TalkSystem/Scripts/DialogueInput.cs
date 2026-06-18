using System;
using UnityEngine;

namespace kkmia.TalkSystem
{
    public enum DialogueInputAction
    {
        Next,
        Skip,
        Auto,
        Backlog,
        ChoiceUp,
        ChoiceDown,
        Confirm
    }

    public interface IDialogueInputSource
    {
        event Action<DialogueInputAction> InputReceived;
    }

    public sealed class DialogueKeyboardInput : MonoBehaviour, IDialogueInputSource
    {
        [SerializeField] private KeyCode nextKey = KeyCode.Space;
        [SerializeField] private KeyCode backlogKey = KeyCode.B;
        [SerializeField] private KeyCode skipKey = KeyCode.LeftControl;

        public event Action<DialogueInputAction> InputReceived;

        private void Update()
        {
            if (Input.GetKeyDown(nextKey))
                Raise(DialogueInputAction.Next);
            if (Input.GetKeyDown(backlogKey))
                Raise(DialogueInputAction.Backlog);
            if (Input.GetKeyDown(skipKey))
                Raise(DialogueInputAction.Skip);
        }

        private void Raise(DialogueInputAction action)
        {
            if (InputReceived != null)
                InputReceived(action);
        }
    }
}
