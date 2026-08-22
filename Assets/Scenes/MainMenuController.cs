using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    private enum MenuState { Splash, MainMenu, Episodes, Options, Loading }
    private MenuState currentState = MenuState.Splash;

    private float sensitivity = 0.14f;
    private int graphicsQuality = 2;

    private bool isReadyToActivate = false;
    private AsyncOperation loadingOperation;
    private float activationDelayTimer = 0f;
    private float fakeLoadingProgress = 0f;
    private float splashAlpha = 0f;
    private float pulseTimer = 0f;

    // Стили и Текстуры для OnGUI
    private GUIStyle panelStyle;
    private GUIStyle buttonStyle;
    private GUIStyle arrowButtonStyle; 
    private GUIStyle qButtonStyle;     
    private GUIStyle titleStyle;
    private GUIStyle titleGlowStyle;
    private GUIStyle labelStyle;
    private GUIStyle splashStyle;
    private GUIStyle centerWindowStyle;
    private GUIStyle episodeBlockStyle;
    private GUIStyle lockedBlockStyle;

    private Texture2D panelTex;       
    private Texture2D centerWindowBg; 
    private Texture2D buttonTex;      
    private Texture2D buttonHoverTex;
    private Texture2D blackTex;       
    private Texture2D sliderBgTex;    
    private Texture2D episodeBlockTex;
    private Texture2D lockedBlockTex; 
    private Texture2D vignetteTex;
    private Texture2D accentLineTex;
    private Texture2D progressBarTex;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        currentState = MenuState.Splash;
    }

    private void Start()
    {
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        sensitivity = PlayerPrefs.GetFloat("CameraSensitivity", 0.14f);
        graphicsQuality = PlayerPrefs.GetInt("GraphicsQuality", QualitySettings.GetQualityLevel());
        QualitySettings.SetQualityLevel(graphicsQuality, true);

        // Генерация стилизованных текстур
        blackTex = CreateColorTexture(Color.black);
        panelTex = CreateGradientTexture(new Color(0.02f, 0.02f, 0.03f, 0.95f), new Color(0.005f, 0.005f, 0.008f, 0.85f)); 
        
        centerWindowBg = CreateBorderedTexture(new Color(0.03f, 0.03f, 0.04f, 0.96f), new Color(0.35f, 0.03f, 0.03f, 0.9f), 2);
        episodeBlockTex = CreateBorderedTexture(new Color(0.05f, 0.05f, 0.06f, 0.95f), new Color(0.6f, 0.05f, 0.05f, 0.7f), 2);
        lockedBlockTex = CreateBorderedTexture(new Color(0.02f, 0.02f, 0.02f, 0.8f), new Color(0.12f, 0.12f, 0.12f, 0.5f), 1);
        
        sliderBgTex = CreateColorTexture(new Color(0.1f, 0.1f, 0.12f, 1f));
        buttonTex = CreateBorderedTexture(new Color(0.06f, 0.06f, 0.07f, 0.9f), new Color(0.4f, 0.05f, 0.05f, 0.6f), 1);
        buttonHoverTex = CreateBorderedTexture(new Color(0.22f, 0.03f, 0.03f, 0.98f), new Color(1f, 0.1f, 0.1f, 1f), 2);

        vignetteTex = CreateVignetteTexture();
        accentLineTex = CreateColorTexture(new Color(0.85f, 0.05f, 0.05f, 0.8f));
        progressBarTex = CreateColorTexture(new Color(0.9f, 0.1f, 0.1f, 1f));

        StartCoroutine(ShowSplashSequence());
    }

    private void Update()
    {
        pulseTimer += Time.deltaTime * 3f;

        if (currentState != MenuState.Loading && currentState != MenuState.Splash)
        {
            if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        if (currentState == MenuState.Loading && isReadyToActivate)
        {
            activationDelayTimer += Time.deltaTime;
            if (activationDelayTimer >= 0.3f)
            {
                if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                {
                    ActivateLoadedScene();
                }
            }
        }
    }

    private void ActivateLoadedScene()
    {
        isReadyToActivate = false;
        if (loadingOperation != null)
        {
            loadingOperation.allowSceneActivation = true;
        }
    }

    private IEnumerator ShowSplashSequence()
    {
        yield return new WaitForSeconds(0.4f); 
        
        float counter = 0f;
        while (counter < 1f)
        {
            counter += Time.deltaTime * 1.0f;
            splashAlpha = Mathf.Clamp01(counter);
            yield return null;
        }

        yield return new WaitForSeconds(1.8f); 

        counter = 1f;
        while (counter > 0f)
        {
            counter -= Time.deltaTime * 1.3f;
            splashAlpha = Mathf.Clamp01(counter);
            yield return null;
        }

        currentState = MenuState.MainMenu;
    }

    #region Texture Generators
    private Texture2D CreateColorTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private Texture2D CreateGradientTexture(Color topColor, Color bottomColor)
    {
        int size = 64;
        Texture2D texture = new Texture2D(1, size);
        for (int y = 0; y < size; y++)
        {
            Color c = Color.Lerp(bottomColor, topColor, (float)y / size);
            texture.SetPixel(0, y, c);
        }
        texture.Apply();
        return texture;
    }

    private Texture2D CreateBorderedTexture(Color bodyColor, Color borderColor, int borderWidth)
    {
        int size = 128;
        Texture2D texture = new Texture2D(size, size);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (x < borderWidth || x >= size - borderWidth || y < borderWidth || y >= size - borderWidth)
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

    private Texture2D CreateVignetteTexture()
    {
        int size = 256;
        Texture2D texture = new Texture2D(size, size);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 1.4f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(dist / maxDist);
                alpha = Mathf.Pow(alpha, 2.5f); 
                texture.SetPixel(x, y, new Color(0, 0, 0, alpha * 0.9f));
            }
        }
        texture.Apply();
        return texture;
    }
    #endregion

    private void InitializeStyles()
    {
        if (panelStyle == null)
        {
            panelStyle = new GUIStyle();
            panelStyle.normal.background = panelTex;
        }

        if (centerWindowStyle == null)
        {
            centerWindowStyle = new GUIStyle();
            centerWindowStyle.normal.background = centerWindowBg;
        }

        if (episodeBlockStyle == null)
        {
            episodeBlockStyle = new GUIStyle();
            episodeBlockStyle.normal.background = episodeBlockTex;
        }

        if (lockedBlockStyle == null)
        {
            lockedBlockStyle = new GUIStyle();
            lockedBlockStyle.normal.background = lockedBlockTex;
        }

        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 24;
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            buttonStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
            buttonStyle.hover.textColor = Color.white;
            buttonStyle.active.textColor = Color.red;
            buttonStyle.normal.background = buttonTex;
            buttonStyle.hover.background = buttonHoverTex;
            buttonStyle.active.background = buttonHoverTex;
        }

        if (arrowButtonStyle == null)
        {
            arrowButtonStyle = new GUIStyle(buttonStyle);
            arrowButtonStyle.fontSize = 26;
        }

        if (qButtonStyle == null)
        {
            qButtonStyle = new GUIStyle(GUI.skin.box);
            qButtonStyle.fontSize = 20;
            qButtonStyle.fontStyle = FontStyle.Bold;
            qButtonStyle.alignment = TextAnchor.MiddleCenter;
            qButtonStyle.normal.textColor = new Color(0.85f, 0.15f, 0.15f, 1f); 
            qButtonStyle.normal.background = buttonTex;
        }

        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 54; 
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleLeft;
            titleStyle.normal.textColor = new Color(0.95f, 0.08f, 0.08f, 1f); 
        }

        if (titleGlowStyle == null)
        {
            titleGlowStyle = new GUIStyle(titleStyle);
            titleGlowStyle.normal.textColor = new Color(0.6f, 0.0f, 0.0f, 0.35f);
        }

        if (splashStyle == null)
        {
            splashStyle = new GUIStyle(GUI.skin.label);
            splashStyle.fontSize = 80;
            splashStyle.fontStyle = FontStyle.Bold;
            splashStyle.alignment = TextAnchor.MiddleCenter;
            splashStyle.normal.textColor = Color.white; 
        }

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 22;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        }

        GUI.skin.horizontalSlider.normal.background = sliderBgTex;
        GUI.skin.horizontalSliderThumb.normal.background = buttonHoverTex;
        GUI.skin.horizontalSliderThumb.hover.background = buttonHoverTex;
    }

    private void OnGUI()
    {
        InitializeStyles();

        // Масштабирование интерфейса под 1080p
        float rx = Screen.width / 1920f;
        float ry = Screen.height / 1080f;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(rx, ry, 1));

        // Виньетка поверх всего для хоррор-атмосферы
        if (vignetteTex != null)
        {
            GUI.DrawTexture(new Rect(0, 0, 1920, 1080), vignetteTex);
        }

        // SPLASH SCREEN
        if (currentState == MenuState.Splash)
        {
            if (blackTex != null) GUI.DrawTexture(new Rect(0, 0, 1920, 1080), blackTex);
            
            GUI.color = new Color(1, 1, 1, splashAlpha);
            GUI.Label(new Rect(4, 4, 1920, 1080), "VOIDBYTE", new GUIStyle(splashStyle) { normal = { textColor = new Color(0.8f, 0f, 0f, 0.5f) } });
            GUI.Label(new Rect(0, 0, 1920, 1080), "VOIDBYTE", splashStyle);
            
            // Декоративный подзаголовок
            GUIStyle subSplash = new GUIStyle(labelStyle) { fontSize = 16, fontStyle = FontStyle.Normal };
            subSplash.normal.textColor = new Color(0.5f, 0.5f, 0.5f, splashAlpha * 0.7f);
            GUI.Label(new Rect(0, 120, 1920, 1080), "SYSTEM INITIALIZATION...", subSplash);
            
            GUI.color = Color.white;
            return;
        }

        switch (currentState)
        {
            case MenuState.MainMenu:
                // Боковая панель
                if (panelTex != null) GUI.Box(new Rect(0, 0, 580, 1080), "", panelStyle);
                
                // Декоративная красная полоса слева
                if (accentLineTex != null) GUI.DrawTexture(new Rect(578, 0, 2, 1080), accentLineTex);

                // Заголовок игры с подложкой и свечением
                float glowOffset = Mathf.Sin(pulseTimer) * 2f;
                GUI.Label(new Rect(85 + glowOffset, 140 + glowOffset, 480, 160), "UNCANNY\nREALITY", titleGlowStyle);
                GUI.Label(new Rect(80, 140, 480, 160), "UNCANNY\nREALITY", titleStyle);

                // Декоративная полоса под заголовком
                if (accentLineTex != null) GUI.DrawTexture(new Rect(80, 310, 320, 3), accentLineTex);

                // Кнопки Главного Меню
                if (GUI.Button(new Rect(80, 420, 400, 75), "PLAY", buttonStyle))
                {
                    currentState = MenuState.Episodes; 
                }

                if (GUI.Button(new Rect(80, 520, 400, 75), "OPTIONS", buttonStyle))
                {
                    currentState = MenuState.Options; 
                }

                if (GUI.Button(new Rect(80, 620, 400, 75), "EXIT", buttonStyle))
                {
                    Application.Quit();
                }

                // Версия в углу
                GUIStyle verStyle = new GUIStyle(labelStyle) { fontSize = 14, alignment = TextAnchor.MiddleLeft };
                verStyle.normal.textColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
                GUI.Label(new Rect(80, 1000, 300, 30), "BUILD v1.0.4 - VOIDBYTE STUDIO", verStyle);
                break;

            case MenuState.Episodes:
                float epWinW = 1520f;
                float epWinH = 860f;
                float epWinX = (1920f - epWinW) / 2f;
                float epWinY = (1080f - epWinH) / 2f;

                if (centerWindowBg != null) GUI.Box(new Rect(epWinX, epWinY, epWinW, epWinH), "", centerWindowStyle);

                GUIStyle headStyle = new GUIStyle(titleStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 42 };
                GUI.Label(new Rect(epWinX, epWinY + 40, epWinW, 70), "SELECT EPISODE", headStyle);

                float blockW = 420f;
                float blockH = 520f;
                float blockY = epWinY + 150f;
                float startX = epWinX + 80f;
                float gap = 50f;

                // --- БЛОК 1: REMINISCENCE ---
                Rect r1 = new Rect(startX, blockY, blockW, blockH);
                GUI.Box(r1, "", episodeBlockStyle);
                
                // Акцентная линия сверху блока
                if (accentLineTex != null) GUI.DrawTexture(new Rect(r1.x, r1.y, blockW, 4), accentLineTex);

                GUI.Label(new Rect(r1.x, r1.y + 30, blockW, 40), "EPISODE I", labelStyle);
                
                GUIStyle epActiveTitle = new GUIStyle(labelStyle) { fontSize = 26, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.95f, 0.1f, 0.1f) } };
                GUI.Label(new Rect(r1.x, r1.y + 80, blockW, 50), "REMINISCENCE", epActiveTitle);
                
                GUIStyle descriptionStyle = new GUIStyle(labelStyle) { fontSize = 16, wordWrap = true, fontStyle = FontStyle.Normal, alignment = TextAnchor.UpperCenter };
                descriptionStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f, 0.9f);
                GUI.Label(new Rect(r1.x + 25, r1.y + 160, blockW - 50, 180), "Dive into the dark rooms of your forgotten past. What stalks you in the silence of your memories?", descriptionStyle);

                if (GUI.Button(new Rect(r1.x + 40, r1.y + 410, blockW - 80, 65), "START", buttonStyle))
                {
                    StartGameProcess();
                }

                // --- БЛОК 2: SOON ---
                Rect r2 = new Rect(startX + blockW + gap, blockY, blockW, blockH);
                GUI.Box(r2, "", lockedBlockStyle);
                
                GUI.color = new Color(1, 1, 1, 0.35f);
                GUI.Label(new Rect(r2.x, r2.y + 30, blockW, 40), "EPISODE II", labelStyle);
                GUI.Label(new Rect(r2.x, r2.y + 80, blockW, 50), "UNKNOWN SYMPTOMS", labelStyle);
                GUI.color = Color.white;

                GUIStyle soonStyle = new GUIStyle(qButtonStyle) { fontSize = 18, normal = { textColor = new Color(0.5f, 0.5f, 0.5f, 0.8f) } };
                GUI.Box(new Rect(r2.x + 40, r2.y + 410, blockW - 80, 65), "LOCKED", soonStyle);

                // --- БЛОК 3: SOON ---
                Rect r3 = new Rect(startX + (blockW + gap) * 2, blockY, blockW, blockH);
                GUI.Box(r3, "", lockedBlockStyle);
                
                GUI.color = new Color(1, 1, 1, 0.35f);
                GUI.Label(new Rect(r3.x, r3.y + 30, blockW, 40), "EPISODE III", labelStyle);
                GUI.Label(new Rect(r3.x, r3.y + 80, blockW, 50), "THE VOID BYTE", labelStyle);
                GUI.color = Color.white;

                GUI.Box(new Rect(r3.x + 40, r3.y + 410, blockW - 80, 65), "LOCKED", soonStyle);

                if (GUI.Button(new Rect(epWinX + (epWinW - 280f) / 2f, epWinY + epWinH - 100f, 280, 65), "BACK", buttonStyle))
                {
                    currentState = MenuState.MainMenu; 
                }
                break;

            case MenuState.Options:
                float oWidth = 720f;
                float oHeight = 820f;
                float oX = (1920f - oWidth) / 2f;
                float oY = (1080f - oHeight) / 2f;

                if (centerWindowBg != null) GUI.Box(new Rect(oX, oY, oWidth, oHeight), "", centerWindowStyle);

                GUIStyle optHeadStyle = new GUIStyle(titleStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 42 };
                GUI.Label(new Rect(oX, oY + 45, oWidth, 70), "SETTINGS", optHeadStyle);

                // Блок: Чувствительность
                GUI.Label(new Rect(oX + 80, oY + 170, oWidth - 160, 35), $"CAMERA SENSITIVITY: {sensitivity:F2}", labelStyle);
                float prevSens = sensitivity;
                sensitivity = GUI.HorizontalSlider(new Rect(oX + 100, oY + 225, oWidth - 200, 25), sensitivity, 0.05f, 0.5f);
                if (prevSens != sensitivity)
                {
                    PlayerPrefs.SetFloat("CameraSensitivity", sensitivity);
                    PlayerPrefs.Save();
                }

                // Блок: Качество графики
                GUI.Label(new Rect(oX + 80, oY + 310, oWidth - 160, 35), "GRAPHICS QUALITY", labelStyle);
                string[] qualities = QualitySettings.names;
                if (qualities.Length > 0)
                {
                    if (GUI.Button(new Rect(oX + 100, oY + 360, 60, 55), "<", arrowButtonStyle))
                    {
                        graphicsQuality--;
                        if (graphicsQuality < 0) graphicsQuality = qualities.Length - 1;
                        
                        QualitySettings.SetQualityLevel(graphicsQuality, true);
                        PlayerPrefs.SetInt("GraphicsQuality", graphicsQuality);
                        PlayerPrefs.Save();
                    }

                    string currentQualityName = qualities[graphicsQuality].ToUpper();
                    GUI.Box(new Rect(oX + 175, oY + 360, oWidth - 350, 55), currentQualityName, qButtonStyle);

                    if (GUI.Button(new Rect(oX + oWidth - 160, oY + 360, 60, 55), ">", arrowButtonStyle))
                    {
                        graphicsQuality++;
                        if (graphicsQuality >= qualities.Length) graphicsQuality = 0;

                        QualitySettings.SetQualityLevel(graphicsQuality, true);
                        PlayerPrefs.SetInt("GraphicsQuality", graphicsQuality);
                        PlayerPrefs.Save();
                    }
                }

                // Кнопка Privacy Policy
                if (GUI.Button(new Rect(oX + 140, oY + 500, oWidth - 280, 65), "PRIVACY POLICY", buttonStyle))
                {
                    Application.OpenURL("https://telegra.ph/POLICE-PRIVACY-07-17");
                }

                // Кнопка Назад
                if (GUI.Button(new Rect(oX + 140, oY + 630, oWidth - 280, 70), "BACK", buttonStyle))
                {
                    currentState = MenuState.MainMenu; 
                }
                break;

            case MenuState.Loading:
                if (blackTex != null) GUI.DrawTexture(new Rect(0, 0, 1920, 1080), blackTex);

                GUIStyle loadStyle = new GUIStyle(titleStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 48 };
                GUI.Label(new Rect(0, 380, 1920, 80), "ENTERING THE VOID...", loadStyle);

                float pBarW = 650f;
                float pBarH = 10f;
                float pBarX = (1920f - pBarW) / 2f;
                float pBarY = 530f;

                // Фон шкалы загрузки
                if (buttonTex != null)
                {
                    GUI.DrawTexture(new Rect(pBarX - 2, pBarY - 2, pBarW + 4, pBarH + 4), buttonTex);
                }

                // Заполнение
                float progressWidth = pBarW * fakeLoadingProgress;
                if (progressBarTex != null && progressWidth > 0)
                {
                    GUI.DrawTexture(new Rect(pBarX, pBarY, progressWidth, pBarH), progressBarTex);
                }

                // Пульсирующий текст активации
                float alphaPulse = isReadyToActivate ? (Mathf.Sin(pulseTimer * 2f) * 0.4f + 0.6f) : 1f;
                string statusText = isReadyToActivate ? "[ PRESS ANYWHERE TO AWAKE ]" : $"LOADING REALITY... {Mathf.RoundToInt(fakeLoadingProgress * 100f)}%";
                
                GUIStyle statusStyle = new GUIStyle(labelStyle) { fontSize = 22 };
                statusStyle.normal.textColor = isReadyToActivate 
                    ? new Color(1f, 0.15f, 0.15f, alphaPulse) 
                    : new Color(0.6f, 0.6f, 0.6f, 0.8f);
                
                GUI.Label(new Rect(0, 590, 1920, 50), statusText, statusStyle);
                break;
        }
    }

    private void StartGameProcess()
    {
        currentState = MenuState.Loading;
        StartCoroutine(LoadSceneAsync());
    }

    private IEnumerator LoadSceneAsync()
    {
        isReadyToActivate = false;
        activationDelayTimer = 0f;
        fakeLoadingProgress = 0f;

        yield return new WaitForSeconds(0.4f);

        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        loadingOperation = SceneManager.LoadSceneAsync(nextSceneIndex);
        
        loadingOperation.allowSceneActivation = false; 

        while (loadingOperation.progress < 0.9f || fakeLoadingProgress < 1f)
        {
            fakeLoadingProgress = Mathf.MoveTowards(fakeLoadingProgress, 1f, Time.deltaTime * 0.35f);
            float realProgress = Mathf.Clamp01(loadingOperation.progress / 0.9f);
            fakeLoadingProgress = Mathf.Min(fakeLoadingProgress, realProgress);
            yield return null; 
        }

        fakeLoadingProgress = 1f;
        isReadyToActivate = true;
    }
}