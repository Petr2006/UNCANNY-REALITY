using UnityEngine;

public class ExitDoorTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Срабатывает мгновенно на любой входящий объект
        if (GameplayUIController.Instance != null)
        {
            GameplayUIController.Instance.TriggerVictory();
        }
    }
}