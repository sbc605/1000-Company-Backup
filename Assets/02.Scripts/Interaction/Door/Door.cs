using Fusion;
using UnityEngine;

public class Door : NetworkBehaviour, IInteractable
{
    [Header("Door Settings")]
    [SerializeField] private float smooth = 2.0f;
    [SerializeField] private float DoorOpenAngle = 90.0f;

    [Header("Audio")]
    private AudioSource audioSource;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioClip knockSound;
    [SerializeField] private AudioClip knockHardSound;
    [SerializeField] private AudioClip lockSound;


    [Header("References")]
    private Outline outline;

    [Networked]
    public NetworkBool IsOpen { get; set; }
    private bool previousIsOpen = false;

    private Quaternion defaultRotation;
    private Quaternion openRotation;
    private bool closeSoundPlayed = true;
    private const float CLOSE_ANGLE_THRESHOLD = 1.0f;

    private void Awake()
    {
        defaultRotation = transform.rotation;
        Vector3 defaultEuler = transform.eulerAngles;
        openRotation = Quaternion.Euler(defaultEuler.x, defaultEuler.y + DoorOpenAngle, defaultEuler.z);

        if (outline == null)
            outline = GetComponent<Outline>();
        if (outline != null)
            outline.enabled = false;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.spatialBlend = 1.0f;

    }

    public override void Spawned()
    {
        previousIsOpen = IsOpen;
        closeSoundPlayed = !IsOpen;
    }

    public void Interact(GameObject interactor)
    {
        if (!HasStateAuthority)
            return;
        IsOpen = !IsOpen;

    }

    public void EnableOutline()
    {
        if (outline != null) outline.enabled = true;
    }

    public void DisableOutline()
    {
        if (outline != null) outline.enabled = false;
    }

    public override void Render()
    {
        if (IsOpen != previousIsOpen)
        {
            if (IsOpen)
            {
                if (openSound != null)
                    audioSource.PlayOneShot(openSound);
                closeSoundPlayed = true;
            }
            else
            {
                closeSoundPlayed = false;
            }
            previousIsOpen = IsOpen;
        }

        Quaternion targetRotation = IsOpen ? openRotation : defaultRotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smooth);

        if (!IsOpen && !closeSoundPlayed)
        {
            float angle = Quaternion.Angle(transform.rotation, defaultRotation);
            if (angle < CLOSE_ANGLE_THRESHOLD)
            {
                if (closeSound != null)
                    audioSource.PlayOneShot(closeSound);
                closeSoundPlayed = true;
            }
        }
    }

    public void LockDoor()
    {
        audioSource.PlayOneShot(lockSound);
    }
}