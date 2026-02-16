using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using TMPro;
using VRC.SDK3.Persistence;

public class PlayerOptionsController : UdonSharpBehaviour
{
    [Header("References")]
    [Tooltip("Fly controller that has the allowControllerToggle setting.")]
    public DMFlyController flyController;

    [Tooltip("Collider for the spawn room speed boost trigger.")]
    public Collider spawnBoostCollider;

    [Tooltip("Dice HUD that shows roll and system messages.")]
    public PublicDiceRollHUD diceHud;

    [Header("Double-Tap Fly UI")]
    [Tooltip("Root object for the Double-Tap Fly option button (will be hidden on Desktop).")]
    public GameObject doubleTapOptionRoot;

    [Tooltip("TMP Text object to display the Double-Tap Fly checkmark.")]
    public TextMeshProUGUI doubleTapCheckmarkText;

    [Header("Button Flip UI")]
    [Tooltip("Root object for the Button Flip option (will be hidden on Desktop or when double-tap is OFF).")]
    public GameObject buttonFlipOptionRoot;

    [Tooltip("TMP Text object to display the Button Flip checkmark.")]
    public TextMeshProUGUI buttonFlipCheckmarkText;

    [Header("Spawn Speed Boost UI")]
    [Tooltip("Root object for the Spawn Speed Boost option.")]
    public GameObject spawnBoostOptionRoot;

    [Tooltip("TMP Text object to display the Spawn Speed Boost checkmark.")]
    public TextMeshProUGUI spawnBoostCheckmarkText;

    [Header("HUD Visibility UI")]
    [Tooltip("Root object for the HUD visibility option row.")]
    public GameObject hudOptionRoot;

    [Tooltip("TMP Text object to display the HUD visibility checkmark.")]
    public TextMeshProUGUI hudCheckmarkText;

    // Persistence keys (unique per setting)
    private const string DOUBLE_TAP_KEY = "optDoubleTapFly";
    private const string BUTTON_FLIP_KEY = "optButtonFlip";
    private const string SPAWN_BOOST_KEY = "optSpawnSpeedBoost";
    private const string HUD_VISIBLE_KEY = "optHudVisible";

    // Local cached state for the toggles
    private bool _doubleTapEnabled = false;   // default OFF
    private bool _buttonFlipped = false;      // default OFF (use Button2/B)
    private bool _spawnBoostEnabled = true;   // default ON
    private bool _hudVisible = true;          // default ON

    private VRCPlayerApi _localPlayer;

    private const string CHECKMARK = "✓";
    private const string EMPTY = "";

    private void Start()
    {
        _localPlayer = Networking.LocalPlayer;

        // Set up initial UI visibility and desktop safety
        UpdateVrVisibility();

        // Apply current states (defaults until OnPlayerRestored runs)
        ApplyDoubleTapState();
        ApplyButtonFlipState();
        ApplySpawnBoostState();
        ApplyHudState();
    }

    private void UpdateVrVisibility()
    {
        if (_localPlayer == null)
            _localPlayer = Networking.LocalPlayer;

        bool inVR = _localPlayer != null && _localPlayer.IsUserInVR();

        // Double-tap option is VR-only
        if (doubleTapOptionRoot != null)
            doubleTapOptionRoot.SetActive(inVR);

        // Button flip option is VR-only AND only visible when double-tap is enabled
        if (buttonFlipOptionRoot != null)
            buttonFlipOptionRoot.SetActive(inVR && _doubleTapEnabled);

        // Spawn boost and HUD options are always visible
        if (spawnBoostOptionRoot != null)
            spawnBoostOptionRoot.SetActive(true);

        if (hudOptionRoot != null)
            hudOptionRoot.SetActive(true);

        // If not in VR, force double-tap and button flip OFF
        if (!inVR)
        {
            _doubleTapEnabled = false;
            ApplyDoubleTapState();
            PlayerData.SetInt(DOUBLE_TAP_KEY, 0);
            
            ResetButtonFlipState();
        }
    }

    // --- Double-Tap Fly ---

    public void ToggleDoubleTapFly()
    {
        if (_localPlayer == null)
            _localPlayer = Networking.LocalPlayer;

        bool inVR = _localPlayer != null && _localPlayer.IsUserInVR();

        // Desktop can't toggle this; force it off
        if (!inVR)
        {
            _doubleTapEnabled = false;
            ApplyDoubleTapState();
            PlayerData.SetInt(DOUBLE_TAP_KEY, 0);
            return;
        }

        _doubleTapEnabled = !_doubleTapEnabled;

        // If disabling double-tap, also disable button flip
        if (!_doubleTapEnabled)
        {
            ResetButtonFlipState();
        }

        ApplyDoubleTapState();
        PlayerData.SetInt(DOUBLE_TAP_KEY, _doubleTapEnabled ? 1 : 0);
        
        // Update button flip option visibility
        UpdateVrVisibility();
    }

    private void ApplyDoubleTapState()
    {
        if (flyController != null)
        {
            // Enable/disable the double-tap feature
            flyController.SetDoubleTapEnabled(_doubleTapEnabled);
            // Also sync the button flip state to ensure DMFlyController stays synchronized
            // (button flip only matters when double-tap is enabled, so they're applied together)
            flyController.SetButtonMode(_buttonFlipped);
        }

        if (doubleTapCheckmarkText != null)
            doubleTapCheckmarkText.text = _doubleTapEnabled ? CHECKMARK : EMPTY;
    }

    // --- Button Flip ---

