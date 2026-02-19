using System.Collections.Generic;
using UnityEngine;
using Eduzo.Games.DetectiveGame.Data;

namespace Eduzo.Games.DetectiveGame.Data
{
    public class CarTollQuestionManager : MonoBehaviour
    {
        [Header("Question Data")]
        public List<CarTollQuestion> questions = new List<CarTollQuestion>();

        private List<int> order = new List<int>();
        private int currentIdx = 0;

        public void InitializeShuffle()
        {
            order.Clear();
            for (int i = 0; i < questions.Count; i++)
            {
                order.Add(i);
            }

            // Fisher-Yates Shuffle logic
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int tmp = order[i];
                order[i] = order[j];
                order[j] = tmp;
            }

            currentIdx = 0;
        }

        public CarTollQuestion GetNextQuestion()
        {
            if (questions.Count == 0 || currentIdx >= order.Count) return null;

            var q = questions[order[currentIdx]];
            currentIdx++;
            return q;
        }

        public void AddQuestionAtRuntime(CarTollQuestion newQ)
        {
            questions.Add(newQ);
        }

        public List<CarTollQuestion> GetQuestions() => questions;

        public void DeleteQuestion(int index)
        {
            if (index >= 0 && index < questions.Count)
                questions.RemoveAt(index);
        }

        public void DeleteAllQuestions() => questions.Clear();
    }
}