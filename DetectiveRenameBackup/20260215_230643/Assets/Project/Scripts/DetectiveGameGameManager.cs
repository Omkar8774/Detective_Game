using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Eduzo.Games.DetectiveGame.Data;
using Eduzo.Games.DetectiveGame.UI;

namespace Eduzo.Games.DetectiveGame
{
    public enum CarTollMode { Practice, Test }

    public class CarTollGameManager : MonoBehaviour
    {
        public CarTollMode mode;

        [Header("Car Movement Settings")]
        public float moveSpeed = 5f;

        [Header("References")]
        public CarTollQuestionManager questionManager;
        public CarTollUIManager uiManager;
        public CarTollTimerController timer;
        public CarTollLivesManager lives;
        public CarTollDataRecorder recorder;

        private CarTollQuestion currentQuestion;
        private int correctIndex = -1;
        private int totalAnswered = 0;
        private int correctCount = 0;
        private bool isInputLocked = true;
        private Coroutine highlighterCoroutine;

        public void StartGame(CarTollMode selectedMode)
        {
            mode = selectedMode;
            correctCount = 0;
            totalAnswered = 0;
            recorder.Clear();
            questionManager.InitializeShuffle();

            uiManager.SetupGameUI(mode == CarTollMode.Test);

            if (mode == CarTollMode.Test)
            {
                lives.ResetLives();
                timer.StartTimer();
            }

            ResetAllCarsToStart();
            LoadNextRound();
        }

        private void LoadNextRound()
        {
            currentQuestion = questionManager.GetNextQuestion();
            if (currentQuestion == null)
            {
                EndGame(true);
                return;
            }

            RandomizeCarSprites();
            StartCoroutine(AllCarsArrivalRoutine());
        }

        private void RandomizeCarSprites()
        {
            if (uiManager.carSprites.Length < 4) return;
            List<Sprite> availableSprites = new List<Sprite>(uiManager.carSprites);

            for (int i = 0; i < uiManager.laneCars.Length; i++)
            {
                UnityEngine.UI.Image carImage = uiManager.laneCars[i].GetComponent<UnityEngine.UI.Image>();
                int randomIndex = Random.Range(0, availableSprites.Count);
                carImage.sprite = availableSprites[randomIndex];
                availableSprites.RemoveAt(randomIndex);

                uiManager.AllLightsOff(i);
            }
        }

        private void StopHighlighter()
        {
            if (highlighterCoroutine != null)
            {
                StopCoroutine(highlighterCoroutine);
                highlighterCoroutine = null;
            }
        }

        private IEnumerator AllCarsArrivalRoutine()
        {
            isInputLocked = true;

            for (int i = 0; i < uiManager.laneCars.Length; i++)
            {
                uiManager.laneCars[i].transform.position = uiManager.carStartPoints[i].position;
                uiManager.SetLights(i, true, false);
            }

            float distance = 1f;
            while (distance > 0.1f)
            {
                distance = 0f;
                for (int i = 0; i < uiManager.laneCars.Length; i++)
                {
                    Transform car = uiManager.laneCars[i].transform;
                    Transform stop = uiManager.carStopPoints[i];
                    car.position = Vector3.MoveTowards(car.position, stop.position, moveSpeed * Time.deltaTime);
                    distance += Vector3.Distance(car.position, stop.position);
                }
                yield return null;
            }

            for (int i = 0; i < uiManager.laneCars.Length; i++)
                uiManager.AllLightsOff(i);

            uiManager.DisplayQuestion(currentQuestion);
            highlighterCoroutine = StartCoroutine(uiManager.StartHighlighterLoop());
            isInputLocked = false;
        }

