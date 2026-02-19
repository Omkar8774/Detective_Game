using System;
using UnityEngine;

namespace Eduzo.Games.DetectiveGame.Data
{
    [Serializable]
    public class CarTollQuestion
    {
        public string prompt;
        public string correctAnswer;
        public string[] wrongAnswers = new string[3]; // Total 4 options (1 correct + 3 wrong)

        public string[] GetShuffledOptions()
        {
            string[] options = new string[4];
            options[0] = correctAnswer;
            options[1] = wrongAnswers[0];
            options[2] = wrongAnswers[1];
            options[3] = wrongAnswers[2];

            System.Random rnd = new System.Random();
            for (int i = options.Length - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                string temp = options[i];
                options[i] = options[j];
                options[j] = temp;
            }
            return options;
        }
    }
}