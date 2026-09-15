using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class speedometer : MonoBehaviour
{
    public Rigidbody source;
    public bool horizontaOnly = false;

    public float unitMultiplier = 3.6f;

    public TMP_Text label;
    public string numberFormat = "0";
    public string suffix = "km/h";

    public RectTransform needle;
    public float maxSpeed = 200f;
    public float angleAtZero = 120f;
    public float angleAtMax = -120f;

    public float speed {  get; private set; }



    // Update is called once per frame
    void Update()
    {
        if (source == null) return;
        
        Vector3 velocity = source.linearVelocity;

        if (horizontaOnly) velocity.y = 0f;

        speed = velocity.magnitude * unitMultiplier;

        if (label != null)
        {
            label.text = speed.ToString(numberFormat) + suffix;
        }

        if (needle != null)
        {
            float t = maxSpeed > 0f ? Mathf.Clamp01(speed / maxSpeed) : 0f;
            float angle = Mathf.Lerp(angleAtZero, angleAtMax, t);
            needle.localEulerAngles = new Vector3(0f, 0f, angle);
        }
    }
}
