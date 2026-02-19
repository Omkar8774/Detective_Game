using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Eduzo.Games.DetectiveGame.Data
{
    [RequireComponent(typeof(CanvasGroup))]
    public class DetectiveGameAutoPopup : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float animationTime = 0.3f;
        [SerializeField] private Vector3 startScale = new Vector3(0.6f, 0.6f, 1f);

        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;

        private bool isAnimating = false;

        #region Unity Lifecycle

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void OnEnable()
        {
            StopAllCoroutines();
            StartCoroutine(OpenRoutine());
        }

        #endregion

        #region Public Methods

        public void Close()
        {
            if (!gameObject.activeInHierarchy || isAnimating) return;

            StopAllCoroutines();
            StartCoroutine(CloseRoutine());
        }

        #endregion

        #region Private Coroutines

        private IEnumerator OpenRoutine()
        {
            isAnimating = true;

            canvasGroup.alpha = 0;
            transform.localScale = startScale;

            float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime / animationTime;
                float curve = Mathf.SmoothStep(0, 1, t);

                canvasGroup.alpha = curve;
                transform.localScale = Vector3.Lerp(startScale, Vector3.one, curve);

                yield return null;
            }

            isAnimating = false;
        }

        private IEnumerator CloseRoutine()
        {
            isAnimating = true;

            float t = 0;
            Vector3 currentScale = transform.localScale;

            while (t < 1)
            {
                t += Time.deltaTime / animationTime;
                float curve = Mathf.SmoothStep(0, 1, t);

                canvasGroup.alpha = 1 - curve;
                transform.localScale = Vector3.Lerp(currentScale, startScale, curve);

                yield return null;
            }

            isAnimating = false;
            gameObject.SetActive(false);
        }

        #endregion
    }
}