    private void ResetButtonFlipState()
    {
        _buttonFlipped = false;
        ApplyButtonFlipState();
        PlayerData.SetInt(BUTTON_FLIP_KEY, 0);
    }

    public void ToggleButtonFlip()
    {
        if (_localPlayer == null)
            _localPlayer = Networking.LocalPlayer;

        bool inVR = _localPlayer != null && _localPlayer.IsUserInVR();

        if (!inVR)
        {
            // Only reset if not in VR
            ResetButtonFlipState();
            return;
        }

        // If we get here, we're in VR. Only allow toggling if double-tap is enabled
        if (!_doubleTapEnabled)
        {
            // Don't toggle, but also don't reset - just return silently
            return;
        }

        _buttonFlipped = !_buttonFlipped;

        ApplyButtonFlipState();
        PlayerData.SetInt(BUTTON_FLIP_KEY, _buttonFlipped ? 1 : 0);
    }

    private void ApplyButtonFlipState()
    {
        if (flyController != null)
        {
            // Re-sync double-tap enabled state to ensure it stays active when flipping buttons
            flyController.SetDoubleTapEnabled(_doubleTapEnabled);
            // Set which button to use: when flipped, use Button1 (A); otherwise use Button2 (B)
            flyController.SetButtonMode(_buttonFlipped);
        }

        if (buttonFlipCheckmarkText != null)
            buttonFlipCheckmarkText.text = _buttonFlipped ? CHECKMARK : EMPTY;
    }

    // --- Spawn Speed Boost (collider on/off) ---

    public void ToggleSpawnBoost()
    {
        _spawnBoostEnabled = !_spawnBoostEnabled;

        ApplySpawnBoostState();
        PlayerData.SetInt(SPAWN_BOOST_KEY, _spawnBoostEnabled ? 1 : 0);
    }

    private void ApplySpawnBoostState()
    {
        if (spawnBoostCollider != null)
            spawnBoostCollider.enabled = _spawnBoostEnabled;

        if (spawnBoostCheckmarkText != null)
            spawnBoostCheckmarkText.text = _spawnBoostEnabled ? CHECKMARK : EMPTY;
    }

    // --- HUD Visibility ---

    public void ToggleHudVisible()
    {
        _hudVisible = !_hudVisible;

        ApplyHudState();
        PlayerData.SetInt(HUD_VISIBLE_KEY, _hudVisible ? 1 : 0);
    }

    private void ApplyHudState()
    {
        // Hide/show the TMP display (whole object, not just text)
        if (diceHud != null && diceHud.outputField != null)
            diceHud.outputField.gameObject.SetActive(_hudVisible);

        if (hudCheckmarkText != null)
            hudCheckmarkText.text = _hudVisible ? CHECKMARK : EMPTY;
    }

    // --- Persistence integration ---

    public override void OnPlayerRestored(VRCPlayerApi player)
    {
        if (!player.isLocal) return;

        // Double-tap fly
        int savedDouble;
        bool hasDouble = PlayerData.TryGetInt(player, DOUBLE_TAP_KEY, out savedDouble);
        _doubleTapEnabled = hasDouble && (savedDouble != 0);

        // Button flip
        int savedFlip;
        bool hasFlip = PlayerData.TryGetInt(player, BUTTON_FLIP_KEY, out savedFlip);
        _buttonFlipped = hasFlip && (savedFlip != 0);

        // Spawn speed boost (default ON if no saved value)
        int savedSpawn;
        bool hasSpawn = PlayerData.TryGetInt(player, SPAWN_BOOST_KEY, out savedSpawn);
        _spawnBoostEnabled = hasSpawn ? (savedSpawn != 0) : true;

        // HUD visibility (default ON if no saved value)
        int savedHud;
        bool hasHud = PlayerData.TryGetInt(player, HUD_VISIBLE_KEY, out savedHud);
        _hudVisible = hasHud ? (savedHud != 0) : true;

        // Re-apply all settings
        ApplyDoubleTapState();
        ApplyButtonFlipState();
        ApplySpawnBoostState();
        ApplyHudState();

        // Re-run VR/Desktop visibility and desktop safety rules
        UpdateVrVisibility();
    }

    public bool GetDoubleTapFlyEnabled()
    {
        return _doubleTapEnabled;
    }

    public void SetDoubleTapFlyEnabled(bool enabled)
    {
        _doubleTapEnabled = enabled;
        ApplyDoubleTapState();
        PlayerData.SetInt(DOUBLE_TAP_KEY, enabled ? 1 : 0);
    }

    public bool GetSpawnBoostEnabled()
    {
        return _spawnBoostEnabled;
    }

    public void SetSpawnBoostEnabled(bool enabled)
    {
        _spawnBoostEnabled = enabled;
        ApplySpawnBoostState();
        PlayerData.SetInt(SPAWN_BOOST_KEY, enabled ? 1 : 0);
    }

    public bool GetHudVisible()
    {
        return _hudVisible;
    }

    public void SetHudVisible(bool visible)
    {
        _hudVisible = visible;
        ApplyHudState();
        PlayerData.SetInt(HUD_VISIBLE_KEY, visible ? 1 : 0);
    }

    // Optional: call this from ShowOptionsMenu() if you want to refresh when opening
    public void OnOptionsMenuOpened()
    {
        UpdateVrVisibility();
        ApplyDoubleTapState();
        ApplyButtonFlipState();
        ApplySpawnBoostState();
        ApplyHudState();
    }
}
