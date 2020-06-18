using System.CodeDom;
using Unity.Collections.LowLevel.Unsafe;
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

    public const float MASS = 1.0f;
    public const int FACE_QUAD_VERTICES = 4;
    public const int FACE_TRIANGLES_VERTICES = 6;
    public const float SIZE = 0.10f;
    public const float HALF_SIZE = SIZE * 0.5f;
    public static readonly Vector3 EXTENTS = new Vector3(SIZE, SIZE, SIZE);
    public static readonly Vector3 HALF_EXTENTS = new Vector3(HALF_SIZE, HALF_SIZE, HALF_SIZE);
    public const float BOUNDING_SPHERE_RADIUS = 0.07071067811f; //Mathf.Cos(45) * SIZE

    public static readonly int[][] VertexIndexFaceTriangleAdditions = new int[][]{
        new int[]{0, 1, 2, 0, 2, 3},
        new int[]{0, 1, 2, 0, 2, 3},
        new int[]{0, 1, 2, 0, 2, 3},
        new int[]{0, 2, 1, 0, 3, 2},
        new int[]{0, 2, 1, 0, 3, 2},
        new int[]{0, 2, 1, 0, 3, 2}};

    public static readonly Vector3[][] VertexVectorAdditions = new Vector3[][]{
        new Vector3[]{ Vector3.zero, Vector3.up * SIZE, (Vector3.up * SIZE) + (Vector3.right * SIZE), Vector3.right * SIZE },
        new Vector3[]{ Vector3.right * SIZE, (Vector3.right * SIZE) + (Vector3.up * SIZE), (Vector3.right * SIZE) + (Vector3.up * SIZE) + (Vector3.forward * SIZE), (Vector3.right * SIZE) + (Vector3.forward * SIZE) },
        new Vector3[]{ Vector3.up * SIZE, (Vector3.up * SIZE) + (Vector3.forward * SIZE), (Vector3.up * SIZE) + (Vector3.forward * SIZE) + (Vector3.right * SIZE), (Vector3.up * SIZE) + (Vector3.right * SIZE) },
        new Vector3[]{ Vector3.zero, Vector3.up* SIZE, (Vector3.up * SIZE) + (Vector3.forward * SIZE), Vector3.forward * SIZE },
        new Vector3[]{ Vector3.zero, Vector3.forward* SIZE, (Vector3.forward* SIZE) + (Vector3.right* SIZE), Vector3.right* SIZE },
        new Vector3[]{ Vector3.forward * SIZE, (Vector3.forward * SIZE) + (Vector3.up * SIZE), (Vector3.forward * SIZE) + (Vector3.up * SIZE) + (Vector3.right * SIZE), (Vector3.forward * SIZE) + (Vector3.right * SIZE) }};

    public static readonly Vector3[] VertexNormals = new Vector3[]{
        -Vector3.forward,
        Vector3.right,
        Vector3.up,
        -Vector3.right,
        -Vector3.up,
        Vector3.forward};

    public const string defaultMeshPath = "GeneratedMeshes/voxel/Meshes/voxel";
}

public class VoxelStruct
{
    public Vector3 localPosition;
    public bool[] drawFaces;
    public bool isAnchor;
    public bool isExposed;
    public bool checkedForFloatingThisFrame;
    public Vector2 meshUV;
    public Color color;
    public Destructible parentDestructible;
    public Vector3 localNormal;

    public DestructibleVoxel destructibleVoxel;
    public VoxelStruct[] adjacentVoxelStructs = new VoxelStruct[(int)Voxel.Faces.SIZE];
    public bool isFloating = false;
    public bool isFalling = false;
    public int listIndex;
    public int exposedListIndex;

    public VoxelStruct(Vector3 _localPosition, bool[] _drawFaces, bool _isAnchor, bool _isExposed, bool _checkedForFloatingThisFrame, Vector2 _meshUV, Color _color, Destructible _parentDestructible, Vector3 _localNormal)
    {
        localPosition = _localPosition;
        drawFaces = Utility.CopyArray(_drawFaces);
        isAnchor = _isAnchor;
        isExposed = _isExposed;
        checkedForFloatingThisFrame = _checkedForFloatingThisFrame;
        meshUV = _meshUV;
        color = _color;
        parentDestructible = _parentDestructible;
        localNormal = _localNormal;
    }
}

[System.Serializable]
public class VoxelExport
{
    public float localPositionX;
    public float localPositionY;
    public float localPositionZ;
    public float colorR;
    public float colorG;
    public float colorB;
    public bool[] drawFaces;
    public int[] adjacentVoxelExportIndexes = new int[(int)Voxel.Faces.SIZE];
    public bool isAnchor;
    public bool isExposed;
    public float meshU;
    public float meshV;
    public float localNormalX;
    public float localNormalY;
    public float localNormalZ;

    public VoxelExport(float _localPositionX, float _localPositionY, float _localPositionZ, float _colorR, float _colorG, float _colorB, bool[] _drawFaces, bool _isAnchor, bool _isExposed, float _meshU, float _meshV, float _localNormalX, float _localNormalY, float _localNormalZ)
    {
        localPositionX = _localPositionX;
        localPositionY = _localPositionY;
        localPositionZ = _localPositionZ;
        colorR = _colorR;
        colorG = _colorG;
        colorB = _colorB;
        drawFaces = _drawFaces;
        isAnchor = _isAnchor;
        isExposed = _isExposed;
        meshU = _meshU;
        meshV = _meshV;
        localNormalX = _localNormalX;
        localNormalY = _localNormalY;
        localNormalZ = _localNormalZ;

        for (int index = 0; index < adjacentVoxelExportIndexes.Length; index++)
        {
            adjacentVoxelExportIndexes[index] = -1;
        }
    }
}
