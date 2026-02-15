using UdonSharp;
using UnityEngine;
using TMPro;
using VRC.SDKBase;
using VRC.Udon.Common;   // For UdonInputEventArgs

public class DMFlyController : UdonSharpBehaviour
{
    [Header("Optional UI")]
    public TextMeshProUGUI buttonLabel;        // TMP label for the fly button
    public string flyOffText = "Fly Off";
    public string flyOnText = "Fly On";

    [Header("Fly Settings")]
    [Tooltip("Horizontal fly speed (forward/back/strafe)")]
    public float flySpeed = 6f;

    [Tooltip("Vertical fly speed (up/down)")]
    public float verticalSpeed = 4f;

    [Header("Controller Toggle Settings")]
    [Tooltip("If true, double-tap B (Button2) will toggle fly on/off.")]
    public bool allowControllerToggle = false;

    [Tooltip("Max time (in seconds) between B presses to count as a double-tap.")]
    public float doubleTapWindow = 0.3f;

    private float _lastButton2TapTime = -999f;

    [Header("Dice HUD Hook (Optional)")]
    [Tooltip("UdonSharpBehaviour for the dice HUD or log system.")]
    public UdonSharpBehaviour diceHud;

    [Tooltip("Custom event sent on the dice HUD when fly is enabled.")]
    public string diceHudFlyOnEvent = "OnFlyEnabled";

    [Tooltip("Custom event sent on the dice HUD when fly is disabled.")]
    public string diceHudFlyOffEvent = "OnFlyDisabled";

    private bool isFlying = false;

    // Input state
    private float moveHorizontal;
    private float moveVertical;
    private bool upHeld;   // Up = Jump

    // Only gravity is modified while flying
    private float originalGravityStrength;

    public void ToggleFlyMode()
    {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (localPlayer == null)
        {
            Debug.LogWarning("[FlyController] LocalPlayer is null.");
            return;
        }

        if (!isFlying)
        {
            EnableFlyMode(localPlayer);
        }
        else
        {
            DisableFlyMode(localPlayer);
        }
    }

    private void EnableFlyMode(VRCPlayerApi player)
    {
        // Save current gravity so we can restore it later
        originalGravityStrength = player.GetGravityStrength();

        // Turn off gravity & default locomotion, we’ll move the player manually
        player.SetGravityStrength(0f);
        player.Immobilize(true);

        // Clear any existing velocity
        player.SetVelocity(Vector3.zero);

        // Reset input state
        moveHorizontal = 0f;
        moveVertical = 0f;
        upHeld = false;

        isFlying = true;

        if (buttonLabel != null)
            buttonLabel.text = flyOnText;

        Debug.Log("[FlyController] TRUE FLY mode ENABLED for " + player.displayName);

        NotifyFlyToggled();
    }

    private void DisableFlyMode(VRCPlayerApi player)
    {
        // Restore gravity to what it was before flying
        player.SetGravityStrength(originalGravityStrength);

        // Re-enable normal locomotion
        player.Immobilize(false);

        // Stop any motion
        player.SetVelocity(Vector3.zero);

        isFlying = false;

        if (buttonLabel != null)
            buttonLabel.text = flyOffText;

        Debug.Log("[FlyController] TRUE FLY mode DISABLED for " + player.displayName);

        NotifyFlyToggled();
    }

    private void NotifyFlyToggled()
    {
        // Local-only notification hook to dice HUD
        if (diceHud == null) return;

        if (isFlying)
        {
            if (!string.IsNullOrEmpty(diceHudFlyOnEvent))
            {
                diceHud.SendCustomEvent(diceHudFlyOnEvent);
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(diceHudFlyOffEvent))
            {
                diceHud.SendCustomEvent(diceHudFlyOffEvent);
            }
        }
    }

    private void FlyUpdate()
    {
        if (!isFlying)
            return;

        VRCPlayerApi player = Networking.LocalPlayer;
        if (player == null)
            return;

        // Basis vectors from player rotation
        Quaternion rot = player.GetRotation();
        Vector3 forward = rot * Vector3.forward;
        Vector3 right = rot * Vector3.right;
        Vector3 up = Vector3.up;

        // Horizontal movement (WASD / stick)
        Vector3 horizontal = forward * moveVertical + right * moveHorizontal;

        // Vertical movement:
        //  - Jump = up
        //  - Shift (desktop) = down
        //  - B button (Oculus_CrossPlatform_Button2) = down in VR
        Vector3 vertical = Vector3.zero;

        if (upHeld)
        {
            vertical += up;
        }

        bool isDown = false;

        // Desktop down: Left or Right Shift
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            isDown = true;
        }

        // VR down: B button (Button2)
        if (Input.GetButton("Oculus_CrossPlatform_Button2"))
        {
            isDown = true;
        }

        if (isDown)
        {
            vertical -= up;
        }

        if (horizontal.sqrMagnitude > 0.0001f)
            horizontal = horizontal.normalized * flySpeed;
        else
            horizontal = Vector3.zero;

        if (vertical.sqrMagnitude > 0.0001f)
            vertical = vertical.normalized * verticalSpeed;
        else
            vertical = Vector3.zero;

        Vector3 velocity = horizontal + vertical;

        if (velocity.sqrMagnitude > 0.0001f)
        {
            player.SetVelocity(velocity);
        }
        else
        {
            // No input: stop dead in the air
            player.SetVelocity(Vector3.zero);
        }
    }

    public void Update()
    {
        // Desktop quick-toggle: F key
        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleFlyMode();
        }

        // Optional: VR controller double-tap B (Button2) to toggle fly
        if (allowControllerToggle && Input.GetButtonDown("Oculus_CrossPlatform_Button2"))
        {
            float now = Time.time;
            if (now - _lastButton2TapTime <= doubleTapWindow)
            {
                // Detected a double-tap
                ToggleFlyMode();
                // Reset so we don't instantly chain into another toggle
                _lastButton2TapTime = -999f;
            }
            else
            {
                // First tap (or too slow), just record time
                _lastButton2TapTime = now;
            }
        }

        FlyUpdate();
    }

    // Input events — only affect movement when fly mode is active

    public override void InputMoveHorizontal(float value, UdonInputEventArgs args)
    {
        if (!isFlying) return;
        moveHorizontal = value;
    }

    public override void InputMoveVertical(float value, UdonInputEventArgs args)
    {
        if (!isFlying) return;
        moveVertical = value;
    }

    public override void InputJump(bool value, UdonInputEventArgs args)
    {
        if (!isFlying) return;
        // value = true when jump is held, false when released
        upHeld = value;
    }

    // NOTE:
    // We intentionally do NOT override InputUse or InputGrab here,
    // so trigger/use and grip remain free for menus & interactions.
}
