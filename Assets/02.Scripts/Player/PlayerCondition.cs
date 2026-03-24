//코드 담당자: 유호정
using UnityEngine;
using System;
using UnityEngine.UI;
using Fusion;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;
using System.Collections.Generic;

// 코드 추가: 김수아
// 서버에 접근한 플레이어를 귀신쪽에서 감지합니다. (GhostCOntroller.FindPlayerRegisteredPlayer에서 사용됨)
public static class ServerPlayerRegistry
{
    // 서버에서만 접근
    public static readonly HashSet<PlayerCondition> Players = new HashSet<PlayerCondition>();
}

public class PlayerCondition : NetworkBehaviour, IInteractable
{
    [Header("Component References")]
    private Animator animator;
    private PlayerController playerController;
    private InventoryManager inventoryManager;
    private AudioListener audioListen;
    private AudioSource audioSource;

    [Header("Health Settings")]
    public int MaxHealth = 2;

    [Header("Sanity Settings")]
    public float MaxSanity = 100f;

    [Header("Injury Settings")]
    public float injurySpeedMultiplier = 0.5f;
    private float originalWalkSpeed;
    private float originalRunSpeed;
    [SerializeField] private Image vignetteOverlay;
    [SerializeField] private float vignetteMaxAlpha = 0.6f;
    [SerializeField] private float vignetteFadeSpeed = 5f;

    [Header("Sound Settings")]
    [SerializeField] private AudioClip[] deathSounds;

    [Networked] public int CurrentHealth { get; set; }
    [Networked] public bool IsDead { get; set; }
    [Networked] public bool IsInjured { get; set; }
    [Networked] public bool IsProtected { get; set; }
    [Networked] public float CurrentSanity { get; set; }
    [Networked] private TickTimer _protectionTimer { get; set; }

    private int _cachedHealth;
    private bool _cachedIsDead;
    private bool _cachedIsInjured;
    private float _cachedSanity;

    [Networked] public NetworkBool isDebuffed { get; set; }

    // 추가 : 최서영, 시체 운반 상태 체크용
    [Networked] public NetworkBool IsBeingCarried { get; set; }
    [Networked] public NetworkObject Carrier { get; set; }

    private Outline outline;

    // 추가 : 정하윤
    [Header("DarkFilter")]
    [SerializeField] private Image darkFilter;
    [SerializeField] private float fadeDuration = 1f;

    private Coroutine darkFadeCoroutine;

    [Header("Hit Effect")]
    [SerializeField] private Image hitEffectOverlay;
    [SerializeField] private float hitEffectFadeInTime = 0.1f;
    [SerializeField] private float hitEffectStayTime = 0.1f;
    [SerializeField] private float hitEffectFadeOutTime = 0.4f;
    [SerializeField][Range(0, 1)] private float hitEffectMaxAlpha = 0.7f;
    private Coroutine hitEffectCoroutine;

    private PlayerInfo playerInfo;

    [Header("DeathCam Settings")]
    [Networked] public NetworkString<_32> Nickname { get; set; }
    private PlayerCameraManager _cameraManager;
    private SpectatorManager _spectatorManager;
    public TextMeshProUGUI overheadNicknameUI;
    private bool _localIsDead = false;

    // 추가: 김수아
    public static event Action<PlayerCondition, bool> OnLowSanityStatus; // 정신력 30이하용 액션
    public static event Action<string, float> OnSanityUpdate; // 정신력 - 타블렛 연결(string: 닉네임, float: 정신력 수치)

    private void Awake()
    {
        // 코드 추가 : 정하윤
        if (darkFilter != null)
            SetAlpha(0f);
        if (hitEffectOverlay != null)
        {
            Color c = hitEffectOverlay.color;
            c.a = 0;
            hitEffectOverlay.color = c;
        }
        if (vignetteOverlay != null)
        {
            Color vColor = vignetteOverlay.color;
            vColor.a = 0;
            vignetteOverlay.color = vColor;
        }

        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        inventoryManager = GetComponent<InventoryManager>();
        audioListen = GetComponentInChildren<AudioListener>();
        audioSource = GetComponent<AudioSource>();

        if (playerController != null)
        {
            originalWalkSpeed = playerController.walkSpeed;
            originalRunSpeed = playerController.runSpeed;
        }
        _cameraManager = GetComponent<PlayerCameraManager>();
        _spectatorManager = GetComponent<SpectatorManager>();

        outline = GetComponent<Outline>();
        if (outline != null) outline.enabled = false;
    }

