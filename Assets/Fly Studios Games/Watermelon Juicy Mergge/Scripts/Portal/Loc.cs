using System;
using System.Collections.Generic;

namespace WatermelonGameClone.Portal
{
    /// <summary>
    /// Flat EN/RU/TR string table. No Unity dependency on purpose, so it can be compiled and
    /// checked outside the editor.
    ///
    /// <see cref="Get"/> falls back to English and never to the key, so a missing translation shows
    /// a real sentence instead of "merge_to_unlock".
    /// </summary>
    public static class Loc
    {
        public const string EN = "en";
        public const string RU = "ru";
        public const string TR = "tr";

        /// <summary>Raised after the language changes, so live labels can re-read themselves.</summary>
        public static event Action Changed;

        public static string Language { get; private set; } = EN;

        private static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
        {
            //  key                            EN                              RU                                  TR
            { "next",                 new[] { "Next",                          "Далее",                            "Sıra" } },
            { "goal",                 new[] { "GOAL",                          "ЦЕЛЬ",                             "HEDEF" } },
            { "high_score",           new[] { "HIGH SCORE",                    "РЕКОРД",                           "REKOR" } },
            { "score",                new[] { "Score {0}",                     "Очки {0}",                         "Puan {0}" } },

            { "settings",             new[] { "SETTINGS",                      "НАСТРОЙКИ",                        "AYARLAR" } },
            { "new_game",             new[] { "New Game",                      "Новая игра",                       "Yeni Oyun" } },
            { "clear_small_fruits",   new[] { "Clear Small Fruits",            "Убрать мелочь",                    "Küçükleri Temizle" } },
            { "version",              new[] { "Version {0}",                   "Версия {0}",                       "Sürüm {0}" } },

            { "skins",                new[] { "SKINS",                         "СКИНЫ",                            "GÖRÜNÜMLER" } },
            { "merge_to_unlock",      new[] { "Merge {0} to unlock",           "Соберите {0}, чтобы открыть",      "Açmak için {0} birleştirin" } },
            { "item_watermelon",      new[] { "watermelon",                    "арбуз",                            "karpuzu" } },
            { "item_bear",            new[] { "bear",                          "медведя",                          "ayıyı" } },

            { "continue_q",           new[] { "CONTINUE ?",                    "ПРОДОЛЖИТЬ?",                      "DEVAM?" } },
            { "play_on",              new[] { "PLAY ON",                       "ИГРАТЬ ДАЛЬШЕ",                    "DEVAM ET" } },
            { "give_up",              new[] { "Give Up",                       "Сдаться",                          "Pes Et" } },

            { "tutorial_drop",        new[] { "Drop items and merge.",         "Бросайте предметы и объединяйте.", "Nesneleri bırakın ve birleştirin." } },
            { "can_you_merge",        new[] { "CAN YOU MERGE?",                "СМОЖЕШЬ ОБЪЕДИНИТЬ?",              "BİRLEŞTİREBİLİR MİSİN?" } },
            { "ok",                   new[] { "OK",                            "ОК",                               "TAMAM" } },

            { "no_thanks",            new[] { "NO, THANKS!",                   "НЕТ, СПАСИБО!",                    "HAYIR, TEŞEKKÜRLER!" } },
            { "free",                 new[] { "FREE",                          "БЕСПЛАТНО",                        "ÜCRETSİZ" } },
            { "congratulations",      new[] { "CONGRATULATIONS!",              "ПОЗДРАВЛЯЕМ!",                     "TEBRİKLER!" } },
            { "item_unlocked",        new[] { "{0} UNLOCKED",                  "{0} ОТКРЫТ",                       "{0} AÇILDI" } },
            { "wanna_get_it",         new[] { "WANNA GET IT ?",                "ХОЧЕШЬ ЕГО?",                      "İSTER MİSİN?" } },

            { "unlocked_new_skin",    new[] { "UNLOCKED NEW SKIN !",           "НОВЫЙ СКИН ОТКРЫТ!",               "YENİ GÖRÜNÜM AÇILDI!" } },
            { "available_in_skins",   new[] { "AVAILABLE IN SKIN SELECTOR",    "ДОСТУПНО В ВЫБОРЕ СКИНОВ",         "GÖRÜNÜM SEÇİMİNDE" } },

            { "combo",                new[] { "{0}X COMBO",                    "{0}X КОМБО",                       "{0}X KOMBO" } },

            { "leaderboard",          new[] { "LEADERBOARD",                   "РЕЙТИНГ",                          "SIRALAMA" } },
            { "log_in",               new[] { "LOG IN",                        "ВОЙТИ",                            "GİRİŞ YAP" } },
            { "you",                  new[] { "You",                           "Вы",                               "Sen" } },
            { "close",                new[] { "CLOSE",                         "ЗАКРЫТЬ",                          "KAPAT" } },
        };

        /// <summary>Locales that Yandex reports for players who read Russian.</summary>
        private static readonly HashSet<string> RussianSpeaking = new HashSet<string>
        {
            "ru", "be", "uk", "kk", "uz", "az", "hy", "ka", "ky", "tg", "tk", "mo", "md"
        };

        public static void SetLanguage(string portalLanguage)
        {
            string resolved = Resolve(portalLanguage);
            if (resolved == Language)
                return;

            Language = resolved;
            Changed?.Invoke();
        }

        /// <summary>Maps whatever the portal reports onto one of the three languages we ship.</summary>
        public static string Resolve(string portalLanguage)
        {
            if (string.IsNullOrEmpty(portalLanguage))
                return EN;

            string code = portalLanguage.Trim().ToLowerInvariant();

            int separator = code.IndexOfAny(new[] { '-', '_' });
            if (separator > 0)
                code = code.Substring(0, separator);

            if (code == TR)
                return TR;

            if (RussianSpeaking.Contains(code))
                return RU;

            return EN;
        }

        public static string Get(string key)
        {
            if (key == null || !Table.TryGetValue(key, out string[] row))
                return key;

            string value = row[IndexOf(Language)];
            return string.IsNullOrEmpty(value) ? row[0] : value;
        }

        public static string Format(string key, params object[] args)
        {
            return string.Format(Get(key), args);
        }

        /// <summary>Name of a sphere as it reads inside "merge X to unlock". Unknown names come
        /// back untranslated rather than as a missing-key marker.</summary>
        public static string ItemName(string rawName)
        {
            if (string.IsNullOrEmpty(rawName))
                return string.Empty;

            string key = "item_" + rawName.Trim().ToLowerInvariant();
            return Table.ContainsKey(key) ? Get(key) : rawName;
        }

        public static bool Has(string key) => key != null && Table.ContainsKey(key);

        public static IEnumerable<string> Keys => Table.Keys;

        private static int IndexOf(string language)
        {
            switch (language)
            {
                case RU: return 1;
                case TR: return 2;
                default: return 0;
            }
        }
    }
}
