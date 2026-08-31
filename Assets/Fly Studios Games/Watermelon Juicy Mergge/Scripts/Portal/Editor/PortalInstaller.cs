using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WatermelonGameClone.Portal;
using YG;

namespace WatermelonGameClone.PortalEditor
{
    /// <summary>
    /// Re-runnable setup for the Yandex integration. Everything here is idempotent: it can be run
    /// after a plugin update, after a prefab is re-authored, or just to check the wiring.
    /// </summary>
    public static class PortalInstaller
    {
        private const string GameScenePath =
            "Assets/Fly Studios Games/Watermelon Juicy Mergge/Demo/Watermelon Game.unity";

        private static readonly string[] LocalizedPrefabs =
        {
            "Assets/Fly Studios Games/Watermelon Juicy Mergge/Prefabs/UI/Settings.prefab",
            "Assets/Fly Studios Games/Watermelon Juicy Mergge/Prefabs/UI/Skin Selector.prefab",
            "Assets/Fly Studios Games/Watermelon Juicy Mergge/Prefabs/UI/Game Over.prefab",
            "Assets/Fly Studios Games/Watermelon Juicy Mergge/Prefabs/UI/Unlocked New Item.prefab",
            "Assets/Fly Studios Games/Watermelon Juicy Mergge/Prefabs/UI/Unlocked New Skin.prefab",
            "Assets/Fly Studios Games/Watermelon Juicy Mergge/Prefabs/UI/Rule Items Tutorial.prefab",
            "Assets/Fly Studios Games/Watermelon Juicy Mergge/Prefabs/UI/Hand Tutorial.prefab",
        };

        /// <summary>
        /// Authored English text mapped onto a string-table key. Matching on the text already in
        /// the label finds the right objects without a hand-written path list, and keeps finding
        /// them if someone moves them around.
        /// </summary>
        private static readonly Dictionary<string, string> ByEnglishText = new Dictionary<string, string>
        {
            { "next", "next" },
            { "goal", "goal" },
            { "high score", "high_score" },
            { "settings", "settings" },
            { "new game", "new_game" },
            { "clear small fruits", "clear_small_fruits" },
            { "skins", "skins" },
            { "continue ?", "continue_q" },
            { "play on", "play_on" },
            { "give up", "give_up" },
            { "drop items and merge.", "tutorial_drop" },
            { "can you merge?", "can_you_merge" },
            { "ok", "ok" },
            { "no, thanks!", "no_thanks" },
            { "free", "free" },
            { "congratulations!", "congratulations" },
            { "wanna get it ?", "wanna_get_it" },
            { "unlocked new skin !", "unlocked_new_skin" },
            { "available in skin selector", "available_in_skins" },
        };

        [MenuItem("Tools/Yandex/1 - Configure plugin settings")]
        public static void ConfigurePlugin()
        {
            InfoYG info = Resources.Load<InfoYG>("SettingsYG2");
            if (info == null)
            {
                Debug.LogError("[Portal] SettingsYG2 not found in Resources.");
                return;
            }

            // The bridge reports Game Ready itself, once the board is actually playable. Left on
            // automatic, the plugin reports it at SDK init, while the game is still empty.
            info.Basic.autoGRA = false;

            // No interstitial on the very first load. The single scene reloads on "new game" and on
            // a skin change, and an ad on each of those would be an ad on almost every action.
            info.InterstitialAdv.showFirstAdv = false;

            // an interstitial straight after a rewarded ad is two ads back to back
            info.RewardedAdv.skipInterAdvAfterReward = true;

            info.Storage.saveLocal = false;   // Yandex: the cloud is the source of truth
            info.Storage.saveCloud = true;

            info.Leaderboards.enable = true;
            info.Leaderboards.saveScoreAnonymousPlayers = true;

            // the editor simulation only answers for a board it knows by name, so point its first
            // fake board at ours - otherwise the window shows "no data" every time in the editor
            if (info.Leaderboards.listLBSim != null && info.Leaderboards.listLBSim.Length > 0)
                info.Leaderboards.listLBSim[0].technoName = "score";

            EditorUtility.SetDirty(info);
            AssetDatabase.SaveAssets();

            Debug.Log("[Portal] Plugin configured: autoGRA off, first-load interstitial off, " +
                      "cloud saves on, leaderboards on, Metrica left off.");
        }

