using System.Collections.Generic;
using UnityEngine;
using Eduzo.Games.DetectiveGame.Data;

namespace Eduzo.Games.DetectiveGame.Data
{
    public class DetectiveGameQuestionManager : MonoBehaviour
    {
        [Header("Question Data")]
        public List<DetectiveGameQuestion> questions = new List<DetectiveGameQuestion>();

        private List<int> order = new List<int>();
        private int currentIdx = 0;

        public void InitializeShuffle()
        {
            order.Clear();
            for (int i = 0; i < questions.Count; i++) order.Add(i);

            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int tmp = order[i];
                order[i] = order[j];
                order[j] = tmp;
            }

            currentIdx = 0;
        }

        public DetectiveGameQuestion GetNextQuestion()
        {
            if (questions.Count == 0 || currentIdx >= order.Count) return null;
            return questions[order[currentIdx++]];
        }

        public bool HasMoreQuestions() => questions.Count > 0 && currentIdx < order.Count;

        public void AddQuestion(DetectiveGameQuestion newQ) => questions.Add(newQ);

        public List<DetectiveGameQuestion> GetQuestions() => questions;

        public int GetTotalQuestions() => questions.Count;

        public void DeleteQuestion(int index)
        {
            if (index >= 0 && index < questions.Count) questions.RemoveAt(index);
        }

        public void DeleteAllQuestions() => questions.Clear();
    }
}