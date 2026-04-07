using UnityEngine;
using UnityEngine.InputSystem;
//using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour, PlayerInputActions.IPlayerActions
{
    private CharacterController character;
    private Vector3 direction;

    public float jumpForce = 8f;
    public float gravity = 9.81f * 2f;
    PlayerInputActions input;

    private void Awake()
    {
        character = GetComponent<CharacterController>();
        input = new PlayerInputActions();
        input.Player.AddCallbacks(this);

    }

    private void OnEnable()
    {
        input.Enable();
        direction = Vector3.zero;
    }
    private void OnDisable()
    {
        input.Disable();
    }

    private void OnDestroy()
    {
        input.Dispose();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        Jump();
    }
    private void Jump()
    {
        jump = true;
    }
    bool jump;
    private void Update()
    {
        if (!GameManager.Instance.isGameStarted)
        {
            return;
        }

        direction += gravity * Time.deltaTime * Vector3.down;
        if (character.isGrounded)
        {
            direction = Vector3.down;

            if (jump)
            {
                direction = Vector3.up * jumpForce;
                jump = false;
            }
        }

        character.Move(direction * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            GameManager.Instance.GameOver();
        }
    }

}
