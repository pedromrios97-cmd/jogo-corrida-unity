using UnityEngine;

public class Upgrade : MonoBehaviour
{
    public Transform motor;
    public Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        motor.LookAt(cam.transform.transform);
    }
}
