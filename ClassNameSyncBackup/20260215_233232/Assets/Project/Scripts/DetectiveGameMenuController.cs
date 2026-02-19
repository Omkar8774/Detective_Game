using UnityEngine;
using Eduzo.Games.DetectiveGame.UI;
using System.Collections;

namespace Eduzo.Games.DetectiveGame
{
    public class CarTollMenuController : MonoBehaviour
    {
        public CarTollGameManager gameManager;
        public CarTollUIManager uiManager;
        public GameObject noQuestionPanel;
        public GameObject queCheckListPanel;
        public GameObject summaryPanel;

        [Header("Form Toggle States")]
        public bool practiceToggle;
        public bool testToggle;

        private CarTollMode selectedMode;

        public void ChoosePractice()
        {
            selectedMode = CarTollMode.Practice;
            OpenForm();
        }

        public void ChooseTest()
        {
            selectedMode = CarTollMode.Test;
            OpenForm();
        }

        private void OpenForm()
        {
            uiManager.homePanel.SetActive(false);
            uiManager.formPanel.SetActive(true);
        }

        public void StartGameFromForm()
        {
            if (gameManager.questionManager.GetQuestions().Count == 0)
            {
                StartCoroutine(ShowNoQuestionWarning());
                return;
            }

            uiManager.formPanel.SetActive(false);
            gameManager.StartGame(selectedMode);
        }

        public void ToggleChecklist()
        {
            if (queCheckListPanel != null)
            {
                bool isActive = !queCheckListPanel.activeSelf;
                queCheckListPanel.SetActive(isActive);

                if (isActive)
                {
                    var listUI = Object.FindFirstObjectByType<Eduzo.Games.DetectiveGame.UI.CarTollQuestionListUI>();
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
            summaryPanel.SetActive(true);
        }

        public void OnClickRestart()
        {
            uiManager.winPanel.SetActive(false);
            if (summaryPanel != null) summaryPanel.SetActive(false);

            gameManager.ResetGameToInitialState();
            gameManager.StartGame(selectedMode);
        }

        public void OnChecklistCloseButtonClicked()
        {
            queCheckListPanel.SetActive(false);
            uiManager.formPanel.SetActive(false);

            bool isPractice = gameManager.mode == CarTollMode.Practice;
            practiceToggle = isPractice;
            testToggle = !isPractice;
        }

        public void OnClickGoToHome()
        {
            uiManager.winPanel.SetActive(false);
            uiManager.practicePanel.SetActive(false);
            uiManager.testPanel.SetActive(false);
            uiManager.formPanel.SetActive(false);

            if (summaryPanel != null) summaryPanel.SetActive(false);

            // Clear all UI Light states
            foreach (GameObject red in uiManager.practiceRedLights) red.SetActive(false);
            foreach (GameObject green in uiManager.practiceGreenLights) green.SetActive(false);
            foreach (GameObject red in uiManager.testRedLights) red.SetActive(false);
            foreach (GameObject green in uiManager.testGreenLights) green.SetActive(false);

            uiManager.homePanel.SetActive(true);

            gameManager.ResetGameToInitialState();
            gameManager.questionManager.DeleteAllQuestions();

            var listUI = Object.FindFirstObjectByType<Eduzo.Games.DetectiveGame.UI.CarTollQuestionListUI>();
            if (listUI != null) listUI.RefreshList();
        }

        public void QuitGame()
        {
            Application.Quit();
        }
    }
}