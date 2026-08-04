using System;
using System.Collections.Generic;
using UnityEngine;

public class Car_Controller : MonoBehaviour
{
    public enum Axel
    {
        Front,
        Rear
    }

    public enum LightType
    {
        Headlight,
        InternalLight,
        Taillight
    }

    // Estado explícito do motor — substitui a inferência por isPlaying
    private enum EngineState
    {
        Off,
        Starting,
        Idle,
        Driving
    }

    [Serializable]
    public struct Wheel
    {
        public GameObject wheelMesh;
        public WheelCollider wheelCollider;
        public Axel axel;
    }

    [Serializable]
    public struct CarLight
    {
        public Light light;
        public LightType type;
    }

    [Header("Driving")]
    public float maxAcceleration = 30f;
    public float brakeAcceleration = 50f;
    public float turnSensitivity = 1f;
    public float maxSteerAngle = 30f;
    public Vector3 centerOfMass;

    [Header("References")]
    public List<Wheel> wheels;
    public List<CarLight> lights;
    [SerializeField] public Transform playerSeat;

    [Header("Audio")]
    public AudioSource carAudio;
    public AudioClip startupAudio;
    public AudioClip idleAudio;
    public AudioClip onAudio;
    public AudioClip offAudio;
    public AudioClip maxRPMAudio;

    [Header("Runtime")]
    public player player;
    public bool carStarted;

    private Rigidbody carRB;
    private float moveInput;
    private float steerInput;
    private InputSystem_Actions actions;
    private EngineState engineState = EngineState.Off;
    private float startupTimer;

    public Transform PlayerSeat => playerSeat;

    void Start()
    {
        carRB = GetComponent<Rigidbody>();
        carRB.centerOfMass = centerOfMass;

        if (playerSeat == null)
            playerSeat = transform.Find("PlayerSeat");
    }

    void Update()
    {
        if (player != null)
        {
            actions = player.actions;
        }

        GetInputs();
        Animate();
    }

    private void FixedUpdate()
    {
        Move();
        Brake();
        Steer();

        StabilizeVehicle();
        AntiRoll();

        UpdateEngineAudio();
    }

    void GetInputs()
    {
        if (player != null && actions != null && carStarted)
        {
            moveInput = actions.driving.move.ReadValue<float>();
            steerInput = actions.driving.direction.ReadValue<Vector2>().x;
        }
        else
        {
            moveInput = 0;
            steerInput = 0;
        }
    }

    void Move()
    {
        if (!carStarted) return;

        foreach (Wheel wheel in wheels)
        {
            wheel.wheelCollider.motorTorque = moveInput * 600 * maxAcceleration * Time.deltaTime;
        }
    }

    void Steer()
    {
        foreach (Wheel wheel in wheels)
        {
            if (wheel.axel == Axel.Front)
            {
                float steerAngle = steerInput * turnSensitivity * maxSteerAngle;
                wheel.wheelCollider.steerAngle = Mathf.Lerp(wheel.wheelCollider.steerAngle, steerAngle, 0.6f);
            }
        }
    }

    void Brake()
    {
        bool isBraking = player != null && actions != null && actions.driving.brake.IsPressed();

        foreach (Wheel wheel in wheels)
        {
            wheel.wheelCollider.brakeTorque = isBraking ? 300 * brakeAcceleration * Time.deltaTime : 0;
        }

        foreach (CarLight light in lights)
        {
            if (light.type == LightType.Taillight)
            {
                light.light.intensity = isBraking ? 4f : 1f;
            }
        }
    }

    void UpdateEngineAudio()
    {
        switch (engineState)
        {
            case EngineState.Off:
                break;

            case EngineState.Starting:
                startupTimer -= Time.deltaTime;
                if (startupTimer <= 0f)
                    TransitionTo(EngineState.Idle);
                break;

            case EngineState.Idle:
                if (moveInput != 0f)
                    TransitionTo(EngineState.Driving);
                break;

            case EngineState.Driving:
                if (moveInput == 0f)
                    TransitionTo(EngineState.Idle);
                break;
        }
    }

    void TransitionTo(EngineState newState)
    {
        engineState = newState;

        switch (newState)
        {
            case EngineState.Starting:
                carAudio.Stop();
                carAudio.loop = false;
                carAudio.PlayOneShot(startupAudio);
                startupTimer = startupAudio != null ? startupAudio.length : 1f;
                break;

            case EngineState.Idle:
                carAudio.Stop();
                carAudio.clip = idleAudio;
                carAudio.loop = true;
                carAudio.Play();
                break;

            case EngineState.Driving:
                carAudio.Stop();
                carAudio.clip = onAudio;
                carAudio.loop = true;
                carAudio.Play();
                break;

            case EngineState.Off:
                carAudio.Stop();
                if (offAudio != null)
                    carAudio.PlayOneShot(offAudio);
                break;
        }
    }

    void StabilizeVehicle()
    {
        Vector3 localVelocity = transform.InverseTransformDirection(carRB.linearVelocity);
        float lateralSpeed = localVelocity.x;

        if (Mathf.Abs(lateralSpeed) > 1f)
        {
            Vector3 counterForce = -transform.right * lateralSpeed * 5f;
            carRB.AddForce(counterForce, ForceMode.Acceleration);
        }
    }

    void AntiRoll()
    {
        Vector3 localAngular = transform.InverseTransformDirection(carRB.angularVelocity);
        float roll = localAngular.z;
        carRB.AddRelativeTorque(0, 0, -roll * 500f);
    }

    public void StartCar()
    {
        if (carStarted) return;

        carStarted = true;

        foreach (CarLight light in lights)
            light.light.enabled = true;

        TransitionTo(EngineState.Starting);
    }

    public void StopCar()
    {
        if (!carStarted) return;

        carStarted = false;

        foreach (CarLight light in lights)
            light.light.enabled = false;

        TransitionTo(EngineState.Off);
    }

    void Animate()
    {
        foreach (Wheel wheel in wheels)
        {
            wheel.wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
            wheel.wheelMesh.transform.SetPositionAndRotation(pos, rot);
        }
    }
}