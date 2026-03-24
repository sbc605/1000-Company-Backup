//코드 담당자: 유호정
using UnityEngine;
using UnityEngine.InputSystem;

public class TabletController : MonoBehaviour
{
    public GameObject tabletCanvas;
    public GameObject settingCanvas;
    public GameObject monitorCanvas;
    public GameObject guideCanvas;
    public MonitorInteract monitor;
    private SettingController setting;

    // private Animator playerAnimator;

    [Header("Component Deactivation")]
    private PlayerController playerController;
    private PlayerInteraction playerInteraction;
    private InventoryManager inventoryManager;
    ItemNoticeUI tutoUi;

    private bool isGuideOpen = false;
    private bool isTabletOpen = false;

    bool findMonitor = false;

    private void Awake()
    {
        // if (playerAnimator == null) playerAnimator = GetComponent<Animator>();
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (playerInteraction == null) playerInteraction = GetComponent<PlayerInteraction>();
        if (inventoryManager == null) inventoryManager = GetComponent<InventoryManager>();
        if (settingCanvas != null) setting = settingCanvas.GetComponent<SettingController>();
        if (tutoUi == null) tutoUi = FindFirstObjectByType<ItemNoticeUI>();
    }

    // 함수 추가 : 정하윤
    private void Start()
    {
        if (tabletCanvas != null)
        {
            tabletCanvas.SetActive(false);
        }
    }

    private void Update()
    {
        if (!findMonitor)
        {
            FindMonitor();
        }
    }

    public void OnTablet(InputValue value)
    {
        ToggleTablet();
    }


    // 함수 수정 : 정하윤
    public void ToggleTablet()
    {
        // if (playerAnimator != null)
        //     playerAnimator.SetBool("IsViewingTablet", isTabletOpen);


        isTabletOpen = !isTabletOpen;
        if (tabletCanvas != null)
            tabletCanvas.SetActive(isTabletOpen);

        //은주 추가 (튜토리얼일 때 퍼즈 풀리지 않기       

        if (tutoUi.isEnter && !isTabletOpen)
        {
            bool shouldFreeze = true;
            playerController.SetPaused(shouldFreeze);
            playerInteraction.enabled = !shouldFreeze;
            inventoryManager.SetPaused(shouldFreeze);
        }
        else
        {
            playerController.SetPaused(isTabletOpen);
            playerInteraction.enabled = !isTabletOpen;
            inventoryManager.SetPaused(isTabletOpen);
        }

        if (isTabletOpen)
            CursorManager.Instance.OpenPushUI();
        else
            CursorManager.Instance.ClosePopUI();
    }

    //함수 추가: 최은주
    public void OnSetting(InputValue value)
    {
        if (playerController == null || setting == null) return;

        // 추가: 김수아
        if (KioskTrigger.CloseCurrentKiosk())
            return;

        if (monitor != null && monitor.isMonitor)
        {
            //if (monitor.Runner && monitor.isMonitor) //CurrentUser == playerController.Runner.LocalPlayer 제외
            Debug.Log("모니터 나가기 요청 (OnSetting)");
            monitor.StopInteraction();
            return;
        }

        // 설정창 켜져있으면 끄기
        if (setting.isTurnon)
        {
            setting.TurnOFFUI();
            return;
        }

        // 아무것도 안 열려 있으면 설정창 켜기
        setting.TurnOnUI();
        Debug.Log("ESC → 설정창 켜기");
    }

    public void OnGuideBook(InputValue value)
    {
        if (!isGuideOpen)
        {
            guideCanvas.SetActive(true);
            isGuideOpen = true;
        }
        else
        {
            guideCanvas.SetActive(false);
            isGuideOpen = false;
        }
    }

    void FindMonitor()
    {
        if (monitorCanvas != null)
        {
            monitorCanvas = GameObject.FindWithTag("Monitor");
            //monitor = GameObject.FindWithTag("Computer").GetComponent<MonitorInteract>();
            findMonitor = true;
        }
    }
}