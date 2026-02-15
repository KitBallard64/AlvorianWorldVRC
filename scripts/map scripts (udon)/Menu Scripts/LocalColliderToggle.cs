using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

public class LocalColliderToggle : UdonSharpBehaviour
{
    [Header("Objects to toggle visibility / collision")]
    public GameObject[] localObjects;   // These colliders/objects will toggle on/off locally only

    [Header("Button visuals")]
    public Button toggleButton;
    public Image buttonColor;           // Button background image
    public Color onColor = Color.green;
    public Color offColor = Color.red;

    private bool isOn = false;

    void Start()
    {
        UpdateVisuals();
    }

    public void ToggleObjects()
    {
        isOn = !isOn;

        foreach (GameObject obj in localObjects)
        {
            if (obj != null)
                obj.SetActive(isOn);
        }

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (buttonColor != null)
            buttonColor.color = isOn ? onColor : offColor;
    }
}
