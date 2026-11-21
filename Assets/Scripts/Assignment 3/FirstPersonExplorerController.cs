using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles first-person movement (walk, run, jump, crouch), camera look,
/// and camera raycasting for hidden object collection and NPC interaction.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FirstPersonExplorerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 4.5f;
    public float runSpeed = 7.5f;
    public float jumpHeight = 1.2f;
    public float gravity = -18f;
    public float standingHeight = 1.8f;
    public float crouchHeight = 1.0f;

    [Header("Camera & Raycast Settings")]
    public Camera playerCamera;
    public float mouseSensitivity = 2.0f;
    public float interactRange = 4.0f;
    public LayerMask interactableLayer;

    [Header("Dynamic Reticle UI")]
    public Image reticleImage;
    public Sprite defaultReticle;
    public Sprite pickupHandReticle;
    public Sprite inspectEyeReticle;
    public Sprite talkBubbleReticle;

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;
    private bool isCrouching = false;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // Freeze movement/camera while reading dialogue or inspecting lore cards
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
            return;

        if (InventoryAndCraftingSystem.Instance != null && InventoryAndCraftingSystem.Instance.IsInspectingUI)
            return;
        if (PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused)
            return;

        HandleMouseLook();
        HandleMovement();
        HandleRaycastInteraction();
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleMovement()
    {
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        // Crouch toggle (C or Left Control)
        if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftControl))
        {
            isCrouching = !isCrouching;
            controller.height = isCrouching ? crouchHeight : standingHeight;
        }

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        float currentSpeed = (!isCrouching && Input.GetKey(KeyCode.LeftShift)) ? runSpeed : walkSpeed;
        controller.Move(move * currentSpeed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && controller.isGrounded && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleRaycastInteraction()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        // Extend range when looking through the Telescope
        float activeRange = (InventoryAndCraftingSystem.Instance != null &&
                             InventoryAndCraftingSystem.Instance.IsTelescopeActive)
                             ? interactRange * 5f : interactRange;

        if (Physics.Raycast(ray, out hit, activeRange, interactableLayer))
        {
            // 1. Check for Hidden Object / Decoy / Tool
            HiddenObjectItem item = hit.collider.GetComponent<HiddenObjectItem>();
            if (item != null)
            {
                SetReticle(pickupHandReticle);
                if (Input.GetMouseButtonDown(0))
                    item.OnClicked();
                return;
            }

            // 2. Check for NPC / Narrative Trigger
            NarrativeTriggerAndTransition trigger = hit.collider.GetComponent<NarrativeTriggerAndTransition>();
            if (trigger != null)
            {
                SetReticle(trigger.isNPC ? talkBubbleReticle : inspectEyeReticle);
                if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
                    trigger.TriggerInteraction();
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
}