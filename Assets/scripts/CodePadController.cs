using UnityEngine;

public class CodePadController : MonoBehaviour
{
    [Header("Настройки пароля")]
    [HideInInspector] public string correctCode = ""; 
    public int codeLength = 4;                       

    [Header("Прямые ссылки из Инспектора")]
    [Tooltip("Перетащите сюда 3D-текст со стены сцены (Canvas или MeshText)")]
    public GameObject wallTextObject; 

    [Tooltip("ОБЯЗАТЕЛЬНО: Перетащите сюда объект DoorPivot, на котором висит скрипт DoorController")]
    public DoorController doorToOpen;

    private string currentInput = "";
    private bool isCleared = false; // Блокировка терминала после успеха

    void Start()
    {
        GenerateRandomCode();
    }

    void GenerateRandomCode()
    {
        int randomNum = Random.Range(1000, 10000);
        correctCode = randomNum.ToString();

        if (wallTextObject != null)
        {
            wallTextObject.SendMessage("SetText", correctCode, SendMessageOptions.DontRequireReceiver);
            
            var legacyComponent = wallTextObject.GetComponent("Text");
            if (legacyComponent != null)
            {
                System.Type type = legacyComponent.GetType();
                var property = type.GetProperty("text");
                if (property != null) property.SetValue(legacyComponent, correctCode, null);
            }
            Debug.Log("Код сгенерирован и отправлен на стену: " + correctCode);
        }
    }

    public void InteractWithPad()
    {
        // Если код уже угадан, терминал больше не реагирует
        if (isCleared) return;

        if (GameplayUIController.Instance != null)
        {
            GameplayUIController.Instance.OpenCodePad(this);
        }
    }

    public void SendNumber(string number)
    {
        if (isCleared) return;
        if (currentInput.Length < codeLength)
        {
            currentInput += number;
            PlayPressSound();
        }
    }

    public void PlayPressSound()
    {
        AudioSource audio = GetComponent<AudioSource>();
        if (audio != null) audio.Play();
    }

    public void ClearInput()
    {
        currentInput = "";
    }

    // Вызывается Зеленой кнопкой (V) из OnGUI
    public void CheckCode()
    {
        if (isCleared) return;

        if (currentInput == correctCode)
        {
            Debug.Log("<color=green>[ТЕРМИНАЛ]</color> КОД ВЕРЕН! Отключаем интерфейс терминала.");
            isCleared = true; // Навсегда блокируем этот терминал
            
            if (doorToOpen != null)
            {
                doorToOpen.UnlockCode(); // Снимаем электронную блокировку на двери
            }
            
            ExitPad();
        }
        else
        {
            Debug.Log("<color=red>[ТЕРМИНАЛ]</color> Неверный код! Сброс.");
            ClearInput();
        }
    }

    // Вызывается Красной кнопкой (X) из OnGUI
    public void ExitPad()
    {
        if (GameplayUIController.Instance != null)
        {
            GameplayUIController.Instance.CloseCodePad();
        }
        ClearInput();
    }

    public string GetCurrentInput()
    {
        return currentInput;
    }

    public bool IsCleared()
    {
        return isCleared;
    }
}