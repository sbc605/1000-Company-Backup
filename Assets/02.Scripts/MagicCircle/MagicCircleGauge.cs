using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Fusion;
using System.Collections;

// 코드 담당자: 김수아

public class MagicCircleGauge : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Canvas gaugeCanvas;
    [SerializeField] private Image gaugeFill;

    [Header("Progress Rule")]
    [SerializeField] private float totalSeconds = 30f; // 1인 기준
    [SerializeField] private float perAddPlayer = 0.5f; // +0.5x per player

    [Header("Trigger Ref")]
    [SerializeField] private RangeTrigger rangeTrigger;

    private MagicCircleCore core;

    private bool forceHidden = false;

    #region 기본 연결

    void Awake()
    {
        core = GetComponent<MagicCircleCore>();
    }

    void OnEnable()
    {
        if (rangeTrigger != null)
            rangeTrigger.OnLocalToggle += HandleLocalToggle;
    }

    void OnDisable()
    {
        if (rangeTrigger != null)
            rangeTrigger.OnLocalToggle -= HandleLocalToggle;
    }

    /// <summary>
    /// 로컬 캐릭터가 트리거 안/밖으로 이동할 때만 로컬 Canvas 토글
    /// </summary>
    private void HandleLocalToggle(bool inside)
    {
        if (core != null && core.IsProcessing)
            SetGaugeActive(inside);
        else
            SetGaugeActive(false);
    }
    #endregion

    void Update()
    {
        // 네트워크 진행도 -> 게이지 시각화(모든 클라)
        if (gaugeFill) gaugeFill.fillAmount = core.ExorcismProgress;
    }

    /// <summary>
    /// Progress 델타 계산(서버에서 호출)
    /// 플레이어 수와 deltaTime에 따라 증가량 반환. 0명이면 0 반환(정지)
    /// </summary>
    public float EvaluateDelta(int playerCount, float deltaT)
    {
        if (playerCount <= 0) return 0f; // 0명이면 정지

        float baseRate = 1f / Mathf.Max(0.001f, totalSeconds);
        float factor = 1f + (playerCount - 1) * perAddPlayer;
        return baseRate * factor * deltaT;
    }

    public void SetGaugeActive(bool active)
    {
        if (forceHidden) active = false;
        if (core != null && (core.ExorcismProgress >= 1f || core.IsProcessing == false))
            active = false;

        if (gaugeCanvas) gaugeCanvas.gameObject.SetActive(active);
    }

    public void OnBeginProcessing()
    {
        forceHidden = false;

        StartCoroutine(CheckRoutine());
    }

    private IEnumerator CheckRoutine()
    {
        yield return null;

        if (rangeTrigger != null) rangeTrigger.CheckLocalPlayerInsideOnStart();
        SetGaugeActive(rangeTrigger != null && rangeTrigger.LocalPlayerInside);
    }

    public void ForceHideGauge()
    {
        forceHidden = true;
        SetGaugeActive(false);
    }
}
