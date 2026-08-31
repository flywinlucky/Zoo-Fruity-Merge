using System;
using System.Collections.Generic;
using UnityEngine;

namespace YG
{
    /// <summary>
    /// The game's half of the plugin's save object. Everything the player can lose lives here and
    /// nowhere else - there is deliberately no PlayerPrefs copy, because two sources of truth is
    /// how a cloud save loses to a stale local one.
    /// </summary>
    public partial class SavesYG
    {
        // --- score ----------------------------------------------------------
        public int bestScore;
        public int currentScore;

        // --- the board of the run in progress -------------------------------
        public bool hasSavedBoard;
        public List<SavedSphere> board = new List<SavedSphere>();

        // --- skins ----------------------------------------------------------
        public int selectedSkin;
        public bool secondSkinUnlocked;
        public int unlockPopupState;
        public List<string> bigItemsUnlocked = new List<string>();

        // --- settings -------------------------------------------------------
        public bool soundOn = true;
        public bool musicOn = true;

        // --- one-off flags --------------------------------------------------
        public bool tutorialSeen;
        public bool authDialogShown;
        public int sessions;

        /// <summary>One sphere resting on the board, stored so a reload can rebuild it.</summary>
        [Serializable]
        public class SavedSphere
        {
            public string itemId;
            public int indexInList;
            public Vector3 position;
            public Quaternion rotation;
        }
    }
}
