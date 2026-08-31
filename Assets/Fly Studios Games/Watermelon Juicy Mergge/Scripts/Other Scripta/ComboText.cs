using TMPro;
using UnityEngine;

public class ComboText : MonoBehaviour
{
    public TMP_Text comboText;
    public ParticleSystem comboParticles; // redenumit pentru a evita conflictul

    public void ComboTextUI(string textMessage, Color textColor)
    {
        if (comboText)
        {
            comboText.text = textMessage;
            comboText.color = textColor;

            if (comboParticles)
            {
                var main = comboParticles.main;
                main.startColor = textColor;
                comboParticles.Play();
            }
        }
    }
}
