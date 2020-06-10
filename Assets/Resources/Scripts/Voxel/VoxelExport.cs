using UnityEngine;

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
    public Vector2 meshUV;
    public int[] faceTriangleStartIndexes;

    public void Init(Vector3 _localPosition, Color _color, bool[] _drawFaces, int[] _adjacentVoxelExportIndexes, bool _isSeperated, bool _isAnchor, bool _isExposed, bool _checkedForFloatingThisFrame, GameObject _gameObject, Vector2 _meshUV)
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
    }

    public static VoxelExport CreateInstance(Vector3 _localPosition, Color _color, bool[] _drawFaces, int[] _adjacentVoxelExportIndexes, bool _isSeperated, bool _isAnchor, bool _isExposed, bool _checkedForFloatingThisFrame, GameObject _gameObject, Vector2 _meshUV)
    {
        VoxelExport voxelExport = CreateInstance<VoxelExport>();
        voxelExport.Init(_localPosition, _color, _drawFaces, _adjacentVoxelExportIndexes, _isSeperated, _isAnchor, _isExposed, _checkedForFloatingThisFrame, _gameObject, _meshUV);
        return voxelExport;
    }
}
