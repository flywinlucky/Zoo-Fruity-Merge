using System.Collections.Generic;
using UnityEngine;
using YG;

namespace WatermelonGameClone.Portal
{
    /// <summary>
    /// The single progress store. Game code talks to this and never to <see cref="YG2"/> directly,
    /// so moving to another portal means rewriting the portal folder and nothing else.
    ///
    /// Writes are collected and flushed by <see cref="PortalBridge"/>: the board is written every
    /// few seconds and the coin-sized changes (a merge, a settings toggle) would otherwise hit the
    /// cloud several times a second.
    /// </summary>
    public static class ZooProgress
    {
        /// <summary>True once the plugin has finished loading (cloud reply included).</summary>
        public static bool Ready { get; internal set; }

        private static bool _dirty;
        private static bool _flushNow;

        private static SavesYG Data => YG2.saves;

        #region Score

        public static int BestScore
        {
            get => Data.bestScore;
            set { Data.bestScore = value; MarkDirty(); }
        }

        public static int CurrentScore
        {
            get => Data.currentScore;
            set { Data.currentScore = value; MarkDirty(); }
        }

        #endregion

        #region Board

        public static bool HasSavedBoard => Data.hasSavedBoard;

        public static IReadOnlyList<SavesYG.SavedSphere> Board => Data.board;

        public static void StoreBoard(List<SavesYG.SavedSphere> spheres, int score)
        {
            Data.board = spheres;
            Data.hasSavedBoard = true;
            Data.currentScore = score;
            MarkDirty();
        }

        public static void ClearBoard()
        {
            Data.board.Clear();
            Data.hasSavedBoard = false;
            Data.currentScore = 0;
            MarkDirty();
        }

        #endregion

        #region Skins

        public static int SelectedSkin
        {
            get => Data.selectedSkin;
            set { Data.selectedSkin = value; SaveNow(); }
        }

        public static bool SecondSkinUnlocked
        {
            get => Data.secondSkinUnlocked;
            set { Data.secondSkinUnlocked = value; SaveNow(); }
        }

        public static int UnlockPopupState
        {
            get => Data.unlockPopupState;
            set { Data.unlockPopupState = value; MarkDirty(); }
        }

        public static List<string> BigItemsUnlocked => Data.bigItemsUnlocked;

        public static void MarkBigItemUnlocked(string itemName)
        {
            if (string.IsNullOrEmpty(itemName) || Data.bigItemsUnlocked.Contains(itemName))
                return;

            Data.bigItemsUnlocked.Add(itemName);
            MarkDirty();
        }

        #endregion

        #region Settings

        public static bool SoundOn
        {
            get => Data.soundOn;
            set { Data.soundOn = value; SaveNow(); }
        }

        public static bool MusicOn
        {
            get => Data.musicOn;
            set { Data.musicOn = value; SaveNow(); }
        }

        #endregion

        #region Flags

        public static bool TutorialSeen
        {
            get => Data.tutorialSeen;
            set { Data.tutorialSeen = value; SaveNow(); }
        }

        public static bool AuthDialogShown
        {
            get => Data.authDialogShown;
            set { Data.authDialogShown = value; MarkDirty(); }
        }

        #endregion

        #region Flushing

        /// <summary>Queue a cloud write; <see cref="PortalBridge"/> flushes it on its own interval.</summary>
        public static void MarkDirty() => _dirty = true;

        /// <summary>Write on the next frame regardless of the interval. For things a player would
        /// notice losing: a skin choice, a settings toggle, a finished run.</summary>
        public static void SaveNow()
        {
            _dirty = true;
            _flushNow = true;
        }

        internal static bool ConsumeFlushRequest(bool intervalElapsed)
        {
            if (!_dirty)
                return false;

            if (!_flushNow && !intervalElapsed)
                return false;

            _dirty = false;
            _flushNow = false;
            return true;
        }

        internal static void ResetForNewSession()
        {
            _dirty = false;
            _flushNow = false;
        }

        #endregion
    }
}
