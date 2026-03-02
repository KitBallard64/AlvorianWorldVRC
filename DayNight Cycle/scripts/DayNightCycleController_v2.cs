using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Serialization;

public class DayNightCycleController_v2 : UdonSharpBehaviour
{
    [Header("UI components")]
    [Tooltip("Requires a canvas")]
    public Slider SpeedSlider;
    public Slider TimeSlider;
    public Toggle LocalToggle;

    [Header("Time")]
    [UdonSynced] public float SetTime = 0.2f;
    [UdonSynced] public int syncid = 0;
    public int lastreceivedid = 0;
    [UdonSynced] public float SetSpeed = 1 / 600f;

    // 0=Morning,1=Day,2=Evening,3=Night,4=TimerRunning,5=Bloodmoon
    [UdonSynced] public int SetMode = 1;

    // Authoritative timer state (server-time based)
    [UdonSynced] private bool TimerRunning = false;
    [UdonSynced] private float TimerStartServerTime = 0f; // Networking.GetServerTimeInSeconds() at start
    [UdonSynced] private float TimerStartTime01 = 0.25f;  // Time01 where timer begins (MorningTime)

    [Range(0, 1)]
    public float CurrentTimeOfDay = 0.2f;

    // Used for local mode and any manual non-timer progression you might keep.
    public float Speed = 1 / 600f;

    // For testing you can crank this, but note: authoritative timer uses TimerRunSpeed, not this.
    public float TimeMultiplier = 1f;

    [Header("DM Button Presets")]
    [Range(0, 1)] public float MorningTime = 0.22f;
    [Range(0, 1)] public float DayTime = 0.30f;
    [Range(0, 1)] public float EveningTime = 0.70f;
    [Range(0, 1)] public float NightTime = 0.88f;

    [Tooltip("Speed to use when DM presses Start Timer (time units per second, in 0-1 day scale).")]
    public float TimerRunSpeed = 1f / 600f;

    [Header("DM Button Checkmarks (Images)")]
    public Image CheckMorning;
    public Image CheckDay;
    public Image CheckEvening;
    public Image CheckNight;
    public Image CheckTimer;

    [Header("Bloodmoon")]
    [Tooltip("The bloodmoon mesh object (enabled during bloodmoon)")]
    public GameObject BloodmoonObject1;
    [Tooltip("The regular moon mesh object (disabled during bloodmoon)")]
    public GameObject BloodmoonObject2;
    public Color BloodmoonAmbientColor = new Color(0.8f, 0.1f, 0.1f, 1f);
    [Tooltip("Optional star/moon tint during bloodmoon (desaturated red)")]
    public Color BloodmoonStarColor = new Color(0.6f, 0.2f, 0.2f, 1f);
    [Tooltip("Button text showing On/Off bloodmoon state")]
    public TextMeshProUGUI BloodmoonButtonText;

    [Header("SET Environment Lighting > Source TO Color IN LIGHTING WINDOW!")]
    public Color AmbientColor1;
    public Color AmbientColor2;
    public Color AmbientColor3;
    public float AmbientPoint1 = 0.2f;
    public float AmbientPoint2 = 0.25f;
    public float AmbientPoint3 = 0.35f;

    [Header("Scene object references")]
    public bool UseSun = true;
    [Tooltip("Directional Light")]
    public Light Sun;

    [Tooltip("Color at set point in the cycle")] public Color SunColor1;
    [Tooltip("Color at set point in the cycle")] public Color SunColor2;
    public float SunPoint1 = 0.25f;
    public float SunPoint2 = 0.35f;

    public float SunIntensityPoint1 = 0.23f;
    public float SunIntensityPoint2 = 0.25f;

    [Space]
    [Header("Custom reflection probe for different times of day")]
    public bool UseReflectionProbe = true;
    [FormerlySerializedAs("Probe")] public ReflectionProbe RefProbe;
    public Cubemap DawnCubemap;
    public Cubemap DayCubemap;
    public Cubemap DuskCubemap;
    public Cubemap NightCubemap;

    [Space]
    [Header("Sky objects")]
    public bool UseSky = true;
    [Tooltip("Should include the stars particle system and moon mesh gameobjects. Allows them to rotate in the sky")]
    public GameObject SkyObject;

    [Header("Stars")]
    [SerializeField] private bool UseStars = true;
    public Material StarsMat;
    [Tooltip("A spherical particle system")]
    public GameObject StarsObject;

