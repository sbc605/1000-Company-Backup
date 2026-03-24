//코드 담당자: 유호정

using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;
using NUnit.Framework;


public class PlayerController : NetworkBehaviour
{
    private NetworkCharacterController _ncc;
    private Animator animator;
    public static PlayerController Local { get; private set; } // 로컬 플레이어 참조 (설정 관련)

    [Header("Movement Speeds")]
    public float walkSpeed = 2.5f;
    public float runSpeed = 5.0f;
    public float crouchSpeed = 1.5f;
    public float injurySpeedMultiplier = 0.5f;
    [Networked] public float SlowMultiplier { get; set; } = 0.5f;

    [Header("Movement Stamina")]
    public float maxRunTime = 5f;
    [Networked] public float CooldownTimer { get; set; }
    [Networked] private NetworkBool IsStaminaCooldown { get; set; } // 5초 쿨타임용
    public float cooltimeSpeed = 1f;

    [Header("Look Settings")]
    public Camera playerCamera;
    public float lookSensitivity = 30f;
    public float minYAngle = -75f;
    public float maxYAngle = 45f;

    [Header("Crouch Settings")]
    public Vector3 standingCameraPosition = new Vector3(0, 1.65f, 0.03f);
    public Vector3 crouchingCameraPosition = new Vector3(0, 1.25f, 0.03f);
    public float crouchTransitionSpeed = 10f;

    [Header("Player Carrying")]
    [Networked] public NetworkId CarriedPlayerId { get; set; }
    [Networked] public NetworkBool IsBeingCarried { get; set; }

    [Header("Animation")]
    [SerializeField] private int itemLayerIndex = 1;

    [Networked] public NetworkBool IsUsingComputer { get; set; }
    [Networked] public NetworkBool IsStandingUp { get; set; } = false;
    private bool _previousIsUsingComputer;

    [Networked] private Vector2 NetworkMoveInput { get; set; }
    private Vector2 lookInput;
    [Networked] public NetworkBool IsCrouching { get; set; } = false;
    [Networked] private NetworkBool IsRunning { get; set; }
    [Networked] private NetworkButtons _previousButtons { get; set; }
    [Networked] public NetworkBool IsInjured { get; set; } = false;
    [Networked] public NetworkBool IsDebuffed { get; set; } = false;
    [Networked] public NetworkBool IsCarrying { get; set; } // Player Carrying인데 위치 바꾸면 시체 동기화 늦어서 여기 둠
    //은주 추가
    public bool isInputLocked = false;
    public bool IsPaused { get; private set; } = false;
    private float cameraVerticalRotation = 0f;
    private NetworkId _cachedCarriedPlayerId;
    private NetworkBool _cachedIsBeingCarried;



    private void Awake()
    {
        _ncc = GetComponent<NetworkCharacterController>();
        animator = GetComponent<Animator>();
    }

    public override void Spawned()
    {
        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (HasInputAuthority)
        {
            Local = this;
            lookSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", lookSensitivity);

            CursorManager.Instance?.ClosePopUI();
            playerCamera.transform.localPosition = standingCameraPosition;
            if (playerInput != null) playerInput.enabled = true;
            CooldownTimer = maxRunTime;
        }
        else
        {
            if (playerCamera != null) playerCamera.enabled = false;
            if (playerInput != null) playerInput.enabled = false;
            int multiLayer = LayerMask.NameToLayer("Multi Player");
            SetLayerRecursively(this.gameObject, multiLayer);
        }
        _previousIsUsingComputer = IsUsingComputer;
    }
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
        if (HasInputAuthority && Local == this)
        {
            Local = null;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (IsUsingComputer || IsStandingUp) return;
        HandleStamina();
        PlayerMove();
    }

    public override void Render()
    {
        if (IsUsingComputer != _previousIsUsingComputer)
        {
            ApplyComputerUseState(IsUsingComputer);
            _previousIsUsingComputer = IsUsingComputer;
        }
        UpdateAnimator();

        // float staminaPercent = currentRunTime / maxRunTime; // 스태미나 UI 연결할 경우
    }

