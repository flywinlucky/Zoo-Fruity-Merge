using TMPro;
using UnityEngine;

namespace WatermelonGameClone.Portal
{
    /// <summary>
    /// Drives one TMP label from the string table. It re-reads on <see cref="Loc.Changed"/> as well
    /// as on enable, which matters because the portal reports the language after the first objects
    /// are already awake.
    ///
    /// It also shrinks the label to fit. Russian and Turkish run roughly a third longer than
    /// English, and this UI was authored to the English word - "Clear Small Fruits" becomes
    /// "Küçükleri Temizle" and would otherwise spill out of its pill.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string _key;

        [Tooltip("Optional argument for keys that contain {0}, e.g. the game version.")]
        [SerializeField] private string _argument;

        [Tooltip("Let a longer translation shrink to fit the rect it was authored for.")]
        [SerializeField] private bool _shrinkToFit = true;

        [Tooltip("Smallest fraction of the authored size the text may shrink to.")]
        [SerializeField, Range(0.3f, 1f)] private float _minimumScale = 0.62f;

        [SerializeField, HideInInspector] private float _authoredSize;

        private TMP_Text _label;

        public string Key
        {
            get => _key;
            set { _key = value; Apply(); }
        }

        private void Awake()
        {
            _label = GetComponent<TMP_Text>();
            CaptureAuthoredSize();
        }

        private void OnEnable()
        {
            Loc.Changed += Apply;
            Apply();
        }

        private void OnDisable()
        {
            Loc.Changed -= Apply;
        }

        public void Apply()
        {
            if (_label == null)
                _label = GetComponent<TMP_Text>();

            if (_label == null || string.IsNullOrEmpty(_key))
                return;

            _label.text = string.IsNullOrEmpty(_argument)
                ? Loc.Get(_key)
                : Loc.Format(_key, _argument);

            if (_shrinkToFit)
                ApplyShrinkToFit();
        }

        private void ApplyShrinkToFit()
        {
            CaptureAuthoredSize();

            if (_authoredSize <= 0f)
                return;

            _label.enableAutoSizing = true;
            _label.fontSizeMax = _authoredSize;
            _label.fontSizeMin = Mathf.Max(1f, _authoredSize * _minimumScale);
        }

        /// <summary>
        /// Remembers the size the label was designed at. Read from <c>fontSizeMax</c> when
        /// auto-sizing is already on, otherwise every re-apply would ratchet the ceiling down to
        /// whatever the last shrink produced.
        /// </summary>
        private void CaptureAuthoredSize()
        {
            if (_authoredSize > 0f || _label == null)
                return;

            _authoredSize = _label.enableAutoSizing ? _label.fontSizeMax : _label.fontSize;
        }
    }
}
