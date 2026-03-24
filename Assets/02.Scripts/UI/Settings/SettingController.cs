using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class SettingController : NetworkBehaviour
{
    public GameObject pauseUi;
    public GameObject SettingUI;
    public Button resumeBtn;
    public Button settingBtn;
    public Button outBtn;

    public bool isTurnon = false;

    public PlayerController playerController;

    private void Start()
    {
        resumeBtn.onClick.AddListener(TurnOFFUI);
        settingBtn.onClick.AddListener(TrunOnSetting);
        outBtn.onClick.AddListener(OnApplicationQuit);
        playerController = GetComponentInParent<PlayerController>();
    }

    public void TurnOnUI()
    {
        isTurnon = true;
        pauseUi.SetActive(true);
        playerController.SetPaused(true);
        CursorManager.Instance.OpenPushUI();
    }

    public void TurnOFFUI() //돌아가기
    {
        CursorManager.Instance.ClosePopUI();
        playerController.SetPaused(false);
        Debug.Log("세팅창 꺼지기");
        isTurnon = false;
        pauseUi.SetActive(false);
    }
    void TrunOnSetting()
    {
        SettingUI.SetActive(true);
    }

    private void OnApplicationQuit()
    {

    }
}
