using System.Collections;
using System.IO;
using UnityEngine;
using TMPro;
using Eduzo.Games.DetectiveGame.Data;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;

namespace Eduzo.Games.DetectiveGame.UI
{
    [Serializable]
    public class OptionFeedbackVFX
    {
        public ParticleSystem[] correctVFX;
        public ParticleSystem[] wrongVFX;
    }

    public class DetectiveGameUIManager : MonoBehaviour
    {
        [Header("References")]
        public DetectiveGameGameManager gameManager;

        [Header("Main Panels")]
        public GameObject homePanel;
        public GameObject practicePanel;
        public GameObject testPanel;
        public GameObject winPanel;
        public GameObject formPanel;

        [Header("HUD Elements")]
        public TextMeshProUGUI timerText;
        public GameObject heartsContainer;

        [Header("Question Layout")]
        public TextMeshProUGUI practicePrompt;
        public TextMeshProUGUI[] practiceOptions;
        public TextMeshProUGUI testPrompt;
        public TextMeshProUGUI[] testOptions;

        [Header("Reference Display")]
        public Image practiceReferenceImageDisplay;
        public Image testReferenceImageDisplay;

        [Header("Feedback VFX")]
        [Tooltip("Assign sets for Option 1 and Option 2")]
        public OptionFeedbackVFX[] practiceOptionVFX;
        [Tooltip("Assign sets for Option 1 and Option 2")]
        public OptionFeedbackVFX[] testOptionVFX;
        public ParticleSystem[] nextQuestionSuccessVFX;

        [Header("Win Panel UI")]
        public TextMeshProUGUI finalScoreText;
        public Image[] starImages;
        public Sprite filledStar;
        public Sprite emptyStar;
        public GameObject winImage;
        public GameObject loseImage;
        public ParticleSystem winEffects;
        public GameObject blockPanel;

        private bool isTestModeActive;

        public void SetupGameUI(bool isTest)
        {
            isTestModeActive = isTest;
            homePanel.SetActive(false);
            practicePanel.SetActive(!isTest);
            testPanel.SetActive(isTest);

            if (timerText != null) timerText.gameObject.SetActive(isTest);
            if (heartsContainer != null) heartsContainer.SetActive(isTest);
        }

        public void DisplayQuestion(DetectiveGameQuestion q)
        {
            TextMeshProUGUI activePrompt = isTestModeActive ? testPrompt : practicePrompt;
            TextMeshProUGUI[] activeOptions = isTestModeActive ? testOptions : practiceOptions;
            Image activeReferenceDisplay = isTestModeActive ? testReferenceImageDisplay : practiceReferenceImageDisplay;

            if (practiceReferenceImageDisplay != null) practiceReferenceImageDisplay.gameObject.SetActive(false);
            if (testReferenceImageDisplay != null) testReferenceImageDisplay.gameObject.SetActive(false);

            if (q == null)
            {
                activePrompt.text = "";
                ClearOptionsUI(activeOptions);
                if (activeReferenceDisplay != null) activeReferenceDisplay.gameObject.SetActive(false);
                return;
            }

            activePrompt.text = q.prompt;

            string[] rawOptions = q.GetOptionsArray();
            int[] order = q.GetShuffledOrder();

            const int MaxOptions = 2;
            int optionCount = Mathf.Min(MaxOptions, activeOptions.Length, order.Length);

            for (int i = 0; i < optionCount; i++)
            {
                int srcIdx = order[i];
                activeOptions[i].text = rawOptions[srcIdx];

                if (rawOptions[srcIdx] == q.correctAnswer)
                    gameManager.SetCorrectIndex(i);
            }

            for (int i = optionCount; i < activeOptions.Length; i++)
            {
                activeOptions[i].text = "";
            }

            if (activeReferenceDisplay != null)
            {
                if (!string.IsNullOrEmpty(q.referenceImagePath) && File.Exists(q.referenceImagePath))
                {
                    StartCoroutine(LoadReferenceImageFromPath(activeReferenceDisplay, q.referenceImagePath));
                }
                else if (q.referenceImage != null)
                {
                    activeReferenceDisplay.sprite = q.referenceImage;
                    activeReferenceDisplay.preserveAspect = true;
                    activeReferenceDisplay.gameObject.SetActive(true);
                }
                else
                {
                    activeReferenceDisplay.gameObject.SetActive(false);
                }
            }
        }

        private IEnumerator LoadReferenceImageFromPath(Image targetDisplay, string path)
        {
            if (targetDisplay == null || string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                if (targetDisplay != null) targetDisplay.gameObject.SetActive(false);
                yield break;
            }

            string uri = path.StartsWith("file://") ? path : "file://" + path;

            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(uri))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    targetDisplay.gameObject.SetActive(false);
                    yield break;
                }

                Texture2D tex = DownloadHandlerTexture.GetContent(uwr);
                if (tex == null)
                {
                    targetDisplay.gameObject.SetActive(false);
                    yield break;
                }

