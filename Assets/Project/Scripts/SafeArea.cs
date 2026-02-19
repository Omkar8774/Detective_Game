using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    void Start() => ApplySafeArea();

    void ApplySafeArea()
    {
        Rect safe = Screen.safeArea;
        RectTransform t = GetComponent<RectTransform>();

        Vector2 anchorMin = safe.position;
        Vector2 anchorMax = safe.position + safe.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        t.anchorMin = anchorMin;
        t.anchorMax = anchorMax;
    }
}
