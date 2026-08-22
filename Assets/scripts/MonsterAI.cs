using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using UnityEngine.SceneManagement; 

[RequireComponent(typeof(NavMeshAgent))]
public class MonsterAI : MonoBehaviour
{
    [Header("Настройки ИИ")]
    public Transform playerTransform;
    private MobileController playerController; // Ссылка на контроллер для проверки пряток
    public float minPatrolDistance = 15f;
    public float maxPatrolRadius = 30f; 
    public float waitTimeAtPoint = 1.5f;

    [Tooltip("Сколько секунд клоун помнит игрока и продолжает погоню после того, как потерял его из виду")]
    public float timeToForgetPlayer = 4.0f; 

    [Header("Зрение")]
    public float viewDistance = 15f;
    public float viewAngle = 180f;
    public LayerMask obstacleMask;

    [Header("Звуки шагов (Настройка)")]
    public AudioClip stepSound;
    [Tooltip("Время между шагами. Попробуйте увеличить (например, до 0.6 или 0.7), если шаги звучат слишком быстро")]
    public float timeBetweenSteps = 0.6f; 
    public float heightOffset = 1.0f;

    [Header("Новые страшные звуки клоуна")]
    [Tooltip("Сюда закиньте звуки смеха клоуна или его бормотания (несколько штук)")]
    public AudioClip[] clownCreepySounds;
    [Tooltip("Минимальное время между случайным смехом")]
    public float minTimeBetweenCreepySounds = 10f;
    [Tooltip("Максимальное время между случайным смехом")]
    public float maxTimeBetweenCreepySounds = 25f;

    [Tooltip("Звук, который проиграет ОДИН РАЗ в момент, когда клоун вас увидит")]
    public AudioClip detectionSound;

    [Tooltip("Жуткая музыка погони, которая будет играть, пока клоун бежит за вами")]
    public AudioClip chaseMusic;

    private AudioSource footstepsSource;
    private AudioSource voicesSource;
    private AudioSource musicSource;

    private NavMeshAgent agent;
    private Animator animator; 
    private float stepTimer = 0f;
    private float creepySoundTimer = 0f;
    private float forgetTimer = 0f; 
    private bool isWaiting = false;

    private bool hasSpottedPlayer = false;
    private bool isChasing = false;
    private Vector3 lastKnownPosition; 

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>(); 

        footstepsSource = gameObject.AddComponent<AudioSource>();
        footstepsSource.spatialBlend = 1.0f; 
        footstepsSource.playOnAwake = false;

        voicesSource = gameObject.AddComponent<AudioSource>();
        voicesSource.spatialBlend = 1.0f; 
        voicesSource.playOnAwake = false;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.spatialBlend = 0.0f; 
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = 0f; 

        agent.baseOffset = heightOffset; 

        if (playerTransform == null)
        {
            var player = FindFirstObjectByType<MobileController>();
            if (player != null) playerTransform = player.transform;
        }

        if (playerTransform != null)
        {
            playerController = playerTransform.GetComponent<MobileController>();
            
            Rigidbody playerRb = playerTransform.GetComponent<Rigidbody>();
            if (playerRb != null) playerRb.sleepThreshold = 0.0f;
        }

        if (obstacleMask.value == 0)
        {
            obstacleMask = LayerMask.GetMask("Default");
        }

        creepySoundTimer = Random.Range(minTimeBetweenCreepySounds, maxTimeBetweenCreepySounds);

