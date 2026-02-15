using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRC.SDKBase;
using VRC.Udon;

public class HiddenRollLogic : UdonSharpBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI resultText;

    [Header("Settings")]
    public float resultDisplayTime = 5f;

    private float clearTimer = 0f;
    private bool resultVisible = false;

    void Update()
    {
        if (resultVisible)
        {
            clearTimer -= Time.deltaTime;
            if (clearTimer <= 0f)
            {
                resultText.text = "Results:";
                resultVisible = false;
            }
        }
    }

    public void RollD20()
    {
        int roll = Random.Range(1, 21); // 1 to 20 inclusive
        resultText.text = $"Results: {roll}";
        ResetResultTimer();
    }

    public void RollDoubleD20()
    {
        int roll1 = Random.Range(1, 21);
        int roll2 = Random.Range(1, 21);
        resultText.text = $"Results: {roll1}, {roll2}";
        ResetResultTimer();
    }

    private void ResetResultTimer()
    {
        clearTimer = resultDisplayTime;
        resultVisible = true;
    }
}
