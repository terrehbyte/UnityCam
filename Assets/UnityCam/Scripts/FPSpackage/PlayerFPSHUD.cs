using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerFPSHUD : MonoBehaviour
{
    public PlayerController player;
    public PlayerHub playerHub { get { return player.hub; } }
    public Text speedometer;

    public void Update()
    {
        speedometer.text = playerHub.motor.actualVelocity.magnitude.ToString("0.00");
    }
}