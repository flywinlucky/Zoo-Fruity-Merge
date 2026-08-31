using System;
using System.Collections;
using UnityEngine;
using YG;

namespace WatermelonGameClone.Portal
{
    /// <summary>
    /// The only file in the game that talks to the Yandex plugin. Everything else goes through
    /// <see cref="ZooProgress"/> and <see cref="Loc"/>, so porting to another portal is rewriting
    /// this one class.
    ///
    /// It owns four things the plugin will not do on its own:
    ///  - the boot gate. <c>YG2.isSDKEnabled</c> flips true BEFORE the cloud save arrives, so the
    ///    game must wait for the data callback, not for the SDK flag.
    ///  - Game Ready. autoGRA is off, because the plugin would report "ready" at SDK init, while
    ///    the board is still being rebuilt; the bridge reports it when the game is actually playable.
    ///  - gameplay start/stop, which the portal uses to decide when it may interrupt the player.
    ///  - throttled cloud writes. A merge chain fires several saves a second.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PortalBridge : MonoBehaviour
    {
        public static PortalBridge Instance { get; private set; }

        [Header("Leaderboard")]
        [Tooltip("Technical name of the leaderboard as created in the Yandex developer console.")]
        [SerializeField] private string _leaderboardName = "score";

        [Header("Timing")]
        [Tooltip("Seconds to wait for the SDK before starting the game anyway.")]
        [SerializeField] private float _sdkTimeout = 4f;

        [Tooltip("Minimum seconds between two cloud writes.")]
        [SerializeField] private float _cloudSaveInterval = 3f;

        /// <summary>Raised once the save data is usable, whether it came from the cloud or not.</summary>
        public static event Action Ready;

        /// <summary>Raised when a cloud save lands after the game already started.</summary>
        public static event Action LateDataArrived;

        public static bool IsReady => ZooProgress.Ready;
        public static bool IsAuthorized => YG2.player != null && YG2.player.auth;
        public static string PlayerName => YG2.player != null ? YG2.player.name : string.Empty;

        public string LeaderboardName => _leaderboardName;

        private float _nextSaveTime;
        private bool _gameplayRunning;
        private bool _bootFinished;

        #region Unity lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ZooProgress.Ready = false;
            ZooProgress.ResetForNewSession();
        }

        private void OnEnable()
        {
            YG2.onGetSDKData += OnSdkData;

            // a popup means the player is reading, not playing - the portal is allowed to use
            // that gap, and must not treat an ad shown during it as an interruption
            PopUpActiveChecker.OnEnabled += OnPopupOpened;
            PopUpActiveChecker.OnDisabled += OnPopupClosed;
#if Localization_yg
            YG2.onSwitchLang += OnLanguage;
#endif
        }

        private void OnDisable()
        {
            YG2.onGetSDKData -= OnSdkData;

            PopUpActiveChecker.OnEnabled -= OnPopupOpened;
            PopUpActiveChecker.OnDisabled -= OnPopupClosed;
#if Localization_yg
            YG2.onSwitchLang -= OnLanguage;
#endif
            StopGameplay();
            FlushIfPending(force: true);
        }

        private void Start()
        {
            ApplyLanguage();
            StartCoroutine(BootRoutine());
        }

        private void Update()
        {
            if (!ZooProgress.Ready)
                return;

            bool intervalElapsed = Time.unscaledTime >= _nextSaveTime;
            if (ZooProgress.ConsumeFlushRequest(intervalElapsed))
                WriteToCloud();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                FlushIfPending(force: true);
        }

        private void OnApplicationQuit()
        {
            FlushIfPending(force: true);
        }

        #endregion

        #region Boot

        /// <summary>
        /// Waits for the plugin to report data, then lets the game start. The timeout exists so a
        /// player on a broken connection still gets a game rather than a loading screen forever.
        /// </summary>
        private IEnumerator BootRoutine()
        {
            float deadline = Time.realtimeSinceStartup + _sdkTimeout;

            while (!YG2.isSDKEnabled && Time.realtimeSinceStartup < deadline)
                yield return null;

            // isSDKEnabled goes true before the cloud reply lands; give the data a moment to arrive
            while (!ZooProgress.Ready && Time.realtimeSinceStartup < deadline)
                yield return null;

            FinishBoot();
        }

        private void OnSdkData()
        {
            ApplyLanguage();

            if (_bootFinished)
            {
                // a cloud save that arrived after the game started
                LateDataArrived?.Invoke();
                return;
            }

            FinishBoot();
        }

        private void FinishBoot()
        {
            if (_bootFinished)
                return;

            _bootFinished = true;
            ZooProgress.Ready = true;
            YG2.saves.sessions++;

            Ready?.Invoke();
        }

        #endregion

        #region Language

        private void OnLanguage(string language) => Loc.SetLanguage(language);

        private void ApplyLanguage()
        {
#if Localization_yg
            Loc.SetLanguage(YG2.lang);
#endif
        }

        #endregion

        #region Portal signals

        /// <summary>Call when the player can actually act on the board.</summary>
        public void StartGameplay()
        {
            ReportGameReady();

            if (_gameplayRunning)
                return;

            _gameplayRunning = true;
            YG2.GameplayStart();
        }

        private void OnPopupOpened() => StopGameplay();

        private void OnPopupClosed()
        {
            // several popups can be up at once; only the last one closing puts the player back
            if (PopUpActiveChecker.PopupsActive <= 0 && _bootFinished)
                StartGameplay();
        }

        /// <summary>Call when play is interrupted: a popup, game over, the tab losing focus.</summary>
        public void StopGameplay()
        {
            if (!_gameplayRunning)
                return;

            _gameplayRunning = false;
            YG2.GameplayStop();
        }

        /// <summary>Tells the portal the loading screen is over. Safe to call repeatedly.</summary>
        public void ReportGameReady() => YG2.GameReadyAPI();

        /// <summary>
        /// An interstitial at a seam in the game - a run ending, a restart, a skin being applied.
        /// Never on a timer: this game has no lulls, so a timed ad always lands mid-merge.
        /// The plugin enforces its own minimum interval on top of this.
        /// </summary>
        public void ShowInterstitialAtBreak()
        {
#if InterstitialAdv_yg
            if (!ZooProgress.Ready)
                return;

            YG2.InterstitialAdvShow();
#endif
        }

        #endregion

        #region Leaderboard and auth

        /// <summary>
        /// Pushes the player's best score. Yandex keeps the higher of the two values, and the call
        /// is a silent no-op for a player who never logged in, so it needs no guard of its own.
        /// </summary>
        public void SubmitBestScore(int score)
        {
            if (score <= 0 || string.IsNullOrEmpty(_leaderboardName))
                return;

#if Leaderboards_yg
            YG2.SetLeaderboard(_leaderboardName, score);
#endif
        }

        public void OpenAuthDialog()
        {
#if Authorization_yg
            if (IsAuthorized)
                return;

            ZooProgress.AuthDialogShown = true;
            YG2.OpenAuthDialog();
#endif
        }

        #endregion

        #region Saving

        private void FlushIfPending(bool force)
        {
            if (!ZooProgress.Ready)
                return;

            if (ZooProgress.ConsumeFlushRequest(intervalElapsed: force))
                WriteToCloud();
        }

        private void WriteToCloud()
        {
            if (!YG2.isSDKEnabled)
                return;

            _nextSaveTime = Time.unscaledTime + _cloudSaveInterval;
            YG2.SaveProgress();
        }

        #endregion
    }
}