    #region Move
    private void PlayerMove()
    {
        PlayerCondition condition = GetComponent<PlayerCondition>();
        if (condition != null && condition.IsDead) return;

        if (isInputLocked)
        {
            NetworkMoveInput = Vector2.zero;
            IsRunning = false;
            lookInput = Vector2.zero;
            _ncc.Move(Vector3.zero);
            return;
        }

        if (GetInput(out NetworkInputData currentInputData))
        {
            if (IsPaused)
            {
                NetworkMoveInput = Vector2.zero;
                IsRunning = false;
                lookInput = Vector2.zero;
            }

            else
            {
                NetworkMoveInput = currentInputData.moveDirection;

                bool inputRun = currentInputData.buttons.IsSet(MyButtons.Run);
                IsRunning = inputRun && !IsInjured && !IsStaminaCooldown && CooldownTimer > 0f;
                lookInput = currentInputData.lookDelta;
                if (currentInputData.buttons.WasPressed(_previousButtons, (int)MyButtons.Crouch))
                {
                    // 시체 들고 있으면 앉기 금지
                    if (!IsCarrying)
                    {
                        IsCrouching = !IsCrouching;
                    }
                }

                if (!IsPaused)
                {
                    RotateCharacterAndCamera(lookInput);
                    HandleCrouchCamera();
                }
                _previousButtons = currentInputData.buttons;
            }

            SetCurrentMaxSpeed();

            Vector3 moveDirection = CalculateMoveDirection();
            _ncc.Move(moveDirection);
        }
    }

    private void SetCurrentMaxSpeed()
    {
        float currentMaxSpeed;

        // 5초 동안 달리고 쿨타임 중이면 느린 속도(5초)
        if (IsStaminaCooldown)
        {
            currentMaxSpeed = cooltimeSpeed;
        }
        else if (IsCrouching)
        {
            currentMaxSpeed = crouchSpeed;
        }
        else if (IsRunning)
        {
            currentMaxSpeed = runSpeed;
        }
        else
        {
            currentMaxSpeed = walkSpeed;
        }
        if (IsInjured)
        {
            currentMaxSpeed *= injurySpeedMultiplier;
        }
        if (IsDebuffed)
        {
            currentMaxSpeed *= SlowMultiplier;
        }
        _ncc.maxSpeed = currentMaxSpeed;
    }

    private Vector3 CalculateMoveDirection()
    {
        Vector3 moveDirectionHorizontal = transform.forward * NetworkMoveInput.y + transform.right * NetworkMoveInput.x;
        if (moveDirectionHorizontal.sqrMagnitude > 1)
        {
            moveDirectionHorizontal.Normalize();
        }
        return moveDirectionHorizontal;
    }

    private void HandleStamina()
    {
        if (!HasStateAuthority) return;

        // 달리는 중이면 스태미나 감소
        if (IsRunning && !IsStaminaCooldown && !IsInjured && !IsDebuffed)
        {
            CooldownTimer -= Runner.DeltaTime;

            if (CooldownTimer <= 0f)
            {
                CooldownTimer = 0f;
                IsRunning = false; // 강제 walk
                IsStaminaCooldown = true; // 탈진 상태
            }
        }

        // 탈진 상태면 5초 후 회복
        if (IsStaminaCooldown)
        {
            CooldownTimer += Runner.DeltaTime;

            if (CooldownTimer >= maxRunTime)
            {
                CooldownTimer = maxRunTime;
                IsStaminaCooldown = false;  // 다시 달리기 가능
            }
        }
    }
    #endregion

    #region 카메라, 애니메이션
    void RotateCharacterAndCamera(Vector2 lookDelta)
    {
        if (IsPaused || IsUsingComputer || IsStandingUp) return;
        cameraVerticalRotation -= lookDelta.y * lookSensitivity * Runner.DeltaTime;
        cameraVerticalRotation = Mathf.Clamp(cameraVerticalRotation, minYAngle, maxYAngle);
        playerCamera.transform.localRotation = Quaternion.Euler(cameraVerticalRotation, 0, 0);
        transform.Rotate(Vector3.up * lookDelta.x * lookSensitivity * Runner.DeltaTime);
    }

