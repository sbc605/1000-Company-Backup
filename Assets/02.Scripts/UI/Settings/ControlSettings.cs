using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ControlSettings : MonoBehaviour
{
    [SerializeField]
    private Slider sensitivitySlider;

    [SerializeField]
    private float defaultSensitivity = 30f;

    private const string SensitivityPrefKey = "MouseSensitivity";

    void Start()
    {
        float savedSensitivity = PlayerPrefs.GetFloat(SensitivityPrefKey, defaultSensitivity);

        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = savedSensitivity;

            sensitivitySlider.onValueChanged.RemoveAllListeners();
            sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
        }
    }

    public void SetSensitivity(float value)
    {
        PlayerPrefs.SetFloat(SensitivityPrefKey, value);
        PlayerPrefs.Save();


        if (PlayerController.Local != null)
        {
            PlayerController.Local.SetSensitivity(value);
        }
    }
}