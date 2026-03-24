using UnityEngine;
using UnityEngine.UI;

public class IntroManager : MonoBehaviour
{
    /*
    //코드 담당자: 최은주
    //임시 메인씬 관리자
    [SerializeField] BasicSpawner bs;

    [SerializeField] Camera mainCamera;
    [SerializeField] GameObject introUi;

    [SerializeField] Button multiPlayBtn;
    [SerializeField] Button createRoomBtn;
    [SerializeField] Button joinRoomBtn;
    [SerializeField] Button outBtn;

    bool isPlayerIn;

    private async void Start()
    {
        this.gameObject.SetActive(true);
        introUi.SetActive(true);

        multiPlayBtn.onClick.AddListener(ClickMultiPlay);
        createRoomBtn.onClick.AddListener(CreateRoomBtn);
        joinRoomBtn.onClick.AddListener(JoinRoomBtn);

        outBtn.onClick.AddListener(OnApplicationQuit);

        // 코드추가: 김수아
        await VivoxManager.Instance.InitVivox();
        Debug.Log("Vivox 준비 완료");
    }

    void ClickMultiPlay()
    {
        createRoomBtn.gameObject.SetActive(true);
        joinRoomBtn.gameObject.SetActive(true);
    }

    void CreateRoomBtn()
    {
        bs.CreateRoom();
        Fade.onFadeAction(3f, Color.black, false, (TurnOffUI));
    }

    void TurnOffUI()
    {
        introUi.SetActive(false);
        mainCamera.gameObject.SetActive(false);
    }

    void JoinRoomBtn()
    {
        bs.JoinRoom();
        introUi.SetActive(false);
        Fade.onFadeAction(3f, Color.black, false, (TurnOffUI));
    }

    private void OnApplicationQuit()
    {

    }
    */
}
