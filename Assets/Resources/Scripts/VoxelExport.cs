using UnityEngine;

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
    public Vector2 meshUVs;
    public GameObject gameObject;
    public VoxelStruct[] adjacentVoxelStructs;

    public VoxelStruct(Vector3 _localPosition, bool[] _drawFaces, bool _isSeperated, bool _isAnchor, bool _isExposed, bool _checkedForFloatingThisFrame, GameObject _gameObject, Vector2 _meshUVs, VoxelStruct[] _adjacentVoxelStructs)
    {
        localPosition = _localPosition;
        drawFaces = _drawFaces;
        isSeperated = _isSeperated;
        isAnchor = _isAnchor;
        isExposed = _isExposed;
        checkedForFloatingThisFrame = _checkedForFloatingThisFrame;
        gameObject = _gameObject;
        meshUVs = _meshUVs;
        adjacentVoxelStructs = _adjacentVoxelStructs;
    }
}

[System.Serializable]
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
    public Vector2 meshUVs;

    public void Init(Vector3 _localPosition, Color _color, bool[] _drawFaces, int[] _adjacentVoxelExportIndexes, bool _isSeperated, bool _isAnchor, bool _isExposed, bool _checkedForFloatingThisFrame, GameObject _gameObject, Vector2 _meshUVs)
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
        meshUVs = _meshUVs;
    }

    public static VoxelExport CreateInstance(Vector3 _localPosition, Color _color, bool[] _drawFaces, int[] _adjacentVoxelExportIndexes, bool _isSeperated, bool _isAnchor, bool _isExposed, bool _checkedForFloatingThisFrame, GameObject _gameObject, Vector2 _meshUVs)
    {
        VoxelExport voxelExport = CreateInstance<VoxelExport>();
        voxelExport.Init(_localPosition, _color, _drawFaces, _adjacentVoxelExportIndexes, _isSeperated, _isAnchor, _isExposed, _checkedForFloatingThisFrame, _gameObject, _meshUVs);
        return voxelExport;
    }
}