        public void OnCarSelected(int index)
        {
            if (isInputLocked) return;

            int activeHighlight = uiManager.GetActiveHighlighterIndex();
            if (index != activeHighlight) return;

            isInputLocked = true;
            StopHighlighter();
            uiManager.DeactivateAllHighlighters();

            totalAnswered++;
            bool isCorrect = (index == correctIndex);
            recorder.RecordResponse(currentQuestion.prompt, uiManager.GetOptionText(index), isCorrect, Time.time.ToString());

            if (isCorrect)
            {
                CarTollSoundManager.instance.PlayCorrect();
                correctCount++;
                StartCoroutine(CorrectDriveRoutine(index));
            }
            else
            {
                CarTollSoundManager.instance.PlayWrong();
                if (mode == CarTollMode.Test)
                {
                    lives.Decrement();
                    if (lives.IsDead())
                    {
                        EndGame(false);
                        return;
                    }
                }
                StartCoroutine(WrongAnswerRoutine(index));
            }
        }

        private IEnumerator WrongAnswerRoutine(int index)
        {
            GameObject selectedCar = uiManager.laneCars[index];
            Transform stopPoint = uiManager.carStopPoints[index];
            Vector3 approachPoint = stopPoint.position + (Vector3.up * 50f);

            uiManager.SetLights(index, true, false);
            while (Vector3.Distance(selectedCar.transform.position, approachPoint) > 0.1f)
            {
                selectedCar.transform.position = Vector3.MoveTowards(selectedCar.transform.position, approachPoint, moveSpeed * Time.deltaTime);
                yield return null;
            }

            uiManager.SetLights(index, false, true);
            uiManager.PlayWrongFeedback(index);
            yield return new WaitForSeconds(1f);

            uiManager.SetLights(index, true, true);
            while (Vector3.Distance(selectedCar.transform.position, stopPoint.position) > 0.1f)
            {
                selectedCar.transform.position = Vector3.MoveTowards(selectedCar.transform.position, stopPoint.position, moveSpeed * Time.deltaTime);
                yield return null;
            }

            uiManager.AllLightsOff(index);
            isInputLocked = false;
            highlighterCoroutine = StartCoroutine(uiManager.StartHighlighterLoop());
        }

        private IEnumerator CorrectDriveRoutine(int index)
        {
            GameObject selectedCar = uiManager.laneCars[index];
            selectedCar.transform.SetParent(uiManager.laneAreas[index], true);

            uiManager.SetLightGreen(index);
            uiManager.OpenBarricade(index);
            yield return new WaitForSeconds(0.5f);

            uiManager.SetLights(index, true, false);

            Transform targetExit = uiManager.passPoints[index];
            while (Vector3.Distance(selectedCar.transform.position, targetExit.position) > 0.1f)
            {
                selectedCar.transform.position = Vector3.MoveTowards(selectedCar.transform.position, targetExit.position, moveSpeed * Time.deltaTime);
                yield return null;
            }

            uiManager.AllLightsOff(index);
            uiManager.CloseBarricade(index);
            selectedCar.transform.position = uiManager.carStartPoints[index].position;
            yield return new WaitForSeconds(1);
            LoadNextRound();
        }

        public void ResetAllCarsToStart()
        {
            for (int i = 0; i < uiManager.laneCars.Length; i++)
            {
                uiManager.laneCars[i].transform.position = uiManager.carStartPoints[i].position;
                uiManager.AllLightsOff(i);
            }
        }

        public void ResetGameToInitialState()
        {
            StopAllCoroutines();
            isInputLocked = true;
            for (int i = 0; i < uiManager.laneCars.Length; i++)
            {
                uiManager.laneCars[i].transform.position = uiManager.carStartPoints[i].position;
                uiManager.laneCars[i].SetActive(true);
                uiManager.AllLightsOff(i);
            }
        }

        public void SetCorrectIndex(int idx) => correctIndex = idx;

        private void EndGame(bool completed)
        {
            isInputLocked = true;
            StopHighlighter();
            if (timer != null) timer.StopTimer();

            float finalScore = (totalAnswered > 0) ? ((float)correctCount / totalAnswered) * 100f : 0;
            StartCoroutine(uiManager.ShowWinPanel(finalScore, correctCount, totalAnswered, completed));
        }
    }
}