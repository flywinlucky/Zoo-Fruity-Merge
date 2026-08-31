using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WatermelonGameClone.Portal;

namespace WatermelonGameClone
{
    public class GameView : MonoBehaviour
    {
        [Header("Score")]
        public GameObject _scorePanel;
        public TMP_Text currentScore_TMP_Text;
        public TMP_Text bestScore_TMP_Text;

        [Header("Current item")]
        public Image currentItemSprite;

        [Header("Merge goal")]
        public GameObject _mergeGoalPanel;
        public Image _mergeGoalSprite;

        public void InitializeTopPanel(bool showMergeGoal)
        {
            //_scorePanel.SetActive(!showMergeGoal);
            //_mergeGoalPanel.SetActive(showMergeGoal);
            _scorePanel.SetActive(false);
            _mergeGoalPanel.SetActive(true);
        }

        public void HideGoalPanel()
        {
            _scorePanel.SetActive(true);
            _mergeGoalPanel.SetActive(false);
        }

        public void SetMergeGoalSprite(Sprite goalSprite)
        {
            _mergeGoalSprite.sprite = goalSprite;
        }

        public void UpdateCurrentScore(int currentScore)
        {
            currentScore_TMP_Text.text = Loc.Format("score", currentScore);
        }

        public void UpdateBestScore(int bestScore)
        {
            bestScore_TMP_Text.text = bestScore.ToString();
        }

        public void SetCurrentItemSprite(Sprite itemSprite)
        {
            currentItemSprite.sprite = itemSprite;
        }
    }
}