        SetNextPatrolPoint();
    }

    void Update()
    {
        if (agent == null || !agent.isOnNavMesh || !agent.enabled) return;

        // Если игрок УЖЕ в шкафу, сбрасываем состояние погони и не даем запустить скример на дистанции
        if (playerController != null && playerController.IsHiding())
        {
            if (isChasing)
            {
                isChasing = false;
                hasSpottedPlayer = false;
                SetNextPatrolPoint(); 
            }
        }
        else if (playerTransform != null)
        {
            // Проверка на скример (работает только если игрок НЕ спрятан)
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= 1.6f) 
            {
                TriggerJumpscareSequence();
                return;
            }
        }

        bool currentlySeesPlayer = playerTransform != null && CanSeePlayer();

        if (currentlySeesPlayer)
        {
            StopAllCoroutines(); 
            isWaiting = false;
            
            lastKnownPosition = playerTransform.position;
            agent.SetDestination(lastKnownPosition);
            forgetTimer = timeToForgetPlayer;

            if (!hasSpottedPlayer)
            {
                hasSpottedPlayer = true;
                if (detectionSound != null)
                {
                    voicesSource.PlayOneShot(detectionSound, 1.0f);
                }
            }

            if (!isChasing)
            {
                isChasing = true;
                if (chaseMusic != null)
                {
                    musicSource.clip = chaseMusic;
                    musicSource.Play();
                }
            }
        }
        else if (isChasing)
        {
            forgetTimer -= Time.deltaTime;
            agent.SetDestination(lastKnownPosition);

            if (forgetTimer <= 0f)
            {
                isChasing = false;
                hasSpottedPlayer = false; 
                SetNextPatrolPoint();
            }
        }
        else 
        {
            if (agent.enabled) agent.isStopped = false;

            if (!isWaiting)
            {
                if (!agent.pathPending && agent.remainingDistance <= 1.5f)
                {
                    StartCoroutine(WaitAtPoint());
                }
            }
        }

        HandleChaseMusicVolume();
        HandleCreepyVoices();
        UpdateAnimationAndSound();
    }

    private void TriggerJumpscareSequence()
    {
        // Дополнительная железная проверка безопасности перед убийством
        if (playerController != null && playerController.IsHiding()) return;

        musicSource.Stop();
        
        if (Jumpscare3D.Instance != null)
        {
            Jumpscare3D.Instance.Trigger3DScare(this.gameObject, playerTransform);
        }

        if (GameplayUIController.Instance != null)
        {
            GameplayUIController.Instance.TriggerGameOver();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    void HandleCreepyVoices()
    {
        if (clownCreepySounds == null || clownCreepySounds.Length == 0) return;

        creepySoundTimer -= Time.deltaTime;
        if (creepySoundTimer <= 0f)
        {
            int randomIndex = Random.Range(0, clownCreepySounds.Length);
            AudioClip clipToPlay = clownCreepySounds[randomIndex];

            if (clipToPlay != null && !voicesSource.isPlaying)
            {
                voicesSource.PlayOneShot(clipToPlay, 1.0f);
            }

            creepySoundTimer = Random.Range(minTimeBetweenCreepySounds, maxTimeBetweenCreepySounds);
        }
    }

    void HandleChaseMusicVolume()
    {
        float targetVolume = isChasing ? 0.8f : 0f;
        musicSource.volume = Mathf.MoveTowards(musicSource.volume, targetVolume, Time.deltaTime * 1.5f);
        
        if (!isChasing && musicSource.volume <= 0.01f && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }

    void UpdateAnimationAndSound()
    {
        if (animator == null) return;
        
        float speed = agent.velocity.magnitude;
        animator.SetFloat("Speed", speed);

        if (speed > 0.1f && agent.isOnNavMesh && !agent.isStopped) 
        {
            stepTimer -= Time.deltaTime;
            
            if (stepTimer <= 0f && !footstepsSource.isPlaying)
            {
                if (stepSound != null)
                {
                    footstepsSource.clip = stepSound;
                    footstepsSource.volume = 0.7f;
                    footstepsSource.Play();
                }
                stepTimer = timeBetweenSteps;
            }
        }
        else
        {
            stepTimer = 0f; 
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TriggerJumpscareSequence();
            return;
        }

        DoorController door = other.GetComponentInParent<DoorController>() ?? other.GetComponentInChildren<DoorController>();
        if (door != null) 
        {
            if (!door.IsBlocked())
            {
                door.NPC_OpenDoor();
            }
            else
            {
                if (!isChasing) SetNextPatrolPoint();
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            TriggerJumpscareSequence();
            return;
        }

        DoorController door = collision.gameObject.GetComponentInParent<DoorController>() ?? collision.gameObject.GetComponentInChildren<DoorController>();
        if (door != null) 
        {
            if (!door.IsBlocked())
            {
                door.NPC_OpenDoor();
            }
            else
            {
                if (!isChasing) SetNextPatrolPoint();
            }
        }
    }

    System.Collections.IEnumerator WaitAtPoint()
    {
        isWaiting = true;
        agent.ResetPath();
        yield return new WaitForSeconds(waitTimeAtPoint);
        if (!CanSeePlayer()) 
        {
            SetNextPatrolPoint();
        }
        isWaiting = false;
    }

    void SetNextPatrolPoint()
    {
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomDir = Random.insideUnitSphere * maxPatrolRadius;
            randomDir += transform.position;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDir, out hit, maxPatrolRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }
    }

    bool CanSeePlayer()
    {
        if (playerTransform == null) return false;
        
        // ЕСЛИ ИГРОК ПРЯЧЕТСЯ — КЛОУН ФИЗИЧЕСКИ НЕ МОЖЕТ ЕГО УВИДЕТЬ
        if (playerController != null && playerController.IsHiding()) return false;
        
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer > viewDistance) return false;

        Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
        if (Vector3.Angle(transform.forward, dirToPlayer) > viewAngle * 0.5f) return false;

        Vector3 eyePos = transform.position + Vector3.up * 1.5f; 
        Vector3 playerEyePos = playerTransform.position + Vector3.up * 1.5f;

        RaycastHit hit;
        if (Physics.Linecast(eyePos, playerEyePos, out hit, obstacleMask))
        {
            return false;
        }

        Debug.Log("<color=red>[ЗРЕНИЕ]</color> Клоун заметил игрока!");
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        Vector3 leftBoundary = Quaternion.Euler(0, -viewAngle * 0.5f, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, viewAngle * 0.5f, 0) * transform.forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position + Vector3.up * 1.5f, leftBoundary * viewDistance);
        Gizmos.DrawRay(transform.position + Vector3.up * 1.5f, rightBoundary * viewDistance);
    }
}