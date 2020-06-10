using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Voxel
{
    public enum Faces
    {
        FRONT,
        RIGHT,
        TOP,
        LEFT,
        BOTTOM,
        BACK,
        SIZE
    }

    public static readonly int[][] VertexIndexFaceTriangleAdditions = new int[][]
    {
        new int[]{0, 1, 2, 0, 2, 3},
        new int[]{0, 1, 2, 0, 2, 3},
        new int[]{0, 1, 2, 0, 2, 3},
        new int[]{0, 2, 1, 0, 3, 2},
        new int[]{0, 2, 1, 0, 3, 2},
        new int[]{0, 2, 1, 0, 3, 2}
    };

    public const int FACE_QUAD_VERTICES = 4;
    public const int FACE_TRIANGLES_VERTICES = 6;
    public const float SIZE = 0.10f;
    public const float HALF_SIZE = SIZE * 0.5f;
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
    public bool[] drawFaces;
    public bool isSeperated;
    public bool isAnchor;
    public bool isExposed;
    public bool checkedForFloatingThisFrame;
    public Vector2 meshUV;
    public VoxelStruct[] adjacentVoxelStructs;
    public Color color;
    public int[] faceTriangleStartIndexes;
    public DestructibleVoxel destructibleVoxel;
    public Vector3 launchDirection;

    public VoxelStruct(Vector3 _localPosition, bool[] _drawFaces, bool _isSeperated, bool _isAnchor, bool _isExposed, bool _checkedForFloatingThisFrame, Vector2 _meshUV, VoxelStruct[] _adjacentVoxelStructs, Color _color, int[] _faceTriangleStartIndexes, DestructibleVoxel _destructibleVoxel)
    {
        localPosition = _localPosition;
        drawFaces = _drawFaces;
        isSeperated = _isSeperated;
        isAnchor = _isAnchor;
        isExposed = _isExposed;
        checkedForFloatingThisFrame = _checkedForFloatingThisFrame;
        meshUV = _meshUV;
        adjacentVoxelStructs = _adjacentVoxelStructs;
        color = _color;
        faceTriangleStartIndexes = _faceTriangleStartIndexes;
        destructibleVoxel = _destructibleVoxel;
    }
}
