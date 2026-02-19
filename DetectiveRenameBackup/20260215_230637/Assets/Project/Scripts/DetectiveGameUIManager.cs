using UnityEngine;
using TMPro;
using Eduzo.Games.DetectiveGame.Data;
using UnityEngine.UI;
using System.Collections;

namespace Eduzo.Games.DetectiveGame.UI
{
    public class CarTollUIManager : MonoBehaviour
    {
        public CarTollGameManager gameManager;

        [Header("Main Panels")]
        public GameObject homePanel;
        public GameObject practicePanel;
        public GameObject testPanel;
        public GameObject winPanel;
        public GameObject formPanel;

        [Header("Car Lights")]
        public GameObject[] frontLights;
        public GameObject[] backLights;

        [Header("Multi-Car Selection")]
        public GameObject[] laneCars;
        public Transform[] carStartPoints;
        public Transform[] carStopPoints;
        public Transform[] passPoints;

        [Header("Car Sprites")]
        public Sprite[] carSprites;

        [Header("HUD (Test Mode Only)")]
        public TextMeshProUGUI timerText;
        public GameObject heartsContainer;

        [Header("Practice UI References")]
        public TextMeshProUGUI practicePrompt;
        public TextMeshProUGUI[] practiceOptions;
        public GameObject[] practiceRedLights;
        public GameObject[] practiceGreenLights;

        [Header("Test UI References")]
        public TextMeshProUGUI testPrompt;
        public TextMeshProUGUI[] testOptions;
        public GameObject[] testRedLights;
        public GameObject[] testGreenLights;

        [Header("Shared Animators")]
        public Animator[] barricadeAnimators;

        [Header("Highlighter Settings")]
        public GameObject[] barricadeHighlighters;
        public float highlightDuration = 2.0f;
        private int currentActiveHighlighter = 0;

        private bool isTestModeActive;

        [Header("Win Panel References")]
        public TextMeshProUGUI finalScoreText;
        public Image[] starImages;
        public Sprite filledStar;
        public Sprite emptyStar;
        public GameObject winImage;
        public GameObject loseImage;

        [Header("Lane Setup")]
        public Transform[] laneAreas;

        public ParticleSystem winEffects;
        public GameObject blockPanel;

        public void SetupGameUI(bool isTest)
        {
            isTestModeActive = isTest;
            homePanel.SetActive(false);
            practicePanel.SetActive(!isTest);
            testPanel.SetActive(isTest);
        }

        public void DisplayQuestion(CarTollQuestion q)
        {
            TextMeshProUGUI activePrompt = isTestModeActive ? testPrompt : practicePrompt;
            TextMeshProUGUI[] activeOptions = isTestModeActive ? testOptions : practiceOptions;
            GameObject[] activeReds = isTestModeActive ? testRedLights : practiceRedLights;
            GameObject[] activeGreens = isTestModeActive ? testGreenLights : practiceGreenLights;

            activePrompt.text = q.prompt;
            string[] shuffled = q.GetShuffledOptions();

            for (int i = 0; i < 4; i++)
            {
                activeOptions[i].text = shuffled[i];
                activeReds[i].SetActive(true);
                activeGreens[i].SetActive(false);

                if (shuffled[i] == q.correctAnswer)
                    gameManager.SetCorrectIndex(i);
            }
        }

        public void SetLightGreen(int i)
        {
            GameObject[] activeReds = isTestModeActive ? testRedLights : practiceRedLights;
            GameObject[] activeGreens = isTestModeActive ? testGreenLights : practiceGreenLights;

            activeReds[i].SetActive(false);
            activeGreens[i].SetActive(true);
        }

        public void SetLights(int index, bool front, bool back)
        {
            if (index < 0 || index >= laneCars.Length) return;

            if (frontLights[index] != null) frontLights[index].SetActive(front);
            if (backLights[index] != null) backLights[index].SetActive(back);
        }

        public void AllLightsOff(int index)
        {
            SetLights(index, false, false);
        }

        public void UpdateTimerText(string time) => timerText.text = time;

        public void OpenBarricade(int i) => barricadeAnimators[i].SetTrigger("Open");

        public void CloseBarricade(int i) => barricadeAnimators[i].SetTrigger("Close");

        public int GetActiveHighlighterIndex() => currentActiveHighlighter;

        public IEnumerator StartHighlighterLoop()
        {
            currentActiveHighlighter = 0;

            while (true)
            {
                for (int i = 0; i < barricadeHighlighters.Length; i++)
                {
                    currentActiveHighlighter = i;

                    DeactivateAllHighlighters();
                    if (barricadeHighlighters[i] != null)
                        barricadeHighlighters[i].SetActive(true);

                    yield return new WaitForSeconds(highlightDuration);
                }
            }
        }

        private void ApplySelectionScale(int activeIdx)
        {
            for (int i = 0; i < laneCars.Length; i++)
            {
                laneCars[i].transform.localScale = (i == activeIdx) ? Vector3.one * 1.15f : Vector3.one;
            }
        }

        public void DeactivateAllHighlighters()
        {
            foreach (var h in barricadeHighlighters) h.SetActive(false);
        }

        public string GetOptionText(int index)
        {
            TextMeshProUGUI[] activeOptions = isTestModeActive ? testOptions : practiceOptions;
            if (index >= 0 && index < activeOptions.Length)
                return activeOptions[index].text;
            return "";
        }

        public void PlayWrongFeedback(int index)
        {
            Debug.Log($"Wrong answer at lane {index}");
        }

        private int CalculateStars(float score)
        {
            if (score >= 90) return 3;
            if (score >= 60) return 2;
            if (score >= 30) return 1;
            return 0;
        }

        public IEnumerator ShowWinPanel(float score, int correct, int total, bool completed)
        {
            blockPanel.gameObject.SetActive(true);
            winEffects.Play();
            CarTollSoundManager.instance.PlayWin();

            yield return new WaitForSeconds(1.5f);
            blockPanel.gameObject.GetComponent<CarTollAutoPopup>().Close();

            practicePanel.SetActive(false);
            testPanel.SetActive(false);
            winPanel.SetActive(true);
            winImage.SetActive(completed);
            loseImage.SetActive(!completed);

            int stars = CalculateStars(score);
            for (int i = 0; i < starImages.Length; i++)
            {
                starImages[i].sprite = (i < stars) ? filledStar : emptyStar;
            }

            var summary = FindFirstObjectByType<CarTollSummaryManager>(FindObjectsInactive.Include);
            if (summary != null) summary.ShowSummary(score, correct, total);

            if (finalScoreText != null)
            {
                finalScoreText.text = $"{Mathf.RoundToInt(score)}%";
            }
        }
    }
}