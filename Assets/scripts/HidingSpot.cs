using UnityEngine;

public class HidingSpot : MonoBehaviour
{
    [Header("Точки для игрока")]
    public Transform insidePosition;  
    public Transform exitPosition;    

    private bool isOccupied = false;

    public void Interact(GameObject player)
    {
        MobileController controller = player.GetComponent<MobileController>();
        if (controller == null) return;

        isOccupied = !isOccupied;
        controller.SetHidingState(isOccupied, this);
    }
}