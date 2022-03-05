using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public PlayerInput playerInput;
    [SerializeField] GameObject playerCam;
    private Vector3 camPos;
    private Rigidbody rb;
    private CapsuleCollider capsule;

    //Key Pieces
    //private int count;
    private int count_level;
    //public GameObject winTextObject;
    public GameObject nextLevelTextObject;
    public GameObject failTextObject;

    //Death
    // public GameObject loseTextObject;
    public GameObject deathController;
    public bool IsDead { get => isDead; }
    [SerializeField] private bool isDead;

    //Moving forward, jumping
    public float movementSpeed;
    public float speedThreshold;
    public bool IsGrounded { get => isGrounded; }
    [SerializeField] private bool isGrounded;

    //Sliding
    [Header("Sliding")] 
    public float slidingSpeed;
    private float slidingCD = 0; //checking value
    [SerializeField] private float slidingCoolDown = 0.3f; //modifiable value
    private float slideInterpolate;
    public bool StopSlide { get => stopSlide; }
    private bool stopSlide;
    public bool IsSliding { get => isSliding; }
    [SerializeField] private bool isSliding;
    private float originalHeight;
    public float slidingHeight;




    //Turning variables
    public float turnDuration;
    private float timeRemaining;
    private bool isTurning;
    private Quaternion toDirection;
    private Quaternion fromDirection;

    // Spells
    [Header("Speed Boost")]
    [SerializeField] private float speedBoostDuration;
    [SerializeField] private float speedBoostSpeedMultiplier;
    [SerializeField] private bool speedBoostIsActive;

    [Header("Jump Boost")]
    [SerializeField] private float jumpBoostDuration;
    [SerializeField] private float jumpBoostForceMultiplier;
    [SerializeField] private bool jumpBoostIsActive;

    [Header("Time Slow")]
    [SerializeField] private float timeSlowMaxDuration;
    [Tooltip("Value is seconds per second while not in use")]
    [SerializeField] private float timeSlowRegenerationPerSecond;
    [SerializeField] [Range(0,1)] private float timeSlowMultiplier;
    private float timeSlowDurationRemaining;
    /// <summary>
    /// This is set the value in the project settings on startup. This is needed since we alter this 
    /// value to keep rotation smooth while the Time Slow spell is active.
    /// </summary>
    private float defaultFixedDeltaTime;
    /// <summary>
    /// Do not modify this value! This should only ever be changed through the property TimeSlowIsActive!
    /// </summary>
    private bool _timeSlowIsActive;
    public bool TimeSlowIsActive
    {
        get => _timeSlowIsActive;
        set { 
            _timeSlowIsActive = value; 
            if (_timeSlowIsActive)
            {
                Time.timeScale = timeSlowMultiplier;
                Time.fixedDeltaTime = defaultFixedDeltaTime * timeSlowMultiplier;
            } else
            {
                Time.timeScale = 1;
                Time.fixedDeltaTime = defaultFixedDeltaTime;
            }
        }
    }


    // Exposed movement variables
    public Vector3 velocity { get { return rb.velocity; } }
    public float speed { get { return velocity.magnitude; } }

    public static PlayerController Instance { get { return _instance; } }
    private static PlayerController _instance;

    private void Awake()
    {
        _instance = this;
        defaultFixedDeltaTime = Time.fixedDeltaTime;
        Sounds.Initialize();
    }

    // Start is called before the first frame update
    void Start()
    {
        camPos = playerCam.transform.localPosition;
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        //count = 0;
        count_level = -1;
        //winTextObject.SetActive(false);
        nextLevelTextObject.SetActive(false);
        failTextObject.SetActive(false);
        originalHeight = capsule.height; 
    }

    // Update is called once per frame
    void Update()
    {
        playerCam.transform.localPosition = camPos;
        HandleInput();
        #region Time Slow
        if (!TimeSlowIsActive)
        {
            timeSlowDurationRemaining = Mathf.Clamp(timeSlowDurationRemaining + Time.unscaledDeltaTime, 0, timeSlowMaxDuration);
        } else
        {
            timeSlowDurationRemaining = Mathf.Clamp(timeSlowDurationRemaining - Time.unscaledDeltaTime, 0, timeSlowMaxDuration);
            if (timeSlowDurationRemaining == 0)
            {
                TimeSlowIsActive = false;
            }
        }
        TimeSlowMeterManager.Instance.UpdateMeter(timeSlowDurationRemaining, timeSlowMaxDuration);
        #endregion
    }

    /// <summary>
    /// Called every frame to check for input and respond accordingly.
    /// </summary>
    private void HandleInput()
    {
        if (!isDead)
        {
            if (playerInput.actions["Left"].WasPerformedThisFrame() && !isTurning)
            {
                fromDirection = transform.rotation;
                toDirection = Quaternion.FromToRotation(Vector3.right, transform.forward);
                isTurning = true;
                timeRemaining = turnDuration;
            }
            if (playerInput.actions["Right"].WasPerformedThisFrame() && !isTurning)
            {
                fromDirection = transform.rotation;
                toDirection = Quaternion.FromToRotation(Vector3.left, transform.forward); //Opposite direciton but it works for some reason lmao.
                isTurning = true;
                timeRemaining = turnDuration;
            }
            if (playerInput.actions["Jump"].WasPressedThisFrame() && isGrounded) // Jumping
            {
                if (jumpBoostIsActive)
                {
                    rb.AddForce(Vector3.up * 5 * jumpBoostForceMultiplier, ForceMode.VelocityChange);
                }
                else
                {
                    rb.AddForce(Vector3.up * 5, ForceMode.VelocityChange);
                }
                isGrounded = false;
                
                // play a jumping sound
                Sounds.StopPlayingRunningSound();
                int rand = Random.Range(0,2);
                if (rand == 0) {
                    Sounds.PlaySound(Sounds.Sound.Player_Jump1);
                } else {
                    Sounds.PlaySound(Sounds.Sound.Player_Jump2);
                }
            }
            if (playerInput.actions["Jump"].WasReleasedThisFrame())
            {

            }
            if (transform.position.y < 0)
            {
                Sounds.StopPlayingRunningSound();
                Sounds.PlaySound(Sounds.Sound.Player_Death_Fall);
                KillPlayer();
            }
            if (playerInput.actions["Slide"].WasPerformedThisFrame() && rb.velocity.y <= 0 && !isSliding)
            {
                isSliding = true;
                stopSlide = false;
                slideInterpolate = 0;
                if (slidingCD == 0)
                {
                    rb.AddForce(transform.forward * slidingSpeed, ForceMode.VelocityChange);
                }
                Sounds.StopPlayingRunningSound();
                Sounds.PlaySound(Sounds.Sound.PlayerSlide);
            }
            if (playerInput.actions["Slide"].WasReleasedThisFrame())
            {
                if(isSliding)
                {
                    stopSlide = true;
                    slidingCD = slidingCoolDown;
                }
            }
            // Spells
            if (playerInput.actions["SpeedBoost"].WasPerformedThisFrame())
            {
                StartCoroutine(SpeedBoost());
            }
            if (playerInput.actions["JumpBoost"].WasPerformedThisFrame())
            {
                StartCoroutine(JumpBoost());
            }
            if (playerInput.actions["TimeSlow"].WasPressedThisFrame())
            {
                TimeSlowIsActive = true;
            }
            if (playerInput.actions["TimeSlow"].WasReleasedThisFrame()) {
                TimeSlowIsActive = false;
            }
            //Camera movement
            //Debug.Log(playerInput.actions["Look"].ReadValue<Vector2>());
            //Vector2 mouseLook = playerInput.actions["Look"].ReadValue<Vector2>();
            //playerCam.transform.Rotate(new Vector3(-mouseLook.y, mouseLook.x, 0));
            
        }
        else
        {
            if (playerInput.actions["Restart"].WasPerformedThisFrame())
            {
                PlayerDeath pd = deathController.GetComponent<PlayerDeath>();
                pd.ReloadLevel();
            }
        }
    }

    private void FixedUpdate()
    {
        rb.AddForce(transform.up * -9.81f, ForceMode.Acceleration); // fake gravity
        if (slidingCD> 0)
        {
            slidingCD -= Time.deltaTime;
        } else
        {
            slidingCD = 0;
        }
        
        if (isTurning)
        {
            if (timeRemaining <= 0f)
            {
                isTurning = false;
            }
            timeRemaining -= Time.deltaTime;
            transform.rotation = Quaternion.Lerp(fromDirection, toDirection, (turnDuration - timeRemaining) / turnDuration);
        }
        if (!isTurning && isGrounded && !isSliding && !isDead)
        {
            if (rb.velocity.magnitude < speedThreshold)
            {
                if (speedBoostIsActive)
                {
                    rb.velocity = transform.forward * movementSpeed * speedBoostSpeedMultiplier;
                } else
                {
                    rb.velocity = transform.forward * movementSpeed;
                }
            }
            Sounds.PlaySound(Sounds.Sound.PlayerRun_Metal);
        }
        if (isSliding)
        {
            if (rb.velocity.magnitude < speedThreshold * 0.8 || stopSlide)
            {
                slideInterpolate -= 3f * Time.deltaTime;
                
            } else
            {
                slideInterpolate += 2f * Time.deltaTime;   
            }
            capsule.height = Mathf.Lerp(originalHeight, slidingHeight, slideInterpolate);
            if (capsule.height == 2)
            {
                isSliding = false;
            } else if (capsule.height == 0.5)
            {
                slideInterpolate = 1;
            }
        }
        RaycastHit hit;
        if (!Physics.Raycast(transform.position, Vector3.down, out hit, 2f))
        {
            isGrounded = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Ground")
        {
            isGrounded = true;
            //preGrounded = false;
            //groundTime = 1f;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.tag == "Ground")
        {
           // preGrounded = true;
           // Debug.Log("lol");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("KeyStone"))
        {
            other.gameObject.SetActive(false);
            count_level++;
            CheckCount_level();
        }
        if (other.gameObject.CompareTag("KeyPiece"))
        {
            other.gameObject.SetActive(false);
            //count++;
            count_level++;
            CheckCount_level();
            //CheckCount();
        }
        if (other.gameObject.CompareTag("Door") && CheckCount_level())
        { 
            other.gameObject.GetComponent<HingedDoor>().OpenDoor();
        }

        if (other.gameObject.CompareTag("Obstacle"))
        {
            // loseTextObject.SetActive(true);
            Debug.Log("bonk");
            Sounds.StopPlayingRunningSound();
            Sounds.PlaySound(Sounds.Sound.Player_Death_Fall);
            KillPlayer();
        }
    }


    private bool CheckCount_level() {
        //Debug.Log(count_level);
        if (count_level >= 3)
        {
            nextLevelTextObject.SetActive(true);
            //count_level = 0;
            //Debug.Log("won");
            return true;
        } else if (count_level < 0)
        {
            failTextObject.SetActive(true);
            Text textComponent = failTextObject.GetComponentInChildren<Text>();
            textComponent.text = "Find the tablet!";
            //count_level = 0;
            return false;
        } 
        else {
            failTextObject.SetActive(true);
            Text textComponent = failTextObject.GetComponentInChildren<Text>();
            textComponent.text = count_level + "/3 runes gathered!";
            return false;
        }
        
    }

    public void KillPlayer()
    {
        deathController.SetActive(true);
        isDead = true;
    }

    /// <summary>
    /// Applies a speed buff to the player for a duration of speedBoostDuration.
    /// This simply toggles speedBoostIsActive. FixedUpdate accounts for this to decide
    /// how quickly the player moves.
    /// </summary>
    private IEnumerator SpeedBoost()
    {
        speedBoostIsActive = true;
        yield return new WaitForSeconds(speedBoostDuration);
        speedBoostIsActive = false;
    }
    /// <summary>
    /// Operates identically to the SpeedBoost method.
    /// </summary>
    private IEnumerator JumpBoost()
    {
        jumpBoostIsActive = true;
        yield return new WaitForSeconds(jumpBoostDuration);
        jumpBoostIsActive = false;
    }

}
