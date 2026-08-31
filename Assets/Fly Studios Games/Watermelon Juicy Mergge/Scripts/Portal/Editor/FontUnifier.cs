using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WatermelonGameClone.PortalEditor
{
    /// <summary>
    /// Puts the whole game on one font.
    ///
    /// Two reasons. Every extra TMP font asset drags its own atlas texture into the build, and the
    /// project was carrying five of them; and only Russo One is set up as a dynamic, multi-atlas
    /// font, so it is the only one that can render Cyrillic and Turkish at runtime instead of
    /// showing blanks.
    ///
    /// Re-runnable: it skips labels that are already on the font.
    /// </summary>
    public static class FontUnifier
    {
        private const string FontPath =
            "Assets/Fly Studios Games/Watermelon Juicy Mergge/Art/Fonts/RussoOne-Regular SDF.asset";

        private const string GameScenePath =
            "Assets/Fly Studios Games/Watermelon Juicy Mergge/Demo/Watermelon Game.unity";

        /// <summary>Folders whose prefabs belong to the game (vendor demo content is left alone).</summary>
        private static readonly string[] PrefabFolders =
        {
            "Assets/Fly Studios Games",
        };

        [MenuItem("Tools/Yandex/Font - Report fonts in use")]
        public static void ReportFonts()
        {
            var counts = new Dictionary<string, int>();

            foreach (var label in LabelsInScene())
                Count(counts, label);

            foreach (var path in PrefabPaths())
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
                        Count(counts, label);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            var sb = new System.Text.StringBuilder("[Font] fonts currently referenced\n");
            foreach (var pair in counts.OrderByDescending(p => p.Value))
                sb.AppendLine("  " + pair.Key + " x" + pair.Value);

            sb.AppendLine("  TMP default: " + (TMP_Settings.defaultFontAsset == null
                ? "none"
                : TMP_Settings.defaultFontAsset.name));

            Debug.Log(sb.ToString());
        }

        [MenuItem("Tools/Yandex/Font - Move everything to Russo One")]
        public static void Unify()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
            {
                Debug.LogError("[Font] Russo One font asset not found at " + FontPath);
                return;
            }

            int inScene = 0;
            int inPrefabs = 0;
            int keptMaterial = 0;

            // --- prefabs first, so scene instances inherit instead of getting overrides ---------
            foreach (var path in PrefabPaths())
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    int changed = Apply(root.GetComponentsInChildren<TMP_Text>(true), font, ref keptMaterial);
                    if (changed > 0)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        inPrefabs += changed;
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();

            // --- then the scene ----------------------------------------------------------------
            if (SceneManager.GetActiveScene().path != GameScenePath)
                EditorSceneManager.OpenScene(GameScenePath);

            var scene = SceneManager.GetActiveScene();
            inScene = Apply(LabelsInScene().ToArray(), font, ref keptMaterial);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            // --- and anything created at runtime ------------------------------------------------
            SetDefaultFont(font);

            Debug.Log($"[Font] Russo One applied to {inScene} labels in the scene and {inPrefabs} " +
                      $"in prefabs. {keptMaterial} labels had a custom material and were switched " +
                      "to the font's own - check those for lost outlines.");
        }

        private static int Apply(IReadOnlyList<TMP_Text> labels, TMP_FontAsset font, ref int customMaterials)
        {
            int changed = 0;

            foreach (var label in labels)
            {
                if (label == null || label.font == font)
                    continue;

                // a label carrying a material preset of its old font cannot keep it: the preset
                // points at the old atlas, so the glyphs would come out of the wrong texture
                if (label.fontSharedMaterial != null && label.font != null &&
                    label.fontSharedMaterial != label.font.material)
                {
                    customMaterials++;
                }

                label.font = font;
                label.fontSharedMaterial = font.material;

                EditorUtility.SetDirty(label);
                changed++;
            }

            return changed;
        }

        /// <summary>Also point TMP's default at the font, so text created at runtime matches.</summary>
        private static void SetDefaultFont(TMP_FontAsset font)
        {
            var settings = TMP_Settings.instance;
            if (settings == null)
                return;

            var serialized = new SerializedObject(settings);
            var property = serialized.FindProperty("m_defaultFontAsset");

            if (property != null)
            {
                property.objectReferenceValue = font;
                serialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
        }

        private static IEnumerable<TMP_Text> LabelsInScene()
        {
            if (SceneManager.GetActiveScene().path != GameScenePath)
                EditorSceneManager.OpenScene(GameScenePath);

            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
                    yield return label;
            }
        }

        private static IEnumerable<string> PrefabPaths()
        {
            return AssetDatabase.FindAssets("t:Prefab", PrefabFolders)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct();
        }

        private static void Count(Dictionary<string, int> counts, TMP_Text label)
        {
            string name = label.font == null ? "(none)" : label.font.name;
            counts[name] = counts.TryGetValue(name, out int n) ? n + 1 : 1;
        }
    }
}
