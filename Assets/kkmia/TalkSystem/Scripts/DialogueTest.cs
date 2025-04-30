using UnityEngine;

namespace kkmia.TalkSystem
{
    /// <summary>
    /// DialogueManager の動作確認用テストスクリプト。
    /// ゲーム起動時に ID=1 の会話を自動で開始します。
    /// </summary>
    public class DialogueTest : MonoBehaviour
    {
        [Tooltip("任意の開始ID（例：1）")]
        public int startDialogueId = 1;

        private void Start()
        {
            if (DialogueManager.Instance == null)
            {
                Debug.LogError("[DialogueManagerTest] DialogueManager.Instance が存在しません。");
                return;
            }

            DialogueManager.Instance.StartDialogue(startDialogueId);
            Debug.Log($"[DialogueManagerTest] 会話 ID {startDialogueId} を開始しました。");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                DialogueManager.Instance.StartDialogueForState("GameOver");
            }
        }
    }
}