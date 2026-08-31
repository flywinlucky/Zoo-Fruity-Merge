using Sirenix.OdinInspector;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WatermelonGameClone.Portal;

public class UnlockBigItemPopUp : MonoBehaviour
{
    public TMP_Text itemName_TMP;
    public Image itemIcon;
    public Button showReward;
    public Button closePopUp;
    public GameObject wannaGetItText;
    public GameObject noThxButton;
    public GameObject rewardButton;
    public GameObject freeButton;
    public GameObject okButton;

    public int currentIndex;

    private Coroutine showNoThanks;

    private void OnEnable()
    {
        InitializeUnlockBigItemPopUp();
    }

    public void InitializeUnlockBigItemPopUp()
    {
        currentIndex = ZooProgress.UnlockPopupState;

        if (currentIndex < 2)
        {
            currentIndex++; // Incrementăm indexul doar dacă este mai mic decât 2 (0 sau 1)
        }
        else
        {
            currentIndex = 3; // Dacă indexul este 2 sau mai mare, setăm direct la 3
        }

        UnlockState(currentIndex);

        ZooProgress.UnlockPopupState = currentIndex;
    }

    private IEnumerator ShowCloseButtonAfter()
    {
        yield return new WaitForSeconds(3);
        noThxButton.SetActive(true);
    }

    public void BigItemPopUpSetData(string itemName, Sprite itemSprite)
    {
        itemName_TMP.text = Loc.Format("item_unlocked", "<color=#00DDFF>" + Loc.ItemName(itemName) + "</color>");
        itemIcon.sprite = itemSprite;
    }

    [Button]
    public void UnlockState(int index)
    {
        switch (index)
        {
            case 3:
                WithReward();
                break;
            case 2:
                WithFree();
                break;
            case 1:
                WithOk();
                break;
        }
    }

    private void WithReward()
    {
        Debug.Log("With Reward Button");
        rewardButton.SetActive(true);
        freeButton.SetActive(false);
        okButton.SetActive(false);
        wannaGetItText.SetActive(true);
        
        showNoThanks = StartCoroutine(ShowCloseButtonAfter());
    }

    private void WithFree()
    {
        Debug.Log("With FREE Button");
        wannaGetItText.SetActive(false);
        rewardButton.SetActive(false);
        freeButton.SetActive(true);
        okButton.SetActive(false);

        DontShowNoThanks();
    }

    private void WithOk()
    {
        Debug.Log("With Ok Button");
        wannaGetItText.SetActive(false);
        rewardButton.SetActive(false);
        freeButton.SetActive(false);
        okButton.SetActive(true);

        DontShowNoThanks();
    }

    private void DontShowNoThanks()
    {
        if (showNoThanks != null)
            StopCoroutine(showNoThanks);
        noThxButton.SetActive(false);
    }
}