
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;


[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class PlayerVolumeController : UdonSharpBehaviour
{
    [Header("Networked Values")]
    [UdonSynced, SerializeField] private byte _volumeControlFlag = 2;
    private byte _cachedVolumeControlFlag = 2;

    [Header("Configurable Values")]
    [SerializeField] private Toggle[] toggleList;
    [Space(10)]

    [SerializeField] private float _whisperVolumeMultiplier = 0.5f;
    [SerializeField] private float _whisperVolumeMinDistance = 4.0f;
    [SerializeField] private float _whisperVolumeMaxDistance = 5.0f;
    [Space(5)]

    [SerializeField] private float _normalVolumeMultiplier = 2.0f;
    [SerializeField] private float _normalVolumeMinDistance = 15.0f;
    [SerializeField] private float _normalVolumeMaxDistance = 10.0f;
    [Space(5)]

    [SerializeField] private float _loudVolumeMultiplier = 5.0f;
    [SerializeField] private float _loudVolumeMinDistance = 25.0f;
    [SerializeField] private float _loudVolumeMaxDistance = 15.0f;


    [Header("Instance Values")]
    [SerializeField] private VRCPlayerApi attachedPlayer = null;

    [Header("Debug Flag")]
    [SerializeField] private bool _debugFlag = false;


    private const int WhisperFlag = 0;
    private const int NormalFlag = 1;
    private const int LoudFlag = 2;


    public void Start()
    {
        if (_debugFlag)
        {
            Debug.LogError("GlitchyDev Says: PlayerVolumeController started! I am setting the initial volume control flag to Normal.");
        }
        attachedPlayer.SetVoiceDistanceNear(_normalVolumeMinDistance);
        attachedPlayer.SetVoiceDistanceFar(_normalVolumeMaxDistance);
        attachedPlayer.SetVoiceGain(_normalVolumeMultiplier);
    }

    public void OnToggleChanged()
    {
        if (_debugFlag)
        {
            Debug.LogError("GlitchyDev Says: Someone pushed a toggle! I am going to set the volume control flag based on the selected toggle.");
        }
        // Read the value
        int newToggleValue = -1;
        for (int i = 0; i < toggleList.Length; i++)
        {
            if (toggleList[i].isOn)
            {
                newToggleValue = i;
            }
        }

        if (newToggleValue != -1)
        {
            if (_debugFlag)
            {
                Debug.LogError("GlitchyDev Says: Selected toggle index: " + newToggleValue);
            }
            _volumeControlFlag = (byte)newToggleValue;
            RequestSerialization();
        }
        else
        {
            Debug.LogError("GlitchyDev Says: No toggle is selected! Please select a toggle to set the volume control flag.");
        }


    }


    public override void OnDeserialization()
    {
        Debug.Log("GlitchyDev Says: OnDeserialization called");

        if (_cachedVolumeControlFlag != _volumeControlFlag)
        {
            Debug.Log("GlitchyDev Says: The value has changed! Updating the player's voice settings.");
            if (attachedPlayer == null)
            {
                if (_debugFlag)
                {
                    Debug.LogError("GlitchyDev Says: Attached player is null! I am going to set it to the owner of this game object.");
                }
                attachedPlayer = Networking.GetOwner(gameObject);
            }


            Debug.Log("GlitchyDev Says: Ok I'm going to set the voice settings based on the volume control flag: " + _volumeControlFlag);

            switch (_volumeControlFlag)
            {
                case 0:
                    Debug.Log("GlitchyDev Says: Setting voice settings to Whisper mode.");
                    attachedPlayer.SetVoiceDistanceNear(_whisperVolumeMinDistance);
                    attachedPlayer.SetVoiceDistanceFar(_whisperVolumeMaxDistance);
                    attachedPlayer.SetVoiceGain(_whisperVolumeMultiplier);
                    break;
                case 1:
                    Debug.Log("GlitchyDev Says: Setting voice settings to Normal mode.");
                    attachedPlayer.SetVoiceDistanceNear(_normalVolumeMinDistance);
                    attachedPlayer.SetVoiceDistanceFar(_normalVolumeMaxDistance);
                    attachedPlayer.SetVoiceGain(_normalVolumeMultiplier);
                    break;
                case 2:
                    Debug.Log("GlitchyDev Says: Setting voice settings to Loud mode.");
                    attachedPlayer.SetVoiceDistanceNear(_loudVolumeMinDistance);
                    attachedPlayer.SetVoiceDistanceFar(_loudVolumeMaxDistance);
                    attachedPlayer.SetVoiceGain(_loudVolumeMultiplier);
                    break;
                default:
                    Debug.LogError("Hey! GlitchyDev says you messed up and the flag is not what it should be!");
                    break;
            }
            _cachedVolumeControlFlag = _volumeControlFlag;
        }
    }
}

