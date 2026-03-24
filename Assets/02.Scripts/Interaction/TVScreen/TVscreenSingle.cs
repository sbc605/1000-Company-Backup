using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TVscreenSingle : MonoBehaviour
{
    //ÄÚµå ´ã´çÀÚ: ÃÖÀºÁÖ 
    //½Ì±Û ¸ðµå¿¡¼­ÀÇ Æ¼ºñ ½ºÅ©¸°
    [SerializeField] GameObject multitv;
    [SerializeField] GameObject singletv;

    [SerializeField] TextMeshProUGUI danger; //À§ÂEµµ
    [SerializeField] TextMeshProUGUI weirdpm; //ÀÌ»óÇö»ó 

    [SerializeField] TextMeshProUGUI information; //ÀÇ·Ú Á¤º¸
    [SerializeField] TextMeshProUGUI title; //ÀÇ·Ú Á¦¸ñ 
    [SerializeField] TextMeshProUGUI readyCountText;

    [SerializeField] Button startButton;
    [SerializeField] Button createRoomBtn;
    [SerializeField] Button joinRoomBtn;

    DetailInfo quest;
    BoxCollider trigger;

    public int readyPlayerNumb; //·¹µðÇÑ ÇÃ·¹ÀÌ¾îµé
    bool isReady;

    private void Start()
    {
        title.text = "ÀÇ·Ú Á¤º¸ : ";
        startButton.onClick.AddListener(StartButton);
        createRoomBtn.onClick.AddListener(CreateRoom);
        joinRoomBtn.onClick.AddListener(JoinRoom);
        trigger = GetComponent<BoxCollider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ShowNowRequest(string requestName, int dangerness, int weirdPM, string content)
    {
        //¸®Äù½ºÆ® ¸Å´ÏÀú¿¡¼­ ÀÇ·Ú Áß »óÅÂ·Î º¯ÇÏ¸é ¿©±â·Î ¿Í¼­ Æ¼ºñ¿¡ ÇöÀç ÀÇ·Ú Á¤º¸ ¶ç¿ò

        title.text = "ÀÇ·Ú Á¤º¸:" + requestName;

        danger.text = "ÀÇ·Ú ³­ÀÌµµ : " + dangerness;
        weirdpm.text = "ÀÌ»ó Çö»ó :" + weirdPM + "°³";
        information.text = content;
    }

    public void ReadyButton() //·¹µðÇÏ±â
    {
        if (RequestManager.Instance.gameMode == GameModeState.Single && RequestManager.Instance.nowState == RequestState.IngReq)
        {
            RequestManager.Instance.SetReadyState(ReadyState.AllReady);
            readyPlayerNumb = 1;
            UpdateReadyCountText();
            return;
        }
    }

    public void ReadyCancel() //·¹µð Ãë¼ÒÇÏ±â
    {             
        RequestManager.Instance.SetReadyState(ReadyState.None);
        readyPlayerNumb = 0;
        UpdateReadyCountText();               
    }


    void StartButton() //½ÃÀÛ ¹öÆ°
    {       
         if(RequestManager.Instance.readyState == ReadyState.AllReady)
        {
            startButton.interactable = false;          
        }
    }

    public void CreateRoom()
    {
        Debug.Log("È£½ºÆ® ¸ðµå ÀüÈ¯ ½Ãµµ");
        singletv.SetActive(false);
        multitv.SetActive(true);
        //StartGame(GameMode.Host, "¹æ ¸¸µé±â");
    }

    public void JoinRoom()
    {
        Debug.Log("Á¶ÀÎ ·ë");
        singletv.SetActive(false);
        multitv.SetActive(true);
    }

    void UpdateReadyCountText()
    {
        readyCountText.text = $"·¹µð ÀÎ¿ø: {readyPlayerNumb}";
    }
}
