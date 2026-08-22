using UnityEngine;
using System.Collections.Generic;

public class FlickeringLight : MonoBehaviour
{
    [Header("Настройки света")]
    [Tooltip("Ссылка на компонент Light. Если пусто, скрипт попробует найти его сам.")]
    public Light targetLight;
    
    [Tooltip("Объект модели лампы (чтобы отключать светящуюся текстуру/материал одновременно со светом).")]
    public Renderer lampRenderer;

    [Header("Настройки мерцания")]
    public float minIntensity = 0.05f;
    public float maxIntensity = 1.5f;
    
    [Tooltip("Шанс того, что лампа полностью погаснет на мгновение (от 0 до 1).")]
    [Range(0f, 1f)]
    public float blackoutChance = 0.15f;

    [Tooltip("Скорость обновления мерцания. Чем меньше, тем быстрее и безумнее мигает.")]
    public float flickerSpeed = 0.08f;

    private float nextFlickerTime;
    private bool isBlackout = false;
    private Color originalEmissionColor;
    private Material lampMaterial;

    void Start()
    {
        // Если свет не привязан вручную, ищем его на этом же объекте
        if (targetLight == null)
            targetLight = GetComponent<Light>();

        if (targetLight == null)
        {
            Debug.LogError($"[VoidByte] На объекте {gameObject.name} нет компонента Light! Привяжите его в инспекторе.");
            enabled = false;
            return;
        }

        // Подхватываем материал для свечения лампочки
        if (lampRenderer != null)
        {
            lampMaterial = lampRenderer.material;
            if (lampMaterial.HasProperty("_EmissionColor"))
            {
                originalEmissionColor = lampMaterial.GetColor("_EmissionColor");
            }
        }
    }

    void Update()
    {
        if (Time.time < nextFlickerTime) return;

        // Планируем следующее мигание с небольшой случайной задержкой
        nextFlickerTime = Time.time + Random.Range(flickerSpeed * 0.5f, flickerSpeed * 1.5f);

        // Случайный сбой (полная темнота)
        if (Random.value < blackoutChance)
        {
            isBlackout = !isBlackout;
        }

        if (isBlackout)
        {
            targetLight.intensity = minIntensity;
            ToggleEmission(false);
        }
        else
        {
            // Случайная интенсивность для эффекта «дрожания» света
            targetLight.intensity = Random.Range(minIntensity, maxIntensity);
            ToggleEmission(targetLight.intensity > (maxIntensity * 0.4f));
        }
    }

    void ToggleEmission(bool isOn)
    {
        if (lampMaterial == null) return;

        if (isOn)
        {
            lampMaterial.SetColor("_EmissionColor", originalEmissionColor);
            DynamicGI.SetEmissive(lampRenderer, originalEmissionColor);
        }
        else
        {
            lampMaterial.SetColor("_EmissionColor", Color.black);
            DynamicGI.SetEmissive(lampRenderer, Color.black);
        }
    }
}