using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMotor : MonoBehaviour, IInputQueriable
{
    public struct PlayerTraceHit
    {
        public Collider collider;
        public Vector3 normal;
        public Vector3 point;
    }

    public struct PlayerOverlapData
    {
        public Collider overlappingCollider;
        public Vector3 minimumTranslationVector;
    }

    protected Rigidbody attachedRigidbody;
    protected Collider attachedCollider;

    protected bool wishJump = false;

    public bool isGrounded { get; private set; }

    public float jumpHeight = 2.0f;
    public float jumpForce
    {
        get
        {
            return Mathf.Sqrt(2 * jumpHeight * Physics.gravity.y);
        }
    }

    public float stepOffset = 0.5f;
    public float minGroundDistance = 0.01f;
    public float minGroundAngle = 60.0f;
    public float groundFriction = 3;
    protected Vector3 avgGroundNormal = Vector3.zero;
    protected Vector3 lastGroundPoint;

    [HideInInspector]
    public Vector3 targetVelocity = Vector3.zero;
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

    // Locomotion Methods

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

    float overlapFudge = 0.1f;

    //
    // Environmental Queries
    //

    bool PlayerOverlap(out PlayerOverlapData[] overlapColliders)
    {
        return PlayerOverlap(transform.position, transform.rotation, out overlapColliders);
    }

    bool PlayerOverlap(Vector3 testPosition, Quaternion testRotation, out PlayerOverlapData[] overlapColliders)
    {
        Bounds colliderWorldBounds = attachedCollider.bounds;        
        Collider[] candidates = Physics.OverlapBox(colliderWorldBounds.center, colliderWorldBounds.extents, Quaternion.identity);

        List<PlayerOverlapData> overlapping = new List<PlayerOverlapData>();
        foreach(var testee in candidates)
        {
            Vector3 mtvDir;
            float mtvDist;

            // let A be the player
            // let B be the Collider we're testing against
            bool overlap = Physics.ComputePenetration(attachedCollider, testPosition, testRotation,
                                                      testee, testee.transform.position, testee.transform.rotation,
                                                      out mtvDir, out mtvDist );

            if (overlap && testee != attachedCollider)
            {
                overlapping.Add(new PlayerOverlapData() { overlappingCollider = testee, minimumTranslationVector = mtvDir * mtvDist } );
            }
        }

        overlapColliders = overlapping.ToArray();

        return overlapColliders.Length > 0;
    }

    bool PlayerTrace(Vector3 destination, out PlayerTraceHit traceHit)
    {        
        PlayerOverlapData[] overlappingColliders;
        if(PlayerOverlap(out overlappingColliders)) { traceHit = new PlayerTraceHit() { collider = overlappingColliders[0].overlappingCollider}; return true; }

        Vector3 traceVector = destination - transform.position;
        RaycastHit[] hits = attachedRigidbody.SweepTestAll(traceVector.normalized, traceVector.magnitude, QueryTriggerInteraction.Ignore);

        foreach(var hit in hits)
        {
            // ignore our collider
            if(hit.collider == attachedCollider) { continue; }

            traceHit = new PlayerTraceHit()
            {
                collider = hit.collider,
                normal = hit.normal,
                point = hit.point
            };
            return true;
        }

        traceHit = new PlayerTraceHit();
        return false;
    }

    // Returns true if the player is grounded.
    bool CheckGrounded()
    {
        bool result = false;
        float groundRayLength = GetGroundCheckLength();

        PlayerTraceHit hit;
        if(PlayerTrace(transform.position + (Vector3.down * (groundRayLength * 2)), out hit))
        {
            avgGroundNormal = hit.normal;
            lastGroundPoint = hit.point;
            result = Vector3.Angle(Vector3.forward, hit.normal) > minGroundAngle;
        }
        else
        {
            avgGroundNormal = Vector3.zero;
        }
        
        return result;
    }

    public void BindInputs()
    {
        // todo
    }

    public void ReceiveInput(InputData inputs)
    {
        // todo
    }

    public void UpdateTargetVelocity()
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

        if(playerInput.magnitude > 0.0f)
            playerInput = Vector3.ProjectOnPlane(playerInput, avgGroundNormal);

        // determine final velocity
        Vector3 finalPlayerVelocity = isGrounded ? MoveGround(playerInput, attachedRigidbody.velocity) :
                                                   MoveAir(playerInput, attachedRigidbody.velocity);

        // handle jump, if requested                                    
        if (wishJump)
            finalPlayerVelocity = Jump(finalPlayerVelocity);

        // assign final target velocity
        targetVelocity = lastVelocity = finalPlayerVelocity;
    }

    public Vector3 ResolveCollisions(Vector3 testPosition, Quaternion testRotation)
    {
        Vector3 totalMTV = Vector3.zero;

        PlayerOverlapData[] overlapping;
        if(PlayerOverlap(testPosition, testRotation, out overlapping))
        {
            foreach(var overlap in overlapping)
            {
                totalMTV += overlap.minimumTranslationVector;
            }
        }

        return testPosition + totalMTV;
    }

    public float GetGroundCheckLength()
    {
        // should be long enough to detect if we will be grounded on the next physics update
        return (-Physics.gravity.y * Time.fixedDeltaTime);
    }

    //
    // Unity Events
    //

    void Start()
    {
        attachedRigidbody = attachedRigidbody == null ? GetComponent<Rigidbody>() : attachedRigidbody;
        attachedCollider = attachedCollider == null ? GetComponent<Collider>() : attachedCollider;
    }

    private Vector3 previousPosition = Vector3.zero;
    public Vector3 actualVelocity;

    void FixedUpdate()
    {
        PlayerOverlapData[] overlapping;

        Vector3 moveVelocity = targetVelocity;

        if(!isGrounded)
        {
            moveVelocity += Physics.gravity;// * Time.deltaTime;
        }

        Vector3 desiredPosition = attachedRigidbody.position + moveVelocity * Time.deltaTime;

        // if grounded, snap to it
        //if(isGrounded && lastGroundPoint != Vector3.zero)
        //{
        //    desiredPosition += lastGroundPoint - transform.position;
        //}

        transform.position = ResolveCollisions(desiredPosition, transform.rotation);
        actualVelocity = (transform.position - previousPosition) / Time.deltaTime;
        previousPosition = transform.position;
    }

    void Reset()
    {
        attachedRigidbody = GetComponent<Rigidbody>();
        attachedCollider = GetComponent<Collider>();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        if(attachedCollider is BoxCollider)
        {
            BoxCollider box = attachedCollider as BoxCollider;
            Vector3 boxExtents = box.size;
            boxExtents.y = 0.01f;
            Gizmos.DrawSphere(box.bounds.center, 0.1f);
        }
        Gizmos.color = Color.cyan;

        Gizmos.DrawRay(transform.position, avgGroundNormal * 10.0f);
    }
}
