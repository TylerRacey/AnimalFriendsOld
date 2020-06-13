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
    public Vector3 launchDirection;
    public int voxelIndex;
    public Destructible parentDestructible;

    public DestructibleVoxel destructibleVoxel;

    public VoxelStruct(Vector3 _localPosition, bool[] _drawFaces, bool _isSeperated, bool _isAnchor, bool _isExposed, bool _checkedForFloatingThisFrame, Vector2 _meshUV, VoxelStruct[] _adjacentVoxelStructs, Color _color, int[] _faceTriangleStartIndexes, int _voxelIndex, Destructible _parentDestructible)
    {
        localPosition = _localPosition;
        drawFaces = Utility.CopyArray(_drawFaces);
        isSeperated = _isSeperated;
        isAnchor = _isAnchor;
        isExposed = _isExposed;
        checkedForFloatingThisFrame = _checkedForFloatingThisFrame;
        meshUV = _meshUV;
        adjacentVoxelStructs = _adjacentVoxelStructs;
        color = _color;
        faceTriangleStartIndexes = _faceTriangleStartIndexes;
        voxelIndex = _voxelIndex;
        parentDestructible = _parentDestructible;
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
    public int[] faceTriangleStartIndexes;
    public int voxelIndex;

    public void Init(Vector3 _localPosition, Color _color, bool[] _drawFaces, int[] _adjacentVoxelExportIndexes, bool _isSeperated, bool _isAnchor, bool _isExposed, bool _checkedForFloatingThisFrame, GameObject _gameObject, Vector2 _meshUV, int _voxelIndex)
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
        faceTriangleStartIndexes = new int[(int)(Voxel.Faces.SIZE)];
        voxelIndex = _voxelIndex;
    }

    public static VoxelExport CreateInstance(Vector3 _localPosition, Color _color, bool[] _drawFaces, int[] _adjacentVoxelExportIndexes, bool _isSeperated, bool _isAnchor, bool _isExposed, bool _checkedForFloatingThisFrame, GameObject _gameObject, Vector2 _meshUV, int _voxelIndex)
    {
        VoxelExport voxelExport = CreateInstance<VoxelExport>();
        voxelExport.Init(_localPosition, _color, _drawFaces, _adjacentVoxelExportIndexes, _isSeperated, _isAnchor, _isExposed, _checkedForFloatingThisFrame, _gameObject, _meshUV, _voxelIndex);
        return voxelExport;
    }
}
