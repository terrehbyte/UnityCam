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

    //public Vector3 groundCheckOriginOffset;

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

    bool PlayerOverlap(out Collider[] overlapColliders)
    {
        // are we already overlapping with something?
        if(attachedCollider is BoxCollider)
        {
            BoxCollider boxColl = attachedCollider as BoxCollider; 
            var overlaps = Physics.OverlapBox(transform.TransformPoint(boxColl.center), boxColl.size / 2, transform.rotation, Physics.AllLayers, QueryTriggerInteraction.Ignore);

            List<Collider> overlapping = new List<Collider>();

            foreach(var overlap in overlaps)
            {
                if(overlap == attachedCollider) {continue;}

                overlapping.Add(overlap);
            }

            overlapColliders = overlapping.ToArray();
            return overlapping.Count > 0;
        }
        else
        {
            throw new Exception("Unsupported collider in use.");
        }
    }

    bool PlayerTrace(Vector3 destination, out RaycastHit traceHit)
    {        
        Collider[] overlappingColliders;
        if(PlayerOverlap(out overlappingColliders)) { traceHit = new RaycastHit(); return true; }

        Vector3 traceVector = destination - transform.position;
        RaycastHit[] hits = attachedRigidbody.SweepTestAll(traceVector.normalized, traceVector.magnitude, QueryTriggerInteraction.Ignore);

        foreach(var hit in hits)
        {
            // ignore our collider
            if(hit.collider == attachedCollider) { continue; }

            traceHit = hit;
            return true;
        }

        traceHit = new RaycastHit();
        return false;
    }

    // Returns true if the player is grounded.
    bool CheckGrounded()
    {
        bool traceHit = false;
        bool result = false;
        float groundRayLength = GetGroundCheckLength();

        Debug.DrawRay(transform.position, (Vector3.down * groundRayLength));

        RaycastHit hit;
        if(PlayerTrace(transform.position + (Vector3.down * (groundRayLength * 2)), out hit))
        {
            traceHit = true;
            avgGroundNormal = hit.normal;
            lastGroundPoint = hit.point;
            result = Vector3.Angle(Vector3.forward, hit.normal) > minGroundAngle;
        }
        else
        {
            avgGroundNormal = Vector3.zero;
        }
        

        if(!result)
        {
            int v = 0;
        }
        return result;
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

        if(playerInput.magnitude > 0.0f)
            playerInput = Vector3.ProjectOnPlane(playerInput, avgGroundNormal);

        // determine final velocity
        Vector3 finalPlayerVelocity = isGrounded ? MoveGround(playerInput, attachedRigidbody.velocity) :
                                                   MoveAir(playerInput, attachedRigidbody.velocity);
        
        // handle jump, if requested                                    
        if (wishJump)
            finalPlayerVelocity = Jump(finalPlayerVelocity);

        // assign final velocity
        targetVelocity = lastVelocity = finalPlayerVelocity;
    }

    public Vector3 GetGroundCheckOrigin()
    {
        BoxCollider box = attachedCollider as BoxCollider;
        return transform.TransformPoint(box.center);// + groundCheckOriginOffset;
    }

    public float GetGroundCheckLength()
    {
        // should be long enough to detect if we will be grounded on the next physics update
        return (Physics.gravity * Time.fixedDeltaTime).magnitude;
    }

    public float GetGroundCheckRadius()
    {
        var colliderExtents = attachedCollider.bounds.extents;
        return Mathf.Max(colliderExtents.x, colliderExtents.y);
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
        // update velocity
        attachedRigidbody.velocity = targetVelocity;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        if(attachedCollider is BoxCollider)
        {
            BoxCollider box = attachedCollider as BoxCollider;
            Vector3 boxExtents = box.size;
            boxExtents.y = 0.01f;
            Gizmos.DrawCube(GetGroundCheckOrigin(), boxExtents);
            //Gizmos.DrawSphere(transform.TransformPoint(box.center), 1.0f);
            Gizmos.DrawRay(GetGroundCheckOrigin(), Vector3.down * GetGroundCheckLength());
        }

        Gizmos.color = Color.cyan;

        BoxCollider boxColl = attachedCollider as BoxCollider; 

        Gizmos.DrawRay(transform.position, avgGroundNormal * 10.0f);
        //Gizmos.DrawCube(transform.TransformPoint(boxColl.center), boxColl.size);
        //Gizmos.DrawCube(lastGroundPoint, Vector3.one / 2);
    }
}
