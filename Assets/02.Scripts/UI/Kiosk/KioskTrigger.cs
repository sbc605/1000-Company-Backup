using System.Collections;
using Fusion;
using UnityEngine;

// 코드 담당자: 김수아
// 키오스크에 달린 Trigger 범위에 들어오면 UI 뜨고 F 누르면 키오스크 UI 뜸
public class KioskTrigger : NetworkedTriggerEventSupporter, IInteractable
{
    [SerializeField] Canvas tasknoticeUI;
    [SerializeField] Canvas kioskUI;
    [SerializeField] private KioskCartController cart;

    private bool isLocalTrigger; // 로컬 플레이어가 범위 안에 있는지
    private bool isKioskOpenLocal; // 로컬에서 상점 UI 열렸는지
    private Transform playerTransform; // 플레이어 추적용

    // 전역으로 현재 열린 키오스크 추적
    public static KioskTrigger CurrentKiosk { get; private set; }

    private Coroutine openCo;
    private Coroutine closeCo;

    public override void Spawned()
    {
        isLocalTrigger = false;
        isKioskOpenLocal = false;
        if (tasknoticeUI) tasknoticeUI.gameObject.SetActive(false);
        if (kioskUI) kioskUI.gameObject.SetActive(false);
    }

    protected override void OnTargetEnter(Collider other)
    {
        if (!IsLocalPlayer(other)) return;

        isLocalTrigger = true;
        playerTransform = other.transform;

        var playerInteraction = other.GetComponent<PlayerInteraction>();
        if (playerInteraction != null)
        {
            playerInteraction.ForceSetInteractable(this);
        }

        if (tasknoticeUI && !isKioskOpenLocal)
            tasknoticeUI.gameObject.SetActive(true);
    }

    protected override void OnTargetExit(Collider other)
    {
        // 로컬 플레이어만 UI 비활성
        if (!IsLocalPlayer(other)) return;

        isLocalTrigger = false;
        playerTransform = null;

        var playerInteraction = other.GetComponent<PlayerInteraction>();
        if (playerInteraction != null)
        {
            playerInteraction.ForceSetInteractable(null);
        }

        if (tasknoticeUI)
            tasknoticeUI.gameObject.SetActive(false);

        if (isKioskOpenLocal)
            StopInteraction(true);

        CursorManager.Instance.ClosePopUI();
    }

    private void Update()
    {
        // 트리거에서 잠깐 빠져도 플레이어가 가까이 있으면 다시 인식
        if (!isLocalTrigger && playerTransform)
        {
            float dist = Vector3.Distance(playerTransform.position, transform.position);
            if (dist < 2f)
            {
                isLocalTrigger = true;
                if (tasknoticeUI && !isKioskOpenLocal)
                    tasknoticeUI.gameObject.SetActive(true);
            }
        }
    }

    // F키 눌러서 모니터 상호작용
    public void Interact(GameObject player)
    {
        Debug.Log("F키 Interact() 호출됨");
        if (!isLocalTrigger)
        {
            float dist = Vector3.Distance(player.transform.position, transform.position);
            if (dist > 2f) return;
            isLocalTrigger = true;
        }

        if (isKioskOpenLocal) return;

        if (closeCo != null)
        {
            StopCoroutine(closeCo);
            closeCo = null;
        }

        openCo = StartCoroutine(OpenShopUI());
    }

    private IEnumerator OpenShopUI()
    {
        isKioskOpenLocal = true;
        CurrentKiosk = this;

        if (cart) cart.HideBasketImmediate();

        Fade.onFadeAction(1f, Color.black, true, null);
        yield return new WaitForSeconds(1.5f);

        if (kioskUI) kioskUI.gameObject.SetActive(true);

        CursorManager.Instance.OpenPushUI();

        openCo = null;
    }

    public void StopInteraction(bool immediate = false)
    {
        if (!isKioskOpenLocal) return;

        if (openCo != null)
        {
            StopCoroutine(openCo);
            openCo = null;
        }

        if (closeCo != null)
        {
            StopCoroutine(closeCo);
            closeCo = null;
        }

        if (immediate)
            QuickClseShopUI();

        else
            closeCo = StartCoroutine(CloseShopUI());
    }

    private void QuickClseShopUI()
    {
        if (cart) cart.ClearAll(false);

        Fade.onFadeAction(0.1f, Color.black, false, null);

        if (kioskUI) kioskUI.gameObject.SetActive(false);

        CursorManager.Instance.ClosePopUI();

        isKioskOpenLocal = false;
        if (CurrentKiosk == this) CurrentKiosk = null;
    }

    // ESC로 나감
    private IEnumerator CloseShopUI()
    {
        Debug.Log("ESC로 상점 나감");
        yield return new WaitForSeconds(1.5f);
        if (cart) cart.ClearAll(false);
        Fade.onFadeAction(1f, Color.black, false, null);
        kioskUI.gameObject.SetActive(false);

        CursorManager.Instance.ClosePopUI();

        isKioskOpenLocal = false;
        if (CurrentKiosk == this) CurrentKiosk = null;

        closeCo = null;
    }

    public static bool CloseCurrentKiosk()
    {
        if (CurrentKiosk == null) return false;
        CurrentKiosk.StopInteraction();
        return true;
    }

    public void DisableOutline()
    {

    }

    public void EnableOutline()
    {

    }

    private bool IsLocalPlayer(Collider other)
    {
        if (Runner && Runner.IsRunning)
        {
            if (!other.TryGetComponent(out NetworkObject netObj)) return false;
            if (netObj.InputAuthority != Runner.LocalPlayer) return false; // 멀티일 때, 내가 아니면 false
        }
        else
        {
            if (!other.CompareTag("Player")) return false; // 싱글일 땐 Player 태그만 확인
        }

        return true;
    }
}
