using UnityEngine;

public class CollectibleItem : MonoBehaviour
{
    [Header("Связь с инвентарем")]
    [Tooltip("Перетащи сюда твоего Player (на котором висит InventoryController)")]
    public InventoryController inventory;

    [Header("Настройки предмета")]
    [Tooltip("Название предмета (например: Фонарик, Лом, Ключ)")]
    public string itemName;

    [Tooltip("Дистанция, с которой игрок может взять предмет")]
    public float pickupDistance = 3f;

    private void Start()
    {
        // Если забыл перетащить в инспекторе, скрипт попытается найти сам в качестве запасного плана
        if (inventory == null)
        {
            inventory = Object.FindFirstObjectByType<InventoryController>();
        }

        if (inventory == null)
        {
            Debug.LogError($"[VoidByte] Ошибка: На сцене нет объекта с компонентом InventoryController! Повесь его на Player.");
        }
    }

    private void Update()
    {
        if (inventory == null) return;

        // Считаем расстояние между фонариком и инвентарем (который висит на игроке)
        float distance = Vector3.Distance(transform.position, inventory.transform.position);

        if (distance <= pickupDistance)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                // Добавляем в инвентарь
                inventory.AddItem(itemName);
                
                // Печатаем в консоль подтверждение, чтобы точно знать, что предмет зашел в список
                Debug.Log($"[VoidByte] Предмет '{itemName}' успешно добавлен! Текущий размер инвентаря: {inventory.items.Count}");
                
                // Уничтожаем физический объект фонарика
                Destroy(gameObject);
            }
        }
    }
}