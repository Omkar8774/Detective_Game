using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Eduzo.Games.DetectiveGame.Data;

namespace Eduzo.Games.DetectiveGame.UI
{
    public class CarTollQuestionEntry : MonoBehaviour
    {
        public TextMeshProUGUI promptText;
        public TextMeshProUGUI correctText;
        public TextMeshProUGUI[] wrongTexts; // Size 3
        public Toggle selectionToggle;

        [HideInInspector] public int index;

        public void Setup(int idx, CarTollQuestion data)
        {
            index = idx;
            promptText.text = $"Q: {data.prompt}";
            //correctText.color = Color.green;
            correctText.text = $"� {data.correctAnswer}" ;
            correctText.color = Color.green;
            for (int i = 0; i < 3; i++)
            {
                wrongTexts[i].text = $"� {data.wrongAnswers[i]}";
                wrongTexts[i].color = Color.red;
            }
        }

        public bool IsSelected() => selectionToggle.isOn;
    }
}