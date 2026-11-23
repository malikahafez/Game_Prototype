using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    /// <summary>
    /// Combined First-Person Controller integrating Unity Starter Assets movement, Cinemachine camera look,
    /// Grounded sphere checks, crouching, physical object carrying/inspection, dynamic HOPA reticles,
    /// and narrative raycast interactions.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class FirstPersonExplorerController : MonoBehaviour
    {
        [Header("Player Movement")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 4.0f;
        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 6.0f;
        [Tooltip("Crouch move speed of the character in m/s")]
        public float CrouchSpeed = 2.2f;
        [Tooltip("Rotation speed of the character")]
        public float RotationSpeed = 1.0f;
        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;
        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.1f;
        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Crouch Settings")]
        public KeyCode crouchKey = KeyCode.C;
        public float standingHeight = 1.8f;
        public float crouchHeight = 1.0f;
        private bool _isCrouching = false;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;
        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;
        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.5f;
        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;
        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 90.0f;
        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -90.0f;

        [Header("HOPA Raycast & Physical Carry Configurations")]
        public float itemPickupDistance = 4.0f;
        public LayerMask interactableLayer;
        public Transform head;
        public KeyCode carryDropKey = KeyCode.G;
        public float attachedDistance = 2.5f;

        [Header("Dynamic Reticle UI")]
        public Image reticleImage;
        public Sprite defaultReticle;
        public Sprite pickupHandReticle;
        public Sprite inspectEyeReticle;
        public Sprite talkBubbleReticle;

        // Runtime carry reference
        private Transform attachedObject = null;

        // Cinemachine
        private float _cinemachineTargetPitch;

        // Player state
        private float _speed;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // Timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput != null && _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// Returns true if any blocking UI (Pause Menu, Dialogue Box, or Lore Inspection Card) is currently open.
        /// </summary>
        private bool IsUIBlockingGameplay
        {
            get
            {
                if (PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused)
                    return true;
                if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
                    return true;
                if (InventoryAndCraftingSystem.Instance != null && InventoryAndCraftingSystem.Instance.IsInspectingUI)
                    return true;
                if (HiddenObjectManager.Instance != null && HiddenObjectManager.Instance.IsLevelCompleteScreenActive)
                    return true;

                return false;
            }
        }

        private void Awake()
        {
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();

#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#else
            Debug.LogError("Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            // Freeze player movement, jumping, and raycast interactions while UI panels are active
            if (IsUIBlockingGameplay)
                return;

            HandleCrouch();
            JumpAndGravity();
            GroundedCheck();
            Move();
            HandleHOPAInteractionAndPickup();
        }

        private void LateUpdate()
        {
            if (IsUIBlockingGameplay)
                return;

            // CameraRotation();
            if (Input.GetMouseButton(1)) // Rotate camera only while holding Right-Click
            {
                CameraRotation();
            }

            // Hold and rotate physically carried environmental objects in front of the player's head
            if (attachedObject != null && head != null)
            {
                attachedObject.position = head.position + head.forward * attachedDistance;
                attachedObject.Rotate(transform.right * Input.mouseScrollDelta.y * 30f, Space.World);
            }
        }

        private void HandleCrouch()
        {
            if (Input.GetKeyDown(crouchKey) || Input.GetKeyDown(KeyCode.LeftControl))
            {
                _isCrouching = !_isCrouching;
                _controller.height = _isCrouching ? crouchHeight : standingHeight;
            }
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
        }

        private void CameraRotation()
        {
            if (_input.look.sqrMagnitude >= _threshold)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
                _rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

                _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

                if (CinemachineCameraTarget != null)
                    CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

                transform.Rotate(Vector3.up * _rotationVelocity);
            }
        }

        private void Move()
        {
            float targetSpeed = _isCrouching ? CrouchSpeed : (_input.sprint ? SprintSpeed : MoveSpeed);

            if (_input.move == Vector2.zero)
                targetSpeed = 0.0f;

            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            if (_input.move != Vector2.zero)
            {
                inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
            }

            _controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;

                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Prevent jumping while crouched
                if (_input.jump && _jumpTimeoutDelta <= 0.0f && !_isCrouching)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                }

                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;

                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }

                _input.jump = false;
            }

            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        /// <summary>
        /// Handles raycasting from the camera/head for:
        /// 1. Collecting Hidden Objects / Tools / Decoys (Left-Click)
        /// 2. Talking to NPCs & activating Narrative Triggers (Left-Click or E)
        /// 3. Carrying and moving "pickable" environmental props aside (G key)
        /// </summary>
        private void HandleHOPAInteractionAndPickup()
        {
            Transform rayOrigin = head != null ? head : (_mainCamera != null ? _mainCamera.transform : transform);
            float activeRange = (InventoryAndCraftingSystem.Instance != null &&
                                 InventoryAndCraftingSystem.Instance.IsTelescopeActive)
                                 ? itemPickupDistance * 5f : itemPickupDistance;

            RaycastHit hit;
            bool cast = Physics.Raycast(rayOrigin.position, rayOrigin.forward, out hit, activeRange, interactableLayer);

            // Handle physical carry/drop of movable props ("pickable" tag)
            if (Input.GetKeyDown(carryDropKey))
            {
                if (attachedObject != null)
                {
                    attachedObject.SetParent(null);
                    if (attachedObject.TryGetComponent<Rigidbody>(out Rigidbody rb))
                        rb.isKinematic = false;
                    if (attachedObject.TryGetComponent<Collider>(out Collider col))
                        col.enabled = true;

                    attachedObject = null;
                    return;
                }
                else if (cast && hit.transform.CompareTag("pickable"))
                {
                    attachedObject = hit.transform;
                    attachedObject.SetParent(transform);
                    if (attachedObject.TryGetComponent<Rigidbody>(out Rigidbody rb))
                        rb.isKinematic = true;
                    if (attachedObject.TryGetComponent<Collider>(out Collider col))
                        col.enabled = false;

                    return;
                }
            }

            // Handle Hidden Object clicks, NPC dialogue, and Narrative Triggers
            if (cast)
            {
                HiddenObjectItem item = hit.collider.GetComponent<HiddenObjectItem>();
                if (item != null)
                {
                    SetReticle(pickupHandReticle);
                    if (Input.GetMouseButtonDown(0))
                        item.OnClicked();
                    return;
                }

                NarrativeTriggerAndTransition trigger = hit.collider.GetComponent<NarrativeTriggerAndTransition>();
                if (trigger != null)
                {
                    SetReticle(trigger.isNPC ? talkBubbleReticle : inspectEyeReticle);
                    if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
                        trigger.TriggerInteraction();
                    return;
                }

                if (hit.transform.CompareTag("pickable"))
                {
                    SetReticle(pickupHandReticle);
                    return;
                }
            }

            SetReticle(defaultReticle);
        }

        private void SetReticle(Sprite sprite)
        {
            if (reticleImage != null && sprite != null)
                reticleImage.sprite = sprite;
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
        }
    }
}