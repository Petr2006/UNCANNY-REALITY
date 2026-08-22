using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(AudioSource))]
public class FPSController : MonoBehaviour
{
    public float walkingSpeed = 7.5f;
    public float gravity = 20.0f;
    public float lookSpeed = 2.0f;
    public float lookXLimit = 45.0f;
    public AudioClip stepSound;
    public float timeBetweenSteps = 0.35f;

    private CharacterController characterController;
    private AudioSource audioSource;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;
    private float stepTimer = 0;
    
    private float initialCameraY; 

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;

        if (!Application.isMobilePlatform)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Camera.main != null)
        {
            Camera.main.nearClipPlane = 0.01f;
            initialCameraY = Camera.main.transform.localPosition.y;
        }
    }

    void Update()
    {
        float h = 0f;
        float v = 0f;

        bool isMobileTesting = Application.isMobilePlatform || Input.touchCount > 0 || FindFirstObjectByType<MobileController>() != null;

        if (!isMobileTesting)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            
            Camera.main.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);

            h = Input.GetAxis("Horizontal");
            v = Input.GetAxis("Vertical");
        }
        else
        {
            Camera.main.transform.localPosition = new Vector3(0, initialCameraY, 0);
        }

        Camera.main.transform.localPosition = new Vector3(0, initialCameraY, 0);

        if (!isMobileTesting)
        {
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right = transform.TransformDirection(Vector3.right);
            
            float speedY = moveDirection.y; 
            moveDirection = (forward * walkingSpeed * v) + (right * walkingSpeed * h);
            moveDirection.y = speedY;

            if (h != 0 || v != 0) PlayFootsteps();
            else StopFootsteps();

            if (!characterController.isGrounded) moveDirection.y -= gravity * Time.deltaTime;
            else moveDirection.y = 0f; 

            characterController.Move(moveDirection * Time.deltaTime);
        }
        else
        {
            // МОМЕНТАЛЬНОЕ ОТКЛЮЧЕНИЕ ЗВУКА: Порог увеличен до 0.5f + проверка на касание земли
            if (characterController.velocity.magnitude > 0.5f && characterController.isGrounded) 
            {
                PlayFootsteps();
            }
            else 
            {
                StopFootsteps();
            }
        }
    }

    void PlayFootsteps()
    {
        if (stepTimer <= 0f)
        {
            audioSource.clip = stepSound;
            audioSource.Play();
            stepTimer = timeBetweenSteps;
        }
        stepTimer -= Time.deltaTime;
    }

    void StopFootsteps()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        stepTimer = 0f; 
    }
}