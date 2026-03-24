using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 코드 담당자: 김수아
// UI 마이크 아이콘 활성화
public class Chat : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private GameObject micIcon;
    // [SerializeField] private Slider volumeSlider;

    private string displayName;
    private bool isMuted;

    public void Setup(string name)
    {
        displayName = name;
        nameText.text = name;

        // volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    public void SetMicActive(bool active)
    {
        micIcon.SetActive(active);
    }

    // private void OnVolumeChanged(float value)
    // {
    //     VivoxManager.Instance.SetPlayerVolume(displayName, value);
    // }
}