    [Tooltip("Color at set point in the cycle")] public Color StarColor1;
    [Tooltip("Color at set point in the cycle")] public Color StarColor2;
    public float StarPoint1 = 0.2f;
    public float StarPoint2 = 0.25f;
    public float StarCutoff = 0.3f;

    [Header("Moon")]
    [SerializeField] private bool UseMoon = true;
    public Material MoonMat;

    [Tooltip("Color at set point in the cycle")] public Color MoonColor1;
    [Tooltip("Color at set point in the cycle")] public Color MoonColor2;
    public float MoonPoint1 = 0.2f;
    public float MoonPoint2 = 0.25f;

    [Space]
    [Header("Cloud Materials (Optional)")]
    [SerializeField] private bool UseClouds;
    public Material LowCloud;
    public Material HighCloud;

    [Tooltip("Color at set point in the cycle")] public Color CloudColor1;
    [Tooltip("Color at set point in the cycle")] public Color CloudColor2;
    [Tooltip("Color at set point in the cycle")] public Color CloudColor3;
    public float CloudPoint1 = 0.2f;
    public float CloudPoint2 = 0.25f;
    public float CloudPoint3 = 0.35f;

    float SunInitialIntensity;

    bool local;
    private bool lowCloudNotNull;
    private bool highCloudNotNull;

    private Color c;

    // Optional safety: keep sky position stable even if something tries to move it.
    private Vector3 _skyBasePos;
    private bool _skyBaseCaptured;

    // Prevent programmatic UI updates from triggering OnValueChanged callbacks
    private bool _ignoreUIEvents;

    void Start()
    {
        SunInitialIntensity = Sun != null ? Sun.intensity : 1f;
        UnityEngine.Random.InitState((int)Time.time);

        SetSliderValueWithoutNotify(TimeSlider, CurrentTimeOfDay);
        SetSliderValueWithoutNotify(SpeedSlider, Speed);

        lowCloudNotNull = LowCloud != null;
        highCloudNotNull = HighCloud != null;
        if (!lowCloudNotNull && !highCloudNotNull) UseClouds = false;

        if (UseStars) if (StarsMat == null || StarsObject == null) UseStars = false;
        if (!UseStars && StarsMat == null && StarsObject != null) StarsObject.SetActive(false);

        if (UseMoon) if (MoonMat == null) UseMoon = false;
        if (UseReflectionProbe) if (RefProbe == null) UseReflectionProbe = false;

        if (SkyObject == null) UseSky = false;

        if (UseSky && SkyObject != null)
        {
            _skyBasePos = SkyObject.transform.position;
            _skyBaseCaptured = true;
        }

        // Default state: DAY selected, timer stopped.
        // Only the OWNER initializes synced values so everyone starts consistent.
        if (Networking.LocalPlayer != null && Networking.IsOwner(gameObject))
        {
            local = false;
            SetToggleIsOnWithoutNotify(LocalToggle, false);

            TimerRunning = false;

            CurrentTimeOfDay = DayTime;
            Speed = 0f;

            SetTime = CurrentTimeOfDay;
            SetSpeed = 0f;
            SetMode = 1; // Day

            syncid = GetID();
            RequestSerialization();
        }

        ApplyDMCheckmarksFromMode();
    }

    public void LocalUpdated()
    {
        if (_ignoreUIEvents) return;

        // Purely client-side
        local = LocalToggle != null && LocalToggle.isOn;

        if (!local)
        {
            // Returning to synced mode: apply last received synced state
            CurrentTimeOfDay = SetTime;
            Speed = SetSpeed;
            ApplyDMCheckmarksFromMode();
        }
    }

    public void SliderUpdated()
    {
        if (_ignoreUIEvents) return;

        // Only owner can change synced values
        if (!local && !Networking.IsOwner(gameObject)) return;

        if (!local)
        {
            if (TimeSlider != null) SetTime = TimeSlider.value;
            if (SpeedSlider != null) SetSpeed = SpeedSlider.value;

            // If you change synced sliders manually, stop the authoritative timer.
            TimerRunning = false;

            syncid = GetID();
            RequestSerialization();
        }
        else
        {
            if (TimeSlider != null) CurrentTimeOfDay = TimeSlider.value;
            if (SpeedSlider != null) Speed = SpeedSlider.value;
        }
    }

