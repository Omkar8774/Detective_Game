using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;

namespace Eduzo.Games.DetectiveGame.UI
{
    [RequireComponent(typeof(Button))]
    public class DetectiveGameButtonPop : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float pressedScale = 0.9f;
        [SerializeField] private float popDuration = 0.1f;
        [SerializeField] private float backDuration = 0.08f;

        [Header("Sound Settings")]
        [SerializeField] private AudioClip popSound;

        [Header("Action After Animation")]
        public UnityEvent onButtonAction;

        private AudioSource audioSource;
        private Vector3 originalScale;
        private Button btn;
        private bool isAnimating = false;


        #region Unity Lifecycle

        private void Awake()
        {
            btn = GetComponent<Button>();
            originalScale = transform.localScale;

            SetupAudioSource();

            btn.onClick.AddListener(() => StartCoroutine(ButtonClickRoutine()));
        }

        #endregion

        #region Private Methods

        private void SetupAudioSource()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        #endregion

        #region Coroutines

        private IEnumerator ButtonClickRoutine()
        {
            if (isAnimating) yield break;

            isAnimating = true;
            btn.interactable = false;

            if (popSound != null)
            {
                audioSource.PlayOneShot(popSound);
            }

            // Scale Down
            float t = 0f;
            while (t < 1)
            {
                t += Time.deltaTime / popDuration;
                transform.localScale = Vector3.Lerp(originalScale, originalScale * pressedScale, t);
                yield return null;
            }

            // Scale Up
            t = 0f;
            while (t < 1)
            {
                t += Time.deltaTime / backDuration;
                transform.localScale = Vector3.Lerp(originalScale * pressedScale, originalScale, t);
                yield return null;
            }

            onButtonAction?.Invoke();

            btn.interactable = true;
            isAnimating = false;
        }

        #endregion
    }
}