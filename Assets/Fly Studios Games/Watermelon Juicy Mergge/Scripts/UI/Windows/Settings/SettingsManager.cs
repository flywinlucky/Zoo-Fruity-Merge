using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using WatermelonGameClone.Portal;

/// <summary>
/// Sound and music switches. The two toggles are the player's setting; the mixer is only ever a
/// reflection of them, so the mixer is written from the saved value and never read back.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    private const float VolumeOn = 0f;
    private const float VolumeOff = -80f;

    [Header("Audio Mixer")]
    [Space]
    public AudioMixerGroup audioMixerGroup;
    [Space]
    public Toggle Sound_Toggle;
    public bool Sound_Bool; //Sound Efects Boll
    [Space]
    public Toggle Music_Toggle;
    public bool Music_Bool; //Music Sound Bool

    private bool _applyingSavedState;

    private void OnEnable()
    {
        ApplySavedState();
    }

    private void Start()
    {
        ApplySavedState();
    }

    /// <summary>
    /// Pushes the saved settings into the toggles and the mixer. Guarded, because assigning
    /// <c>isOn</c> fires the toggle's own callback and would otherwise write the value straight
    /// back out as if the player had just pressed it.
    /// </summary>
    private void ApplySavedState()
    {
        _applyingSavedState = true;

        Sound_Bool = ZooProgress.SoundOn;
        Music_Bool = ZooProgress.MusicOn;

        if (Sound_Toggle != null)
            Sound_Toggle.isOn = Sound_Bool;

        if (Music_Toggle != null)
            Music_Toggle.isOn = Music_Bool;

        ApplyToMixer("sfx_volume", Sound_Bool);
        ApplyToMixer("music_volume", Music_Bool);

        _applyingSavedState = false;
    }

    public void UpdateSoundSettings()
    {
        if (Sound_Toggle == null || _applyingSavedState)
            return;

        Sound_Bool = Sound_Toggle.isOn;
        ApplyToMixer("sfx_volume", Sound_Bool);
        ZooProgress.SoundOn = Sound_Bool;
    }

    public void UpdateMusicSettings()
    {
        if (Music_Toggle == null || _applyingSavedState)
            return;

        Music_Bool = Music_Toggle.isOn;
        ApplyToMixer("music_volume", Music_Bool);
        ZooProgress.MusicOn = Music_Bool;
    }

    private void ApplyToMixer(string parameter, bool on)
    {
        if (audioMixerGroup != null)
            audioMixerGroup.audioMixer.SetFloat(parameter, on ? VolumeOn : VolumeOff);
    }
}