    private int GetID()
    {
        return UnityEngine.Random.Range(-2000000000, 2000000000);
    }

    private void SetSliderValueWithoutNotify(Slider slider, float value)
    {
        if (slider == null) return;
        _ignoreUIEvents = true;
        slider.SetValueWithoutNotify(value);
        _ignoreUIEvents = false;
    }

    private void SetToggleIsOnWithoutNotify(Toggle toggle, bool isOn)
    {
        if (toggle == null) return;
        _ignoreUIEvents = true;
        toggle.SetIsOnWithoutNotify(isOn);
        _ignoreUIEvents = false;
    }

    public override void OnDeserialization()
    {
        if (!local)
        {
            // If timer is running, Update() computes authoritative time.
            // If timer isn't running, SetTime is authoritative.
            if (!TimerRunning)
            {
                CurrentTimeOfDay = SetTime;
                Speed = SetSpeed;
            }

            SetSliderValueWithoutNotify(TimeSlider, CurrentTimeOfDay);
            SetSliderValueWithoutNotify(SpeedSlider, TimerRunning ? 0f : Speed);
        }

        ApplyDMCheckmarksFromMode();
    }

    void Update()
    {
        // Detect sync changes (legacy mechanism preserved)
        if (syncid != lastreceivedid)
        {
            lastreceivedid = syncid;

            if (!local)
            {
                if (!TimerRunning)
                {
                    CurrentTimeOfDay = SetTime;
                    Speed = SetSpeed;
                }

                SetSliderValueWithoutNotify(TimeSlider, CurrentTimeOfDay);
                SetSliderValueWithoutNotify(SpeedSlider, TimerRunning ? 0f : Speed);

                ApplyDMCheckmarksFromMode();
            }
        }

        // --- Authoritative synced timer ---
        if (!local && TimerRunning)
        {
            float now = (float)Networking.GetServerTimeInSeconds();
            float elapsed = now - TimerStartServerTime;

            float t = TimerStartTime01 + (elapsed * TimerRunSpeed);

            if (t >= NightTime)
            {
                t = NightTime;

                // Only owner stops and serializes the final state.
                if (Networking.IsOwner(gameObject))
                {
                    TimerRunning = false;

                    SetTime = NightTime;
                    SetSpeed = 0f;
                    SetMode = 3; // Night

                    syncid = GetID();
                    RequestSerialization();
                }
            }

            CurrentTimeOfDay = Mathf.Clamp01(t);
            SetSliderValueWithoutNotify(TimeSlider, CurrentTimeOfDay);
        }
        else if (local)
        {
            // Local-only advance (optional)
            if (Speed > 0f)
            {
                CurrentTimeOfDay += (Time.deltaTime * Speed) * TimeMultiplier;

                if (CurrentTimeOfDay >= 1f) CurrentTimeOfDay = 1f;
                if (CurrentTimeOfDay <= 0f) CurrentTimeOfDay = 0f;
            }
        }

        // --- Lighting/Sky application ---
        if (SetMode == 5)
            RenderSettings.ambientLight = BloodmoonAmbientColor;
        else
            RenderSettings.ambientLight = ThreePoint(AmbientPoint1, AmbientPoint2, AmbientPoint3,
                AmbientColor1, AmbientColor2, AmbientColor3);

        if (UseSun && Sun != null)
        {
            if (SetMode == 5)
            {
                Sun.enabled = false;
            }
            else
            {
                Sun.enabled = true;
                Sun.transform.localRotation = Quaternion.Euler((CurrentTimeOfDay * 360f) - 90, 140, 30);
                Sun.color = TwoPoint(SunPoint1, SunPoint2, SunColor1, SunColor2);
                float sunintensity = TwoPointFloat(SunIntensityPoint1, SunIntensityPoint2);
                Sun.intensity = (SunInitialIntensity * sunintensity) + 0.001f;
            }
        }

        if (UseSky && SkyObject != null)
        {
            if (_skyBaseCaptured) SkyObject.transform.position = _skyBasePos;
            SkyObject.transform.localRotation = Quaternion.Euler((CurrentTimeOfDay * 360f) - 90, 140, 30);
        }

        if (UseClouds)
        {
            c = ThreePoint(CloudPoint1, CloudPoint2, CloudPoint3, CloudColor1, CloudColor2, CloudColor3);
            if (lowCloudNotNull) LowCloud.SetColor("_CloudColor", c);
            if (highCloudNotNull) HighCloud.SetColor("_CloudColor", c);
        }

        if (UseStars)
        {
            if (SetMode == 5)
            {
                StarsMat.SetColor("_EmissionColor", BloodmoonStarColor);
                if (!StarsObject.activeInHierarchy) StarsObject.SetActive(true);
            }
            else
            {
                c = TwoPoint(StarPoint1, StarPoint2, StarColor1, StarColor2);
                StarsMat.SetColor("_EmissionColor", c);

                if (c.a <= StarCutoff)
                {
                    if (StarsObject.activeInHierarchy) StarsObject.SetActive(false);
                }
                else if (!StarsObject.activeInHierarchy)
                {
                    StarsObject.SetActive(true);
                }
            }
        }

        if (UseMoon) MoonMat.color = TwoPoint(MoonPoint1, MoonPoint2, MoonColor1, MoonColor2);

        if (UseReflectionProbe) RefProbe.customBakedTexture =
            CycleCubemap(SunPoint1, SunPoint2, NightCubemap, DawnCubemap, DayCubemap, DuskCubemap);
    }

