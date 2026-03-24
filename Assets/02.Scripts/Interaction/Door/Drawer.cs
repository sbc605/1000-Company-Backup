using Fusion;
using UnityEngine;

public class Drawer : NetworkBehaviour, IInteractable
{
    [Header("Drawer Settings")]
    [SerializeField] private float smooth = 2.0f;
    [SerializeField] private Vector3 openOffset = new Vector3(0.3f, 0, 0);

    [Header("Audio")]
    private AudioSource audioSource;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;

    [Header("References")]
    private Outline outline;

    [Networked]
    public NetworkBool IsOpen { get; set; }

    private bool previousIsOpen = false;
    private bool closeSoundPlayed = true;
    
    private Vector3 defaultPosition;
    private Vector3 openPosition;
    
    private const float CLOSE_DISTANCE_THRESHOLD = 0.01f;

    private void Awake()
    {
        defaultPosition = transform.localPosition;
        openPosition = defaultPosition + openOffset;

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

        Vector3 targetPosition = IsOpen ? openPosition : defaultPosition;
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * smooth);

        if (!IsOpen && !closeSoundPlayed)
        {
            float distance = Vector3.Distance(transform.localPosition, defaultPosition);

            if (distance < CLOSE_DISTANCE_THRESHOLD)
            {
                if (closeSound != null)
                    audioSource.PlayOneShot(closeSound);
                
                closeSoundPlayed = true;
            }
        }
    }
}