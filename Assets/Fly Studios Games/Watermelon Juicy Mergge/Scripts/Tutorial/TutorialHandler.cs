using System.Collections.Generic;
using UnityEngine;
using WatermelonGameClone;
using WatermelonGameClone.Portal;

public class TutorialHandler : MonoBehaviour
{
    [Header("Tutorial objects")]
    [SerializeField] private GameObject _tutorialHand;
    [SerializeField] private WindowTutorial _tutorialWindow;

    [Header("Utility")]
    [SerializeField] private TouchDetection _touchDetection;

    [Header("Settings")]
    [SerializeField] private float _timerInterval = 6f;

    private bool _timerRunning;
    private float _timeLeft;

    private void Start()
    {
        PopUpActiveChecker.OnEnabled += StopTimer;
        PopUpActiveChecker.OnDisabled += ResetTimer;

        if (GameManager.Instance != null)
            GameManager.Instance.OnSphereDrop += DisableHandTutorial;

        _touchDetection.OnTouchDetected += ResetIfRunning;

        ResetTimer();
    }

    private void Update()
    {
        if (_timerRunning)
        {
            _timeLeft -= Time.deltaTime;

            if (_timeLeft <= 0)
                TimerRanOut();
        }
    }

    private void OnDisable()
    {
        PopUpActiveChecker.OnEnabled -= StopTimer;
        PopUpActiveChecker.OnDisabled -= ResetTimer;

        if (GameManager.Instance != null)
            GameManager.Instance.OnSphereDrop -= DisableHandTutorial;

        _touchDetection.OnTouchDetected -= ResetIfRunning;
    }

    #region Event methods

    private void StopTimer()
    {
        _timerRunning = false;
    }

    private void DisableHandTutorial()
    {
        _tutorialHand.SetActive(false);
        ResetTimer();
    }

    private void ResetIfRunning()
    {
        if (_timerRunning)
            ResetTimer();
    }

    #endregion

    private void TimerRanOut()
    {
        _timerRunning = false;
        _tutorialHand.SetActive(PopUpActiveChecker.PopupsActive == 0);
    }

    private void ResetTimer()
    {
        _timeLeft = _timerInterval;
        _timerRunning = true;
    }

    private void ShowTutorialPopupIfNeeded()
    {
        if (!ZooProgress.TutorialSeen)
            _tutorialWindow.gameObject.SetActive(true);
    }

    public void CloseTutorialPopup()
    {
        _tutorialWindow.gameObject.SetActive(false);

        if (!ZooProgress.TutorialSeen)
        {
            _tutorialHand.SetActive(true);
            ZooProgress.TutorialSeen = true;
        }
    }

    public void InitializeWindow(int skinId, List<GameManager.SpheresCategory> sphereSkins)
    {
        _tutorialWindow.Initialize(skinId, sphereSkins);
        ShowTutorialPopupIfNeeded();
    }
}
