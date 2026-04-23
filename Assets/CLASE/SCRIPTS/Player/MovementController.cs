using UnityEngine;
using Fusion;
using System;
using System.Reflection;

public class MovementController : NetworkBehaviour
{
    private NetworkCharacterController fusionController;
    private Component simpleKccComponent;
    private MethodInfo simpleKccMoveMethodOneParam;
    private MethodInfo simpleKccMoveMethodTwoParams;

    [SerializeField] private Animator animator;

    private InputInfo input;

    [SerializeField] private float walkSpeed = 5.5f;
    [SerializeField] private float runSpeed = 7.7f;
    [SerializeField] private float crouchSpeed = 3.9f;

    private bool isDead;

    public override void Spawned()
    {
        fusionController = GetComponent<NetworkCharacterController>();
        CacheSimpleKccApi();
    }

    public override void FixedUpdateNetwork()
    {
        if (isDead) return;

        if (GetInput(out input))
        {
            Movement();
            Animation();
        }
    }

    public void OnDeath()
    {
        isDead = true;

    }

    public void OnRespawn()
    {
        isDead = false;

    }

    private void Animation()
    {
        if (animator == null) return;

        animator.SetBool("IsWalking", input.isMoving);
        animator.SetBool("IsRunning", input.isRunInputPressed);
        animator.SetFloat("WalkingZ", input.playerPos.y);
        animator.SetFloat("WalkingX", input.playerPos.x);
    }

    private void Movement()
    {
        Vector3 inputDirection = new Vector3(input.playerPos.x, 0f, input.playerPos.y);
        if (inputDirection.sqrMagnitude > 1f)
            inputDirection.Normalize();

        float speed = Speed(input);

        CameraController cam = GetComponentInChildren<CameraController>();
        if (cam == null) cam = FindObjectOfType<CameraController>();

        Quaternion yawRotation = Quaternion.Euler(0f, cam != null ? cam.YawAngle : 0f, 0f);
        Vector3 worldDirection = yawRotation * inputDirection * speed;

        if (fusionController != null)
        {
            fusionController.Move(worldDirection);
            return;
        }

        if (simpleKccComponent != null)
        {
            if (simpleKccMoveMethodOneParam != null)
            {
                simpleKccMoveMethodOneParam.Invoke(simpleKccComponent, new object[] { worldDirection });
                return;
            }

            if (simpleKccMoveMethodTwoParams != null)
            {
                simpleKccMoveMethodTwoParams.Invoke(simpleKccComponent, new object[] { worldDirection, 0f });
            }
        }
    }

    private float Speed(InputInfo info)
    {
        return info.isMovingBackwards || info.isMovingOnXAxis ? walkSpeed :
            info.isRunInputPressed ? runSpeed : walkSpeed;
    }

    private void CacheSimpleKccApi()
    {
        simpleKccComponent = null;
        simpleKccMoveMethodOneParam = null;
        simpleKccMoveMethodTwoParams = null;

        Component[] components = GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component == null) continue;

            Type type = component.GetType();
            if (type.Name == "SimpleKCC")
            {
                simpleKccComponent = component;

                MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
                foreach (var method in methods)
                {
                    if (method.Name == "Move")
                    {
                        var parameters = method.GetParameters();

                        if (parameters.Length == 1 && parameters[0].ParameterType == typeof(Vector3))
                            simpleKccMoveMethodOneParam = method;
                        else if (parameters.Length == 2 && parameters[0].ParameterType == typeof(Vector3))
                            simpleKccMoveMethodTwoParams = method;
                    }
                }
                break;
            }
        }
    }
}