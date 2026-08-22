using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayUIController : MonoBehaviour
{
    public static GameplayUIController Instance { get; private set; }

    private enum UIState { ActiveGameplay, PauseMenu, OptionsMenu, DeathMenu, VictoryScreen, CodePadMenu }
    private UIState currentState = UIState.ActiveGameplay;

    [Header("Настройки")]
    public float sensitivity = 0.14f;
    private int graphicsQuality = 2;

    [Header("Настройки задержки смерти")]
    public float deathScreenDelay = 3.0f;

    private Texture2D bgTex;          
    private Texture2D buttonTex;      
    private Texture2D buttonActiveTex;
    private Texture2D darkOverlayTex; 
    private Texture2D solidBlackTex;   

    // Текстуры для цветных кнопок в OnGUI
    private Texture2D greenButtonTex;
    private Texture2D redButtonTex;

    private GUIStyle buttonStyle;
    private GUIStyle acceptButtonStyle; // Стиль для зеленой кнопки
    private GUIStyle cancelButtonStyle; // Стиль для красной кнопки
    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private GUIStyle victorySubtitleStyle; 
    private GUIStyle codeDisplayStyle; 

    private bool isDead = false; 
    private float deathTimer = 0f;
    private bool startDeathTimer = false;

    private float gameplayTime = 0f;
    private bool isGameFinished = false;
    private string formattedFinalTime = "00:00";

    private CodePadController activeCodePad;

    public readonly Rect pauseButtonRectGUI = new Rect(1920 - 160, 40, 120, 120);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Time.timeScale = 1f; 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        sensitivity = PlayerPrefs.GetFloat("CameraSensitivity", 0.14f);
        graphicsQuality = PlayerPrefs.GetInt("GraphicsQuality", QualitySettings.GetQualityLevel());
        
        ApplyGraphicsSettings(graphicsQuality);

        bgTex = CreateBackgroundTexture();
        buttonTex = CreateColorTexture(new Color(0.12f, 0.12f, 0.12f, 0.95f)); 
        buttonActiveTex = CreateColorTexture(new Color(0.5f, 0.05f, 0.05f, 1f)); 
        darkOverlayTex = CreateColorTexture(new Color(0f, 0f, 0f, 0.85f));
        solidBlackTex = CreateColorTexture(Color.black); 

        // Инициализируем текстуры для цветных кнопок терминала
        greenButtonTex = CreateColorTexture(new Color(0.05f, 0.4f, 0.05f, 0.95f));
        redButtonTex = CreateColorTexture(new Color(0.5f, 0.05f, 0.05f, 0.95f));
    }

    private void Update()
    {
        if (currentState == UIState.ActiveGameplay && !startDeathTimer && !isGameFinished)
        {
            gameplayTime += Time.deltaTime;
        }

        if (startDeathTimer && !isDead)
        {
            deathTimer += Time.unscaledDeltaTime;
            if (deathTimer >= deathScreenDelay)
            {
                isDead = true;
                currentState = UIState.DeathMenu;
                Time.timeScale = 0f; 
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape) && !startDeathTimer && !isDead && !isGameFinished)
        {
            if (currentState == UIState.ActiveGameplay) SetPause(true);
            else if (currentState == UIState.PauseMenu) SetPause(false);
            else if (currentState == UIState.OptionsMenu) currentState = UIState.PauseMenu;
            else if (currentState == UIState.CodePadMenu && activeCodePad != null) activeCodePad.ExitPad(); 
        }
    }

    public bool IsGamePausedOrDead()
    {
        return currentState != UIState.ActiveGameplay || isDead || startDeathTimer || isGameFinished;
    }

    public void TogglePause()
    {
        if (isDead || startDeathTimer || isGameFinished) return;

        if (currentState == UIState.ActiveGameplay) SetPause(true);
        else if (currentState == UIState.PauseMenu) SetPause(false);
    }

    private void SetPause(bool pause)
    {
        if (pause)
        {
            currentState = UIState.PauseMenu;
            Time.timeScale = 0f; 
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            currentState = UIState.ActiveGameplay;
            Time.timeScale = 1f; 
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void OpenCodePad(CodePadController pad)
    {
        if (isDead || startDeathTimer || isGameFinished) return;

        activeCodePad = pad;
        currentState = UIState.CodePadMenu;
        Time.timeScale = 0f; 
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseCodePad()
    {
        activeCodePad = null;
        currentState = UIState.ActiveGameplay;
        Time.timeScale = 1f; 
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void TriggerGameOver()
    {
        if (isDead || startDeathTimer || isGameFinished) return;
        startDeathTimer = true;
        deathTimer = 0f;
    }

    public void TriggerVictory()
    {
        if (isDead || startDeathTimer || isGameFinished) return;

        isGameFinished = true;
        currentState = UIState.VictoryScreen;
        Time.timeScale = 0f; 

        int minutes = Mathf.FloorToInt(gameplayTime / 60f);
        int seconds = Mathf.FloorToInt(gameplayTime % 60f);
        formattedFinalTime = string.Format("{0:00}:{1:00}", minutes, seconds);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ApplyGraphicsSettings(int level)
    {
        QualitySettings.SetQualityLevel(level, true);
        PlayerPrefs.SetInt("GraphicsQuality", level);
        PlayerPrefs.Save();
    }

    private void OnGUI()
    {
        InitializeStyles();

        float scaleX = Screen.width / 1920f;
        float scaleY = Screen.height / 1080f;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scaleX, scaleY, 1));

        if (currentState == UIState.ActiveGameplay && !startDeathTimer && !isDead)
        {
            if (GUI.Button(pauseButtonRectGUI, "||", buttonStyle))
            {
                TogglePause();
            }
            return; 
        }

        if (startDeathTimer && !isDead)
        {
            if (darkOverlayTex != null)
            {
                float alpha = Mathf.Clamp01(deathTimer / deathScreenDelay);
                GUI.color = new Color(1, 1, 1, alpha);
                GUI.DrawTexture(new Rect(0, 0, 1920, 1080), darkOverlayTex);
                GUI.color = Color.white;
            }
            return;
        }

        if (currentState == UIState.VictoryScreen)
        {
            if (solidBlackTex != null) GUI.DrawTexture(new Rect(0, 0, 1920, 1080), solidBlackTex);
        }
        else
        {
            if (bgTex != null) GUI.DrawTexture(new Rect(0, 0, 1920, 1080), bgTex);
        }

        switch (currentState)
        {
            case UIState.PauseMenu:
                GUI.Label(new Rect(100, 180, 1720, 120), "PAUSE", titleStyle);
                if (GUI.Button(new Rect(1920 / 2 - 225, 380, 450, 90), "RESUME", buttonStyle)) SetPause(false);
                if (GUI.Button(new Rect(1920 / 2 - 225, 510, 450, 90), "OPTIONS", buttonStyle)) currentState = UIState.OptionsMenu;
                if (GUI.Button(new Rect(1920 / 2 - 225, 640, 450, 90), "MAIN MENU", buttonStyle)) GoToMainMenu();
                break;

            case UIState.OptionsMenu:
                GUI.Label(new Rect(100, 180, 1720, 100), "OPTIONS", titleStyle);
                GUI.Label(new Rect(1920 / 2 - 250, 340, 500, 40), $"SENSITIVITY: {sensitivity:F2}", labelStyle);
                float prevSens = sensitivity;
                sensitivity = GUI.HorizontalSlider(new Rect(1920 / 2 - 250, 400, 500, 30), sensitivity, 0.05f, 0.5f);
                if (prevSens != sensitivity)
                {
                    PlayerPrefs.SetFloat("CameraSensitivity", sensitivity);
                    PlayerPrefs.Save();
                }

                GUI.Label(new Rect(1920 / 2 - 250, 490, 500, 40), "GRAPHICS QUALITY", labelStyle);
                string[] qualities = QualitySettings.names;
                if (qualities.Length > 0)
                {
                    GUILayout.BeginArea(new Rect(1920 / 2 - 250, 540, 500, 100));
                    GUILayout.BeginHorizontal();
                    for (int i = 0; i < qualities.Length; i++)
                    {
                        GUI.backgroundColor = (graphicsQuality == i) ? Color.red : Color.gray;
                        if (GUILayout.Button(qualities[i], GUILayout.Height(55)))
                        {
                            graphicsQuality = i;
                            ApplyGraphicsSettings(i);
                        }
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.EndArea();
                    GUI.backgroundColor = Color.white; 
                }
                if (GUI.Button(new Rect(1920 / 2 - 150, 750, 300, 80), "BACK", buttonStyle)) currentState = UIState.PauseMenu;
                break;

            case UIState.DeathMenu:
                GUI.Label(new Rect(100, 180, 1720, 150), "YOU DIED", titleStyle);
                if (GUI.Button(new Rect(1920 / 2 - 225, 430, 450, 90), "RETRY", buttonStyle)) RestartGame();
                if (GUI.Button(new Rect(1920 / 2 - 225, 590, 450, 90), "MAIN MENU", buttonStyle)) GoToMainMenu();
                if (GUI.Button(new Rect(1920 / 2 - 225, 750, 450, 90), "QUIT", buttonStyle)) ExitGame();
                break;

            case UIState.VictoryScreen:
                GUI.Label(new Rect(100, 250, 1720, 80), $"YOUR TIME: {formattedFinalTime}", labelStyle);
                GUI.Label(new Rect(100, 380, 1720, 160), "TO BE CONTINUED", victorySubtitleStyle);
                if (GUI.Button(new Rect(1920 / 2 - 225, 680, 450, 90), "MAIN MENU", buttonStyle)) GoToMainMenu();
                if (GUI.Button(new Rect(1920 / 2 - 225, 810, 450, 90), "QUIT GAME", buttonStyle)) ExitGame();
                break;

            case UIState.CodePadMenu:
                if (activeCodePad != null)
                {
                    float winWidth = 440f;
                    float winHeight = 620f;
                    float winX = (1920f / 2f) - (winWidth / 2f);
                    float winY = (1080f / 2f) - (winHeight / 2f);

                    GUI.Box(new Rect(winX, winY, winWidth, winHeight), "", buttonStyle);

                    string displayStr = activeCodePad.GetCurrentInput().PadRight(activeCodePad.codeLength, '_');
                    GUI.Box(new Rect(winX + 40, winY + 40, winWidth - 80, 70), displayStr, codeDisplayStyle);

                    float startBtnX = winX + 45;
                    float startBtnY = winY + 140;
                    float btnSize = 100f;
                    float spacing = 25f;

                    // Кнопки 1-9
                    for (int i = 0; i < 9; i++)
                    {
                        int num = i + 1;
                        float row = i / 3;
                        float col = i % 3;
                        Rect btnRect = new Rect(startBtnX + col * (btnSize + spacing), startBtnY + row * (btnSize + spacing), btnSize, btnSize);

                        if (GUI.Button(btnRect, num.ToString(), buttonStyle))
                        {
                            activeCodePad.SendNumber(num.ToString());
                        }
                    }

                    float bottomRowY = startBtnY + 3 * (btnSize + spacing);

                    // ЗЕЛЕНАЯ КНОПКА: Принять (V) - Левая снизу
                    if (GUI.Button(new Rect(startBtnX, bottomRowY, btnSize, btnSize), "V", acceptButtonStyle))
                    {
                        activeCodePad.CheckCode();
                    }
                    
                    // Кнопка Ноль (0) - По центру
                    if (GUI.Button(new Rect(startBtnX + (btnSize + spacing), bottomRowY, btnSize, btnSize), "0", buttonStyle))
                    {
                        activeCodePad.SendNumber("0");
                    }
                    
                    // КРАСНАЯ КНОПКА: Отмена / Выход (X) - Правая снизу
                    if (GUI.Button(new Rect(startBtnX + 2 * (btnSize + spacing), bottomRowY, btnSize, btnSize), "X", cancelButtonStyle))
                    {
                        activeCodePad.ExitPad();
                    }
                }
                break;
        }
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0); 
    }

    private void ExitGame()
    {
        Time.timeScale = 1f;
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    private Texture2D CreateColorTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private Texture2D CreateBackgroundTexture()
    {
        int width = 256;
        int height = 256;
        Texture2D texture = new Texture2D(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float distFromCenter = Vector2.Distance(new Vector2(x, y), new Vector2(width / 2, height / 2)) / (width / 1.4f);
                distFromCenter = Mathf.Clamp01(distFromCenter);
                Color color = Color.Lerp(new Color(0.04f, 0.04f, 0.05f, 0.95f), new Color(0.18f, 0.02f, 0.02f, 0.98f), distFromCenter);
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
        return texture;
    }

    private void InitializeStyles()
    {
        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 28;
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.hover.textColor = Color.red;
            buttonStyle.active.textColor = Color.red;
            buttonStyle.normal.background = buttonTex;
            buttonStyle.hover.background = buttonActiveTex;
            buttonStyle.active.background = buttonActiveTex;
        }

        // Стиль для зеленой кнопки (Принять)
        if (acceptButtonStyle == null)
        {
            acceptButtonStyle = new GUIStyle(buttonStyle);
            acceptButtonStyle.normal.background = greenButtonTex;
            acceptButtonStyle.hover.background = CreateColorTexture(new Color(0.05f, 0.6f, 0.05f, 1f));
            acceptButtonStyle.active.background = CreateColorTexture(new Color(0.05f, 0.6f, 0.05f, 1f));
            acceptButtonStyle.hover.textColor = Color.white;
            acceptButtonStyle.active.textColor = Color.white;
        }

        // Стиль для красной кнопки (Отмена)
        if (cancelButtonStyle == null)
        {
            cancelButtonStyle = new GUIStyle(buttonStyle);
            cancelButtonStyle.normal.background = redButtonTex;
            cancelButtonStyle.hover.background = CreateColorTexture(new Color(0.7f, 0.05f, 0.05f, 1f));
            cancelButtonStyle.active.background = CreateColorTexture(new Color(0.7f, 0.05f, 0.05f, 1f));
            cancelButtonStyle.hover.textColor = Color.white;
            cancelButtonStyle.active.textColor = Color.white;
        }

        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 65;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.normal.textColor = new Color(0.85f, 0.05f, 0.05f, 1f); 
        }

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 32; 
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = Color.white;
        }

        if (victorySubtitleStyle == null)
        {
            victorySubtitleStyle = new GUIStyle(GUI.skin.label);
            victorySubtitleStyle.fontSize = 75; 
            victorySubtitleStyle.fontStyle = FontStyle.Bold;
            victorySubtitleStyle.alignment = TextAnchor.MiddleCenter;
            victorySubtitleStyle.normal.textColor = new Color(0.7f, 0.0f, 0.0f, 1f); 
        }

        if (codeDisplayStyle == null)
        {
            codeDisplayStyle = new GUIStyle(GUI.skin.box);
            codeDisplayStyle.fontSize = 36;
            codeDisplayStyle.fontStyle = FontStyle.Bold;
            codeDisplayStyle.alignment = TextAnchor.MiddleCenter;
            codeDisplayStyle.normal.textColor = Color.red; 
            codeDisplayStyle.normal.background = CreateColorTexture(new Color(0.05f, 0.05f, 0.05f, 0.95f));
        }
    }
}