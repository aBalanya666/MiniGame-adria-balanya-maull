using UnityEngine;
using UnityEngine.InputSystem;

// CHARACTER CONTROL + INPUT: moves the player with a CharacterController.
// Uses the new Input System package (Keyboard.current / Mouse.current).
// Keyboard: WASD/arrows = move, Space = jump, Left Shift = sprint.
// Mouse: Left click = dash (with cooldown).
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 10f;
    public float jumpHeight = 1.5f;
    public float gravity = -20f;
    public float rotationSpeed = 12f;

    [Header("Dash (left mouse button)")]
    public float dashSpeed = 20f;
    public float dashTime = 0.15f;
    public float dashCooldown = 1.5f;

    [Header("Damage")]
    public float invulnerabilityTime = 1.5f;

    [Header("Optional (only if your character has an Animator)")]
    public Animator animator; // needs a float "Speed" and a bool "Grounded"

    CharacterController controller;
    Renderer[] renderers;
    float verticalVelocity;
    float dashTimer;
    float dashCooldownTimer;
    float invulnerableTimer;
    Vector3 dashDirection;

    public bool DashReady => dashCooldownTimer <= 0f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        renderers = GetComponentsInChildren<Renderer>();
    }

    void Update()
    {
        // Stop controlling the player when the game ended
        if (GameManager.Instance != null && GameManager.Instance.GameOver) return;

        // --- INPUT (keyboard) ---
        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;
        float h = 0f, v = 0f;
        bool sprint = false, jumpPressed = false;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v += 1f;
            sprint = kb.leftShiftKey.isPressed;
            jumpPressed = kb.spaceKey.wasPressedThisFrame;
        }
        Vector3 input = new Vector3(h, 0f, v).normalized;
        float speed = sprint ? sprintSpeed : walkSpeed;

        // --- DASH (mouse) ---
        dashCooldownTimer -= Time.deltaTime;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && DashReady)
        {
            dashTimer = dashTime;
            dashCooldownTimer = dashCooldown;
            dashDirection = input != Vector3.zero ? input : transform.forward;
        }

        // --- GRAVITY + JUMP ---
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f; // small push so isGrounded stays true

        if (controller.isGrounded && jumpPressed)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalVelocity += gravity * Time.deltaTime;

        // --- MOVE ---
        Vector3 move;
        if (dashTimer > 0f)
        {
            move = dashDirection * dashSpeed;
            dashTimer -= Time.deltaTime;
        }
        else
        {
            move = input * speed;
        }
        move.y = verticalVelocity;
        controller.Move(move * Time.deltaTime);

        // --- ROTATE towards the movement direction ---
        if (input != Vector3.zero)
        {
            Quaternion target = Quaternion.LookRotation(input);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
        }

        // --- ANIMATION (optional) ---
        if (animator != null)
        {
            animator.SetFloat("Speed", input.magnitude * (speed / sprintSpeed));
            animator.SetBool("Grounded", controller.isGrounded);
        }

        // --- Blink while invulnerable ---
        invulnerableTimer -= Time.deltaTime;
        bool visible = invulnerableTimer <= 0f || Mathf.FloorToInt(Time.time * 10f) % 2 == 0;
        foreach (Renderer r in renderers) r.enabled = visible;
    }

    // Called by the bombs
    public void TakeDamage()
    {
        if (invulnerableTimer > 0f) return; // can't be hit twice in a row
        invulnerableTimer = invulnerabilityTime;
        GameManager.Instance.LoseLife();
    }
}
