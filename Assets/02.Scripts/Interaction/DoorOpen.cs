using Fusion;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class DoorOpen : NetworkBehaviour, IInteractable
{
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    public Animator openandclose1;
    private AudioSource audioSource;

    [Networked, OnChangedRender(nameof(OnDoorStateChanged))]
    private bool isOpen { get; set; }

    private void Start()
    {
      
        isOpen = false;
    }

    void OnDoorStateChanged()
    {
        if (isOpen)
            StartCoroutine(opening());
        else
            StartCoroutine(closing());
    }


    IEnumerator opening()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        print("you are opening the door");
        openandclose1.Play("doorOpen1");
        audioSource.PlayOneShot(openSound);
        isOpen = true;
        yield return new WaitForSeconds(.5f);
    }

    IEnumerator closing()
    {
        print("you are closing the door");
        openandclose1.Play("doorOpen1 0");
        audioSource.PlayOneShot(closeSound);
        isOpen = false;
        yield return new WaitForSeconds(.5f);
    }

    public void Interact(GameObject interactor)
    {
        if (!isOpen)
            DoorOpening();
        else
            DoorClosing();
    }
 

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_OpenDoor() //호스트 호출
    {
        StartCoroutine(opening());
        isOpen = true;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void RPC_RequestDoorOpen() //클라이언트가 호출
    {
        RPC_OpenDoor();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_CloseDoor()
    {
        StartCoroutine(closing());
        isOpen = false;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void RPC_RequestDoorClose()
    {
        RPC_CloseDoor();
    }

    public override void Render()
    {
        if (isOpen && !openandclose1.GetCurrentAnimatorStateInfo(0).IsName("doorOpen1"))
        {
            openandclose1.Play("doorOpen1");
        }
    }

    public void DoorOpening()
    {
        if (!Object.HasStateAuthority) //호스트가 아닌 경우 요청
        {
            RPC_RequestDoorOpen();
            return;
        }

        RPC_OpenDoor(); //호스트인 경우 문 열기       

    }

    public void DoorClosing()
    {
        if (!Object.HasStateAuthority)
        {
            RPC_RequestDoorClose();
            return;
        }

        RPC_CloseDoor();
    }
    public void EnableOutline()
    {
       
    }

    public void DisableOutline()
    {
       
    }
}
