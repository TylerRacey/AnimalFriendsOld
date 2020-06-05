using UnityEngine;

[System.Serializable]
public class VoxelRender : ScriptableObject
{
    public Vector3 localPosition;
    public Vector3 forward;
    public Vector3 right;
    public Vector3 up;
    public Quaternion localRotation;
    public Color color;
    public bool[] drawFaces;
    public int[] adjacentVoxelRenderIndexes;
    public VoxelRender[] adjacentVoxelRenders;
    public bool isSeperated;
    public bool isAnchor;
    public bool isExposed;
    public bool checkedForFloatingThisFrame;
    public GameObject gameObject;
    public Vector2 meshUVs;

    public void Init(Vector3 _localPosition, Vector3 _forward, Vector3 _right, Vector3 _up, Quaternion _localRotation, Color _color, bool[] _drawFaces, int[] _adjacentVoxelRenderIndexes, VoxelRender[] _adjacentVoxelRenders, bool _isSeperated, bool _isAnchor, bool _isExposed, bool _checkedForFloatingThisFrame, GameObject _gameObject, Vector2 _meshUVs)
    {
        localPosition = _localPosition;
        forward = _forward;
        right = _right;
        up = _up;
        localRotation = _localRotation;
        color = _color;
        drawFaces = _drawFaces;
        adjacentVoxelRenderIndexes = _adjacentVoxelRenderIndexes;
        adjacentVoxelRenders = _adjacentVoxelRenders;
        isSeperated = _isSeperated;
        isAnchor = _isAnchor;
        isExposed = _isExposed;
        checkedForFloatingThisFrame = _checkedForFloatingThisFrame;
        gameObject = _gameObject;
        meshUVs = _meshUVs;
    }

    public static VoxelRender CreateInstance(Vector3 _localPosition, Vector3 _forward, Vector3 _right, Vector3 _up, Quaternion _localRotation, Color _color, bool[] _drawFaces, int[] _adjacentVoxelRenderIndexes, VoxelRender[] _adjacentVoxelRenders, bool _isSeperated, bool _isAnchor, bool _isExposed, bool _checkedForFloatingThisFrame, GameObject _gameObject, Vector2 _meshUVs)
    {
        VoxelRender voxelRender = CreateInstance<VoxelRender>();
        voxelRender.Init(_localPosition, _forward, _right, _up, _localRotation, _color, _drawFaces, _adjacentVoxelRenderIndexes, _adjacentVoxelRenders, _isSeperated, _isAnchor, _isExposed, _checkedForFloatingThisFrame, _gameObject, _meshUVs);
        return voxelRender;
    }
}
