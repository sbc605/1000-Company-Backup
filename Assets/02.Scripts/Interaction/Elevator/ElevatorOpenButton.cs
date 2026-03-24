using Fusion;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ElevatorOpenButton : NetworkBehaviour, IInteractable
{
    //코드 담당자 : 최은주
    //기존 엘레베이터 버튼 코드 리팩토링. 버튼 상호작용 

    bool isOpen; 
    public UnityEvent onButtonPressed;
    // public Collider button;

    AudioSource audioSource;
    public AudioClip buttonSound;

    [ColorUsage(true, true)] public Color inactiveColor = Color.black;
    [ColorUsage(true, true)] public Color activeColor = new Color(121, 191, 97, 255);

    MaterialPropertyBlock propertyBlock;
    MeshRenderer meshRender;
    ElevatorInteract outsideDoors;
    //float switchOffTimer = 0;
    private Outline outline;
    private bool isPressed = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        meshRender = GetComponent<MeshRenderer>();
        // button = GetComponent<Collider>();
        outsideDoors = GetComponentInParent<ElevatorInteract>();
        propertyBlock = new MaterialPropertyBlock();
        propertyBlock.SetColor("_EmissionColor", inactiveColor);

        if (outline == null)
            outline = GetComponent<Outline>();
        
        if (outline != null)
            outline.enabled = false;
    }

    public void Interact(GameObject interactor)
    {
        if (isPressed) return;
        if (interactor.TryGetComponent(out NetworkObject networkObject))
        {
            RPC_ButtonPress();
        }
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
    void RPC_ButtonPress()
    {
        if (isPressed) return;
        RPC_ActiveButton();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_ActiveButton()
    {
        SwitchOn();
    }

    public void SwitchOn()
    {
        if (isPressed) return;
        isPressed = true;
        
        Debug.Log("버튼 눌림");
        propertyBlock.SetColor("_EmissionColor", activeColor);
        audioSource.PlayOneShot(buttonSound);
        outsideDoors.DoorOpening();

        DisableOutline();

        meshRender.SetPropertyBlock(propertyBlock);
        StartCoroutine(TurnOffColor());
    }

    IEnumerator TurnOffColor()
    {
        yield return new WaitForSeconds(4f);
        propertyBlock.SetColor("_EmissionColor", inactiveColor);
        meshRender.SetPropertyBlock(propertyBlock);
        isPressed = false;
    }
}
