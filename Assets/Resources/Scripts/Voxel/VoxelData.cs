using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class VoxelData : MonoBehaviour
{
    public bool[] drawFaces = new bool[(int)Voxel.Faces.SIZE];
    public int voxelIndex;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