        [MenuItem("Tools/Yandex/2 - Install bridge and translations")]
        public static void InstallIntoScene()
        {
            if (SceneManager.GetActiveScene().path != GameScenePath)
                EditorSceneManager.OpenScene(GameScenePath);

            var scene = SceneManager.GetActiveScene();

            // --- the bridge ---------------------------------------------------
            var bridge = Object.FindObjectOfType<PortalBridge>(true);
            if (bridge == null)
            {
                var host = GameObject.Find("GameManager");
                if (host == null)
                {
                    Debug.LogError("[Portal] No GameManager object to host the bridge.");
                    return;
                }

                bridge = host.AddComponent<PortalBridge>();
                Debug.Log("[Portal] PortalBridge added to " + host.name);
            }

            // --- translations -------------------------------------------------
            // Prefabs first. A label that lives in a prefab must get its component IN the prefab,
            // otherwise the scene pass adds one to the instance and the prefab adds a second.
            int inPrefabs = 0;
            foreach (string path in LocalizedPrefabs)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                if (root == null)
                    continue;

                try
                {
                    int added = InstallLabels(new[] { root });
                    if (added > 0)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        inPrefabs += added;
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();

            int inScene = InstallLabels(scene.GetRootGameObjects());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[Portal] Localized labels wired: " + inScene + " in the scene, " + inPrefabs + " in prefabs.");
        }

        private static int InstallLabels(IEnumerable<GameObject> roots)
        {
            int count = 0;

            foreach (var root in roots)
            {
                foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (label.GetComponent<LocalizedText>() != null)
                        continue;

                    string key = KeyFor(label.text);
                    if (key == null)
                        continue;

                    var localized = label.gameObject.AddComponent<LocalizedText>();
                    localized.Key = key;
                    EditorUtility.SetDirty(label.gameObject);
                    count++;
                }
            }

            return count;
        }

