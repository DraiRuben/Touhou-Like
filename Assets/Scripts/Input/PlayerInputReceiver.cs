using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerMovementHandler))]
public class PlayerInputReceiver : MonoBehaviour
{
    private PlayerMovementHandler m_movementHandler;
    private void Awake()
    {
        m_movementHandler = GetComponent<PlayerMovementHandler>();
    }
    public void MovementInput(InputAction.CallbackContext ctx)
    {
        m_movementHandler.MovementInput = ctx.ReadValue<Vector2>();
    }

    public void Ability(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {

        }
    }
    public void Shield(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {

        }
    }
    public void Pause(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {

        }
    }
}
