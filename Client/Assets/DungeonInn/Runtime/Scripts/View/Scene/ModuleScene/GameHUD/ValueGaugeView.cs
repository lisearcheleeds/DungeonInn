using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public sealed class ValueGaugeView : MonoBehaviour
    {
        [SerializeField] Image fillImage;
        [SerializeField] TMP_Text valueText;

        string label = string.Empty;

        void Awake()
        {
            Debug.Assert(fillImage != null, $"{nameof(fillImage)} is not assigned.", this);
            Debug.Assert(valueText != null, $"{nameof(valueText)} is not assigned.", this);
        }

        public void SetLabel(string value)
        {
            label = value ?? string.Empty;
        }

        public void SetFillColor(Color value)
        {
            if (fillImage != null)
            {
                fillImage.color = value;
            }
        }

        public void SetValue(int current, int max)
        {
            if (fillImage == null || valueText == null)
            {
                return;
            }

            var safeMax = Mathf.Max(0, max);
            var safeCurrent = Mathf.Clamp(current, 0, safeMax);
            fillImage.fillAmount = safeMax <= 0 ? 0f : (float)safeCurrent / safeMax;
            valueText.text = $"{label} {safeCurrent}/{safeMax}";
        }
    }
}