    // ------------------------------------------------------------
    // DM Panel Button Hooks
    // ------------------------------------------------------------

    public void SetMorning() { ApplyPresetAndMode(MorningTime, 0f, 0, false); }
    public void SetDay() { ApplyPresetAndMode(DayTime, 0f, 1, false); }
    public void SetEvening() { ApplyPresetAndMode(EveningTime, 0f, 2, false); }
    public void SetNight() { ApplyPresetAndMode(NightTime, 0f, 3, false); }

    public void StartTimer()
    {
        // Start timer at the predetermined Morning setting
        ApplyPresetAndMode(MorningTime, 0f, 4, true);
    }

    public void SetBloodmoon()
    {
        if (SetMode == 5)
        {
            // Bloodmoon is active — toggle it off by returning to night
            SetNight();
        }
        else
        {
            // Activate bloodmoon
            local = false;
            SetToggleIsOnWithoutNotify(LocalToggle, false);

            VRCPlayerApi lp = Networking.LocalPlayer;
            if (lp != null && !Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(lp, gameObject);
            }

            TimerRunning = false;
            CurrentTimeOfDay = 0.99f; // Just before midnight for maximum darkness
            Speed = 0f;

            SetTime = 0.99f;
            SetSpeed = 0f;
            SetMode = 5; // Bloodmoon
            syncid = GetID();
            RequestSerialization();

            SetSliderValueWithoutNotify(TimeSlider, CurrentTimeOfDay);
            SetSliderValueWithoutNotify(SpeedSlider, 0f);

            ApplyDMCheckmarksFromMode();
        }
    }

    private void ApplyPresetAndMode(float newTime01, float newSpeed, int newMode, bool startTimer)
    {
        local = false;
        SetToggleIsOnWithoutNotify(LocalToggle, false);

        // Ensure ownership so synced vars serialize reliably
        VRCPlayerApi lp = Networking.LocalPlayer;
        if (lp != null && !Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(lp, gameObject);
        }

        newTime01 = Mathf.Clamp01(newTime01);
        if (newSpeed < 0f) newSpeed = 0f;

        CurrentTimeOfDay = newTime01;

        if (startTimer)
        {
            TimerRunning = true;
            TimerStartTime01 = newTime01;
            TimerStartServerTime = (float)Networking.GetServerTimeInSeconds();

            SetSpeed = 0f;
            Speed = 0f;
        }
        else
        {
            TimerRunning = false;

            Speed = newSpeed;
            SetSpeed = newSpeed;
        }

        SetTime = newTime01;
        SetMode = newMode;
        syncid = GetID();
        RequestSerialization();

        SetSliderValueWithoutNotify(TimeSlider, CurrentTimeOfDay);
        SetSliderValueWithoutNotify(SpeedSlider, startTimer ? 0f : Speed);

        ApplyDMCheckmarksFromMode();
    }