                targetDisplay.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                targetDisplay.preserveAspect = true;
                targetDisplay.gameObject.SetActive(true);
            }
        }

        public IEnumerator PlayFeedbackRoutine(int index, bool isCorrect)
        {
            RectTransform targetButton = GetButtonTransform(index);
            if (targetButton == null) yield break;

            var pop = targetButton.GetComponent<DetectiveGameButtonPop>();
            if (pop != null) pop.enabled = false;

            PlaySceneParticlesForOption(index, isCorrect);

            if (isCorrect)
                yield return StartCoroutine(ScaleButtonFeedback(targetButton));
            else
                yield return StartCoroutine(ShakeButtonFeedback(targetButton));

            if (pop != null) pop.enabled = true;
        }

        private void PlaySceneParticlesForOption(int optionIndex, bool isCorrect)
        {
            OptionFeedbackVFX[] targetSource = isTestModeActive ? testOptionVFX : practiceOptionVFX;

            if (targetSource == null || optionIndex < 0 || optionIndex >= targetSource.Length) return;

            ParticleSystem[] targetList = isCorrect ? targetSource[optionIndex].correctVFX : targetSource[optionIndex].wrongVFX;

            if (targetList == null || targetList.Length == 0) return;

            foreach (var ps in targetList)
            {
                if (ps == null) continue;
                ps.gameObject.SetActive(true);
                ps.Play();
                StartCoroutine(StopParticleDelayed(ps, 2.0f));
            }
        }

        public IEnumerator PlayNextQuestionSuccessVFXRoutine()
        {
            if (nextQuestionSuccessVFX == null || nextQuestionSuccessVFX.Length == 0) yield break;

            foreach (var ps in nextQuestionSuccessVFX)
            {
                if (ps == null) continue;
                ps.gameObject.SetActive(true);
                ps.Play();
                StartCoroutine(StopParticleDelayed(ps, 2.0f));
            }

            yield return new WaitForSeconds(2.0f);
        }

        private IEnumerator StopParticleDelayed(ParticleSystem ps, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private IEnumerator ScaleButtonFeedback(RectTransform button)
        {
            Vector3 startScale = button.localScale;
            Vector3 upScale = startScale * 1.3f;
            float duration = 0.15f;
            float elapsed = 0;

            while (elapsed < duration)
            {
                button.localScale = Vector3.Lerp(startScale, upScale, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            button.localScale = upScale;

            elapsed = 0;
            while (elapsed < duration)
            {
                button.localScale = Vector3.Lerp(upScale, startScale, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            button.localScale = startScale;
        }

        private IEnumerator ShakeButtonFeedback(RectTransform button)
        {
            Vector3 originalPos = button.anchoredPosition;
            float duration = 0.3f;
            float speed = 80f;
            float magnitude = 20f;
            float elapsed = 0;

            while (elapsed < duration)
            {
                float x = Mathf.Sin(elapsed * speed) * magnitude;
                button.anchoredPosition = new Vector3(originalPos.x + x, originalPos.y, originalPos.z);
                elapsed += Time.deltaTime;
                yield return null;
            }

            button.anchoredPosition = originalPos;
        }

        private RectTransform GetButtonTransform(int index)
        {
            TextMeshProUGUI[] activeOptions = isTestModeActive ? testOptions : practiceOptions;
            if (activeOptions != null && index >= 0 && index < activeOptions.Length && activeOptions[index] != null)
            {
                RectTransform rt = activeOptions[index].transform.parent as RectTransform;
                return rt != null ? rt : activeOptions[index].rectTransform;
            }
            return null;
        }

        public void PlayCorrectVFX(int index) => PlaySceneParticlesForOption(index, true);
        public void PlayWrongVFX(int index) => PlaySceneParticlesForOption(index, false);

        private void ClearOptionsUI(TextMeshProUGUI[] options)
        {
            for (int i = 0; i < options.Length; i++) options[i].text = "";
        }

        public void UpdateTimerText(string time) => timerText.text = time;

        public string GetOptionText(int index)
        {
            TextMeshProUGUI[] activeOptions = isTestModeActive ? testOptions : practiceOptions;
            if (index >= 0 && index < activeOptions.Length) return activeOptions[index].text;
            return "";
        }

        public void PlayWrongFeedback(int index) { }

        private int CalculateStars(float score)
        {
            if (score >= 90) return 3;
            if (score >= 60) return 2;
            if (score >= 30) return 1;
            return 0;
        }

        public IEnumerator ShowWinPanel(float score, int correct, int total, bool completed, GameMode mode)
        {
            bool isWin = completed && score >= 30f;
            blockPanel.gameObject.SetActive(true);

            if (isWin)
            {
                if (winEffects != null) winEffects.Play();
                DetectiveGameSoundManager.instance?.PlayWin();
            }
            else
            {
                DetectiveGameSoundManager.instance?.PlayLose();
            }

            yield return new WaitForSeconds(1.5f);
            blockPanel.gameObject.GetComponent<DetectiveGameAutoPopup>().Close();

            practicePanel.SetActive(false);
            testPanel.SetActive(false);
            winPanel.SetActive(true);

            if (winImage != null) winImage.SetActive(isWin);
            if (loseImage != null) loseImage.SetActive(!isWin);

            int stars = CalculateStars(score);
            for (int i = 0; i < starImages.Length; i++)
            {
                starImages[i].sprite = (i < stars) ? filledStar : emptyStar;
            }

            var summary = FindFirstObjectByType<DetectiveGameSummaryManager>(FindObjectsInactive.Include);
            if (summary != null) summary.ShowSummary(score, total, correct, mode);

            if (finalScoreText != null) finalScoreText.text = $"{Mathf.RoundToInt(score)}%";
        }
    }
}