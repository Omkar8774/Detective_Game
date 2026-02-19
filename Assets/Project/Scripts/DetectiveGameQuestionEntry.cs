using System.IO;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Networking;
using Eduzo.Games.DetectiveGame.Data;

namespace Eduzo.Games.DetectiveGame.UI
{
    public class DetectiveGameQuestionEntry : MonoBehaviour
    {
        public TextMeshProUGUI promptText;
        public TextMeshProUGUI correctText;
        public TextMeshProUGUI[] wrongTexts; // Size should be 1
        public Image referenceImageDisplay;
        public Toggle selectionToggle;

        [HideInInspector] public int index;

        public void Setup(int idx, DetectiveGameQuestion data)
        {
            index = idx;
            promptText.text = $"Q: {data.prompt}";
            correctText.text = $"{data.correctAnswer}";
            correctText.color = Color.green;

            if (wrongTexts != null && wrongTexts.Length > 0)
            {
                wrongTexts[0].text = $"{data.wrongAnswers[0]}";
                wrongTexts[0].color = Color.red;
            }

            // Start loading reference image (prefers saved path, falls back to inspector sprite)
            if (referenceImageDisplay != null)
            {
                referenceImageDisplay.sprite = null;
                referenceImageDisplay.gameObject.SetActive(false);
                if (!string.IsNullOrEmpty(data.referenceImagePath) && File.Exists(data.referenceImagePath))
                {
                    // Fix: Ensure the game object is active before starting a coroutine.
                    // If it's inactive (part of a hidden panel), we skip StartCoroutine here
                    // and let OnEnable handle it when the panel is shown.
                    if (gameObject.activeInHierarchy)
                    {
                        StartCoroutine(LoadImageFromPath(data.referenceImagePath));
                    }
                    else
                    {
                        // Store path for OnEnable
                        pendingImagePath = data.referenceImagePath;
                    }
                }
                else if (data.referenceImage != null)
                {
                    referenceImageDisplay.sprite = data.referenceImage;
                    referenceImageDisplay.preserveAspect = true;
                    referenceImageDisplay.gameObject.SetActive(true);
                }
            }
        }

        private string pendingImagePath;

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(pendingImagePath))
            {
                StartCoroutine(LoadImageFromPath(pendingImagePath));
                pendingImagePath = null;
            }
        }

        private IEnumerator LoadImageFromPath(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                if (referenceImageDisplay != null) referenceImageDisplay.gameObject.SetActive(false);
                yield break;
            }

            string uri = path;
            if (!uri.StartsWith("file://", System.StringComparison.OrdinalIgnoreCase))
                uri = "file://" + path;

            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(uri))
            {
                yield return uwr.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
                if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
#else
                if (uwr.isNetworkError || uwr.isHttpError)
#endif
                {
                    if (referenceImageDisplay != null) referenceImageDisplay.gameObject.SetActive(false);
                    yield break;
                }

                Texture2D tex = DownloadHandlerTexture.GetContent(uwr);
                if (tex == null)
                {
                    if (referenceImageDisplay != null) referenceImageDisplay.gameObject.SetActive(false);
                    yield break;
                }

                referenceImageDisplay.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                referenceImageDisplay.preserveAspect = true;
                referenceImageDisplay.gameObject.SetActive(true);
            }
        }

        public bool IsSelected() => selectionToggle.isOn;
    }
}