        private static string KeyFor(string authoredText)
        {
            if (string.IsNullOrEmpty(authoredText))
                return null;

            // collapse the double spaces and stray padding the UI was authored with
            string[] words = authoredText.Split(
                new[] { ' ', '\t', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0)
                return null;

            string normalized = string.Join(" ", words).ToLowerInvariant();
            return ByEnglishText.TryGetValue(normalized, out string key) ? key : null;
        }

        /// <summary>
        /// Removes the second LocalizedText from any object that ended up with two: one coming
        /// from its prefab and one added straight onto the scene instance.
        /// </summary>
        [MenuItem("Tools/Yandex/5 - Remove duplicate localized labels")]
        public static void RemoveDuplicateLabels()
        {
            var scene = SceneManager.GetActiveScene();
            int removed = 0;

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    var components = label.GetComponents<LocalizedText>();
                    if (components.Length < 2)
                        continue;

                    // keep the one that comes from the prefab, drop what was added on the instance
                    for (int i = components.Length - 1; i >= 0 && label.GetComponents<LocalizedText>().Length > 1; i--)
                    {
                        if (!PrefabUtility.IsAddedComponentOverride(components[i]))
                            continue;

                        Object.DestroyImmediate(components[i], true);
                        removed++;
                    }

                    // nothing was an instance override, so just keep the first
                    var left = label.GetComponents<LocalizedText>();
                    for (int i = left.Length - 1; i >= 1; i--)
                    {
                        Object.DestroyImmediate(left[i], true);
                        removed++;
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Portal] Removed " + removed + " duplicate localized labels.");
        }

        [MenuItem("Tools/Yandex/Language - English")]
        public static void SwitchEnglish() => SwitchLanguage(Loc.EN);

        [MenuItem("Tools/Yandex/Language - Russian")]
        public static void SwitchRussian() => SwitchLanguage(Loc.RU);

        [MenuItem("Tools/Yandex/Language - Turkish")]
        public static void SwitchTurkish() => SwitchLanguage(Loc.TR);

        /// <summary>Drives the language the way the portal would, so the live UI is what gets tested.</summary>
        private static void SwitchLanguage(string language)
        {
            if (Application.isPlaying)
                YG2.SwitchLanguage(language);
            else
                Loc.SetLanguage(language);

            Debug.Log("[Portal] language -> " + language);
        }

        /// <summary>
        /// Drops the old ad driver. It fired an interstitial on a blind 70-second timer, called
        /// Game Ready a second time on its own, and forced the language to English on every boot -
        /// which is why the game never showed up in Russian. All three now live in the bridge.
        /// </summary>
        [MenuItem("Tools/Yandex/7 - Remove the old ad driver")]
        public static void RemoveLegacyAdDriver()
        {
            if (SceneManager.GetActiveScene().path != GameScenePath)
                EditorSceneManager.OpenScene(GameScenePath);

            var scene = SceneManager.GetActiveScene();
            int removed = 0;

            foreach (var root in scene.GetRootGameObjects().ToList())
            {
                if (root.name != "ADS")
                    continue;

                Object.DestroyImmediate(root);
                removed++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            foreach (string path in new[]
            {
                "Assets/Fly Studios Games/Watermelon Juicy Mergge/Prefabs/Services/ADS.prefab",
                "Assets/InterstitialYG.cs",
                "Assets/FYGDontDestroyOnLoad.cs",
            })
            {
                if (AssetDatabase.LoadAssetAtPath<Object>(path) != null && AssetDatabase.DeleteAsset(path))
                    removed++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Portal] Old ad driver removed (" + removed + " objects/assets).");
        }

        [MenuItem("Tools/Yandex/3 - Report integration state")]
        public static void Report()
        {
            var info = Resources.Load<InfoYG>("SettingsYG2");
            var bridge = Object.FindObjectOfType<PortalBridge>(true);
            var labels = Object.FindObjectsOfType<LocalizedText>(true);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[Portal] integration state");
            sb.AppendLine("  bridge in scene: " + (bridge == null ? "MISSING" : "ok, leaderboard = " + bridge.LeaderboardName));
            sb.AppendLine("  localized labels in scene: " + labels.Length);

            if (info != null)
            {
                sb.AppendLine("  autoGRA: " + info.Basic.autoGRA + "   (want False)");
                sb.AppendLine("  saveCloud: " + info.Storage.saveCloud + "   saveLocal: " + info.Storage.saveLocal);
                sb.AppendLine("  leaderboards: " + info.Leaderboards.enable);
                sb.AppendLine("  metrica: " + info.Metrica.enable);
                sb.AppendLine("  first-load interstitial: " + info.InterstitialAdv.showFirstAdv);
            }

            foreach (var label in labels.OrderBy(l => l.Key))
                sb.AppendLine("    " + label.Key + " -> " + label.name);

            Debug.Log(sb.ToString());
        }

        [MenuItem("Tools/Yandex/Debug - Open the settings popup")]
        public static void DebugOpenSettings()
        {
            var settings = Object.FindObjectsOfType<SettingsManager>(true).FirstOrDefault();
            if (settings == null)
            {
                Debug.LogError("[Portal] no settings popup");
                return;
            }

            settings.gameObject.SetActive(true);
            Debug.Log("[Portal] settings opened, frame " + Time.frameCount);
        }

        [MenuItem("Tools/Yandex/4 - Check translations")]
        public static void CheckTranslations()
        {
            var missing = new List<string>();

            foreach (string key in Loc.Keys.ToList())
            {
                foreach (string language in new[] { Loc.EN, Loc.RU, Loc.TR })
                {
                    Loc.SetLanguage(language);
                    string value = Loc.Get(key);

                    if (string.IsNullOrEmpty(value) || value == key)
                        missing.Add(key + " [" + language + "]");
                }
            }

            Loc.SetLanguage(Loc.EN);

            if (missing.Count == 0)
                Debug.Log("[Portal] Translations OK: " + Loc.Keys.Count() + " keys x 3 languages.");
            else
                Debug.LogError("[Portal] Missing translations:\n" + string.Join("\n", missing));
        }
    }
}