    public override void Spawned()
    {
        if (Runner.IsServer)
        {
            CurrentHealth = MaxHealth;
            CurrentSanity = MaxSanity;
            IsDead = false;
            IsInjured = false;
            IsProtected = false;

            // 추가 최서영 (시체 관련)
            IsBeingCarried = false;
            Carrier = null;
        }

        _cachedHealth = -1;
        _cachedIsDead = true;
        _cachedIsInjured = true;
        _cachedSanity = -1f;

        if (Object.HasInputAuthority)
        {
            RPC_SetNickname($"Player_{Object.InputAuthority.PlayerId}");
        }
        _localIsDead = IsDead;
        RefreshVisuals();

        // 코드 추가: 김수아
        if (Object.HasStateAuthority)
        {
            ServerPlayerRegistry.Players.Add(this);
        }


        //코드 추가: 최은주 
        if (!Object.HasInputAuthority && audioListen != null)
            audioListen.enabled = false;

        // if (Object.HasInputAuthority)
        // {
        //     playerInfo = FindFirstObjectByType<PlayerInfo>();

        //     if (playerInfo != null)
        //     {
        //         playerInfo.maxSanity = this.MaxSanity;
        //         playerInfo.UpdateSanityUI(CurrentSanity);
        //     }

        // }
    }

