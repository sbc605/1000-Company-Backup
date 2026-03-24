using Fusion;
using UnityEngine;

// 코드 담당자: 김수아

public class GhostFX : NetworkBehaviour
{
    [Header("Dissolve")]
    [SerializeField] private Renderer[] dissolveRenderers;
    [SerializeField] private float defaultDissolveDuration = 3f;

    private MaterialPropertyBlock mpb;
    private static readonly int DissolveID = Shader.PropertyToID("_Dissolve");
    private static readonly int EdgeColorID = Shader.PropertyToID("_EdgeColor");

    [Networked] public NetworkBool IsDissolving { get; private set; }
    [Networked] public int DissolveStartTick { get; private set; }
    [Networked] public float DissolveDuration { get; private set; }

    [Header("Aura Effect")]
    [SerializeField] private GameObject auraEffect;
    private Vector3 standPos   = new Vector3(0f, 1f, -0.1f);
    private Vector3 standEuler = Vector3.zero;
    private Vector3 crawlPos   = new Vector3(0f, 0.6f, -0.1f);
    private Vector3 crawlEuler = new Vector3(90f, 0f, 0f);

    #region Dissolve
    void Awake()
    {
        if (dissolveRenderers == null || dissolveRenderers.Length == 0)
            dissolveRenderers = GetComponentsInChildren<Renderer>(true);
        mpb = new MaterialPropertyBlock();
        SetDissolve(0f);
    }

    public override void Render()
    {
        if (IsDissolving)
        {
            int tickElapsed = Runner.Tick - DissolveStartTick;
            float elapsed = tickElapsed * Runner.DeltaTime;
            float t = Mathf.Clamp01(DissolveDuration <= 0f ? 1f : (elapsed / DissolveDuration));
            SetDissolve(t);
        }
    }

    // 서버에서 호출(GhostDead)
    public void BeginDissolve(float durationSeconds)
    {
        if (!Object.HasStateAuthority) return;

        DissolveDuration = durationSeconds > 0f ? durationSeconds : defaultDissolveDuration;
        IsDissolving = true;
        DissolveStartTick = Runner.Tick;
    }

    private void SetDissolve(float v)
    {
        if (mpb == null || dissolveRenderers == null) return;

        mpb.Clear();
        mpb.SetFloat(DissolveID, v);
        mpb.SetColor(EdgeColorID, new Color(255f, 0f, 0f, 128f));

        for (int r = 0; r < dissolveRenderers.Length; r++)
        {
            var rend = dissolveRenderers[r];
            if (!rend) continue;

            var mats = rend.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                rend.SetPropertyBlock(mpb, i);
        }
    }

    // Dissolve t(0~1)을 서버,클라 공통으로 구함 (GhostDead)
    public float GetDissolveT()
    {
        if (!IsDissolving) return 0f;
        int tickElapsed = Runner.Tick - DissolveStartTick;
        float elapsed = tickElapsed * Runner.DeltaTime;
        return Mathf.Clamp01(DissolveDuration <= 0f ? 1f : (elapsed / DissolveDuration));
    }
    #endregion

    #region Aura Effect

    public void ChangePos(bool isCrawling)
    {        
        if (!auraEffect) return;

        Vector3 pos = isCrawling ? crawlPos : standPos;
        Vector3 rot = isCrawling ? crawlEuler : standEuler;

        auraEffect.transform.localPosition = pos;
        auraEffect.transform.localRotation = Quaternion.Euler(rot); 
    }

    #endregion
}
