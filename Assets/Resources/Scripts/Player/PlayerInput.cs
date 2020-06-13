using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public bool CrouchPressed()
    {
        return Input.GetKeyDown(KeyCode.C);
    }

    public bool PronePressed()
    {
        return Input.GetKeyDown(KeyCode.LeftControl);
    }

    public bool SprintPressed()
    {
        return Input.GetKey(KeyCode.LeftShift);
    }

    public bool SprintReleased()
    {
         return Input.GetKeyUp(KeyCode.LeftShift);
    }

    public bool JumpPressed()
    {
        return Input.GetKeyDown(KeyCode.Space);
    }

    public Vector3 NormalizedMovement()
    {
        return new Vector3(RightMovementNormalized(), 0.0f, ForwardMovementNormalized());
    }

    public float RightMovementNormalized()
    {
        return Input.GetAxis(Axis.HORIZONTAL);
    }

    public float ForwardMovementNormalized()
    {
        return Input.GetAxis(Axis.VERTICAL);
    }

    public bool ZoomPressed()
    {
        return Input.GetMouseButton(1);
    }

    public bool UsePressed()
    {
        return (Input.GetKeyDown(KeyCode.E) );
    }

    public bool SwingPressed()
    {
        return (Input.GetMouseButtonDown(0));
    }

    public bool ScrollToolbarRightPressed()
    {
        return (Input.GetAxis("Mouse ScrollWheel") > 0.0f);
    }

    public bool ScrollToolbarLeftPressed()
    {
        return (Input.GetAxis("Mouse ScrollWheel") < 0.0f);
    }
}