    private void ApplyDMCheckmarksFromMode()
    {
        int mode = SetMode;

        SetDMButtonChecks(
            mode == 0,
            mode == 1,
            mode == 2,
            mode == 3,
            mode == 4
        );

        bool bloodmoon = mode == 5;
        if (BloodmoonObject1 != null)
        {
            MeshRenderer mr1 = BloodmoonObject1.GetComponent<MeshRenderer>();
            if (mr1 != null) mr1.enabled = bloodmoon;
        }
        if (BloodmoonObject2 != null)
        {
            MeshRenderer mr2 = BloodmoonObject2.GetComponent<MeshRenderer>();
            if (mr2 != null) mr2.enabled = !bloodmoon;
        }
        if (BloodmoonButtonText != null) BloodmoonButtonText.text = bloodmoon ? "On" : "Off";
    }

    private void SetDMButtonChecks(bool morning, bool day, bool evening, bool night, bool timer)
    {
        if (CheckMorning != null) CheckMorning.enabled = morning;
        if (CheckDay != null) CheckDay.enabled = day;
        if (CheckEvening != null) CheckEvening.enabled = evening;
        if (CheckNight != null) CheckNight.enabled = night;
        if (CheckTimer != null) CheckTimer.enabled = timer;
    }

    public float TwoPointFloat(float p1, float p2)
    {
        float p3 = 1 - p2;
        float p4 = 1 - p1;

        float ret = 1f;

        if (CurrentTimeOfDay < p1) ret = 0f;
        else if (CurrentTimeOfDay < p2) ret = (CurrentTimeOfDay - p1) / (p2 - p1);
        else if (CurrentTimeOfDay < p3) ret = 1f;
        else if (CurrentTimeOfDay < p4) ret = 1 - ((CurrentTimeOfDay - p3) / (p4 - p3));
        else ret = 0f;

        return ret;
    }

    public Color TwoPoint(float p1, float p2, Color c1, Color c2)
    {
        Color ret = new Color(0f, 0f, 0f);

        float p3 = 1 - p2;
        float p4 = 1 - p1;

        if (CurrentTimeOfDay < p1) ret = c1;
        else if (CurrentTimeOfDay < p2)
        {
            float v = (CurrentTimeOfDay - p1) / (p2 - p1);
            ret = Color.Lerp(c1, c2, v);
        }
        else if (CurrentTimeOfDay < p3) ret = c2;
        else if (CurrentTimeOfDay < p4)
        {
            float v = (CurrentTimeOfDay - p3) / (p4 - p3);
            ret = Color.Lerp(c2, c1, v);
        }
        else ret = c1;

        return ret;
    }

    public Color ThreePoint(float p1, float p2, float p3, Color c1, Color c2, Color c3)
    {
        Color ret = new Color(1f, 1f, 1f);

        float p4 = 1 - p3;
        float p5 = 1 - p2;
        float p6 = 1 - p1;

        if (CurrentTimeOfDay < p1) ret = c1;
        else if (CurrentTimeOfDay < p2)
        {
            float v = (CurrentTimeOfDay - p1) / (p2 - p1);
            ret = Color.Lerp(c1, c2, v);
        }
        else if (CurrentTimeOfDay < p3)
        {
            float v = (CurrentTimeOfDay - p2) / (p3 - p2);
            ret = Color.Lerp(c2, c3, v);
        }
        else if (CurrentTimeOfDay < p4) ret = c3;
        else if (CurrentTimeOfDay < p5)
        {
            float v = (CurrentTimeOfDay - p4) / (p5 - p4);
            ret = Color.Lerp(c3, c2, v);
        }
        else if (CurrentTimeOfDay < p6)
        {
            float v = (CurrentTimeOfDay - p5) / (p6 - p5);
            ret = Color.Lerp(c2, c1, v);
        }
        else ret = c1;

        return ret;
    }

    public Cubemap CycleCubemap(float p1, float p2, Cubemap night, Cubemap dawn, Cubemap day, Cubemap dusk)
    {
        Cubemap cubemap = night;

        float p3 = 1 - p2, p4 = 1 - p1;
        p1 -= 0.05f;
        p4 += 0.05f;

        if (CurrentTimeOfDay < p1) { }
        else if (p1 < CurrentTimeOfDay && CurrentTimeOfDay < p2) cubemap = dawn;
        else if (p2 < CurrentTimeOfDay && CurrentTimeOfDay < p3) cubemap = day;
        else if (CurrentTimeOfDay < p4) cubemap = dusk;

        return cubemap;
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    public void TestChangeGUI(object value)
    {
        var casted = ((SerializedProperty)value)?.floatValue;
        var actualVal = Convert.ToSingle(casted);
    }
#endif
}

