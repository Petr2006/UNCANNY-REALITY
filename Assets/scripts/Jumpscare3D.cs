using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class Jumpscare3D : MonoBehaviour
{
    public static Jumpscare3D Instance;

    [Header("Настройки 3D Скримера / Jumpscare")]
    [Tooltip("Durata del jumpscare prima del riavvio")]
    public float jumpscareDuration = 2.5f; 
    
    [Tooltip("Suono del grido")]
    public AudioClip screamSound;          
    
    [Tooltip("Intensità del tremolio (ridotta per evitare movimenti troppo bruschi)")]
    public float cameraShakeIntensity = 0.04f; 

    [Tooltip("Altezza della faccia del clown. Abbassa questo valore (es. 0.8 o 0.7) se la camera guarda troppo in alto!")]
    public float enemyFaceHeight = 0.8f; 

    [Tooltip("Distanza dal clown. Aumentala (es. 1.3 o 1.5) per allontanarlo leggermente e vedere più del corpo")]
    public float distanceToPlayerFace = 1.3f;

    private AudioSource audioSource;
    private bool isJumpscareActive = false;
    private Camera playerCamera;
    private Vector3 originalCameraLocalPos;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        audioSource.spatialBlend = 0f; 
        audioSource.playOnAwake = false;
    }

    public void Trigger3DScare(GameObject monster, Transform playerTransform)
    {
        if (isJumpscareActive) return;
        isJumpscareActive = true;

        Debug.Log("<color=red><b>[3D JUMPSCARE]</b></color> Attivazione!");

        playerCamera = Camera.main;
        MobileController playerController = FindFirstObjectByType<MobileController>();

        // 1. DISABILITIAMO IL GIOCATORE E LA SUA FISICA (Evita che si muova a destra/sinistra)
        if (playerController != null)
        {
            playerController.enabled = false; 
        }

        // Spegniamo completamente il CharacterController del giocatore per congelarlo sul posto
        CharacterController charController = playerTransform.GetComponent<CharacterController>();
        if (charController != null)
        {
            charController.enabled = false;
        }

        // Se c'è un Rigidbody sul giocatore, azzeriamo la sua velocità
        Rigidbody playerRb = playerTransform.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
            playerRb.isKinematic = true; // Impedisce alla fisica di spingerlo
        }

        // 2. DISABILITIAMO IL MOSTRO
        MonsterAI monsterAI = monster.GetComponent<MonsterAI>();
        UnityEngine.AI.NavMeshAgent agent = monster.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (monsterAI != null) monsterAI.enabled = false;
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false; 
        }

        // 3. POSIZIONAMENTO CAMERA E MOSTRO
        if (playerCamera != null)
        {
            originalCameraLocalPos = playerCamera.transform.localPosition;

            // Posizioniamo il mostro davanti al giocatore (leggermente più lontano per inquadrarlo meglio)
            Vector3 scarePosition = playerCamera.transform.position + playerCamera.transform.forward * distanceToPlayerFace; 
            scarePosition.y = monster.transform.position.y; 
            monster.transform.position = scarePosition;

            // Ruotiamo il mostro verso il giocatore
            Vector3 lookAtPlayer = playerCamera.transform.position;
            lookAtPlayer.y = monster.transform.position.y; 
            monster.transform.LookAt(lookAtPlayer);

            // Puntiamo la telecamera del giocatore ESATTAMENTE sulla faccia del clown
            Vector3 facePosition = monster.transform.position + Vector3.up * enemyFaceHeight; 
            playerCamera.transform.LookAt(facePosition);

            // Avviamo il tremolio leggero (effetto paura)
            StartCoroutine(ShakeCameraEffect());
        }

        // 4. ANIMAZIONE
        Animator monsterAnim = monster.GetComponent<Animator>();
        if (monsterAnim != null)
        {
            monsterAnim.SetTrigger("Scare");
        }

        // 5. SUONO
        if (audioSource != null && screamSound != null)
        {
            audioSource.PlayOneShot(screamSound, 1f);
        }

        // 6. RESTART
        StartCoroutine(RestartLevelTimer());
    }

    IEnumerator ShakeCameraEffect()
    {
        float elapsed = 0f;
        while (elapsed < jumpscareDuration)
        {
            if (playerCamera != null)
            {
                // Tremolio molto più leggero e realistico
                float randomX = Random.Range(-cameraShakeIntensity, cameraShakeIntensity);
                float randomY = Random.Range(-cameraShakeIntensity, cameraShakeIntensity);
                
                playerCamera.transform.localPosition = originalCameraLocalPos + new Vector3(randomX, randomY, 0f);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (playerCamera != null) playerCamera.transform.localPosition = originalCameraLocalPos;
    }

    IEnumerator RestartLevelTimer()
    {
        yield return new WaitForSeconds(jumpscareDuration);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}