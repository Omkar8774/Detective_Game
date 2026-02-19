  using UnityEngine;
using TMPro;
using System.Text;
using Eduzo.Games.DetectiveGame.Data;

namespace Eduzo.Games.DetectiveGame.UI
{
    public class DetectiveGameSummaryManager : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI summaryText;

        [Header("Dependencies")]
        public DetectiveGameDataRecorder recorder;
        public DetectiveGameTimerController timer;

        public void ShowSummary(float scorePercent, int totalTaps, int correctAnswers, GameMode mode)
        {
            if (summaryText == null) return;

            StringBuilder sb = new StringBuilder();
            var logs = recorder.GetLogs();

            sb.AppendLine("<size=120%><b>==== GAME SCORE SUMMARY ====</b></size>");
            sb.AppendLine($"Mode: <b>{mode}</b>");
            sb.AppendLine($"Score: <b>{scorePercent:F0}%</b>");

            if (mode == GameMode.Test)
            {
                sb.AppendLine($"Active Time: {timer.GetFormattedTime()}");
                sb.AppendLine($"Correct Answers: <color=green>{correctAnswers}</color>");
                sb.AppendLine($"Wrong Answers: <color=red>{totalTaps - correctAnswers}</color>");
            }
            else
            {
                sb.AppendLine($"Total Attempts: {totalTaps}");
                sb.AppendLine($"Unique Questions Solved: {correctAnswers}");
            }

            sb.AppendLine("\n<size=110%><b>ATTEMPT BREAKDOWN</b></size>");
            sb.AppendLine("====");

            int currentQuestionNumber = 1;
            for (int i = 0; i < logs.Count; i++)
            {
                var log = logs[i];
                string resultColor = log.correct ? "#00FF00" : "#FF0000";
                string resultText = log.correct ? "CORRECT" : "INCORRECT";

                sb.AppendLine($"<b>Attempt {i + 1}</b> (Question {currentQuestionNumber})");
                sb.AppendLine($"<b>Question Prompt:</b> {log.question}");
                sb.AppendLine($"<b>User Choice:</b> {log.chosenAnswer}");
                sb.AppendLine($"<b>Expected Answer:</b> {log.correctAnswer}");
                sb.AppendLine($"<b>Result:</b> <color={resultColor}><b>{resultText}</b></color>");

                if (mode == GameMode.Test) sb.AppendLine($"<b>Timestamp:</b> {log.timestamp}");

                sb.AppendLine("-------------------------------------------\n");

                if (log.correct || mode == GameMode.Test) currentQuestionNumber++;
            }

            summaryText.text = sb.ToString();
        }
    }
}
