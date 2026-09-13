using UnityEngine;
using UnityEngine.InputSystem;

public class TimeHandler : MonoBehaviour
{
    [SerializeField] private float increment = 0.1f;

    private bool paused;
    private float currentTimeScale = 1f;

    // Applies the saved time scale to the whole game.
    private void ApplyTimeScale()
    {
        Time.timeScale = currentTimeScale;
    }

    // Saves a new time scale and immediately applies it.
    private void ChangeTimeScale(float value)
    {
        currentTimeScale = value;
        ApplyTimeScale();
    }

    // Changes the current time scale by the supplied amount while keeping it between 0 and 1.
    private void AdjustTimeScale(float amount)
    {
        ChangeTimeScale(Mathf.Clamp01(currentTimeScale + amount));
    }

    // Pauses the game by setting Unity's global time scale to zero.
    private void Pause()
    {
        paused = true;
        Time.timeScale = 0f;
    }

    // Restores the time scale that was active before pausing.
    private void Unpause()
    {
        paused = false;
        ApplyTimeScale();
    }

    // Switches between paused and unpaused.
    private void TogglePause()
    {
        if (paused)
            Unpause();
        else
            Pause();
    }

    // InputAction.CallbackContext tells this method which phase of an Input System action just occurred.
    // Increases the game speed when the input action is performed.
    public void OnIncrease(InputAction.CallbackContext context)
    {
        if (context.performed)
            AdjustTimeScale(increment);
    }

    // Decreases the game speed when the input action is performed.
    public void OnDecrease(InputAction.CallbackContext context)
    {
        if (context.performed)
            AdjustTimeScale(-increment);
    }

    // Toggles pause when the input action is performed.
    public void OnToggle(InputAction.CallbackContext context)
    {
        if (context.performed)
            TogglePause();
    }
}
