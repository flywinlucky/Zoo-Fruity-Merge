using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using WatermelonGameClone.Portal;

public class ComboSystem : MonoBehaviour
{
    public float timeIncreser;

    public float timeRemaining;
    public int count;

    [Space]
  
    public AudioSource clipsSource;
    public string txtCombo;
    public Transform textSpawnPoint;
    public GameObject textComboPrefab;

    [Header("Combo Text Collor")]
    [Space]

    public Color[] colors;
    public AudioClip[] audioClips;


    [Header("Outside Aray Color")]
    [Space]
    public Color outsideAraycolor;

    // Start is called before the first frame update
    void Start()
    {
        comborestart();
    }

    // Update is called once per frame
    void Update()
    {
        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
        }
        else
        {
            timeRemaining = 0;

            comborestart();
        }
    }

 
    public void IncreaseComboCount()
    {
        timeRemaining =+ timeIncreser;

        count++;

        if (count >= 2)
        {
            txtCombo = Loc.Format("combo", count - 1);

            SpawnNewCombotextPrefab(txtCombo);
        }

        //textComboPrefab.GetComponent<Animator>().Play("Base Layer.Open", 0, 0.25f);
    }

 
    public void SpawnNewCombotextPrefab(string txtMessage)
    {
        // the text goes on the SPAWNED copy. It used to be written into textComboPrefab itself,
        // which is the prefab asset - so every pop showed the previous combo and the asset was
        // being edited at runtime
        GameObject spawned = Instantiate(
            textComboPrefab, textSpawnPoint.transform.position, Quaternion.identity, textSpawnPoint);

        var comboText = spawned.GetComponent<ComboText>();
        if (comboText != null)
            comboText.ComboTextUI(txtMessage, AplyTextColor());
    }

    public void comborestart()
    {
        count = 0;

        txtCombo = "";
    }

    #region Combo Text Color

    //check which text state fits the bar fill amount and use that one for the text and color
    public Color AplyTextColor()
    {
        // Verificăm dacă count este în intervalul corect pentru array-ul de culori
        if (count >= 0 && count < colors.Length)
        {
            if (clipsSource && audioClips != null && count < audioClips.Length)
            {
                clipsSource.clip = audioClips[count];
                clipsSource.Play();
            }

            return colors[count];
        }
        else
        {
            // Dacă count este în afara intervalului, returnăm o culoare de rezervă sau puteți trata această situație în alt mod
            Debug.LogWarning("Indexul count este în afara intervalului pentru array-ul de culori. Returning default color.");
            return outsideAraycolor; // sau orice altă culoare implicită dorită
        }
    }


    #endregion
}