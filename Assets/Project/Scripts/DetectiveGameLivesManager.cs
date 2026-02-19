using UnityEngine;
using UnityEngine.UI;

namespace Eduzo.Games.DetectiveGame
{
    public class DetectiveGameLivesManager : MonoBehaviour
    {
        public int maxLives = 3;
        private int currentLives;

        public Image[] heartImages;
        public Sprite fullHeart;
        public Sprite emptyHeart;

        public void ResetLives()
        {
            currentLives = maxLives;
            UpdateHearts();
        }

        public void Decrement()
        {
            currentLives = Mathf.Max(0, currentLives - 1);
            UpdateHearts();
        }

        public bool IsDead() => currentLives <= 0;

        private void UpdateHearts()
        {
            for (int i = 0; i < heartImages.Length; i++)
            {
                heartImages[i].sprite = (i < currentLives) ? fullHeart : emptyHeart;
            }
        }
    }
}