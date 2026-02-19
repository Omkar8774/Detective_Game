using UnityEngine;
using UnityEngine.EventSystems;

namespace Eduzo.Games.DetectiveGame
{
    public class CarTollCarHandler : MonoBehaviour, IPointerClickHandler
    {
        [Header("Toll Settings")]
        public int carIndex; // Set this to 0, 1, 2, or 3 in the Inspector

        private CarTollGameManager gameManager;

        private void Awake()
        {
            gameManager = Object.FindFirstObjectByType<CarTollGameManager>();
        }

        // Handles Mobile/Mouse Tap
        public void OnPointerClick(PointerEventData eventData)
        {
            SelectThisBarricade();
        }

        // Call this from your Motion Gesture script when "Both Hands are Raised"
        public void SelectThisBarricade()
        {
            if (gameManager != null)
            {
                gameManager.OnCarSelected(carIndex);
            }
        }
    }
}