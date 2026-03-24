using System.Collections;
using UnityEngine;
using Fusion;

public class InsideElevatorInteract : NetworkBehaviour, IInteractable
{

    public enum ButtonType { Open, Close }
    [SerializeField] private ButtonType buttonType;

    public MeshRenderer openMesh;
    public MeshRenderer openMesh2;

    public MeshRenderer closeMesh;
    public MeshRenderer closeMesh2;

  
   

    AudioSource audioSource;
    public AudioClip buttonSound;

    [ColorUsage(true, true)] public Color inactiveColor = Color.black;
    [ColorUsage(true, true)] public Color activeColor = new Color(121, 191, 97, 255);

    MaterialPropertyBlock propertyBlock;

    public ElevatorInteract elevator;

    private Outline outline;
    private bool isPressed = false;

    private void Awake()
    {        
        propertyBlock = new MaterialPropertyBlock();
        propertyBlock.SetColor("_EmissionColor", inactiveColor);
        audioSource = GetComponent<AudioSource>();

        if (outline == null)
            outline = GetComponent<Outline>();
        
        if (outline != null)
            outline.enabled = false;   
    }

    public void Interact(GameObject interactor)
    {
        if (isPressed) return;


        if (buttonType == ButtonType.Open)
        {
            OpenInsideDoor();
        }
        else if (buttonType == ButtonType.Close)
        {
            CloseInsideDoor();
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

    void OpenInsideDoor()
    {      
        elevator.DoorOpening();
        audioSource.PlayOneShot(buttonSound);
        propertyBlock.SetColor("_EmissionColor", activeColor);
        openMesh.SetPropertyBlock(propertyBlock);
        openMesh2.SetPropertyBlock(propertyBlock);

        StartCoroutine(TurnOffOpenColor());
    }

    void CloseInsideDoor()
    {
        audioSource.PlayOneShot(buttonSound);     
        propertyBlock.SetColor("_EmissionColor", activeColor);
        closeMesh.SetPropertyBlock(propertyBlock);
        closeMesh2.SetPropertyBlock(propertyBlock);

        StartCoroutine(TurnOffCloseColor());
    }

    IEnumerator TurnOffOpenColor()
    {        
        yield return new WaitForSeconds(3f);
        propertyBlock.SetColor("_EmissionColor", inactiveColor);
        openMesh.SetPropertyBlock(propertyBlock);
        openMesh2.SetPropertyBlock(propertyBlock);  
    }

    IEnumerator TurnOffCloseColor()
    {
        yield return new WaitForSeconds(3f);
        propertyBlock.SetColor("_EmissionColor", inactiveColor);
        closeMesh.SetPropertyBlock(propertyBlock);
        closeMesh2.SetPropertyBlock(propertyBlock);     
    }
}
