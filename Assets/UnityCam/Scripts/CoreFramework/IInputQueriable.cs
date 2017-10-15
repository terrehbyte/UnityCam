using System.Collections.Generic;

public interface IInputQueriable
{
    void BindInputs();

    void ReceiveInput(InputData inputs);
}

[System.Serializable]
public struct AxisInput
{
    public string axisName;
    public float axisValue;

    public AxisInput(string axisNameValue, float axisValueValue)
    {
        axisName = axisNameValue;
        axisValue = axisValueValue;
    }
}

[System.Serializable]
public struct ActionInput
{
    public string actionName;
    bool actionValue;
    
    public ActionInput(string actionNameValue, bool isEngagedValue)
    {
        actionName = actionNameValue;
        actionValue = isEngagedValue;
    }
}

public delegate void AxisBinding(float axisValue);
public delegate void ActionBinding(bool actionValue); 

// TODO: Create a custom inspector so we can see the input values live

[System.Serializable]
public class InputData
{
    public Dictionary<string, AxisInput> axes = new Dictionary<string, AxisInput>();
    public Dictionary<string, ActionInput> actions = new Dictionary<string, ActionInput>();

    public void ConsumeAxis(string axisName)
    {
        axes.Remove(axisName);
    }

    public void ConsumeAction(string actionName)
    {
        actions.Remove(actionName);
    }
}