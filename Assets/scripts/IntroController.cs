using UnityEngine;
using System.Collections; // Исправлено для Unity

public class IntroController : MonoBehaviour
{
    [Header("Настройки")]
    public float waitBeforeFade = 2f;    // Сколько секунд темноты на старте         
    public float fadeSpeed = 0.5f;       // Скорость проявления комнаты
    public AudioClip wakeUpSound;        // Сюда перетащи только аудиофайл вздоха

    private MonoBehaviour mobileController; // Скрипт джойстика
    private Texture2D blackTexture;         // Черная текстура для экрана
    private float currentAlpha = 1f;        // Текущая прозрачность (1 - темно)
    private bool isFading = false;
    private AudioSource audioSource;

    void Start()
    {
        // 1. Автоматически ищем скрипт мобильного управления на Игроке
        // Замени "MobileController" ниже на точное название твоего скрипта ходьбы, если оно другое!
        mobileController = GetComponent<MobileController>();
        if (mobileController == null)
        {
            mobileController = GetComponentInChildren<MonoBehaviour>();
        }

        // Выключаем джойстики на старте
        if (mobileController != null) mobileController.enabled = false;

        // 2. Настраиваем звук
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // 3. Создаем черную текстуру для рисования на экране
        blackTexture = new Texture2D(1, 1);
        blackTexture.SetPixel(0, 0, Color.black);
        blackTexture.Apply();

        // 4. Запускаем проявление
        StartCoroutine(IntroRoutine());
    }

    // Этот метод рисует черный экран прямо поверх игры БЕЗ создания UI объектов
    void OnGUI()
    {
        if (currentAlpha > 0f)
        {
            GUI.color = new Color(0f, 0f, 0f, currentAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), blackTexture);
        }
    }

    IEnumerator IntroRoutine()
    {
        // Ждем в полной темноте
        yield return new WaitForSeconds(waitBeforeFade);

        isFading = true;

        // Плавно уменьшаем прозрачность черного экрана
        while (currentAlpha > 0f)
        {
            currentAlpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }

        currentAlpha = 0f; // Экран полностью чистый
        isFading = false;

        // 5. Включаем игроку управление!
        if (mobileController != null) mobileController.enabled = true;
        
        // 6. Воспроизводим звук вздоха
        if (audioSource != null && wakeUpSound != null)
        {
            audioSource.PlayOneShot(wakeUpSound);
        }

        // Скрипт сделал работу и удаляет себя
        Destroy(this);
    }
}