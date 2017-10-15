using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public PlayerHub hub;

    public void Update()
    {
        string[] axes = {"Mouse X", "Mouse Y", "Horizontal", "Vertical"};
        string[] actions = {"Fire1", "Jump"};

        InputData inputs = new InputData();
        foreach(var axis in axes)
        {
            inputs.axes[axis] = new AxisInput(axis, Input.GetAxisRaw(axis));
        }

        foreach(var action in actions)
        {
            inputs.actions[action] = new ActionInput(action, Input.GetButton(action));
        }

        hub.ReceiveInput(inputs);
    }

    public void Possess(PlayerHub newHub)
    {
        hub = newHub;
        hub.controller = this;
    }

    public void Unpossess()
    {
        hub.controller = null;
        hub = null;
    }
}
