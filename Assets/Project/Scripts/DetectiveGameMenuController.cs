using System.Collections;
using System.IO;
using UnityEngine;
using Eduzo.Games.DetectiveGame.UI;

namespace Eduzo.Games.DetectiveGame
{
    public class DetectiveGameMenuController : MonoBehaviour
    {
        [Header("Dependencies")]
        public DetectiveGameGameManager gameManager;
        public DetectiveGameUIManager uiManager;

        [Header("Panels")]
        public GameObject noQuestionPanel;
        public GameObject queCheckListPanel;
        public GameObject summaryPanel;

        [Header("Mode Configuration")]
        public bool practiceToggle;
        public bool testToggle;

        public void ChoosePractice()
        {
            practiceToggle = true;
            testToggle = false;
            ChoosePlay();
        }

        public void ChooseTest()
        {
            practiceToggle = false;
            testToggle = true;
            ChoosePlay();
        }

        public void ChoosePlay()
        {
            DetectiveGameSoundManager.instance?.PlayButtonClick();
            ClearAllUploadedQuestionsAndImages();
            OpenForm();
        }

        private void OpenForm()
        {
            uiManager.homePanel.SetActive(false);
            uiManager.formPanel.SetActive(true);
        }

        public void StartGameFromForm()
        {
            DetectiveGameSoundManager.instance?.PlayButtonClick();
            if (gameManager.questionManager.GetQuestions().Count == 0)
            {
                StartCoroutine(ShowNoQuestionWarning());
                return;
            }

            uiManager.formPanel.SetActive(false);
            gameManager.StartGame(practiceToggle ? GameMode.Practice : GameMode.Test);
        }

        public void ToggleChecklist()
        {
            DetectiveGameSoundManager.instance?.PlayButtonClick();
            if (queCheckListPanel != null)
            {
                bool isActive = !queCheckListPanel.activeSelf;
                queCheckListPanel.SetActive(isActive);

                if (isActive)
                {
                    var listUI = UnityEngine.Object.FindFirstObjectByType<Eduzo.Games.DetectiveGame.UI.DetectiveGameQuestionListUI>();
                    if (listUI != null) listUI.RefreshList();
                }
            }
        }

        private IEnumerator ShowNoQuestionWarning()
        {
            noQuestionPanel.SetActive(true);
            yield return new WaitForSeconds(1.5f);
            noQuestionPanel.SetActive(false);

            uiManager.formPanel.SetActive(false);
            uiManager.homePanel.SetActive(true);
        }

        public void OnClickCheckSummary()
        {
            DetectiveGameSoundManager.instance?.PlayButtonClick();
            summaryPanel.SetActive(true);
        }

        public void OnClickRestart()
        {
            DetectiveGameSoundManager.instance?.PlayButtonClick();
            uiManager.winPanel.SetActive(false);
            if (summaryPanel != null) summaryPanel.SetActive(false);

            GameMode currentMode = gameManager.CurrentMode;
            gameManager.ResetGameToInitialState();
            gameManager.StartGame(currentMode);
        }

        public void OnChecklistCloseButtonClicked()
        {
            DetectiveGameSoundManager.instance?.PlayButtonClick();
            queCheckListPanel.SetActive(false);
            uiManager.formPanel.SetActive(false);
        }

        public void OnClickGoToHome()
        {
            DetectiveGameSoundManager.instance?.PlayButtonClick();
            uiManager.winPanel.SetActive(false);
            uiManager.practicePanel.SetActive(false);
            uiManager.testPanel.SetActive(false);
            uiManager.formPanel.SetActive(false);

            if (summaryPanel != null) summaryPanel.SetActive(false);
            uiManager.homePanel.SetActive(true);

            gameManager.ResetGameToInitialState();
            ClearAllUploadedQuestionsAndImages();
        }

        public void QuitGame()
        {
            DetectiveGameSoundManager.instance?.PlayButtonClick();
            Application.Quit();
        }

        private void ClearAllUploadedQuestionsAndImages()
        {
            if (gameManager == null || gameManager.questionManager == null) return;

            var qm = gameManager.questionManager;
            var questions = qm.GetQuestions();
            if (questions != null)
            {
                foreach (var q in questions)
                {
                    if (!string.IsNullOrEmpty(q.referenceImagePath))
                    {
                        try { if (File.Exists(q.referenceImagePath)) File.Delete(q.referenceImagePath); }
                        catch { }
                    }
                }
            }

            qm.DeleteAllQuestions();
            var listUI = UnityEngine.Object.FindFirstObjectByType<Eduzo.Games.DetectiveGame.UI.DetectiveGameQuestionListUI>();
            if (listUI != null) listUI.RefreshList();
        }
    }
}