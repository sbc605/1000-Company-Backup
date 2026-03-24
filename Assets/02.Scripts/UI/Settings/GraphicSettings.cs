//코드 담당자: 유호정
using TMPro; 
using UnityEngine;
using UnityEngine.UI;

public class GraphicSettings : MonoBehaviour
{

    [SerializeField]
    private TMP_Dropdown screenModeDropdown;


    private const string ScreenModePrefKey = "ScreenModeSetting_Fullscreen";

    void Start()
    {

        if (screenModeDropdown != null)
        {

            screenModeDropdown.onValueChanged.RemoveAllListeners();
            

            screenModeDropdown.onValueChanged.AddListener(SetScreenMode);
        }


        LoadScreenSettings();
    }


    void LoadScreenSettings()
    {
        int savedIndex = PlayerPrefs.GetInt(ScreenModePrefKey, 0);
        if (screenModeDropdown != null)
        {
            screenModeDropdown.value = savedIndex;
        }

        ApplyScreenMode(savedIndex);
    }

    public void SetScreenMode(int index)
    {
        ApplyScreenMode(index);

        PlayerPrefs.SetInt(ScreenModePrefKey, index);
        PlayerPrefs.Save();
    }

    private void ApplyScreenMode(int index)
    {
        FullScreenMode mode;

        if (index == 0)
        {
            mode = FullScreenMode.ExclusiveFullScreen;
        }
        else
        {
            mode = FullScreenMode.Windowed;
        }
        Screen.fullScreenMode = mode;
        Debug.Log("화면 모드 변경: " + mode.ToString());
    }
}