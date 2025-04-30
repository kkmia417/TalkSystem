using System;
using System.Collections;
using UnityEngine;
using TMPro;

namespace kkmia.TalkSystem
{
    /// <summary>
    /// テキストを一文字ずつ表示するタイプライター演出コンポーネント
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class TypewriterEffect : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("1文字あたりの表示間隔（秒）")]
        private float interval = 0.05f;

        private TMP_Text _textComponent;
        private Coroutine _typingCoroutine;
        private string _fullText;
        private Action _onComplete;

        /// <summary>
        /// 現在タイプ中かどうかを返します。
        /// </summary>
        public bool IsTyping => _typingCoroutine != null;

        private void Awake()
        {
            _textComponent = GetComponent<TMP_Text>();
        }

        /// <summary>
        /// テキストを指定してタイプライター演出を開始します。
        /// </summary>
        /// <param name="fullText">表示する全文</param>
        /// <param name="onComplete">表示完了時に呼ばれるコールバック</param>
        public void Play(string fullText, Action onComplete)
        {
            StopTyping();

            if (_textComponent == null)
            {
                Debug.LogError("[TypewriterEffect] TMP_Text が見つかりません。");
                return;
            }

            _fullText = fullText ?? string.Empty;
            _onComplete = onComplete;
            _typingCoroutine = StartCoroutine(TypeRoutine());
        }

        /// <summary>
        /// 表示中のテキストを即座に全表示に切り替え、コールバックを実行します。
        /// </summary>
        public void Complete()
        {
            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);
                _typingCoroutine = null;

                if (_textComponent != null)
                    _textComponent.text = _fullText;

                _onComplete?.Invoke();
            }
        }

        /// <summary>
        /// 表示間隔を変更します。
        /// </summary>
        /// <param name="newInterval">特定のタイミングで文字表示を遅らせたいときに使えます。</param>
        public void SetInterval(float newInterval)
        {
            interval = Mathf.Max(0f, newInterval);
        }

        private void StopTyping()
        {
            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);
                _typingCoroutine = null;
            }
        }

        private IEnumerator TypeRoutine()
        {
            _textComponent.text = string.Empty;

            foreach (var c in _fullText)
            {
                _textComponent.text += c;
                yield return new WaitForSeconds(interval);
            }

            _typingCoroutine = null;
            _onComplete?.Invoke();
        }
    }
}
