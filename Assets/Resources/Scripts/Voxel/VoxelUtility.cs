using System.CodeDom;
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
    public Color color;
    public Destructible parentDestructible;
    public int[] adjacentVoxelIndexes = new int[(int)Voxel.Faces.SIZE];
    public Vector3 localNormal;

    public DestructibleVoxel destructibleVoxel;
    public VoxelStruct[] adjacentVoxelStructs = new VoxelStruct[(int)Voxel.Faces.SIZE];
    public bool isFloating = false;

    public VoxelStruct(Vector3 _localPosition, bool[] _drawFaces, bool _isSeperated, bool _isAnchor, bool _isExposed, bool _checkedForFloatingThisFrame, Vector2 _meshUV, Color _color, Destructible _parentDestructible, int[] _adjacentVoxelIndexes, Vector3 _localNormal)
    {
        localPosition = _localPosition;
        drawFaces = Utility.CopyArray(_drawFaces);
        isSeperated = _isSeperated;
        isAnchor = _isAnchor;
        isExposed = _isExposed;
        checkedForFloatingThisFrame = _checkedForFloatingThisFrame;
        meshUV = _meshUV;
        color = _color;
        parentDestructible = _parentDestructible;
        adjacentVoxelIndexes = _adjacentVoxelIndexes;
        localNormal = _localNormal;
    }
}

public class VoxelExport : ScriptableObject
{
    public Vector3 localPosition;
    public Color color;
    public bool[] drawFaces;
    public int[] adjacentVoxelExportIndexes;
    public bool isSeperated;
    public bool isAnchor;
    public bool isExposed;
    public bool checkedForFloatingThisFrame;
    public GameObject gameObject;
    public Vector2 meshUV;
    public int voxelIndex;
    public Vector3 localNormal;

    public void Init(Vector3 _localPosition, Color _color, bool[] _drawFaces, int[] _adjacentVoxelExportIndexes, bool _isSeperated, bool _isAnchor, bool _isExposed, bool _checkedForFloatingThisFrame, GameObject _gameObject, Vector2 _meshUV, int _voxelIndex, Vector3 _localNormal)
    {
        localPosition = _localPosition;
        color = _color;
        drawFaces = _drawFaces;
        adjacentVoxelExportIndexes = _adjacentVoxelExportIndexes;
        isSeperated = _isSeperated;
        isAnchor = _isAnchor;
        isExposed = _isExposed;
        checkedForFloatingThisFrame = _checkedForFloatingThisFrame;
        gameObject = _gameObject;
        meshUV = _meshUV;
        voxelIndex = _voxelIndex;
        localNormal = _localNormal;
    }

    public static VoxelExport CreateInstance(Vector3 _localPosition, Color _color, bool[] _drawFaces, int[] _adjacentVoxelExportIndexes, bool _isSeperated, bool _isAnchor, bool _isExposed, bool _checkedForFloatingThisFrame, GameObject _gameObject, Vector2 _meshUV, int _voxelIndex, Vector3 _localNormal)
    {
        VoxelExport voxelExport = CreateInstance<VoxelExport>();
        voxelExport.Init(_localPosition, _color, _drawFaces, _adjacentVoxelExportIndexes, _isSeperated, _isAnchor, _isExposed, _checkedForFloatingThisFrame, _gameObject, _meshUV, _voxelIndex, _localNormal);
        return voxelExport;
    }
}
