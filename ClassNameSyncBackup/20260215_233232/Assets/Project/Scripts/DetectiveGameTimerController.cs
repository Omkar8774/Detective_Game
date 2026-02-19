using UnityEngine;
using System.Collections;
using Eduzo.Games.DetectiveGame.UI;

namespace Eduzo.Games.DetectiveGame
{
    public class CarTollTimerController : MonoBehaviour
    {
        public float sessionTime = 60f;
        private float timeLeft;
        private bool running = false;

        public CarTollUIManager ui;
        public CarTollGameManager gameManager;

        public void StartTimer()
        {
            timeLeft = sessionTime;
            running = true;
            StartCoroutine(Tick());
        }

        public void StopTimer() => running = false;

        private IEnumerator Tick()
        {
            while (running && timeLeft > 0f)
            {
                yield return new WaitForSeconds(1f);
                timeLeft -= 1f;
                ui.UpdateTimerText(FormatTime(timeLeft));
            }

            if (timeLeft <= 0f)
            {
                running = false;
                gameManager.OnCarSelected(-1); // Trigger time-up logic
            }
        }

        public string GetFormattedTime() => FormatTime(sessionTime - timeLeft);

        private string FormatTime(float t)
        {
            int minutes = Mathf.FloorToInt(t / 60f);
            int seconds = Mathf.FloorToInt(t % 60f);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
}