using UnityEngine;

namespace kkmia.TalkSystem
{
    /// <summary>
    /// DialogueView を持つオブジェクトが有効化されたとき、自動的に DialogueManager に登録する補助クラス。
    /// </summary>
    [RequireComponent(typeof(DialogueView))]
    public class DialogueViewBinder : MonoBehaviour
    {
        private void Start()
        {
            if (DialogueManager.Instance != null)
            {
                var view = GetComponent<DialogueView>();
                DialogueManager.Instance.SetView(view);
            }
        }
    }
}