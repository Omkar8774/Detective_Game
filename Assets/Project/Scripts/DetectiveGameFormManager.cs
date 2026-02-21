using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using Eduzo.Games.DetectiveGame.Data;

namespace Eduzo.Games.DetectiveGame.UI
{
    public class DetectiveGameFormManager : MonoBehaviour
    {
        [Header("Input Fields")]
        public TMP_InputField questionInput;
        public TMP_InputField correctInput;
        public TMP_InputField wrongInput;

        [Header("Reference Sprites")]
        public Sprite defaultReferenceSprite;
        public Sprite referenceSprite;
        public Image referenceImagePreview;

        [Header("Dependencies")]
        public DetectiveGameQuestionManager questionManager;
        public DetectiveGameUIManager uiManager;

        [Header("UI Feedback")]
        public TextMeshProUGUI feedbackText;
        public Button submitButton;

        private string pickedReferenceImageSavedPath;

        private void OnEnable() => ClearFields();
        private void OnDisable() => ClearFields();

        private void Start()
        {
            if (referenceImagePreview != null)
            {
                if (defaultReferenceSprite != null)
                {
                    referenceImagePreview.sprite = defaultReferenceSprite;
                    referenceImagePreview.preserveAspect = true;
                }

                var btn = referenceImagePreview.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveListener(OnPickReferenceImageButton);
                    btn.onClick.AddListener(OnPickReferenceImageButton);
                }
            }
            if (submitButton != null) submitButton.interactable = false;
        }

        public void OnPickReferenceImageButton()
        {
            DetectiveGameSoundManager.instance?.PlayButtonClick();
            StartCoroutine(PickImageFromDeviceRoutine());
        }

        private IEnumerator PickImageFromDeviceRoutine()
        {
            yield return null;
            string pickedPath = null;
            bool isDone = false;

#if UNITY_EDITOR
            pickedPath = UnityEditor.EditorUtility.OpenFilePanel("Select Reference Image", "", "png,jpg,jpeg");
            isDone = true;
#else
            Type galleryType = Type.GetType("NativeGallery, NativeGallery");
            if (galleryType == null) galleryType = Type.GetType("NativeGallery, Assembly-CSharp"); 
            if (galleryType == null) galleryType = Type.GetType("NativeGallery, NativeGallery.Runtime"); // UPM assembly name

            if (galleryType == null)
            {
                string errorMsg = "NativeGallery plugin missing! Please see NativeGalleryInstallation.md in project root.";
                Debug.LogError(errorMsg);
                StartCoroutine(ShowFeedback("Plugin Missing!", Color.red));
                yield break;
            }

            var delegateType = galleryType.GetNestedType("MediaPickCallback");
            if (delegateType == null) delegateType = galleryType.GetNestedType("GetImageDelegate"); // Fallback for older versions

            Action<string> cb = (path) => { pickedPath = path; isDone = true; };
            Delegate callback = Delegate.CreateDelegate(delegateType, cb.Target, cb.Method);

            var method = galleryType.GetMethod("GetImageFromGallery", new Type[] { delegateType, typeof(string), typeof(string) });
            if (method != null)
            {
                method.Invoke(null, new object[] { callback, "Select Reference Image", "image/*" });
                while (!isDone) yield return null;
            }
            else
            {
                StartCoroutine(ShowFeedback("NativeGallery API mismatch!", Color.red));
                yield break;
            }
#endif

            if (!string.IsNullOrEmpty(pickedPath))
            {
                LoadAndSavePickedImage(pickedPath);
                if (submitButton != null) submitButton.interactable = true;
            }
        }

        private void LoadAndSavePickedImage(string path)
        {
            if (!File.Exists(path)) return;
            byte[] fileData = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            if (tex.LoadImage(fileData))
            {
                referenceSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                if (referenceImagePreview != null)
                {
                    referenceImagePreview.sprite = referenceSprite;
                    referenceImagePreview.preserveAspect = true;
                }

                string fileName = $"ref_{DateTime.Now.Ticks}{Path.GetExtension(path)}";
                pickedReferenceImageSavedPath = Path.Combine(Application.persistentDataPath, fileName);
                File.WriteAllBytes(pickedReferenceImageSavedPath, fileData);
                StartCoroutine(ShowFeedback("Image Ready!", Color.green));
            }
        }

        public void OnSubmitForm()
        {
            DetectiveGameSoundManager.instance?.PlayButtonClick();
            if (string.IsNullOrEmpty(questionInput.text) || string.IsNullOrEmpty(correctInput.text) || string.IsNullOrEmpty(wrongInput.text))
            {
                StartCoroutine(ShowFeedback("Please fill all fields!", Color.red));
                return;
            }

            if (referenceSprite == defaultReferenceSprite || string.IsNullOrEmpty(pickedReferenceImageSavedPath))
            {
                StartCoroutine(ShowFeedback("Please upload an image!", Color.red));
                return;
            }

            DetectiveGameQuestion q = new DetectiveGameQuestion
            {
                prompt = questionInput.text,
                correctAnswer = correctInput.text,
                wrongAnswers = new string[] { wrongInput.text },
                referenceImage = referenceSprite,
                referenceImagePath = pickedReferenceImageSavedPath
            };

            questionManager.AddQuestion(q);

            var listUI = UnityEngine.Object.FindFirstObjectByType<DetectiveGameQuestionListUI>();
            if (listUI != null) listUI.RefreshList();

            StartCoroutine(ShowFeedback("Question Added!", Color.green));
            ClearFields();
        }

        private IEnumerator ShowFeedback(string msg, Color col)
        {
            if (feedbackText == null) yield break;
            feedbackText.text = msg;
            feedbackText.color = col;
            yield return new WaitForSeconds(2f);
            feedbackText.text = "";
        }

        public void ClearFields()
        {
            questionInput.text = "";    
            correctInput.text = "";
            wrongInput.text = "";

            pickedReferenceImageSavedPath = null;
            referenceSprite = defaultReferenceSprite;

            if (referenceImagePreview != null)
            {
                referenceImagePreview.sprite = defaultReferenceSprite;
                referenceImagePreview.preserveAspect = true;
                referenceImagePreview.gameObject.SetActive(defaultReferenceSprite != null);
            }

            if (submitButton != null) submitButton.interactable = false;
            if (feedbackText != null) feedbackText.text = "";
        }
    }
}