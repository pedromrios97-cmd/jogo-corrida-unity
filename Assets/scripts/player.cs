using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class player : MonoBehaviour
{
    [Header("Movement")]
    public float playerSpeed;
    public float playerSprint;
    public float currentSpeed;
    public float smoothSpeed;
    public CharacterController playerController;
    Vector3 velocity;

    [Header("Animation")]
    public Animator animm;
    public RuntimeAnimatorController animController;
    public RuntimeAnimatorController drivingController;

    [Header("Camera")]
    public Transform camTransform;
    public float turnSmoothTime;
    float turnSmoothVelocity;
    public CinemachineCamera freeLookCam;
    public CinemachineCamera drivingCam;

    [Header("gravity")]
    public Transform surfaceChack;
    public LayerMask surfaceMask;
    public float surfaceDistance;
    public bool onSurface;
    public float gravity;

    [Header("Driving")]
    public bool isDriving;
    public float enterDistance;
    public Transform playerSeat;
    private GameObject currentCar;
    public GameObject nearestCar;
    public List<GameObject> nearestCars = new List<GameObject>();

    [Header("input")]
    public InputSystem_Actions actions;

    private void Awake()
    {
        actions = new InputSystem_Actions();
        actions.Enable();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        freeLookCam = GameObject.Find("FreeLook Camera").GetComponent<CinemachineCamera>();
        drivingCam = GameObject.Find("driving Camera").GetComponent<CinemachineCamera>();
    }

       void Start()
       {
            playerSpeed = 2f;
            playerSprint = 5f;
            smoothSpeed = 10f;
            currentSpeed = 0.0f;
            turnSmoothTime = 0.1f;
            surfaceDistance = 0.4f;
            gravity = -9.8f;
            enterDistance = 3.0f;
            isDriving = false;
       }

    private void FixedUpdate()
    {
        if (actions.Player.Drive.IsPressed())
        {
            if (isDriving)
            {
                ExiteCar();
            }
            else
            {
                EnterCar();
            }
        }

        if (isDriving) 
        {
            animm.runtimeAnimatorController = drivingController;
            return;
        }
        else
        {
            animm.runtimeAnimatorController = animController;
        }

        Gravity();
        Move();

        Debug.Log(actions.Player.Drive.IsPressed());
    }

    private void EnterCar()
    {
        if(nearestCar != null)
        {
            float distanceToCar = Vector3.Distance(transform.position, nearestCar.transform.position);

            if (distanceToCar <= enterDistance)
            {
                StartCoroutine(EnterCarCoroutine());
            }
        }
    }

    IEnumerator EnterCarCoroutine()
    {
        yield return new WaitForSeconds(0.5f);

        if (currentCar) yield break;

        drivingCam.Priority = 1;

        currentCar = nearestCar;
        isDriving = true;

        currentCar.GetComponent<Car_Controller>().player = this;
        playerSeat = currentCar.GetComponent<Car_Controller>().playerSeat;

        if (!currentCar.GetComponent<Car_Controller>().carStarted)
        {
            currentCar.GetComponent<Car_Controller>().StartCar();
        }

        playerController.enabled = false;
        //playerColiider.enabled = false;

        transform.SetParent(playerSeat);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        currentCar.GetComponent<Car_Controller>().enabled = true;
    }

    private void ExiteCar()
    {
        if(nearestCar != null)
        {
            StartCoroutine(ExitCarCouroutine());
        }
    }

    IEnumerator ExitCarCouroutine()
    {
        yield return new WaitForSeconds(0.5f);

        if (!currentCar) yield break;

        currentCar.GetComponent<Car_Controller>().enabled = false;

        transform.SetParent(null);
        transform.position = currentCar.transform.position + currentCar.transform.right * -2.0f;

        playerController.enabled = true;
        //playerCollider.enabled = true;

        playerSeat = null;

        currentCar.GetComponent<Car_Controller>().player = null;

        currentCar = null;
        isDriving = false;
        nearestCar = null;

        drivingCam.Priority = -1;
    }

    private void Gravity()
    {
        onSurface = Physics.CheckSphere(surfaceChack.position, surfaceDistance, surfaceMask);

        if(onSurface && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.fixedDeltaTime;
        playerController.Move(playerSpeed * Time.fixedDeltaTime * velocity.normalized);

        animm.SetBool("OnSurface", onSurface);
    }

    private void Move()
    {
        float inputX = actions.Player.Move.ReadValue<Vector2>().x;
        float inputZ = actions.Player.Move.ReadValue<Vector2>().y;
        float moveSpeed;

        Vector3 direction = new Vector3(inputX, 0, inputZ).normalized;
        float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);

        moveSpeed = actions.Player.Sprint.IsPressed() ? playerSprint : playerSpeed;

        Vector3 moveDirection = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;

        if (direction.magnitude >= 0.1f)
        {
            transform.rotation = Quaternion.Euler(0,angle, 0);
            playerController.Move(moveSpeed * Time.fixedDeltaTime * moveDirection.normalized);
            currentSpeed = Mathf.Lerp(currentSpeed, moveSpeed, smoothSpeed * Time.fixedDeltaTime);
        }
        else
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, smoothSpeed * Time.fixedDeltaTime);
        }

        animm.SetFloat("velocity", currentSpeed);
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            if (!nearestCars.Contains(other.gameObject))
            {
                nearestCars.Add(other.gameObject);
            }

            float nearestDistance = Mathf.Infinity;
            nearestCar = null;

            foreach (GameObject car in nearestCars)
            {
                float distanceToCar = Vector3.Distance(transform.position, car.transform.position);

                if(distanceToCar < nearestDistance)
                {
                    nearestDistance = distanceToCar;
                    nearestCar = car;
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            nearestCars.Remove(other.gameObject);
        }

        if(nearestCars.Count <= 0)
        {
            nearestCar = null;
        }
    }
}
