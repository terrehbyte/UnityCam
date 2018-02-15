using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMotorDebug : MonoBehaviour
{
    [HideInInspector]
    [SerializeField]
    private PlayerMotor motor;

    private Renderer playerRenderer;

    void Reset()
    {
        motor = GetComponent<PlayerMotor>();
    }

    void Start()
    {
        playerRenderer = GetComponent<Renderer>();
        if(playerRenderer == null)
        {
            playerRenderer = GetComponentInChildren<Renderer>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        playerRenderer.material.color = motor.isGrounded ? Color.green : Color.red;
    }
}
