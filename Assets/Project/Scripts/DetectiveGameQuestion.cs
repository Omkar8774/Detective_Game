using System;
using UnityEngine;

namespace Eduzo.Games.DetectiveGame.Data
{
    [Serializable]
    public class DetectiveGameQuestion
    {
        public string prompt;
        public string correctAnswer;
        public string[] wrongAnswers = new string[1];

        // Editor default sprite (optional)
        public Sprite referenceImage;

        // Persistent path for runtime-uploaded image (Application.persistentDataPath/...)
        // When user picks an image on-device we save it to persistentDataPath and store the path here.
        public string referenceImagePath;

        // Optional per-option sprites (keeps backward/extended support)
        public Sprite[] optionSprites = new Sprite[1];

        // Returns raw options in fixed order (correct first, then wrongs)
        public string[] GetOptionsArray()
        {
            int total = 1 + (wrongAnswers != null ? wrongAnswers.Length : 0);
            string[] options = new string[total];
            options[0] = correctAnswer;
            for (int i = 0; i < (wrongAnswers?.Length ?? 0); i++)
            {
                options[1 + i] = wrongAnswers[i];
            }
            return options;
        }

        // Returns raw sprites in fixed order aligned with GetOptionsArray()
        public Sprite[] GetOptionSpritesArray()
        {
            int total = 1 + (wrongAnswers != null ? wrongAnswers.Length : 0);
            Sprite[] sprites = new Sprite[total];

            if (optionSprites != null)
            {
                for (int i = 0; i < total; i++)
                {
                    sprites[i] = (i < optionSprites.Length) ? optionSprites[i] : null;
                }
            }
            else
            {
                for (int i = 0; i < total; i++) sprites[i] = null;
            }

            return sprites;
        }

        // Returns a shuffled order array: indices into the raw options array.
        public int[] GetShuffledOrder()
        {
            int total = 1 + (wrongAnswers != null ? wrongAnswers.Length : 0);
            int[] order = new int[total];
            for (int i = 0; i < total; i++) order[i] = i;

            System.Random rnd = new System.Random();
            for (int i = total - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                int tmp = order[i];
                order[i] = order[j];
                order[j] = tmp;
            }

            return order;
        }

        // Backwards-compatible convenience (keeps original API)
        public string[] GetShuffledOptions()
        {
            var opts = GetOptionsArray();
            var order = GetShuffledOrder();
            string[] shuffled = new string[opts.Length];
            for (int i = 0; i < opts.Length; i++) shuffled[i] = opts[order[i]];
            return shuffled;
        }
    }
}