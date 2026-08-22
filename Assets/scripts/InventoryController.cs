using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryController : MonoBehaviour
{
    [Header("Current Items")]
    public List<string> items = new List<string>();

    [Header("UI Settings (OnGUI)")]
    [Tooltip("Toggle inventory visibility on screen")]
    public bool showInventory = true;

    // Стили OnGUI
    private GUIStyle panelStyle;
    private GUIStyle itemSlotStyle;
    private GUIStyle titleStyle;

    // Текстуры для отрисовки
    private Texture2D panelTex;
    private Texture2D slotTex;
    private Texture2D activeSlotTex;

    private void Start()
    {
        // Создаем стильный темно-серый фон с красной хоррор-рамкой для панели
        panelTex = CreateBorderedTexture(new Color(0.03f, 0.03f, 0.03f, 0.85f), new Color(0.5f, 0.05f, 0.05f, 0.9f), 4);
        // Обычный пустой слот (темный с серой рамкой)
        slotTex = CreateBorderedTexture(new Color(0.08f, 0.08f, 0.08f, 0.9f), new Color(0.2f, 0.2f, 0.2f, 0.6f), 2);
        // Активный слот (более яркий с выраженной красной рамкой)
        activeSlotTex = CreateBorderedTexture(new Color(0.15f, 0.05f, 0.05f, 0.95f), new Color(0.8f, 0.05f, 0.05f, 0.95f), 3);
    }

    public void AddItem(string itemName)
    {
        if (!items.Contains(itemName))
        {
            items.Add(itemName);
            Debug.Log($"[VoidByte] Added to inventory: {itemName}");
        }
    }

    public void RemoveItem(string itemName)
    {
        if (items.Contains(itemName))
        {
            items.Remove(itemName);
            Debug.Log($"[VoidByte] Removed from inventory: {itemName}");
        }
    }

    public bool HasItem(string itemName)
    {
        // Приводим к нижнему регистру для исключения ошибок с разным регистром букв
        return items.Exists(x => x.ToLower() == itemName.ToLower());
    }

    private void InitializeStyles()
    {
        if (panelStyle == null)
        {
            panelStyle = new GUIStyle();
            panelStyle.normal.background = panelTex;
        }

        if (itemSlotStyle == null)
        {
            itemSlotStyle = new GUIStyle(GUI.skin.box);
            itemSlotStyle.fontSize = 20; // Увеличенный шрифт текста в слотах
            itemSlotStyle.fontStyle = FontStyle.Bold;
            itemSlotStyle.alignment = TextAnchor.MiddleCenter;
            itemSlotStyle.normal.textColor = Color.white;
        }

        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 16; // Чуть крупнее заголовок
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.UpperLeft;
            titleStyle.normal.textColor = new Color(0.9f, 0.1f, 0.1f, 0.95f);
        }
    }

    private void OnGUI()
    {
        if (!showInventory) return;

        // Если пауза или смерть, отключаем отрисовку инвентаря
        if (GameplayUIController.Instance != null && GameplayUIController.Instance.IsGamePausedOrDead())
        {
            return;
        }

        InitializeStyles();

        // Масштабирование под FullHD (1920x1080)
        float rx = Screen.width / 1920f;
        float ry = Screen.height / 1080f;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(rx, ry, 1));

        // --- НОВЫЕ БОЛЬШИЕ РАЗМЕРЫ ПАНЕЛИ ---
        float panelWidth = 900f;   // Было 700, стало намного шире!
        float panelHeight = 140f;  // Было 110, стало выше!
        float panelX = (1920f / 2f) - (panelWidth / 2f); 
        float panelY = 25f; // Отступ сверху

        // Отрисовка подложки инвентаря
        GUI.Box(new Rect(panelX, panelY, panelWidth, panelHeight), "", panelStyle);

        // Заголовок
        GUI.Label(new Rect(panelX + 30, panelY + 12, 200, 25), "EQUIPMENT", titleStyle);

        // Настройки слотов
        int maxSlots = 5; 
        float slotWidth = 150f;   // Было 115, слоты стали солидными и большими!
        float slotHeight = 80f;   // Было 60, гораздо больше места под текст!
        float slotSpacing = 20f;  // Отступы между слотами
        float startX = panelX + (panelWidth - (maxSlots * slotWidth + (maxSlots - 1) * slotSpacing)) / 2f;
        float slotsY = panelY + 45f;

        for (int i = 0; i < maxSlots; i++)
        {
            Rect slotRect = new Rect(startX + i * (slotWidth + slotSpacing), slotsY, slotWidth, slotHeight);

            if (i < items.Count)
            {
                // Если ячейка занята предметом
                string displayItemName = items[i].ToUpper();
                itemSlotStyle.normal.background = activeSlotTex; // Красивая красная рамка для активного предмета
                
                GUI.Box(slotRect, displayItemName, itemSlotStyle);
            }
            else
            {
                // Пустая ячейка
                itemSlotStyle.normal.background = slotTex;
                GUI.color = new Color(1f, 1f, 1f, 0.25f); // Делаем полупрозрачным надпись "EMPTY"
                GUI.Box(slotRect, "[ EMPTY ]", itemSlotStyle);
                GUI.color = Color.white; // Сброс прозрачности
            }
        }
    }

    private Texture2D CreateBorderedTexture(Color bodyColor, Color borderColor, int borderWidth)
    {
        int size = 128;
        Texture2D texture = new Texture2D(size, size);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (x < borderWidth || x > size - (borderWidth + 1) || y < borderWidth || y > size - (borderWidth + 1))
                {
                    texture.SetPixel(x, y, borderColor);
                }
                else
                {
                    texture.SetPixel(x, y, bodyColor);
                }
            }
        }
        texture.Apply();
        return texture;
    }
}