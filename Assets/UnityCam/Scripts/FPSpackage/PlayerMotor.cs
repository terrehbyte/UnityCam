using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMotor : MonoBehaviour, IInputQueriable
{
    protected Rigidbody attachedRigidbody;
    protected Collider attachedCollider;

    protected bool wishJump = false;

    public bool isGrounded { get; set; }

    public float jumpForce = 2f;

    public float minGroundAngle = 60.0f;
    public float groundFriction = 3;
    protected Vector3 avgGroundNormal;

    [HideInInspector]
    public Vector3 targetVelocity;
    public Vector3 lastVelocity { get; protected set;}

    public float groundAcceleration = 300.0f;
    public float maxGroundVelocity = 10.0f;

    public float airAcceleration = 100.0f;
    public float maxAirVelocity = 15.0f;

    [System.Serializable]
    protected struct PlayerMovementInput
    {
        public float moveForward;
        public float moveRight;

        public bool jump;
    }
    protected PlayerMovementInput movementInput;

    public void AddMoveForward(float val)
    {
        movementInput.moveForward += val;
    }

    public void AddMoveRight(float val)
    {
        movementInput.moveRight += val;
    }

    protected void ClearMovementInput()
    {
        // TODO: refactor this to avoid creating garbage
        movementInput = new PlayerMovementInput();
    }

    // Returns the final velocity of the player after accelerating in a certain direction.
    protected Vector3 Accelerate(Vector3 wishDir, Vector3 prevVelocity, float accelerate, float maxVelocity)
    {
        float projectVel = Vector3.Dot(prevVelocity, wishDir);
        float accelerationVel = accelerate * Time.fixedDeltaTime;  // match fixed time step

        // cap acceleration vector
        if (projectVel + accelerationVel > maxVelocity)
            accelerationVel = maxVelocity - projectVel;

        return prevVelocity + wishDir * accelerationVel;
    }

    // Returns the final velocity of the player after accelerating on the ground.
    protected Vector3 MoveGround(Vector3 wishDir, Vector3 prevVelocity)
    {
        // apply friction if was moving
        float speed = prevVelocity.magnitude;
        if (speed != 0) // To avoid divide by zero errors
        {
            // decelerate due to friction
            float drop = speed * groundFriction * Time.fixedDeltaTime;

            // scale the velocity based on friction.
            prevVelocity *= Mathf.Max(speed - drop, 0) / speed; // be careful to not drop below zero
            
        }

        return Accelerate(wishDir, prevVelocity, groundAcceleration, maxGroundVelocity);
    }
    
    // Returns the final velocity of the player after accelerating mid-air.
    protected Vector3 MoveAir(Vector3 accelDir, Vector3 prevVelocity)
    {
        return Accelerate(accelDir, prevVelocity, airAcceleration, maxAirVelocity);
    }

    protected Vector3 Jump(Vector3 prevVelocity)
    {
        wishJump = false;
        return isGrounded ? prevVelocity + (Vector3.up * jumpForce) : prevVelocity;
    }

    // Returns true if the player is grounded.
    bool CheckGrounded()
    {
        RaycastHit hitInfo;

        const float groundRayLength = 1.1f;
        Debug.DrawRay(transform.position, Vector3.down * groundRayLength, Color.green);

        // TODO: adjust raycast by collider size
        if(Physics.Raycast(new Ray(transform.position, Vector3.down), out hitInfo, groundRayLength))
        {
            float angle = Vector3.Angle(Vector3.forward, hitInfo.normal);
            return angle > minGroundAngle;
        }

        return false;
    }

    public void BindInputs()
    {

    }

    public void ReceiveInput(InputData inputs)
    {
        
    }

    public void UpdateMovement()
    {
        // determine if the player is grounded
        isGrounded = CheckGrounded();

        // retrieve player input
        Vector3 playerInput = new Vector3(movementInput.moveRight,
                                          0,
                                          movementInput.moveForward);
        ClearMovementInput();
        playerInput.Normalize();

        // transform player movement into world-space
        playerInput = transform.TransformVector(playerInput);

        // determine final velocity
        Vector3 finalPlayerVelocity = isGrounded ? MoveGround(playerInput, attachedRigidbody.velocity) :
                                                   MoveAir(playerInput, attachedRigidbody.velocity);
        
        // handle jump, if requested                                    
        if (wishJump)
            finalPlayerVelocity = Jump(finalPlayerVelocity);

        // assign final velocity
        targetVelocity = lastVelocity = finalPlayerVelocity;
    }

    void Reset()
    {
        attachedRigidbody = GetComponent<Rigidbody>();
        attachedCollider = GetComponent<Collider>();
    }

    void Start()
    {
        attachedRigidbody = attachedRigidbody == null ? GetComponent<Rigidbody>() : attachedRigidbody;
        attachedCollider = attachedCollider == null ? GetComponent<Collider>() : attachedCollider;
    }

    void FixedUpdate()
    {
        attachedRigidbody.velocity = targetVelocity;
    }
}
