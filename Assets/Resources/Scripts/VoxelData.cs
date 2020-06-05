using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelData : MonoBehaviour
{
    public GameObject[] adjacentVoxels = new GameObject[(int)Common.VoxelFaces.SIZE];
    public bool[] drawFaces = new bool[(int)Common.VoxelFaces.SIZE];
    public bool isSeperated = false;
    public bool isAnchor = false;
    public bool isExposed = false;
    public bool checkedForFloatingThisFrame = false;
}
