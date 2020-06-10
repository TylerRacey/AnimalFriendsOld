using System.Collections.Generic;
using UnityEngine;

public class FloatingVoxelChunk : MonoBehaviour
{
    public Transform floatingVoxelChunkTransform;
    public Rigidbody rigidBody;
    public BoxCollider boxCollider;

    public HashSet<VoxelStruct> floatingVoxelStructs = new HashSet<VoxelStruct>();

    public Mesh mesh;
    public MeshFilter meshFilter;

    public List<int> meshTriangles = new List<int>();

    private int[][] VertexIndexFaceTriangleAdditions = Voxel.VertexIndexFaceTriangleAdditions;

    public static FloatingVoxelChunk CreateFloatingVoxelChunk(Transform _parentTransform, Mesh _parentMesh, Material _parentMaterial, HashSet<VoxelStruct> _floatingVoxelStructs)
    {
        GameObject floatingVoxelChunkGameObject = new GameObject("FloatingVoxelChunk");
        floatingVoxelChunkGameObject.AddComponent<MeshRenderer>().material = _parentMaterial;

        FloatingVoxelChunk floatingVoxelChunk = floatingVoxelChunkGameObject.AddComponent<FloatingVoxelChunk>();
        floatingVoxelChunk.floatingVoxelChunkTransform = floatingVoxelChunkGameObject.transform;
        floatingVoxelChunk.floatingVoxelChunkTransform.position = _parentTransform.position;
        floatingVoxelChunk.floatingVoxelChunkTransform.rotation = _parentTransform.rotation;

        floatingVoxelChunk.rigidBody = floatingVoxelChunkGameObject.AddComponent<Rigidbody>();
        floatingVoxelChunk.boxCollider = floatingVoxelChunkGameObject.AddComponent<BoxCollider>();
        Utility.ScaleBoxColliderBoundsToVoxelStructs(floatingVoxelChunk.boxCollider, _floatingVoxelStructs, _parentTransform);
        floatingVoxelChunk.floatingVoxelStructs = new HashSet<VoxelStruct>(_floatingVoxelStructs);
        floatingVoxelChunk.mesh = new Mesh();
        floatingVoxelChunk.mesh.vertices = _parentMesh.vertices;
        floatingVoxelChunk.meshTriangles = new List<int>(_parentMesh.triangles);
        floatingVoxelChunk.mesh.normals = _parentMesh.normals;
        floatingVoxelChunk.mesh.uv = _parentMesh.uv;

        floatingVoxelChunk.UpdateMesh();

        return floatingVoxelChunk;
    }

    public void UpdateMesh()
    {
        meshTriangles.Clear();

        // Draw Remaining Floating Voxel Face Triangles
        foreach (VoxelStruct floatingVoxelStruct in floatingVoxelStructs)
        {
            int triangleRootVerticeIndex = 0;
            bool[] drawFaces = floatingVoxelStruct.drawFaces;
            int[] faceTriangleStartIndexes = floatingVoxelStruct.faceTriangleStartIndexes;
            for (int faceIndex = 0; faceIndex < (int)Voxel.Faces.SIZE; faceIndex++)
            {
                if (drawFaces[faceIndex])
                {
                    triangleRootVerticeIndex = faceTriangleStartIndexes[faceIndex];
                    for (int triangleIndex = 0; triangleIndex < Voxel.FACE_TRIANGLES_VERTICES; triangleIndex++)
                    {
                        meshTriangles.Add(triangleRootVerticeIndex + VertexIndexFaceTriangleAdditions[faceIndex][triangleIndex]);
                    }
                }
            }
        }

        mesh.triangles = meshTriangles.ToArray();
    }

    private void OnCollisionEnter(Collision collision)
    {
        
    }
}