    void HandleCrouchCamera()
    {
        Vector3 targetPosition = IsCrouching ? crouchingCameraPosition : standingCameraPosition;
        playerCamera.transform.localPosition = Vector3.Lerp(
            playerCamera.transform.localPosition,
            targetPosition,
            crouchTransitionSpeed * Runner.DeltaTime);
    }

    void UpdateAnimator()
    {
        Vector2 finalMoveInput = IsUsingComputer ? Vector2.zero : NetworkMoveInput;
        bool finalIsCrouching = IsUsingComputer ? false : IsCrouching;
        bool finalIsRunning = IsUsingComputer ? false : IsRunning;

        bool canRun = !finalIsCrouching && finalIsRunning;
        float speedMultiplier = canRun ? 2.0f : 1.0f;

        animator.SetFloat("InputX", finalMoveInput.x * speedMultiplier);
        animator.SetFloat("InputY", finalMoveInput.y * speedMultiplier);
        animator.SetBool("IsCrouch", finalIsCrouching);
        animator.SetBool("IsInjured", IsInjured);

        animator.SetBool("IsCarrying", IsCarrying);
        animator.SetBool("IsBeingCarried", IsBeingCarried);

        if (IsBeingCarried != _cachedIsBeingCarried)
        {
            // Debug.Log($"[Anim] {name} IsBeingCarried = {IsBeingCarried}");
            _cachedIsBeingCarried = IsBeingCarried;
        }
    }
    #endregion

    #region 멈춤, 감도
    public void SetPaused(bool paused)
    {
        IsPaused = paused;
        if (_ncc != null)
        {
            _ncc.enabled = !paused;
        }
        if (paused && HasInputAuthority)
        {
            NetworkMoveInput = Vector2.zero;
            IsRunning = false;
            animator.SetFloat("InputX", 0);
            animator.SetFloat("InputY", 0);
        }
    }
    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
    public void SetSensitivity(float newSensitivity)
    {
        lookSensitivity = newSensitivity;
    }
    #endregion

    #region RPC와 컴퓨터
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_SetSlowDebuff(bool isSlowed, float multiplier = 0.5f)
    {
        IsDebuffed = isSlowed;
        SlowMultiplier = multiplier;
    }

    public void RequestInteract(NetworkId interactableId)
    {
        Rpc_RequestInteract(interactableId);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestInteract(NetworkId interactableId)
    {
        Debug.Log($"[Rpc_RequestInteract] from {Object.Id}, target={interactableId}");

        if (Runner.TryFindObject(interactableId, out NetworkObject targetObject))
        {
            if (targetObject.TryGetComponent(out IInteractable interactable))
            {
                Debug.Log($"[Rpc_RequestInteract] Interact 호출 대상 = {targetObject.name}");
                interactable.Interact(this.gameObject);
            }
        }
    }

    public void Server_StartComputerInteraction(Vector3 sitPosition, Quaternion sitRotation)
    {
        if (!HasStateAuthority)
        {

            return;
        }

        IsUsingComputer = true;
        _ncc.Teleport(sitPosition, sitRotation);
    }

    public void Client_StopUsingComputer()
    {
        if (HasInputAuthority && IsUsingComputer)
        {
            Rpc_RequestStopUsingComputer();

        }
        else
        {
            Debug.LogWarning($"[PlayerController] Client_StopUsingComputer called but HasInputAuthority is {HasInputAuthority} or IsUsingComputer is {IsUsingComputer}.");
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestStopUsingComputer()
    {
        IsUsingComputer = false;
        IsStandingUp = true;
    }

    private void ApplyComputerUseState(bool isUsing)
    {
        if (isUsing)
        {
            animator.SetTrigger("StartUsingComputer");
            animator.SetLayerWeight(itemLayerIndex, 0);
        }
        else
        {
            animator.SetTrigger("StopUsingComputer");
        }
    }
    public void EnableMovement()
    {
        animator.SetLayerWeight(itemLayerIndex, 1);
        if (HasInputAuthority)
        {
            Rpc_NotifyStandUpComplete();
        }

    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_NotifyStandUpComplete()
    {
        IsStandingUp = false;
    }
    #endregion
}