using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ElectricBox : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Перетащи сюда объект HouseLights, в котором лежат все лампы")]
    public GameObject lightsGroup;

    [Header("Звук включения")]
    [Tooltip("Сюда перетащи аудиофайл щелчка рубильника или гула")]
    public AudioClip switchOnSound;

    private AudioSource audioSource;
    private bool isActivated = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        // На старте проверяем, что свет выключен
        if (lightsGroup != null)
        {
            lightsGroup.SetActive(false);
        }
    }

    // Этот метод вызовется автоматически из нашего MobileController при нажатии кнопки
    public void ActivatePower()
    {
        if (isActivated) return; // Если уже включили, второй раз нажать нельзя

        isActivated = true;

        // 1. Включаем всю группу света
        if (lightsGroup != null)
        {
            lightsGroup.SetActive(true);
            Debug.Log("<color=yellow>[ЭЛЕКТРИЧЕСТВО]</color> Свет в доме успешно включен!");
        }

        // 2. Воспроизводим звук рубильника
        if (audioSource != null && switchOnSound != null)
        {
            audioSource.PlayOneShot(switchOnSound);
        }
    }
}