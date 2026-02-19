using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eduzo.Games.DetectiveGame.Data
{
    [Serializable]
    public class CarTollResponseRecord
    {
        public string question;
        public string chosenAnswer;
        public bool correct;
        public string timestamp;
    }

    public class CarTollDataRecorder : MonoBehaviour
    {
        private List<CarTollResponseRecord> responses = new List<CarTollResponseRecord>();

        public void Clear() => responses.Clear();

        public void RecordResponse(string q, string choice, bool isCorrect, string time)
        {
            responses.Add(new CarTollResponseRecord
            {
                question = q,
                chosenAnswer = choice,
                correct = isCorrect,
                timestamp = time
            });
        }

        public List<CarTollResponseRecord> GetLogs() => responses;
    }
}