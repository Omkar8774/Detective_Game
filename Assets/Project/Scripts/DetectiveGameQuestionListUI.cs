using UnityEngine;
using System.Collections.Generic;
using Eduzo.Games.DetectiveGame.Data;
using TMPro;

namespace Eduzo.Games.DetectiveGame.UI
{
    public class DetectiveGameQuestionListUI : MonoBehaviour
    {
        public DetectiveGameQuestionManager questionManager;
        public GameObject entryPrefab;
        public Transform container;
        
        private List<DetectiveGameQuestionEntry> entries = new List<DetectiveGameQuestionEntry>();

        public void RefreshList()
        {
            foreach (Transform t in container) Destroy(t.gameObject);
            entries.Clear();

            var questions = questionManager.GetQuestions();
            for (int i = 0; i < questions.Count; i++)
            {
                GameObject go = Instantiate(entryPrefab, container);
                var entry = go.GetComponent<DetectiveGameQuestionEntry>();
                entry.Setup(i, questions[i]);
                entries.Add(entry);
            }
        }
        
        public void DeleteSelected()
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].IsSelected()) questionManager.DeleteQuestion(entries[i].index);
            }
            RefreshList();
        }
    }
}