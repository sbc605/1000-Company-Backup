using UnityEngine;
using UnityEngine.UI;

// 작성자 : 정하윤
public class ButtonClickSound : MonoBehaviour
{
    [Header("재생할 사운드 이름")]
    [SerializeField]
    private string clickSoundName = "ui_click";

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(PlayClickSound);
    }

    private void PlayClickSound()
    {
        if (!string.IsNullOrEmpty(clickSoundName))
        {
            // SoundManager가 씬에 없을 경우를 대비
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.EventSoundPlay(clickSoundName);
            }
            else
            {
                Debug.LogWarning("SoundManager가 씬에 없음");
            }
        }
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlayClickSound);
        }
    }
}
