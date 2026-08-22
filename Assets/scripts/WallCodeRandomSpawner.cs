using UnityEngine;
using System.Collections.Generic;
using TMPro; // Подключаем TextMeshPro

public class ShowWallCode : MonoBehaviour
{
    [Header("Настройка Текста")]
    public TextMeshProUGUI textComponent; // Перетащите сюда сам компонент Текста

    [Header("Точки появления (Empty GameObjects)")]
    public List<Transform> possiblePoints; // СЮДА ПЕРЕТАСКИВАТЬ ТОЧКИ

    void Start()
    {
        // 1. Если забыли перетащить текст в инспекторе, берем его с этого же объекта
        if (textComponent == null)
        {
            textComponent = GetComponent<TextMeshProUGUI>();
        }

        // 2. Генерируем 4 случайные цифры
        if (textComponent != null)
        {
            string code = Random.Range(1000, 9999).ToString();
            textComponent.text = code;
            Debug.Log($"<color=green>[VoidByte]</color> Сгенерирован код: {code}");
        }

        // 3. Перемещаем Canvas/Текст в случайную точку из списка
        if (possiblePoints != null && possiblePoints.Count > 0)
        {
            int randomIndex = Random.Range(0, possiblePoints.Count);
            Transform chosenPoint = possiblePoints[randomIndex];

            if (chosenPoint != null)
            {
                // Если текст внутри Canvas, перемещаем весь Canvas (родителя)
                Transform objectToMove = transform.parent != null ? transform.parent : transform;
                
                objectToMove.position = chosenPoint.position;
                objectToMove.rotation = chosenPoint.rotation;
            }
        }
    }
}