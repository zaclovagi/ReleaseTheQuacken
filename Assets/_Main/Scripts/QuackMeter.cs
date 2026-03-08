using UnityEngine;
using UnityEngine.UI;

public class QuackMeter : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Slider slider;
    [SerializeField] private Gradient colorGradient;

    void Start()
    {
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.interactable = false;
        }
    }

    void Update()
    {
        if (player == null || slider == null) return;

        float t = player.QuackMeterNormalized;
        slider.value = t;

        if (colorGradient != null && slider.fillRect != null)
        {
            Image fill = slider.fillRect.GetComponent<Image>();
            if (fill != null)
                fill.color = colorGradient.Evaluate(t);
        }
    }
}
