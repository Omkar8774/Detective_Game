using UnityEngine;
using TMPro;
using System.Collections;
using Eduzo.Games.DetectiveGame.Data;

namespace Eduzo.Games.DetectiveGame.UI
{
    public class CarTollFormManager : MonoBehaviour
    {
        [Header("UI Fields")]
        public TMP_InputField questionInput;
        public TMP_InputField correctInput;
        public TMP_InputField[] wrongInputs;

        [Header("References")]
        public CarTollQuestionManager questionManager;
        public TextMeshProUGUI feedbackText;

        public void OnSubmitForm()
        {
            if (string.IsNullOrEmpty(questionInput.text) || string.IsNullOrEmpty(correctInput.text))
            {
                StartCoroutine(ShowFeedback("Please fill main fields!", Color.red));
                return;
            }

            foreach (var input in wrongInputs)
            {
                if (string.IsNullOrEmpty(input.text))
                {
                    StartCoroutine(ShowFeedback("Need 3 wrong answers!", Color.red));
                    return;
                }
            }

            CarTollQuestion newQuestion = new CarTollQuestion();
            newQuestion.prompt = questionInput.text;
            newQuestion.correctAnswer = correctInput.text;
            newQuestion.wrongAnswers = new string[]
            {
                wrongInputs[0].text,
                wrongInputs[1].text,
                wrongInputs[2].text
            };

            questionManager.AddQuestionAtRuntime(newQuestion);

            CarTollQuestionListUI listUI = Object.FindFirstObjectByType<CarTollQuestionListUI>();
            if (listUI != null)
                listUI.RefreshList();

            StartCoroutine(ShowFeedback("Question Added!", Color.green));
            ClearFields();
        }

        private IEnumerator ShowFeedback(string msg, Color col)
        {
            if (feedbackText != null)
            {
                feedbackText.text = msg;
                feedbackText.color = col;
            }

            yield return new WaitForSeconds(2);
            feedbackText.text = null;
        }

        private void ClearFields()
        {
            questionInput.text = "";
            correctInput.text = "";

            foreach (var input in wrongInputs)
                input.text = "";
        }
    }
}