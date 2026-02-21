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

            uiManager?.SetOptionsInteractable(true);
            uiManager?.DisplayQuestion(currentQuestion);
            isInputLocked = false;
        }

        public void OnOptionSelected(int index)
        {
            if (isInputLocked) return;

            isInputLocked = true;
            uiManager?.SetOptionsInteractable(false);
            DetectiveGameSoundManager.instance?.PlayButtonClick();

            bool isCorrect = (index == correctIndex);
            
            // In Practice mode, we only count the "answered" state when they get it right
            // or if it's the first time they answer in Test mode.
            bool shouldAdvance = isCorrect || mode == GameMode.Test;
            if (shouldAdvance) totalAnswered++;

            recorder?.RecordResponse(currentQuestion?.prompt, uiManager?.GetOptionText(index) ?? "", currentQuestion?.correctAnswer ?? "", isCorrect, Time.time.ToString());

            bool isGameOver = false;
            if (isCorrect)
            {
                correctCount++;
                DetectiveGameSoundManager.instance?.PlayCorrect();
            }
            else
            {
                DetectiveGameSoundManager.instance?.PlayWrong();
                uiManager?.PlayWrongFeedback(index);

                if (mode == GameMode.Test)
                {
                    lives?.Decrement();
                    if (lives != null && lives.IsDead())
                    {
                        isGameOver = true;
                    }
                }
            }

            StartCoroutine(PostAnswerRoutine(index, isCorrect, isGameOver, shouldAdvance));
        }

        private IEnumerator PostAnswerRoutine(int index, bool wasCorrect, bool isGameOver, bool shouldAdvance)
        {
            if (uiManager != null)
            {
                yield return StartCoroutine(uiManager.PlayFeedbackRoutine(index, wasCorrect));
                
                bool hasNext = questionManager != null && questionManager.HasMoreQuestions();

                // Determine if we are about to show the final win/lose panel
                bool isAdvancingToPanel = isGameOver || (shouldAdvance && !hasNext);

                // Play general VFX if we are NOT showing the final panel yet
                if (!isAdvancingToPanel)
                {
                    if (wasCorrect) 
                        yield return StartCoroutine(uiManager.PlayNextQuestionSuccessVFXRoutine());
                    else
                        yield return StartCoroutine(uiManager.PlayNextQuestionFailureVFXRoutine());
                }
                else if (!shouldAdvance)
                {
                    // If staying on question (Practice mode wrong answer), brief delay for feel
                    yield return new WaitForSeconds(0.2f);
                }
                else
                {
                    yield return new WaitForSeconds(0.5f); // Tiny delay for feel before win panel
                }
            }
            else
            {
                yield return new WaitForSeconds(2.0f);
            }

            if (shouldAdvance)
            {
                if (isGameOver)
                    EndGame(false);
                else
                    LoadNextRound();
            }
            else
            {
                // In Practice mode, if wrong, unlock for another try
                isInputLocked = false;
                uiManager?.SetOptionsInteractable(true);
            }
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