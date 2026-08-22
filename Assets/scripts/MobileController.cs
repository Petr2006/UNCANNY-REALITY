using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class MobileController : MonoBehaviour
{
    [Header("Ссылки")]
    public Camera playerCamera;
    public float interactionDistance = 4f;

    [Header("Настройки Фонарика")]
    public GameObject flashlightLight;
    [HideInInspector] public bool hasFlashlight = false; 
    private bool isFlashlightOn = false;

    [Header("Настройки Лома")]
    public bool hasCrowbar = false;
    public CrowbarController crowbarController;

    [Header("Настройки Передвижения")]
    public float moveSpeed = 5f;
    public float lookSpeed = 0.14f;
    public float gravity = 20f;

    private bool isHiding = false;
    private HidingSpot currentCloset;
    private List<MonoBehaviour> disabledScripts = new List<MonoBehaviour>();

    private GUIStyle buttonStyle;
    private GUIStyle joystickStyle;
    
    private Texture2D buttonNormalTex;
    private Texture2D buttonActiveTex;
    private Texture2D buttonAccentTex;
    
    private Texture2D joystickBgTex;
    private Texture2D joystickStickTex;
    private Texture2D crosshairTexture;

    private Rect actionRectGUI;
    private Rect attackRectGUI;
    private Rect flashlightRectGUI;

    private Rect actionRectScreen;
    private Rect attackRectScreen;
    private Rect flashlightRectScreen;
    private Rect pauseRectScreen; 

    private int joystickFingerId = -1;
    private Vector2 joystickCenter;
    private Vector2 joystickCurrentPos;
    private bool showJoystick = false;
    private float joystickRadius = 80f; 
    private float stickRadius = 30f;

    private int cameraFingerId = -1;
    private Vector2 lastCameraTouchPos;
    private float rotationX = 0f;

    private bool canInteractRightNow = false;
    private CharacterController charController;
    private Rigidbody rb;
    private float verticalVelocity = 0f;

    public bool IsHiding() => isHiding;

    void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        
        charController = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();

        if (rb != null && charController != null)
        {
            rb.isKinematic = true;
        }

        if (crowbarController == null && playerCamera != null)
        {
            crowbarController = playerCamera.GetComponent<CrowbarController>();
        }

        if (playerCamera != null)
        {
            Vector3 currentEuler = playerCamera.transform.localEulerAngles;
            rotationX = currentEuler.x;
            if (rotationX > 180f) rotationX -= 360f;
        }

        if (flashlightLight != null) flashlightLight.SetActive(false);

        UpdateSensitivity();
        CreateCustomTextures();
        CalculateUI();
    }

    void Update()
    {
        UpdateSensitivity();
        CalculateUI();
        HandlePauseInputOnly();

        if (GameplayUIController.Instance != null && GameplayUIController.Instance.IsGamePausedOrDead())
        {
            showJoystick = false;
            joystickFingerId = -1;
            cameraFingerId = -1;
            return; 
        }

        if (isHiding && currentCloset != null && currentCloset.insidePosition != null)
        {
            transform.position = currentCloset.insidePosition.position;
            verticalVelocity = 0f; 
        }

        CheckInteractionAvailability();
        HandleAllInputs();
    }

    public void SetHidingState(bool state, HidingSpot closet)
    {
        isHiding = state;
        currentCloset = closet;

        if (charController != null) charController.enabled = !state;

        if (state)
        {
            AudioSource[] allAudioSources = GetComponentsInChildren<AudioSource>();
            foreach (AudioSource source in allAudioSources)
            {
                if (source != null) source.Stop();
            }

            disabledScripts.Clear();
            MonoBehaviour[] allScripts = GetComponentsInChildren<MonoBehaviour>();
            foreach (MonoBehaviour script in allScripts)
            {
                if (script != this && script.enabled && 
                    !script.GetType().Name.Contains("Inventory") && 
                    !script.GetType().Name.Contains("UI"))
                {
                    script.enabled = false;
                    disabledScripts.Add(script);
                }
            }

            if (closet != null && closet.insidePosition != null)
            {
                transform.position = closet.insidePosition.position;
                transform.rotation = closet.insidePosition.rotation;
                
                if (playerCamera != null)
                {
                    playerCamera.transform.localRotation = Quaternion.identity;
                }
                rotationX = 0f;
            }
        }
        else
        {
            foreach (MonoBehaviour script in disabledScripts)
            {
                if (script != null) script.enabled = true;
            }
            disabledScripts.Clear();

            if (closet != null && closet.exitPosition != null)
            {
                transform.position = closet.exitPosition.position;
                
                // ПОВОРАЧИВАЕМ ИГРОКА НА 180 ГРАДУСОВ ОТНОСИТЕЛЬНО ШКАФА (ВПЕРЕД)
                transform.rotation = Quaternion.Euler(0, closet.exitPosition.rotation.eulerAngles.y + 180f, 0);
                
                if (playerCamera != null)
                {
                    playerCamera.transform.localRotation = Quaternion.identity;
                }
                rotationX = 0f;
            }

            if (charController != null) charController.enabled = true;
        }
    }

    void UpdateSensitivity()
    {
        lookSpeed = PlayerPrefs.GetFloat("CameraSensitivity", 0.14f);
    }

    void CalculateUI()
    {
        float sw = Screen.width;
        float sh = Screen.height;

        if (GameplayUIController.Instance != null)
        {
            Rect pRect = GameplayUIController.Instance.pauseButtonRectGUI;
            float rx = sw / 1920f;
            float ry = sh / 1080f;
            pauseRectScreen = new Rect(pRect.x * rx, sh - (pRect.y + pRect.height) * ry, pRect.width * rx, pRect.height * ry);
        }

        float btnSize = 115f; 
        float attackSize = 130f; 

        actionRectGUI = new Rect(sw - btnSize - 60f, sh - btnSize - 70f, btnSize, btnSize);
        attackRectGUI = new Rect(sw - attackSize - 200f, sh - attackSize - 60f, attackSize, attackSize);
        flashlightRectGUI = new Rect(sw - btnSize - 60f, sh - (btnSize * 2f) - 100f, btnSize, btnSize);

        actionRectScreen = new Rect(sw - btnSize - 60f, 70f, btnSize, btnSize);
        attackRectScreen = new Rect(sw - attackSize - 200f, 60f, attackSize, attackSize);
        flashlightRectScreen = new Rect(sw - btnSize - 60f, btnSize + 100f, btnSize, btnSize);
    }

    void HandlePauseInputOnly()
    {
        if (GameplayUIController.Instance == null) return;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Began && pauseRectScreen.Contains(touch.position))
            {
                GameplayUIController.Instance.TogglePause();
                return;
            }
        }

        if (Input.GetMouseButtonDown(0) && pauseRectScreen.Contains(Input.mousePosition))
        {
            GameplayUIController.Instance.TogglePause();
        }
    }

    void CreateCustomTextures()
    {
        buttonNormalTex = CreateCircleTexture(new Color(0.05f, 0.05f, 0.05f, 0.65f), new Color(0.4f, 0.4f, 0.4f, 0.8f));
        buttonActiveTex = CreateCircleTexture(new Color(0.05f, 0.15f, 0.05f, 0.75f), new Color(0.2f, 0.8f, 0.2f, 0.9f));
        buttonAccentTex = CreateCircleTexture(new Color(0.18f, 0.02f, 0.02f, 0.75f), new Color(0.85f, 0.1f, 0.1f, 0.95f));

        joystickBgTex = CreateCircleTexture(new Color(0.05f, 0.05f, 0.05f, 0.2f), new Color(0.4f, 0.4f, 0.4f, 0.3f));
        joystickStickTex = CreateCircleTexture(new Color(0.35f, 0.05f, 0.05f, 0.55f), new Color(0.75f, 0.08f, 0.08f, 0.75f));

        crosshairTexture = CreateColorTexture(new Color(1f, 1f, 1f, 0.35f));
    }

    Texture2D CreateColorTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    Texture2D CreateCircleTexture(Color bodyColor, Color borderColor)
    {
        int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        float radius = size / 2f - 2f;
        float borderThickness = 4f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist < radius - borderThickness)
                {
                    texture.SetPixel(x, y, bodyColor);
                }
                else if (dist < radius)
                {
                    float alpha = Mathf.Clamp01((radius - dist) / 1.5f);
                    texture.SetPixel(x, y, Color.Lerp(Color.clear, borderColor, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        texture.Apply();
        return texture;
    }

    private bool IsTouchOverAnyButton(Vector2 touchPosition)
    {
        if (actionRectScreen.Contains(touchPosition)) return true;
        if (IsCrowbarActive() && !isHiding && attackRectScreen.Contains(touchPosition)) return true;
        if (hasFlashlight && flashlightRectScreen.Contains(touchPosition)) return true;
        if (pauseRectScreen.Contains(touchPosition)) return true; 
        return false;
    }

    void HandleAllInputs()
    {
        Vector3 moveDirection = Vector3.zero;

        if (!isHiding && charController != null && charController.enabled)
        {
            if (charController.isGrounded) verticalVelocity = -1f;
            else verticalVelocity -= gravity * Time.deltaTime;
        }

        if (Input.touchCount == 0)
        {
            SimulateMouse(ref moveDirection);
            if (!isHiding) ApplyMovement(moveDirection);
            return;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            Vector2 touchPos = touch.position;
            Vector2 touchGUIPos = new Vector2(touch.position.x, Screen.height - touch.position.y);

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId)) continue;

            if (touch.phase == TouchPhase.Began)
            {
                if (pauseRectScreen.Contains(touchPos)) continue; 

                if (actionRectScreen.Contains(touchPos))
                {
                    TryInteract();
                    continue;
                }
                if (!isHiding && IsCrowbarActive() && attackRectScreen.Contains(touchPos))
                {
                    TryCrowbarAttack();
                    continue;
                }
                if (hasFlashlight && flashlightRectScreen.Contains(touchPos))
                {
                    ToggleFlashlight();
                    continue;
                }

                if (!IsTouchOverAnyButton(touchPos))
                {
                    if (touchPos.x < Screen.width * 0.48f && !isHiding)
                    {
                        if (joystickFingerId == -1)
                        {
                            joystickFingerId = touch.fingerId;
                            joystickCenter = touchGUIPos;
                            joystickCurrentPos = joystickCenter;
                            showJoystick = true;
                        }
                    }
                    else
                    {
                        if (cameraFingerId == -1)
                        {
                            cameraFingerId = touch.fingerId;
                            lastCameraTouchPos = touchPos;
                        }
                    }
                }
            }
        }

        if (joystickFingerId != -1 && !isHiding)
        {
            bool joystickTouchFound = false;
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.fingerId == joystickFingerId)
                {
                    joystickTouchFound = true;
                    Vector2 touchGUIPos = new Vector2(touch.position.x, Screen.height - touch.position.y);

                    if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                    {
                        Vector2 touchDelta = touchGUIPos - joystickCenter;
                        if (touchDelta.magnitude > joystickRadius)
                            joystickCurrentPos = joystickCenter + touchDelta.normalized * joystickRadius;
                        else
                            joystickCurrentPos = touchGUIPos;

                        Vector2 moveDelta = (joystickCurrentPos - joystickCenter) / joystickRadius;
                        moveDirection = transform.forward * (-moveDelta.y) + transform.right * moveDelta.x;
                        moveDirection.y = 0;
                    }

                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        joystickFingerId = -1;
                        showJoystick = false;
                    }
                    break;
                }
            }
            if (!joystickTouchFound)
            {
                joystickFingerId = -1;
                showJoystick = false;
            }
        }

        if (cameraFingerId != -1)
        {
            bool cameraTouchFound = false;
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.fingerId == cameraFingerId)
                {
                    cameraTouchFound = true;

                    if (touch.phase == TouchPhase.Moved)
                    {
                        Vector2 deltaTouch = touch.position - lastCameraTouchPos;
                        lastCameraTouchPos = touch.position;

                        transform.Rotate(Vector3.up * deltaTouch.x * lookSpeed);
                        rotationX -= deltaTouch.y * lookSpeed;
                        rotationX = Mathf.Clamp(rotationX, -80f, 80f);
                        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
                    }
                    else if (touch.phase == TouchPhase.Stationary)
                    {
                        lastCameraTouchPos = touch.position;
                    }

                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) cameraFingerId = -1;
                    break;
                }
            }
            if (!cameraTouchFound) cameraFingerId = -1;
        }

        if (!isHiding) ApplyMovement(moveDirection);
    }

    void ApplyMovement(Vector3 moveDirection)
    {
        if (charController != null && charController.enabled)
        {
            Vector3 move = moveDirection.normalized * moveSpeed;
            move.y = verticalVelocity;
            
            charController.Move(move * Time.deltaTime);
        }
    }

    void CheckInteractionAvailability()
    {
        if (isHiding)
        {
            canInteractRightNow = true; 
            return;
        }

        if (playerCamera == null) return;

        Physics.SyncTransforms();

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        canInteractRightNow = Physics.Raycast(ray, out _, interactionDistance, Physics.AllLayers, QueryTriggerInteraction.Collide);
    }

    void TryInteract()
    {
        if (isHiding)
        {
            if (currentCloset != null) currentCloset.Interact(this.gameObject);
            return;
        }

        if (playerCamera == null) return;
        
        Physics.SyncTransforms();
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance, Physics.AllLayers, QueryTriggerInteraction.Collide))
        {
            GameObject target = hit.transform.gameObject;

            ItemInteractable genericItem = target.GetComponentInParent<ItemInteractable>() ?? target.GetComponentInChildren<ItemInteractable>();
            if (genericItem != null)
            {
                InventoryController inventory = GetComponent<InventoryController>();
                if (inventory != null)
                {
                    inventory.AddItem(genericItem.itemId);
                    
                    string idLower = genericItem.itemId.ToLower();
                    if (idLower.Contains("crowbar") || idLower.Contains("лом")) hasCrowbar = true;
                    if (idLower.Contains("flashlight") || idLower.Contains("фонарик")) hasFlashlight = true;

                    Destroy(genericItem.gameObject);
                    return;
                }
            }

            CrowbarPickup crowbar = target.GetComponentInParent<CrowbarPickup>() ?? target.GetComponentInChildren<CrowbarPickup>();
            if (crowbar != null) 
            { 
                InventoryController inventory = GetComponent<InventoryController>();
                if (inventory != null) inventory.AddItem("Crowbar");
                hasCrowbar = true;
                crowbar.Pickup(); 
                return; 
            }

            string targetNameLower = target.name.ToLower();
            bool isFlashlight = targetNameLower.Contains("flashlight") || targetNameLower.Contains("фонарик");
            if (!isFlashlight && target.transform.parent != null)
            {
                string parentNameLower = target.transform.parent.name.ToLower();
                isFlashlight = parentNameLower.Contains("flashlight") || parentNameLower.Contains("фонарик");
            }

            if (isFlashlight)
            {
                InventoryController inventory = GetComponent<InventoryController>();
                if (inventory != null) inventory.AddItem("Flashlight");
                hasFlashlight = true;
                
                GameObject toDestroy = (target.transform.parent != null && target.transform.parent.name.ToLower().Contains("flashlight")) 
                    ? target.transform.parent.gameObject 
                    : target;
                    
                Destroy(toDestroy);
                return;
            }

            DoorController door = target.GetComponentInParent<DoorController>() ?? target.GetComponentInChildren<DoorController>();
            if (door != null)
            {
                door.ExecuteDirectInteraction();
                return;
            }

            ElectricBox eBox = target.GetComponentInParent<ElectricBox>() ?? target.GetComponentInChildren<ElectricBox>();
            if (eBox != null) { eBox.ActivatePower(); return; }

            KeyItem key = target.GetComponentInParent<KeyItem>() ?? target.GetComponentInChildren<KeyItem>();
            if (key != null)
            {
                InventoryController inventory = GetComponent<InventoryController>();
                if (inventory != null) inventory.AddItem(key.itemId); 
                key.gameObject.SetActive(false);
                return;
            }

            CodePadController pad = target.GetComponentInParent<CodePadController>() ?? target.GetComponentInChildren<CodePadController>();
            if (pad != null)
            {
                if (!pad.IsCleared()) pad.InteractWithPad();
                return;
            }

            HidingSpot closet = target.GetComponentInParent<HidingSpot>() ?? target.GetComponentInChildren<HidingSpot>();
            if (closet != null)
            {
                closet.Interact(this.gameObject);
                return;
            }
        }
    }

    bool IsCrowbarActive() => hasCrowbar && crowbarController != null;

    void TryCrowbarAttack()
    {
        if (IsCrowbarActive() && !isHiding) crowbarController.TryAttack();
    }

    void ToggleFlashlight()
    {
        isFlashlightOn = !isFlashlightOn;
        if (flashlightLight != null) flashlightLight.SetActive(isFlashlightOn);
    }

    void OnGUI()
    {
        if (GameplayUIController.Instance != null && GameplayUIController.Instance.IsGamePausedOrDead()) return;

        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.active.textColor = Color.red;
            
            buttonStyle.border = new RectOffset(0, 0, 0, 0);
            buttonStyle.margin = new RectOffset(0, 0, 0, 0);
        }

        if (joystickStyle == null) joystickStyle = new GUIStyle(GUI.skin.label);

        if (showJoystick && !isHiding)
        {
            joystickStyle.normal.background = joystickBgTex;
            Rect baseRect = new Rect(joystickCenter.x - joystickRadius, joystickCenter.y - joystickRadius, joystickRadius * 2, joystickRadius * 2);
            GUI.Box(baseRect, "", joystickStyle);

            joystickStyle.normal.background = joystickStickTex;
            Rect stickRect = new Rect(joystickCurrentPos.x - stickRadius, joystickCurrentPos.y - stickRadius, stickRadius * 2, stickRadius * 2);
            GUI.Box(stickRect, "", joystickStyle);
        }

        buttonStyle.normal.background = (canInteractRightNow || isHiding) ? buttonActiveTex : buttonNormalTex;
        buttonStyle.active.background = buttonActiveTex;

        string actionText = "HAND";
        if (isHiding) actionText = "EXIT";
        else if (canInteractRightNow) actionText = "TAKE";

        GUI.Button(actionRectGUI, actionText, buttonStyle);

        if (IsCrowbarActive() && !isHiding)
        {
            buttonStyle.normal.background = buttonAccentTex;
            GUI.Button(attackRectGUI, "HIT", buttonStyle);
        }

        if (hasFlashlight)
        {
            buttonStyle.normal.background = isFlashlightOn ? buttonAccentTex : buttonNormalTex;
            GUI.Button(flashlightRectGUI, isFlashlightOn ? "FLASH" : "LIGHT", buttonStyle);
        }

        if (crosshairTexture != null && !isHiding)
        {
            float dotSize = 6f; 
            Rect dotRect = new Rect((Screen.width - dotSize) / 2f, (Screen.height - dotSize) / 2f, dotSize, dotSize);
            GUIStyle crosshairStyle = new GUIStyle { normal = { background = crosshairTexture } };
            GUI.Box(dotRect, "", crosshairStyle);
        }
    }

    void SimulateMouse(ref Vector3 moveDirection)
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            if (Input.GetMouseButtonUp(0)) showJoystick = false;
            return;
        }

        Vector2 mousePos = Input.mousePosition;
        Vector2 mouseGUIPos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

        if (Input.GetMouseButtonDown(0))
        {
            if (pauseRectScreen.Contains(mousePos)) return; 

            if (actionRectScreen.Contains(mousePos)) { TryInteract(); return; }
            if (!isHiding && IsCrowbarActive() && attackRectScreen.Contains(mousePos)) { TryCrowbarAttack(); return; }
            if (hasFlashlight && flashlightRectScreen.Contains(mousePos)) { ToggleFlashlight(); return; }

            if (Input.mousePosition.x < Screen.width * 0.45f && !isHiding)
            {
                joystickCenter = mouseGUIPos;
                joystickCurrentPos = mouseGUIPos;
                showJoystick = true;
            }
            else lastCameraTouchPos = Input.mousePosition;
        }

        if (Input.GetMouseButton(0))
        {
            if (showJoystick && !isHiding)
            {
                joystickCurrentPos = mouseGUIPos;
                Vector2 touchDelta = joystickCurrentPos - joystickCenter;
                if (touchDelta.magnitude > joystickRadius)
                {
                    joystickCurrentPos = joystickCenter + touchDelta.normalized * joystickRadius;
                }
                Vector2 moveDelta = (joystickCurrentPos - joystickCenter) / joystickRadius;
                moveDirection = transform.forward * (-moveDelta.y) + transform.right * moveDelta.x;
                moveDirection.y = 0;
            }
            else if (!actionRectScreen.Contains(mousePos) && !flashlightRectScreen.Contains(mousePos) && (!IsCrowbarActive() || !attackRectScreen.Contains(mousePos)))
            {
                Vector2 deltaMouse = (Vector2)Input.mousePosition - lastCameraTouchPos;
                lastCameraTouchPos = Input.mousePosition;

                transform.Rotate(Vector3.up * deltaMouse.x * lookSpeed);
                rotationX -= deltaMouse.y * lookSpeed;
                rotationX = Mathf.Clamp(rotationX, -85f, 85f);
                playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            }
        }
        if (Input.GetMouseButtonUp(0)) showJoystick = false;
    }
}