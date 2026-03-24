using System.Collections;
using UnityEngine;
using Fusion;

public class ElevatorInteract : NetworkBehaviour
{
    //코드 담당자 : 최은주 
    //바깥 엘레베이터 문 애니메이션 스크립트

    string animName;
    public Animation doorAnim;
    public AudioClip openSound;
    public AudioClip closeSound;
    AudioSource audioSource;
    public Animation doorAnim2;
    string animName2;
    GameObject player;   

    bool isOpen;

    void Awake()
    {
        doorAnim = GetComponent<Animation>();
        player = GameObject.FindWithTag("Player");
        animName = doorAnim.clip.name;
        animName2 = doorAnim2.clip.name;
        audioSource = GetComponent<AudioSource>();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestDoorOpen()
    {
        if (!isOpen)
            RPC_DoorOpen();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_DoorOpen()
    {
        if (isOpen) return;
        isOpen = true;

        doorAnim[animName].speed = 1;
        doorAnim[animName].normalizedTime = 0f;
        doorAnim.Play(); //애니메이션 재생

        doorAnim2[animName2].speed = 1;
        doorAnim2[animName2].normalizedTime = 0f;
        doorAnim2.Play();

        if (openSound)
        {
            audioSource.PlayOneShot(openSound);
        }

        StartCoroutine(DoorClosing());
    }

    public void DoorOpening()
    { 
        if(!Object.HasStateAuthority) //호스트가 아닌 경우 요청
        {
            RPC_RequestDoorOpen();
            return;
        }

        RPC_DoorOpen(); //호스트인 경우 문 열기       

    }

    public IEnumerator DoorClosing()
    {
        yield return new WaitForSeconds(3.9f);
        isOpen = false;
        Debug.Log("문이 닫힘");

        doorAnim[animName].speed = -1;
        doorAnim[animName].normalizedTime = 1f;
        doorAnim.Play();

        InsideDoorClose();

        if (closeSound)
        {
            audioSource.PlayOneShot(closeSound);
        }       
    }

    public void OutsideDoorClose()
    {       
        doorAnim[animName].speed = -1;
        doorAnim[animName].normalizedTime = 1f;
        doorAnim.Play();
    }
    public void InsideDoorClose()
    {        
        doorAnim2[animName2].speed = -1;
        doorAnim2[animName2].normalizedTime = 1f;
        doorAnim2.Play();
    }
}