    // 코드 추가: 김수아
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Object.HasStateAuthority)
        {
            ServerPlayerRegistry.Players.Remove(this);
        }
    }

    public override void Render()
    {
        RefreshVisuals();

        if (overheadNicknameUI != null && overheadNicknameUI.text != Nickname.ToString())
        {
            overheadNicknameUI.text = Nickname.ToString();
        }
        if (!Object.HasInputAuthority) return;

        if (playerInfo == null && PlayerInfo.Instance != null)
        {
            playerInfo = PlayerInfo.Instance;

            Debug.Log("PlayerInfo 싱글톤 찾음! UI 초기화.");
            playerInfo.maxSanity = this.MaxSanity;
            playerInfo.UpdateSanityUI(CurrentSanity);
            int currentLevel = 0;
            if (CurrentSanity <= 15f) currentLevel = 2;
            else if (CurrentSanity <= 30f) currentLevel = 1;

            playerInfo.UpdateSanityEffect(currentLevel);
            _cachedSanityLevel = currentLevel;
        }

        if (IsDead != _localIsDead)
        {
            if (IsDead == true)
            {
                _cameraManager.ActivateDeathCam();
                _spectatorManager.enabled = true;
            }
            _localIsDead = IsDead;
        }
    }

    private void RefreshVisuals()
    {
        if (_cachedHealth != CurrentHealth || _cachedIsInjured != IsInjured)
        {
            if (Object.HasInputAuthority && _cachedHealth != -1)//&& CurrentHealth < _cachedHealth
            {
                ShowHitEffect();
            }

            SetInjuredState(IsInjured);
            _cachedHealth = CurrentHealth;
            _cachedIsInjured = IsInjured;
        }
        if (Object.HasInputAuthority)
        {
            UpdateVignetteEffect();
        }

        if (_cachedIsDead != IsDead)
        {
            SetDeadState(IsDead);

            if (Object.HasInputAuthority)
            {
                VivoxManager.Instance.UpdatePlayerState(IsDead);
            }

            _cachedIsDead = IsDead;
        }

        if (_cachedSanity != CurrentSanity)
        {
            OnSanityChanged(CurrentSanity, _cachedSanity);
            _cachedSanity = CurrentSanity;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Runner.IsServer)
        {
            if (IsProtected && _protectionTimer.Expired(Runner))
            {
                IsProtected = false;
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.StateAuthority)]
    public void Rpc_TakeDamage(int amount)
    {
        if (IsDead) return;
        Debug.Log($"TakeDamage 호출됨 - Player:{Object.Id} / HP:{CurrentHealth} / Authority:{Object.HasStateAuthority}");

        if (IsProtected)
        {
            IsProtected = false;
            Debug.Log($"서버: 플레이어 {Object.Id}가 공격을 막았습니다");
            return;
        }

        if (amount >= MaxHealth)
        {
            Debug.Log($"서버: 플레이어 {Object.Id}가 즉사했습니다.");
            Die();
            return;
        }
        if (IsInjured)
        {
            Debug.Log($"서버: 플레이어 {Object.Id}가 부상 상태에서 공격받아 사망.");
            Die();
        }
        else if (CurrentHealth == 1)
        {
            Debug.Log($"서버: 플레이어 {Object.Id}가 부상 상태가 됨.");
            CurrentHealth = 1;
            IsInjured = true;
            if (playerController != null) playerController.IsInjured = true;
        }
        else
        {
            CurrentHealth -= amount;
            Debug.Log($"서버: 플레이어 {Object.Id}가 데미지 {amount} 받음. 체력: {CurrentHealth}");
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_Heal(int amount)
    {
        if (IsDead) return;

        CurrentHealth += amount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
        Debug.Log($"서버: 플레이어 {Object.Id}가 체력 {amount} 회복. 체력: {CurrentHealth}");

        if (CurrentHealth > 1 && IsInjured)
        {
            Debug.Log($"서버: 플레이어 {Object.Id}가 부상 상태에서 회복됨.");
            IsInjured = false;
            if (playerController != null) playerController.IsInjured = false;
        }
    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_HealInjury()
    {
        if (IsDead || !IsInjured) return;
        IsInjured = false;
        if (playerController != null) playerController.IsInjured = false;
        Debug.Log($"서버: 플레이어 {Object.InputAuthority}가 부상을 치료함.");
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_ApplyTimedProtection(float duration)
    {
        if (IsDead) return;
        IsProtected = true;
        _protectionTimer = TickTimer.CreateFromSeconds(Runner, duration);
        Debug.Log($"서버: 플레이어 {Object.Id}에게 {duration}초 보호막 부여");
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_ApplyProtectionCharges(int amount)
    {
        if (IsDead) return;
        IsProtected = true;
        Debug.Log($"서버: 플레이어 {Object.Id}에게 1회용 보호막 부여");
    }

    private IEnumerator ProtectionCoroutine(float duration)
    {
        IsProtected = true;
        Debug.Log($"{duration}초 동안 보호막을 얻었습니다.");
        yield return new WaitForSeconds(duration);
        if (IsProtected)
        {
            IsProtected = false;
            Debug.Log("보호막이 해제되었습니다.");
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_DecreaseSanity(float amount)
    {
        if (IsDead) return;
        CurrentSanity -= amount;
        CurrentSanity = Mathf.Clamp(CurrentSanity, 0f, MaxSanity);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_RestoreSanity(float amount)
    {
        if (IsDead) return;
        CurrentSanity += amount;
        CurrentSanity = Mathf.Clamp(CurrentSanity, 0f, MaxSanity);
    }

    private void Die()
    {
        if (!Runner.IsServer || IsDead) return;

        Debug.Log($"서버: 플레이어 {Object.Id} 사망");

        if (inventoryManager != null)
        {
            // Vector3 positionToDrop = inventoryManager.dropPosition.position;
            inventoryManager.DropAllItems();
        }

        if (TryGetComponent<PlayerAbnormalManager>(out var abnormal))
        {
            abnormal.ForceStopAbnormal();
        }

        IsDead = true;
    }

    private void SetDeadState(bool isDead)
    {
        if (isDead)
        {
            Debug.Log("로컬: 사망 상태가 됨.");

            if (audioSource != null && deathSounds != null && deathSounds.Length > 0)
            {
                int index = UnityEngine.Random.Range(0, deathSounds.Length);
                AudioClip clipToPlay = deathSounds[index];

                if (clipToPlay != null)
                {
                    audioSource.PlayOneShot(clipToPlay);
                }
            }
            if (animator != null) { animator.SetInteger("EquippedItemType", 0); }
            animator.SetTrigger("Die");
            // playerController.enabled = false;
            if (TryGetComponent<PlayerInput>(out var playerInput))
                playerInput.enabled = false;
            if (TryGetComponent<PlayerInteraction>(out var interaction))
                interaction.enabled = false;
            if (Object.HasInputAuthority && playerInfo != null)
            {
                playerInfo.gameObject.SetActive(false);
            }
        }
        else
        {
            if (Object.HasInputAuthority && playerInfo != null)
            {
                playerInfo.gameObject.SetActive(true);
            }
        }
    }

    private void SetInjuredState(bool state)
    {
        // if(playerController != null) playerController.IsInjured = state;
        animator.SetBool("IsInjured", state);

        if (state)
        {
            playerController.IsCrouching = false;
            animator.SetBool("IsCrouch", false);
        }
    }

    private void UpdateVignetteEffect()
    {
        if (vignetteOverlay == null) return;

        Color vColor = vignetteOverlay.color;
        float targetAlpha = (IsInjured && !IsDead) ? vignetteMaxAlpha : 0f;
        vColor.a = Mathf.Lerp(vColor.a, targetAlpha, Time.deltaTime * vignetteFadeSpeed);
        vignetteOverlay.color = vColor;
    }

    // 함수 추가 : 정하윤
    public void StartFade(float targetAlpha)
    {
        if (darkFadeCoroutine != null)
            StopCoroutine(darkFadeCoroutine);

        darkFadeCoroutine = StartCoroutine(DarkFadeCoroutine(targetAlpha));
    }

    // 함수 추가 : 정하윤
    private IEnumerator DarkFadeCoroutine(float targetAlpha)
    {
        float startAlpha = darkFilter.color.a;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    // 함수 추가 : 정하윤
    private void SetAlpha(float alpha)
    {
        Color c = darkFilter.color;
        c.a = alpha;
        darkFilter.color = c;
    }

    private int _cachedSanityLevel = 0;
    private void OnSanityChanged(float newSanity, float oldSanity)
    {
        Debug.Log($"로컬: 현재 정신력: {newSanity}%");

        if (Object.HasInputAuthority && playerInfo != null)
        {
            playerInfo.UpdateSanityUI(newSanity);
            int currentLevel = 0;
            if (newSanity <= 15f) currentLevel = 2;
            else if (newSanity <= 30f) currentLevel = 1;


            if (currentLevel != _cachedSanityLevel)
            {
                playerInfo.UpdateSanityEffect(currentLevel);
                _cachedSanityLevel = currentLevel;
            }
        }

        OnSanityUpdate?.Invoke(Nickname.ToString(), newSanity);

        // 정신력 30% 이벤트 로직 (로컬)
        bool wasLowSan = oldSanity <= 30f;
        bool isLowSan = newSanity <= 30f;

        if (!wasLowSan && isLowSan) // 30% 이하 진입
        {
            OnLowSanityStatus?.Invoke(this, true);
        }
        else if (wasLowSan && !isLowSan) // 30% 이상 회복
        {
            OnLowSanityStatus?.Invoke(this, false);
        }
    }

    private void ShowHitEffect()
    {
        if (hitEffectOverlay == null) return;

        if (hitEffectCoroutine != null)
            StopCoroutine(hitEffectCoroutine);

        hitEffectCoroutine = StartCoroutine(HitEffectFadeCoroutine());
    }
    private IEnumerator HitEffectFadeCoroutine()
    {
        float timer = 0f;
        Color color = hitEffectOverlay.color;

        timer = 0f;
        while (timer < hitEffectFadeInTime)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Lerp(0, hitEffectMaxAlpha, timer / hitEffectFadeInTime);
            hitEffectOverlay.color = color;
            yield return null;
        }
        color.a = hitEffectMaxAlpha;
        hitEffectOverlay.color = color;

        yield return new WaitForSeconds(hitEffectStayTime);

        timer = 0f;
        while (timer < hitEffectFadeOutTime)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Lerp(hitEffectMaxAlpha, 0, timer / hitEffectFadeOutTime);
            hitEffectOverlay.color = color;
            yield return null;
        }

        color.a = 0;
        hitEffectOverlay.color = color;
        hitEffectCoroutine = null;
    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetNickname(string nickname)
    {
        Nickname = nickname;
    }

    // 추가 : 최서영 (시체 관련)
    public void Interact(GameObject interactor)
    {
        Debug.Log($"[PlayerCondition] {interactor.name}이(가) {this.name}과(와) 상호작용 시도.");

        // 살아있으면 시체 상호작용 X
        if (!IsDead)
        {
            Debug.Log("[PlayerCondition] 아직 안 죽어서 무시");
            return;
        }

        // 이미 누가 들고 있으면 또 들 수 없음
        if (IsBeingCarried) return;

        if (!interactor.TryGetComponent<CorpseCarryHandler>(out var carrier))
        {
            Debug.Log("[PlayerCondition] interactor에 CorpseCarryHandler 없음");
            return;
        }

        Debug.Log("[PlayerCondition] CorpseCarryHandler 찾음, 업기 시도");
        carrier.StartCarry(Object);
    }

    public void EnableOutline()
    {
        if (outline != null && IsDead)
        {
            outline.enabled = true;
        }
    }

    public void DisableOutline()
    {
        if (outline != null)
        {
            outline.enabled = false;
        }
    }
}