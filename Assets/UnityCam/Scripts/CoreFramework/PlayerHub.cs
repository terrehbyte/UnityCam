using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHub : MonoBehaviour, IInputQueriable
{
    public PlayerController controller;
    public PlayerMotor motor;

    protected Dictionary<string, Func<float, bool>> axisBindings = new Dictionary<string, Func<float, bool>>();
    protected Dictionary<string, Func<bool, bool>> actionBindings = new Dictionary<string, Func<bool, bool>>();

    [System.Serializable]
    public class MouseLook
    {
        public Transform playerTransform;
        public Transform cameraTransform;

        [Tooltip("Rotations per second, given in degrees.")]
        [SerializeField]
        private float rotationsPerSecond = 68.8f;
        
        public float sensitivity = 1;

        private float lookUp;
        private float turnRight;

        public void AddLookUp(float value)
        {
            lookUp += value;
        }

        public void AddTurnRight(float value)
        {
            turnRight += value;
        }

        public void ClearInput()
        {
            lookUp = turnRight = 0.0f;
        }

        public void Update()
        {
            float mod = rotationsPerSecond * sensitivity * Time.deltaTime;

            // player is affected by Y rotation
            playerTransform.Rotate(Vector3.up, turnRight * mod);

            // camera is affected by X rotation
            cameraTransform.Rotate(Vector3.left, lookUp * mod);

            ClearInput();
        }
    }
    public MouseLook lookSettings;

    protected bool OnLookUp(float value)
    {
        lookSettings.AddLookUp(value);
        return true;
    }

    protected bool OnTurnRight(float value)
    {
        lookSettings.AddTurnRight(value);
        return true;
    }

    protected bool OnMoveForward(float value)
    {
        motor.AddMoveForward(value);
        return true;
    }

    protected bool OnMoveRight(float value)
    {
        motor.AddMoveRight(value);
        return true;
    }

    public void BindInputs()
    {
        axisBindings["Mouse X"] = OnTurnRight;
        axisBindings["Mouse Y"] = OnLookUp;
        axisBindings["Horizontal"] = OnMoveRight;
        axisBindings["Vertical"] = OnMoveForward;
    }

    public void ReceiveInput(InputData inputs)
    {
        List<string> axisForRemoval = new List<string>();
        foreach(var axis in inputs.axes)
        {
            if(axisBindings.ContainsKey(axis.Key))
            {
                if(axisBindings[axis.Key](axis.Value.axisValue))
                {
                    axisForRemoval.Add(axis.Key);
                }
            }
        }
        foreach(var target in axisForRemoval)
        {
            inputs.axes.Remove(target);
        }

        //List<string> actionsForRemoval = new List<string>();
        motor.ReceiveInput(inputs);
    }

    private void Update()
    {
        lookSettings.Update();
        motor.UpdateMovement();
    }

    void Awake()
    {
        BindInputs();
    }

    void FixedUpdate()
    {
        
    }
}
