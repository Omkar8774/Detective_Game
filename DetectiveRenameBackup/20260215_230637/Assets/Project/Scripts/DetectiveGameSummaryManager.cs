using UnityEngine;
using TMPro;
using System.Text;
using Eduzo.Games.DetectiveGame.Data;

namespace Eduzo.Games.DetectiveGame.UI
{
    public class CarTollSummaryManager : MonoBehaviour
    {
        public TextMeshProUGUI summaryText;
        public CarTollDataRecorder recorder;
        public CarTollTimerController timer;
        //public CarTollGameManager gameManager;
        private CarTollMode testMode;
        private void Start()
        {
            //testMode = CarTollMode.Test;
        }
        public void ShowSummary(float scorePercent, int total, int correct)
        {
            StringBuilder sb = new StringBuilder();
            var logs = recorder.GetLogs();

            // Header Section
            sb.AppendLine("<size=120%><b>==== GAME SCORE SUMMARY ====</b></size>");
            sb.AppendLine($"Score: <b>{scorePercent:F0}%</b>");
            if (CarTollMode.Test== testMode)
            {
                sb.AppendLine($"Active Time: {timer.GetFormattedTime()}");
            }
            
            sb.AppendLine($"Total Responses: {total}");
            sb.AppendLine($"Correct Answers: <color=green>{correct}</color>");
            sb.AppendLine($"Wrong Answers: <color=red>{total - correct}</color>");
            sb.AppendLine("\n<size=110%><b>QUESTION BREAKDOWN</b></size>");
            sb.AppendLine("====");

            // Dynamic Question Loop
            for (int i = 0; i < logs.Count; i++)
            {
                var log = logs[i];
                string resultColor = log.correct ? "#00FF00" : "#FF0000";
                string resultText = log.correct ? "CORRECT" : "INCORRECT";

                sb.AppendLine($"<b>Question {i + 1}</b>");
                sb.AppendLine($"<b>Question:</b> {log.question}");
                sb.AppendLine($"<b>User's Response:</b> {log.chosenAnswer}");
                sb.AppendLine($"<b>Result:</b> <color={resultColor}><b>{resultText}</b></color>");
                if (CarTollMode.Test == testMode)
                {
                    sb.AppendLine($"<b>Response Time:</b> {log.timestamp}");
                }                 
                sb.AppendLine("<b>Mistakes:</b> " + (log.correct ? "None" : "Wrong answer selected"));
                sb.AppendLine("-------------------------------------------\n");
            }

            // Apply to UI
            summaryText.text = sb.ToString();

            //StartCoroutine(ResetScroll());
        }
    }
}