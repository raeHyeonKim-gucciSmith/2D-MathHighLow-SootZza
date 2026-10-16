using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class FinalGameUiLayoutUpdater
{
    private const string ScenePath = "Assets/Scenes/MathHighLowGame.unity";

    [MenuItem("MathHighLow/Apply Center Pile And Status Log Layout")]
    public static void ApplyLayout()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject drawPile = FindSceneObject(scene, "HWATU_DRAW_PILE");
        RectTransform drawPileRect = drawPile != null ? drawPile.GetComponent<RectTransform>() : null;
        if (drawPileRect == null)
        {
            throw new MissingReferenceException("HWATU_DRAW_PILE is missing.");
        }

        SetAnchors(drawPileRect, new Vector2(0.42f, 0.39f), new Vector2(0.58f, 0.63f));

        GameObject statusObject = FindSceneObject(scene, "Status_Message");
        Text statusText = statusObject != null ? statusObject.GetComponent<Text>() : null;
        if (statusText == null)
        {
            throw new MissingReferenceException("Status_Message is missing.");
        }

        RectTransform statusRect = statusText.rectTransform;
        RectTransform panelRect = statusRect.parent as RectTransform;
        if (panelRect == null || panelRect.name != "Status_Log_Panel")
        {
            Transform originalParent = statusRect.parent;
            int siblingIndex = statusRect.GetSiblingIndex();
            GameObject panelObject = new GameObject(
                "Status_Log_Panel", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.SetParent(originalParent, false);
            panelRect.SetSiblingIndex(siblingIndex);
            statusRect.SetParent(panelRect, false);
        }

        SetAnchors(panelRect, new Vector2(0.02f, 0.38f), new Vector2(0.25f, 0.68f));
        Image panelImage = panelRect.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.68f);
        panelImage.raycastTarget = false;

        SetAnchors(statusRect, new Vector2(0.07f, 0.06f), new Vector2(0.955f, 0.94f));
        statusText.color = Color.white;
        statusText.fontSize = 17;
        statusText.alignment = TextAnchor.UpperLeft;
        statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
        statusText.verticalOverflow = VerticalWrapMode.Truncate;
        statusText.supportRichText = false;
        statusText.raycastTarget = false;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[MathHighLow] Center pile and status log layout applied.");
    }

    private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static GameObject FindSceneObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform candidate in transforms)
            {
                if (candidate.name == objectName)
                {
                    return candidate.gameObject;
                }
            }
        }

        return null;
    }
}
