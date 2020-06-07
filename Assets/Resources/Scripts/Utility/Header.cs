using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Common
{
    public enum VoxelFaces
    {
        FRONT,
        RIGHT,
        TOP,
        LEFT,
        BOTTOM,
        BACK,
        SIZE
    }

    public const float VOXEL_SIZE = 0.10f;
}

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
}

public class VoxelStruct
{
    public Vector3 localPosition;
    public Vector3 forward;
    public Vector3 right;
    public Vector3 up;
    public bool[] drawFaces;
    public bool isSeperated;
    public bool isAnchor;
    public bool isExposed;
    public bool checkedForFloatingThisFrame;
    public Vector2 meshUV;
    public GameObject gameObject;
    public VoxelStruct[] adjacentVoxelStructs;
    public BoxCollider boxCollider;

    public VoxelStruct(Vector3 _localPosition, bool[] _drawFaces, bool _isSeperated, bool _isAnchor, bool _isExposed, bool _checkedForFloatingThisFrame, GameObject _gameObject, Vector2 _meshUV, VoxelStruct[] _adjacentVoxelStructs, BoxCollider _boxCollider)
    {
        localPosition = _localPosition;
        drawFaces = _drawFaces;
        isSeperated = _isSeperated;
        isAnchor = _isAnchor;
        isExposed = _isExposed;
        checkedForFloatingThisFrame = _checkedForFloatingThisFrame;
        gameObject = _gameObject;
        meshUV = _meshUV;
        adjacentVoxelStructs = _adjacentVoxelStructs;
        boxCollider = _boxCollider;
    }
}
