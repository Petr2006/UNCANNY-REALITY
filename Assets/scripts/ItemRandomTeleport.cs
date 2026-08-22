using UnityEngine;
using System.Collections.Generic;

public class ItemRandomTeleport : MonoBehaviour
{
    [Header("Точки появления для ЭТОГО предмета")]
    public List<Transform> possiblePoints;

    [Header("Настройка ID для инвентаря")]
    public string itemIdForInventory = "Key1";

    void Start()
    {
        // 1. Принудительно настраиваем ID
        KeyItem keyScript = GetComponent<KeyItem>() ?? GetComponentInChildren<KeyItem>() ?? GetComponentInParent<KeyItem>();
        if (keyScript != null)
        {
            keyScript.itemId = itemIdForInventory;
        }
        else
        {
            // Если скрипта вообще не было, добавляем его прямо сюда
            keyScript = gameObject.AddComponent<KeyItem>();
            keyScript.itemId = itemIdForInventory;
        }

        // 2. Убеждаемся, что на этом ИМЕННО объекте есть физический коллайдер для Raycast
        Collider coll = GetComponent<Collider>();
        if (coll == null)
        {
            // Если коллайдера нет, создаем BoxCollider автоматически
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = false; // Делаем твердым
            // Немного увеличим размер для удобства подбора на телефоне
            box.size = new Vector3(0.3f, 0.3f, 0.3f); 
        }
        else
        {
            coll.isTrigger = false; // Выключаем триггер, если он был включен
        }

        // 3. Ставим правильный слой
        gameObject.layer = LayerMask.NameToLayer("Default");

        // 4. Телепортируем в случайную точку
        if (possiblePoints == null || possiblePoints.Count == 0)
        {
            Debug.LogError($"[VoidByte] У объекта {gameObject.name} не настроены точки спавна!");
            return;
        }

        int randomIndex = Random.Range(0, possiblePoints.Count);
        Transform chosenPoint = possiblePoints[randomIndex];

        if (chosenPoint != null)
        {
            transform.position = chosenPoint.position;
            transform.rotation = chosenPoint.rotation;
            Debug.Log($"<color=cyan>[VoidByte]</color> {gameObject.name} успешно настроен и прыгнул в: {chosenPoint.name}");
        }
    }
}