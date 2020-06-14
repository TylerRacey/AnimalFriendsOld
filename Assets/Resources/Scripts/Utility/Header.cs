using UnityEngine;

public class Axis
{
    public const string HORIZONTAL = "Horizontal";
    public const string VERTICAL = "Vertical";
}

public class MouseAxis
{
    public const string MOUSE_Y = "Mouse Y";
    public const string MOUSE_X = "Mouse X";
}

public class Math
{
    public const double COS_10 = 0.98480775301;
    public const double COS_30 = 0.86602540378;

    public static readonly Vector3 smallestXPositionBase = new Vector3(float.MaxValue, 0, 0);
    public static readonly Vector3 largestXPositionBase = new Vector3(float.MinValue, 0, 0);
    public static readonly Vector3 smallestYPositionBase = new Vector3(0, float.MaxValue, 0);
    public static readonly Vector3 largestYPositionBase = new Vector3(0, float.MinValue, 0);
    public static readonly Vector3 smallestZPositionBase = new Vector3(0, 0, float.MaxValue);
    public static readonly Vector3 largestZPositionBase = new Vector3(0, 0, float.MinValue);
}
