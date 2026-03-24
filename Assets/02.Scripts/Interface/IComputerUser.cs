using UnityEngine;

public interface IComputerUser
{
    void StartComputerInteraction(Vector3 pos, Quaternion rot);
    void StopComputerInteraction();
}
