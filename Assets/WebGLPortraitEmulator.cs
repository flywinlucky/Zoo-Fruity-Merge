using System.Collections;
using UnityEngine;
using YG;

/// <summary>Fakes a phone screen on desktop: the browser window there is wide, so the camera is
/// clamped to a 9:16 strip in the middle and the leftover space is filled with
/// <see cref="sideColor"/>.
///
/// On mobile the page is already the right shape and the game must fill it, so the emulator
/// switches itself off completely - it does not touch the camera rect and it draws nothing.
///
/// The side colour is produced by CLEARING, not by drawing. The previous version painted two
/// bars in OnGUI, and that had an off-by-one it could not avoid: the bar widths came from
/// `(Screen.width - strip) / 2`, which truncates, so on every window width where that subtraction
/// is odd the two bars and the camera strip added up to one pixel less than the screen. The
/// column left over belonged to nobody - the camera does not clear outside its rect and no bar
/// reached it - so the raw backbuffer showed through as a thin black line down the side. It only
/// appeared at some widths, which is exactly what an odd/even bug looks like.
///
/// A camera that clears the whole screen has no arithmetic to get wrong: every pixel is covered
/// before the game is drawn on top of it, whatever the width. It also costs less than an OnGUI
/// pass every frame.</summary>
public class WebGLPortraitEmulator : MonoBehaviour
{
    [Header("Resolution Settings")]
    public Color sideColor = new Color32(0xFF, 0xEB, 0x9D, 0xFF);

    [Tooltip("How long to wait for the SDK to report the device before deciding. Until it answers " +
             "the plugin reports the default (desktop), so acting on frame 0 would letterbox phones.")]
    [SerializeField] float _sdkTimeout = 3f;

    private int portraitWidth = 720;
    private int portraitHeight = 1280;

    /// <summary>Desktop only. Stays false for the whole SDK wait so nothing is clamped before we
    /// know what we are running on.</summary>
    private bool isActive;

    private Camera cam;

    /// <summary>Renders nothing and exists only to clear. It sits behind the game camera and owns
    /// every pixel the game camera does not.</summary>
    private Camera background;

    private int lastScreenWidth;
    private int lastScreenHeight;
    private Color lastSideColor;

    private IEnumerator Start()
    {
        float waited = 0f;
        while (!YG2.isSDKEnabled && waited < _sdkTimeout)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        if (IsMobile())
        {
            // Nothing to undo if we never started, but a stale rect from a previous run of this
            // component would keep the letterbox alive, so make the full screen explicit.
            ResetCameraRect();
            enabled = false;
            yield break;
        }

        CreateBackgroundCamera();
        isActive = true;
        SetupResolution();
    }

    private bool IsMobile()
    {
        return YG2.envir.isMobile || Application.isMobilePlatform;
    }

    private Camera Cam
    {
        get
        {
            if (cam == null) cam = Camera.main;
            return cam;
        }
    }

    private void CreateBackgroundCamera()
    {
        if (Cam == null || background != null) return;

        var host = new GameObject("PortraitSideFill");
        host.transform.SetParent(Cam.transform, false);

        background = host.AddComponent<Camera>();
        background.clearFlags = CameraClearFlags.SolidColor;
        background.backgroundColor = sideColor;

        // Draws nothing at all. Clearing is the entire job, and rendering the scene a second time
        // to achieve it would be the expensive way to get the same pixels.
        background.cullingMask = 0;
        background.orthographic = true;
        background.rect = new Rect(0f, 0f, 1f, 1f);

        // Behind the game camera, so the game paints over the middle of what this just filled.
        background.depth = Cam.depth - 1;

        // One AudioListener per scene: this camera must not bring a second one.
        background.useOcclusionCulling = false;
        background.allowHDR = false;
        background.allowMSAA = false;

        lastSideColor = sideColor;
    }

    private void ResetCameraRect()
    {
        if (Cam != null) Cam.rect = new Rect(0f, 0f, 1f, 1f);
        if (background != null) background.enabled = false;
    }

    private void SetupResolution()
    {
        if (Cam == null) return;

        float targetAspect = (float)portraitWidth / portraitHeight;
        float currentAspect = (float)Screen.width / Screen.height;

        if (currentAspect > targetAspect)
        {
            // Kept in floats end to end. The rect is the only consumer now, and rounding it to
            // whole pixels was what created a column that belonged to neither side.
            float strip = targetAspect / currentAspect;
            Cam.rect = new Rect((1f - strip) * 0.5f, 0f, strip, 1f);
        }
        else
        {
            Cam.rect = new Rect(0f, 0f, 1f, 1f);
        }

        if (background != null) background.enabled = true;
    }

    private void Update()
    {
        if (!isActive) return;

        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            SetupResolution();
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
        }

        // So the colour can be tried out in the inspector while the game is running.
        if (background != null && sideColor != lastSideColor)
        {
            background.backgroundColor = sideColor;
            lastSideColor = sideColor;
        }
    }
}
