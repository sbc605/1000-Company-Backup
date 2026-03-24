using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Fusion;

public class ElevatorGoButton : NetworkBehaviour, IInteractable
{
    //코드 담당자 : 최은주 
    //Go 버튼 눌렀을 때 Fade 되고 로딩창 뜨고 씬 매니저로 씬 이동

    public UnityEvent onButtonPressed;
    float pressingDepth = 0.012f;
    float initPosZ;

    public Collider button;
    public AudioClip buttonSound;
    public AudioClip movingSound;

    [ColorUsage(true, true)] public Color inactiveColor = Color.black;
    [ColorUsage(true, true)] public Color activeColor = new Color(121, 191, 97, 255);

    AudioSource audioSource;
    MaterialPropertyBlock propertyBlock;
    MeshRenderer meshRender;

    TriggerCheck playerCheck;

    private Outline outline;
    private bool isPressed = false;
    [SerializeField] private string sceneToLoad = "Request1";
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        meshRender = GetComponent<MeshRenderer>();
        button = GetComponent<Collider>();
        playerCheck = GetComponentInParent<TriggerCheck>();
        propertyBlock = new MaterialPropertyBlock();
        propertyBlock.SetColor("_EmissionColor", inactiveColor);

        initPosZ = transform.localPosition.z;

        if (outline == null)
            outline = GetComponent<Outline>();

        if (outline != null)
            outline.enabled = false;
    }

    public void Interact(GameObject interactor)
    {
        if (playerCheck == null || !playerCheck.isPlayerInside)
        {
            Debug.Log("플레이어가 엘리베이터 안에 없습니다.");
            return;
        }

        if (isPressed) return;
        BFSceneManager.Instance.AssignRunner(this.Runner);

        if (RequestManager.Instance.readyState == ReadyState.AllReady && RequestManager.Instance.nowState == RequestState.IngReq) 
            RPC_PressGoButton();
    }
    public void EnableOutline()
    {
        if (outline != null && !isPressed) outline.enabled = true;
    }

    public void DisableOutline()
    {
        if (outline != null) outline.enabled = false;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_PressGoButton()
    {
        if (isPressed) return;
        if (RequestManager.Instance.readyState != ReadyState.AllReady) return;
        if(RequestManager.Instance.nowState != RequestState.IngReq) return;

        Debug.Log("고함수 시작");

        isPressed = true; 
        RPC_DoButtonEffects();
        BFSceneManager.Instance.WaitFade(sceneToLoad);
    }
   
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_DoButtonEffects()
    {
        isPressed = true;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(buttonSound);
            audioSource.PlayOneShot(movingSound);
        }

        UpdateEmissionColor(activeColor);

        Vector3 newPos = transform.localPosition;
        newPos.z = initPosZ - pressingDepth;
        transform.localPosition = newPos;

        DisableOutline();
    }
    
    private void UpdateEmissionColor(Color color)
    {
        if (propertyBlock == null || meshRender == null) return;
        
        propertyBlock.SetColor("_EmissionColor", color);
        meshRender.SetPropertyBlock(propertyBlock);
    }

}

    // private void Update()
    // {
    //     if (Input.GetMouseButtonDown(0))
    //     {
    //         float depth = 0;

    //         if (playerCheck.isPlayerInside)
    //         {              
    //             Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

    //             RaycastHit hit;

    //             if (Physics.Raycast(ray, out hit))
    //             {                    
    //                 if (hit.collider == button) //버튼을 누른 경우
    //                 {
    //                     audioSource.PlayOneShot(buttonSound);                   
    //                     depth = initPosZ - pressingDepth;
    //                     Debug.Log("고 버튼 눌림 눌림");
    //                     GoButton();
    //                 }
    //             }
    //             else //버튼 안 누른 경우
    //             {
    //                 depth = initPosZ;
    //                 propertyBlock.SetColor("_EmissionColor", inactiveColor);
    //             }
    //         }
    //     }
    // }
    

    // public void GoButton()
    // {           
    //     //if(RequestManager.Instance.readyState == ReadyState.AllReady) //모두 레디한 상태여야 함.
    //     //{
    //     if(Object.HasStateAuthority) //호스트만 가능
    //     {
    //         Debug.Log("고 함수 시작");   
    //         propertyBlock.SetColor("_EmissionColor", activeColor);       
    //         audioSource.PlayOneShot(movingSound);          
    //         BFSceneManager.Instance.OnLoadScene("testReq1"); //씬 로드. 추후에 인덱스 값 리퀘스트 매니저에서 받아옴        
    //     }
    //     //else
    //     //{
    //     //    Debug.Log("모든 인원이 레디하지 않았습니다.");
    //     //}
    // }