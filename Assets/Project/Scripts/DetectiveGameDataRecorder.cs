using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eduzo.Games.DetectiveGame.Data
{
    [Serializable]
    public class DetectiveGameResponseRecord
    {
        public string question;
        public string chosenAnswer;
        public string correctAnswer;
        public bool correct;
        public string timestamp;
    }

    public class DetectiveGameDataRecorder : MonoBehaviour
    {
        private List<DetectiveGameResponseRecord> responses = new List<DetectiveGameResponseRecord>();

        public void Clear() => responses.Clear();

        public void RecordResponse(string q, string choice, string correctAns, bool isCorrect, string time)
        {
            responses.Add(new DetectiveGameResponseRecord
            {
                question = q,
                chosenAnswer = choice,
                correctAnswer = correctAns,
                correct = isCorrect,
                timestamp = time
            });
        }

        public List<DetectiveGameResponseRecord> GetLogs() => responses;
    }
}