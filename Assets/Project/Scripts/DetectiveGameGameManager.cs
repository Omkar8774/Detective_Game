using System.Collections;
using UnityEngine;
using Eduzo.Games.DetectiveGame.Data;
using Eduzo.Games.DetectiveGame.UI;

namespace Eduzo.Games.DetectiveGame
{
    public enum GameMode { Practice, Test }

    public class DetectiveGameGameManager : MonoBehaviour
    {
        [Header("Settings")]
        public GameMode mode = GameMode.Practice;

        [Header("References")]
        public DetectiveGameUIManager uiManager;
        public DetectiveGameQuestionManager questionManager;
        public DetectiveGameTimerController timer;
        public DetectiveGameLivesManager lives;
        public DetectiveGameDataRecorder recorder;

        private DetectiveGameQuestion currentQuestion;
        private int correctIndex;
        private int correctCount;
        private int totalAnswered;
        private bool isInputLocked;

        public GameMode CurrentMode => mode;

        private void Start()
        {
            isInputLocked = true;
        }

        public void StartGame(GameMode selectedMode)
        {
            mode = selectedMode;
            ResetGameState();

            if (uiManager != null)
                uiManager.SetupGameUI(mode == GameMode.Test);

            if (questionManager != null)
            {
                questionManager.InitializeShuffle();
                LoadNextRound();
            }

            if (mode == GameMode.Test)
            {
                timer?.StartTimer();
                lives?.ResetLives();
            }
        }

        private void ResetGameState()
        {
            correctCount = 0;
            totalAnswered = 0;
            isInputLocked = false;
            recorder?.Clear();
        }

        private void LoadNextRound()
        {
            currentQuestion = questionManager.GetNextQuestion();
            if (currentQuestion == null)
            {
                EndGame(true);
                return;
            }

            uiManager.DisplayQuestion(currentQuestion);
            isInputLocked = false;
        }

        public void OnOptionSelected(int index)
        {
            if (isInputLocked) return;

            DetectiveGameSoundManager.instance?.PlayButtonClick();

            bool isCorrect = (index == correctIndex);
            totalAnswered++;

            recorder?.RecordResponse(currentQuestion?.prompt, uiManager?.GetOptionText(index) ?? "", currentQuestion?.correctAnswer ?? "", isCorrect, Time.time.ToString());

            if (isCorrect)
            {
                isInputLocked = true;
                correctCount++;
                DetectiveGameSoundManager.instance?.PlayCorrect();
                StartCoroutine(PostAnswerRoutine(index, true));
            }
            else
            {
                DetectiveGameSoundManager.instance?.PlayWrong();
                uiManager?.PlayWrongFeedback(index);

                if (mode == GameMode.Test)
                {
                    isInputLocked = true;
                    lives?.Decrement();
                    if (lives != null && lives.IsDead())
                    {
                        EndGame(false);
                        return;
                    }
                }
                
                StartCoroutine(PlayWrongStayRoutine(index));
            }
        }

        private IEnumerator PlayWrongStayRoutine(int index)
        {
            isInputLocked = true;
            if (uiManager != null)
                yield return StartCoroutine(uiManager.PlayFeedbackRoutine(index, false));
            isInputLocked = false;
        }

        private IEnumerator PostAnswerRoutine(int index, bool wasCorrect)
        {
            if (uiManager != null)
            {
                bool hasNext = questionManager != null && questionManager.HasMoreQuestions();

                if (hasNext)
                {
                    yield return StartCoroutine(uiManager.PlayFeedbackRoutine(index, wasCorrect));
                    if (wasCorrect) yield return StartCoroutine(uiManager.PlayNextQuestionSuccessVFXRoutine());
                }
                else
                {
                    StartCoroutine(uiManager.PlayFeedbackRoutine(index, wasCorrect));
                    yield return new WaitForSeconds(0.1f);
                }
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }

            isInputLocked = false;
            LoadNextRound();
        }

        public void HandleTimeUp()
        {
            if (mode == GameMode.Test) EndGame(false);
        }

        public void ResetGameToInitialState()
        {
            StopAllCoroutines();
            isInputLocked = true;
            timer?.StopTimer();
        }

        public void SetCorrectIndex(int idx) => correctIndex = idx;

        private void EndGame(bool completed)
        {
            isInputLocked = true;
            timer?.StopTimer();

            float finalScore = 0;
            int totalQuestions = questionManager != null ? questionManager.GetTotalQuestions() : totalAnswered;

            if (mode == GameMode.Practice)
                finalScore = (totalAnswered > 0) ? ((float)correctCount / totalAnswered) * 100f : 0;
            else
                finalScore = (totalQuestions > 0) ? ((float)correctCount / totalQuestions) * 100f : 0;

            StartCoroutine(uiManager.ShowWinPanel(finalScore, correctCount, totalAnswered, completed, mode));
        }
    }
}