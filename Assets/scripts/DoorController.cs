using UnityEngine;
using TMPro;
using UnityEngine.AI;

[RequireComponent(typeof(AudioSource))]
public class DoorController : MonoBehaviour
{
    [Header("Настройки Вращения")]
    public float openAngle = 90f;        
    public float rotationSpeed = 3f;     
    public float interactionDistance = 4f; 
    
    [Tooltip("Если включено — дверь открывается наружу (-openAngle). Если выключено — внутрь (+openAngle).")]
    public bool openOutwards = false; 

    [Header("Настройки Запертой Двери")]
    public bool isLocked = false;          
    [Tooltip("ОБЯЗАТЕЛЬНО: Перетащите сюда префаб ключа или объект ключа со сцены")]
    public GameObject requiredKey;         

    [Header("Настройки Хоррор Блокировок")]
    public bool isCodeLocked = false;
    public bool hasWoodenPlanks = false;
    
    [Tooltip("Список ВСЕХ досок на двери")]
    public GameObject[] planksList; 

    [Header("Звуки (Аудиоклипы)")]
    public AudioClip openSound;          
    public AudioClip lockedSound;        
    public AudioClip lockedVoice;        
    public AudioClip woodBreakSound; 

    [Header("Интерфейс (Canvas)")]
    public TextMeshProUGUI keyIndicatorText; 
    public TextMeshProUGUI actionText;       

    private bool isOpen = false;
    private Quaternion closedRotation;
    private Quaternion openRotation;
    private AudioSource audioSource;
    private float textTimer = 0f;
    private bool hasPlayedVoice = false; 
    private NavMeshObstacle navObstacle;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f; 

        closedRotation = transform.localRotation;
        
        // Направление вращения в зависимости от галочки openOutwards
        float finalAngle = openOutwards ? -Mathf.Abs(openAngle) : Mathf.Abs(openAngle);
        openRotation = Quaternion.Euler(transform.localEulerAngles.x, transform.localEulerAngles.y + finalAngle, transform.localEulerAngles.z);

        if (keyIndicatorText != null) keyIndicatorText.text = "";
        if (actionText != null) actionText.text = "";

        navObstacle = GetComponentInChildren<NavMeshObstacle>();
        UpdateObstacleState();
    }

    void Update()
    {
        if (textTimer > 0)
        {
            textTimer -= Time.deltaTime;
            if (textTimer <= 0 && actionText != null) actionText.text = "";
        }

        Quaternion targetRotation = isOpen ? openRotation : closedRotation;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * rotationSpeed);
    }

    public bool IsBlocked()
    {
        return isLocked || isCodeLocked || hasWoodenPlanks;
    }

    public void UnlockCode()
    {
        isCodeLocked = false;
        Debug.Log("<color=green>[ДВЕРЬ]</color> Код принят.");
        
        if (!isLocked && !hasWoodenPlanks)
        {
            isOpen = true;
            PlaySound(openSound);
            UpdateObstacleState();
        }
    }

    public void ExecuteDirectInteraction()
    {
        // 1. Проверка досок
        if (hasWoodenPlanks)
        {
            InventoryController inventory = FindFirstObjectByType<InventoryController>();
            if (inventory != null && (inventory.HasItem("Crowbar") || inventory.HasItem("Лом"))) 
            {
                hasWoodenPlanks = false;
                PlaySound(woodBreakSound);
                
                if (planksList != null)
                {
                    foreach (GameObject plank in planksList)
                    {
                        if (plank != null) plank.SetActive(false);
                    }
                }
                
                ShowMessage("Вы сорвали доски ломом!");
                
                if (!isCodeLocked && !isLocked)
                {
                    isOpen = true;
                    PlaySound(openSound);
                    UpdateObstacleState();
                }
            }
            else
            {
                ShowMessage("Дверь заколочена. Нужен лом!");
                PlaySound(lockedSound);
            }
            return;
        }

        // 2. Проверка электронного кода
        if (isCodeLocked)
        {
            ShowMessage("Заперто на электронный замок.");
            PlaySound(lockedSound);
            return;
        }

        // 3. Проверка механического ключа
        if (isLocked)
        {
            InventoryController inventory = FindFirstObjectByType<InventoryController>();
            
            if (inventory != null && requiredKey != null)
            {
                KeyItem keyScript = requiredKey.GetComponent<KeyItem>() ?? requiredKey.GetComponentInChildren<KeyItem>();
                string keyIdNeeded = (keyScript != null) ? keyScript.itemId : requiredKey.name;

                if (inventory.HasItem(keyIdNeeded))
                {
                    inventory.RemoveItem(keyIdNeeded); 
                    isLocked = false; 
                    isOpen = true;
                    
                    PlaySound(openSound);
                    UpdateObstacleState();
                    ShowMessage("Дверь открыта ключом!");
                    return; 
                }
            }

            ShowMessage("Заперто. Нужен ключ!");
            PlaySound(lockedSound);

            if (!hasPlayedVoice && lockedVoice != null)
            {
                hasPlayedVoice = true; 
                audioSource.PlayOneShot(lockedVoice); 
            }
            return;
        }

        // 4. Обычное открытие/закрытие
        isOpen = !isOpen;
        PlaySound(openSound);
        UpdateObstacleState();
    }

    private void UpdateObstacleState()
    {
        if (navObstacle != null) navObstacle.enabled = !isOpen; 
    }

    void ShowMessage(string message)
    {
        if (actionText != null)
        {
            actionText.text = message;
            textTimer = 3f; 
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void NPC_OpenDoor()
    {
        if (!isOpen)
        {
            isOpen = true;
            PlaySound(openSound);
            UpdateObstacleState();
        }
    }
}