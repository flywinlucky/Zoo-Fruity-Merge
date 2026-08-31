using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// One-shot editor helpers used to merge "Watermelon Home" into "Watermelon Game"
/// so the project ships as a single scene. Safe to delete once the merge is done.
/// </summary>
public static class SingleSceneSetup
{
    private const string SkinsIconPath =
        "Assets/Fly Studios Games/Watermelon Juicy Mergge/Art/ART/Watermelon_GUI_ART_1/Skins_Icon.png";

    private static GameObject FindInScene(string name)
    {
        var scene = SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            var found = FindRecursive(root.transform, name);
            if (found != null)
                return found.gameObject;
        }
        return null;
    }

    private static Transform FindRecursive(Transform t, string name)
    {
        if (t.name == name)
            return t;

        for (int i = 0; i < t.childCount; i++)
        {
            var found = FindRecursive(t.GetChild(i), name);
            if (found != null)
                return found;
        }
        return null;
    }

    [MenuItem("Tools/Single Scene/1 - Turn Home Button into Skins Button")]
    public static void WireSkinsButton()
    {
        var skinSelector = Object.FindObjectsOfType<WindowSelectSkin>(true).FirstOrDefault();
        if (skinSelector == null)
        {
            Debug.LogError("[SingleSceneSetup] Skin Selector not found in the scene.");
            return;
        }

        var button = FindInScene("Home Button") ?? FindInScene("Skins Button");
        if (button == null)
        {
            Debug.LogError("[SingleSceneSetup] Home Button / Skins Button not found.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(button, "Wire Skins Button");

        var open = button.GetComponent<OpenClosePanel>();
        if (open == null)
            open = button.AddComponent<OpenClosePanel>();

        open.UIpanel = new[] { skinSelector.gameObject };
        open.openPanels = new GameObject[0];
        open.closePanels = new GameObject[0];
        open.audioSource = button.GetComponent<AudioSource>();

        // the old "you have something new in the menu" badge no longer means anything
        var badge = FindRecursive(button.transform, "Home Notifi");
        if (badge != null)
            Object.DestroyImmediate(badge.gameObject);

        // swap the home icon for the skins (t-shirt) icon
        var icon = button.transform.childCount > 0 ? button.transform.GetChild(0) : null;
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SkinsIconPath);
        if (icon != null && sprite != null)
        {
            var image = icon.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = sprite;
                image.preserveAspect = true;
            }
            icon.name = "Icon";
            icon.localScale = Vector3.one * 0.62f;
        }
        else
        {
            Debug.LogWarning("[SingleSceneSetup] Icon child or Skins_Icon sprite missing.");
        }

        button.name = "Skins Button";

        EditorUtility.SetDirty(button);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[SingleSceneSetup] Skins Button wired to " + skinSelector.name);
    }

    [MenuItem("Tools/Single Scene/2 - Remove duplicate and empty objects")]
    public static void RemoveDeadObjects()
    {
        var scene = SceneManager.GetActiveScene();
        var removed = new List<string>();

        foreach (var root in scene.GetRootGameObjects())
        {
            bool isDuplicatePopupCanvas = root.name == "Popups Canvas (1)";
            bool isEmptyPlaceholder = root.name.Trim('.').Length == 0 &&
                                      root.transform.childCount == 0 &&
                                      root.GetComponents<Component>().Length == 1;

            if (isDuplicatePopupCanvas || isEmptyPlaceholder)
            {
                removed.Add(root.name);
                Object.DestroyImmediate(root);
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[SingleSceneSetup] Removed: " + (removed.Count == 0 ? "nothing" : string.Join(", ", removed)));
    }

    [MenuItem("Tools/Single Scene/4 - Add New Game button to Settings popup")]
    public static void AddNewGameButton()
    {
        const string prefabPath =
            "Assets/Fly Studios Games/Watermelon Juicy Mergge/Prefabs/UI/Settings.prefab";

        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var panel = root.transform.Find("Panel");
            if (panel == null)
            {
                Debug.LogError("[SingleSceneSetup] Settings prefab has no Panel.");
                return;
            }

            var adButton = panel.Find("Clear small fruits for ad");
            if (adButton == null)
            {
                Debug.LogError("[SingleSceneSetup] 'Clear small fruits for ad' not found.");
                return;
            }

            var existing = panel.Find("New game button");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            var copy = Object.Instantiate(adButton.gameObject, panel);
            copy.name = "New game button";

            var animator = copy.GetComponent<Animator>();
            if (animator != null)
                Object.DestroyImmediate(animator);

            var adIcon = copy.transform.Find("Ad icon");
            if (adIcon != null)
                Object.DestroyImmediate(adIcon.gameObject);

            var button = copy.GetComponent<Button>();
            if (button != null)
                button.onClick = new Button.ButtonClickedEvent();

            if (copy.GetComponent<NewGameButton>() == null)
                copy.AddComponent<NewGameButton>();

            var label = copy.transform.Find("Text");
            if (label != null)
            {
                var tmp = label.GetComponent<TMPro.TMP_Text>();
                if (tmp != null)
                {
                    tmp.text = "New Game";
                    var labelRect = (RectTransform)label;
                    labelRect.anchoredPosition = new Vector2(0f, labelRect.anchoredPosition.y);
                }
            }

            var panelRect = (RectTransform)panel;
            var adRect = (RectTransform)adButton;
            var copyRect = (RectTransform)copy.transform;

            panelRect.sizeDelta += new Vector2(0f, 170f);

            var options = panel.Find("Options panel") as RectTransform;
            if (options != null)
                options.anchoredPosition += new Vector2(0f, 70f);

            adRect.anchoredPosition = new Vector2(adRect.anchoredPosition.x, 285f);
            copyRect.anchoredPosition = new Vector2(adRect.anchoredPosition.x, 125f);
            copy.transform.SetSiblingIndex(adButton.GetSiblingIndex() + 1);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log("[SingleSceneSetup] New Game button added to the Settings prefab.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    [MenuItem("Tools/Single Scene/5 - Drop the Home scene from the build")]
    public static void DropHomeScene()
    {
        const string gamePath = "Assets/Fly Studios Games/Watermelon Juicy Mergge/Demo/Watermelon Game.unity";
        const string homePath = "Assets/Fly Studios Games/Watermelon Juicy Mergge/Demo/Watermelon Home.unity";

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(gamePath, true) };

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(homePath) != null)
        {
            if (!AssetDatabase.DeleteAsset(homePath))
            {
                Debug.LogError("[SingleSceneSetup] Could not delete " + homePath);
                return;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[SingleSceneSetup] Build now contains a single scene: " + gamePath);
    }


    [MenuItem("Tools/Single Scene/3 - Report scene state")]
    public static void Report()
    {
        var scene = SceneManager.GetActiveScene();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Scene: " + scene.name + "  roots: " + scene.rootCount);

        var skins = FindInScene("Skins Button");
        sb.AppendLine("Skins Button: " + (skins == null ? "MISSING" : "ok"));
        if (skins != null)
        {
            var open = skins.GetComponent<OpenClosePanel>();
            sb.AppendLine("  OpenClosePanel: " + (open == null ? "MISSING" : "panels=" +
                (open.UIpanel == null ? "null" : string.Join(",", open.UIpanel.Select(p => p == null ? "NULL" : p.name)))));
        }

        var selector = Object.FindObjectsOfType<WindowSelectSkin>(true).FirstOrDefault();
        sb.AppendLine("Skin Selector: " + (selector == null ? "MISSING" : selector.transform.parent.name + "/" + selector.name +
            "  active=" + selector.gameObject.activeSelf));

        foreach (var root in scene.GetRootGameObjects())
            sb.AppendLine("  root: " + root.name);

        Debug.Log(sb.ToString());
    }
    [MenuItem("Tools/Single Scene/Debug - Unlock second skin")]
    public static void DebugUnlockSkin()
    {
        PlayerPrefs.SetInt("unlockedLastItem", 1);
        PlayerPrefs.Save();
        Debug.Log("[Debug] unlockedLastItem set");
    }

    [MenuItem("Tools/Single Scene/Debug - Click the Skins button")]
    public static void DebugClickSkins()
    {
        var button = FindInScene("Skins Button");
        if (button == null)
        {
            Debug.LogError("[Debug] Skins Button not found");
            return;
        }

        button.GetComponent<Button>().onClick.Invoke();

        var selector = Object.FindObjectsOfType<WindowSelectSkin>(true).FirstOrDefault();
        Debug.Log("[Debug] clicked. selector active = " + (selector != null && selector.gameObject.activeSelf));
    }

    [MenuItem("Tools/Single Scene/Debug - Pick skin 1 and close the picker")]
    public static void DebugPickSkinOne()
    {
        var selector = Object.FindObjectsOfType<WindowSelectSkin>(true).FirstOrDefault();
        if (selector == null)
        {
            Debug.LogError("[Debug] no skin selector");
            return;
        }

        selector.SetSkinIndex(1);
        selector.gameObject.SetActive(false);
        Debug.Log("[Debug] picked skin 1 and closed the picker");
    }

    [MenuItem("Tools/Single Scene/Debug - Report runtime state")]
    public static void DebugRuntimeState()
    {
        var gm = WatermelonGameClone.GameManager.Instance;
        if (gm == null)
        {
            Debug.Log("[Debug] no GameManager instance (not playing?)");
            return;
        }

        Debug.Log("[Debug] selectedIndex=" + gm.selectedIndex +
                  " pref=" + PlayerPrefs.GetInt(WindowSelectSkin.SELECTED_SKIN_KEY, -1) +
                  " current=" + (gm.currentSphere == null ? "null" : gm.currentSphere.name) +
                  " next=" + (gm.nextSphere == null ? "null" : gm.nextSphere.name) +
                  " score=" + gm.CurrentScore.Value +
                  " popupsActive=" + PopUpActiveChecker.PopupsActive);
    }

    [MenuItem("Tools/Single Scene/Debug - Dump prefs")]
    public static void DebugDumpPrefs()
    {
        Debug.Log("[Debug] prefs: skin=" + PlayerPrefs.GetInt(WindowSelectSkin.SELECTED_SKIN_KEY, -1) +
                  " unlockedLastItem=" + PlayerPrefs.GetInt("unlockedLastItem", -1) +
                  " restoreSpheresData=" + PlayerPrefs.GetInt("restoreSpheresData", -1) +
                  " TotalItems=" + PlayerPrefs.GetInt("TotalItems", -1) +
                  " CurrentScore=" + PlayerPrefs.GetInt("CurrentScore", -1) +
                  " BestScore=" + PlayerPrefs.GetInt("BestScore", -1) +
                  " frame=" + Time.frameCount);
    }

}