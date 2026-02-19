using UnityEngine;
using UnityEngine.EventSystems;

namespace Eduzo.Games.DetectiveGame
{
   

    // Attach to each option button (or option container). Set optionIndex in inspector.
    public class DetectiveGameOptionButton : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("0-based index of this option slot")]
        public int optionIndex;

        private DetectiveGameGameManager gameManager;

        private void Awake()
        {
            gameManager = Object.FindFirstObjectByType<DetectiveGameGameManager>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (gameManager != null)
            {
                gameManager.OnOptionSelected(optionIndex);
            }
        }
    }
}