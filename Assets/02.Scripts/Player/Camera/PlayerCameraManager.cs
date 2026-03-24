using UnityEngine;
using Fusion;

public class PlayerCameraManager : NetworkBehaviour
{
    [Header("Cameras")]
    public GameObject gameplayCam;
    public GameObject viewmodelCam;
    public GameObject deathCam;
    public GameObject observerCam;

    [Header("Outline Aim Settings")]
    public float outlineMaxDistance = 5f;
    public LayerMask outlineLayer;

    private Camera activeCam;
    private Outline currentOutline;

    public override void Spawned()
    {
        if (!Object.HasInputAuthority)
        {
            DisableAllCams();
            return;
        }

        // 로컬 플레이어 기본 카메라
        ActivateGameplay();
    }

    void Update()
    {
        if (!Object.HasInputAuthority) return;
        if (activeCam == null) return;

        HandleOutlineRay();
    }

    // ======================
    // Camera State Control
    // ======================

    public void ActivateGameplay()
    {
        DisableAllCams();

        gameplayCam.SetActive(true);
        viewmodelCam.SetActive(true);

        activeCam = gameplayCam.GetComponent<Camera>();
        ClearOutline();
    }

    public void ActivateDeathCam()
    {
        DisableAllCams();

        deathCam.SetActive(true);
        activeCam = deathCam.GetComponent<Camera>();

        ClearOutline();
    }

    public void ActivateObserverCam()
    {
        DisableAllCams();

        observerCam.SetActive(true);
        activeCam = observerCam.GetComponent<Camera>();

        ClearOutline();
    }

    public void DisableAllCams()
    {
        gameplayCam.SetActive(false);
        viewmodelCam.SetActive(false);
        deathCam.SetActive(false);
        observerCam.SetActive(false);
    }

    // ======================
    // Outline Logic
    // ======================

    void HandleOutlineRay()
    {
        Ray ray = activeCam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        RaycastHit hit;

        bool hitOutlineThisFrame = false;

        if (Physics.Raycast(ray, out hit, outlineMaxDistance, outlineLayer))
        {
            Outline outline = hit.collider.GetComponentInParent<Outline>();

            if (outline != null)
            {
                // 다른 대상이면 교체
                if (currentOutline != outline)
                {
                    ClearOutline();
                    currentOutline = outline;
                    currentOutline.enabled = true;
                }

                hitOutlineThisFrame = true;
            }
        }

        // 이번 프레임에 정중앙에 맞은 Outline이 없으면 즉시 OFF
        if (!hitOutlineThisFrame)
        {
            ClearOutline();
        }
    }

    void ClearOutline()
    {
        if (currentOutline != null)
        {
            currentOutline.enabled = false;
            currentOutline = null;
        }
    }
}