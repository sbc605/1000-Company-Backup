//코드 담당자: 유호정


using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public Camera playerCamera;
    public float interactionDistance = 5f;
    public LayerMask interactionLayer;

    public IInteractable CurrentInteractable { get; private set; }

    public bool HasValidHit { get; private set; }
    public RaycastHit HitInfo { get; private set; }

    private IInteractable lastInteractable;
    private PlayerController _playerController;

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        // 입력 권한 없으면 전부 해제
        if (_playerController == null || !_playerController.HasInputAuthority)
        {
            ClearInteractable();
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        HasValidHit = Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionLayer);

        IInteractable newInteractable = null;

        if (HasValidHit)
        {
            HitInfo = hit;
            newInteractable = hit.collider.GetComponentInParent<IInteractable>();
        }

        // 핵심 수정: Raycast 실패 시 CurrentInteractable 해제
        if (newInteractable != null)
        {
            CurrentInteractable = newInteractable;
        }
        else
        {
            // UI용 인터랙션(Kiosk 등)은 유지
            if (!(CurrentInteractable is KioskTrigger || CurrentInteractable is ReturnTrigger))
            {
                CurrentInteractable = null;
            }
        }

        // Outline 상태 관리
        if (lastInteractable != CurrentInteractable)
        {
            lastInteractable?.DisableOutline();
            CurrentInteractable?.EnableOutline();
            lastInteractable = CurrentInteractable;
        }
    }

    public void OnInteract(InputValue value)
    {
        // 핵심 수정: HasValidHit 조건 추가
        if (_playerController == null ||
            !_playerController.HasInputAuthority ||
            CurrentInteractable == null ||
            !HasValidHit)
        {
            return;
        }

        NetworkBehaviour interactableNB = CurrentInteractable as NetworkBehaviour;

        bool canUseNetworkPath =
            interactableNB != null &&
            interactableNB.Object != null &&
            _playerController.Runner != null &&
            _playerController.Runner.IsRunning;

        // UI 상호작용 (로컬)
        if (CurrentInteractable is KioskTrigger || CurrentInteractable is ReturnTrigger)
        {
            CurrentInteractable.Interact(gameObject);
        }
        // 네트워크 상호작용
        else if (canUseNetworkPath)
        {
            _playerController.RequestInteract(interactableNB.Object.Id);
        }
    }

    public void ForceSetInteractable(IInteractable interactable)
    {
        // 기존 아웃라인 정리
        if (CurrentInteractable != interactable)
        {
            lastInteractable?.DisableOutline();
            CurrentInteractable = interactable;
            CurrentInteractable?.EnableOutline();
            lastInteractable = CurrentInteractable;
        }
    }

    private void ClearInteractable()
    {
        lastInteractable?.DisableOutline();
        lastInteractable = null;
        CurrentInteractable = null;
        HasValidHit = false;
    }
}