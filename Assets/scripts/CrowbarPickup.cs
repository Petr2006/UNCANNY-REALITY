using UnityEngine;

public class CrowbarPickup : MonoBehaviour
{
    public void Pickup()
    {
        // Ищем контроллер игрока в сцене
        MobileController player = FindFirstObjectByType<MobileController>();
        if (player != null)
        {
            player.hasCrowbar = true; // Разрешаем кнопку удара
            Debug.Log("<color=orange>[ЛОМ]</color> Лом успешно подобран!");
            Destroy(gameObject); // Уничтожаем лом со стола
        }
        else
        {
            Debug.LogError("Игрок со скриптом MobileController не найден на сцене!");
        }
    }
}