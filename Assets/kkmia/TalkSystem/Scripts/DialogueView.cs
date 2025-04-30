using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
// using DG.Tweening;

namespace kkmia.TalkSystem
{
    [Serializable]
    public struct CharacterSprite
    {
        public string key;
        public Sprite sprite;
    }

    public class DialogueView : MonoBehaviour
    {
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Image characterImage;
        [SerializeField] private Image dialogWindow;
        [SerializeField] private Sprite defaultSprite;
        [SerializeField] private TypewriterEffect typewriter;
        [SerializeField] private List<CharacterSprite> characterSprites;

        public event Action OnNextRequested;

        private Sprite initialSprite;
        private Coroutine autoNextCoroutine;

        private void Awake()
        {
            if (nextButton != null)
                nextButton.onClick.AddListener(HandleNextButtonClicked);

            if (characterImage != null)
                initialSprite = defaultSprite != null ? defaultSprite : characterImage.sprite;
        }

        public void Show(DialogueData data, Action onComplete)
        {
            ForceStop();
            CancelAutoNextTimer();

            gameObject.SetActive(false); // UI再描画対策
            gameObject.SetActive(true);

            if (dialogWindow != null)
                dialogWindow.gameObject.SetActive(true);

            if (speakerText != null)
                speakerText.text = data.Speaker;

            if (bodyText != null)
                bodyText.text = string.Empty;

            UpdateCharacterSprite(data.EmotionKey);

            if (nextButton != null)
                nextButton.gameObject.SetActive(false);

            if (typewriter != null)
            {
                typewriter.Play(data.Text, () =>
                {
                    if (nextButton != null)
                        nextButton.gameObject.SetActive(true);

                    StartAutoNextTimer();
                    onComplete?.Invoke();
                });
            }
            else
            {
                bodyText.text = data.Text;

                if (nextButton != null)
                    nextButton.gameObject.SetActive(true);

                StartAutoNextTimer();
                onComplete?.Invoke();
            }
        }

        private void UpdateCharacterSprite(string emotionKey)
        {
            if (characterImage == null) return;

            characterImage.gameObject.SetActive(true);

            if (string.IsNullOrEmpty(emotionKey))
            {
                characterImage.sprite = initialSprite;
                return;
            }

            foreach (var character in characterSprites)
            {
                if (character.key == emotionKey)
                {
                    characterImage.sprite = character.sprite;
                    return;
                }
            }

            characterImage.sprite = initialSprite;
        }

        private void HandleNextButtonClicked()
        {
            CancelAutoNextTimer();

            if (typewriter != null && typewriter.IsTyping)
            {
                typewriter.Complete();
            }
            else
            {
                OnNextRequested?.Invoke();
            }
        }

        private void StartAutoNextTimer()
        {
            CancelAutoNextTimer();
            autoNextCoroutine = StartCoroutine(AutoNextCoroutine());
        }

        private void CancelAutoNextTimer()
        {
            if (autoNextCoroutine != null)
            {
                StopCoroutine(autoNextCoroutine);
                autoNextCoroutine = null;
            }
        }

        private IEnumerator AutoNextCoroutine()
        {
            yield return new WaitForSeconds(1f);
            OnNextRequested?.Invoke();
        }

        public void StartDelay(float seconds, Action onCompleted)
        {
            if (seconds < 0f) seconds = 0f;
            StartCoroutine(DelayCoroutine(seconds, onCompleted));
        }

        private IEnumerator DelayCoroutine(float seconds, Action onCompleted)
        {
            yield return new WaitForSeconds(seconds);
            onCompleted?.Invoke();
        }

        public void Clear()
        {
            ForceStop();
            CancelAutoNextTimer();

            if (speakerText != null)
                speakerText.text = string.Empty;

            if (bodyText != null)
                bodyText.text = string.Empty;

            if (characterImage != null)
            {
                characterImage.sprite = initialSprite;
                characterImage.gameObject.SetActive(false);
            }

            if (nextButton != null)
                nextButton.gameObject.SetActive(false);

            if (dialogWindow != null)
                dialogWindow.gameObject.SetActive(false);
        }

        public void ForceStop()
        {
            CancelAutoNextTimer();
            if (typewriter != null && typewriter.IsTyping)
                typewriter.Complete();
        }

        public void SetTypewriterSpeed(float newInterval)
        {
            if (typewriter != null)
                typewriter.SetInterval(newInterval);
        }

        /*public void StartFadeOutWindowAndCharacter()
        {
            FadeOutElement(dialogWindow);
            FadeOutElement(characterImage);
        }*/

        /*private void FadeOutElement(Graphic target)
        {
            if (target == null) return;

            var canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = target.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 1f;
            canvasGroup.DOFade(0f, 1.5f).SetEase(Ease.InOutQuad);
        }*/
    }
}
