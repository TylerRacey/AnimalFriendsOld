using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;

public class FloatingVoxelChunk : MonoBehaviour
{
    private Game game;
    private List<SeperatedVoxel> seperatedVoxels;

    public Transform floatingVoxelChunkTransform;
    public Rigidbody rigidBody;
    public BoxCollider boxCollider;
    public BoxCollider parentBoxCollider;

    public HashSet<VoxelStruct> voxelStructs = new HashSet<VoxelStruct>();

    public Mesh mesh;

    public List<int> meshTriangles = new List<int>();

    private readonly int[][] VertexIndexFaceTriangleAdditions = Voxel.VertexIndexFaceTriangleAdditions;
    public const float launchMagnitude = 2.0f;

    public static FloatingVoxelChunk CreateFloatingVoxelChunk(Transform _parentTransform, Mesh _parentMesh, Material _parentMaterial, HashSet<VoxelStruct> _floatingVoxelStructs, Vector3 _fallDirection, BoxCollider _parentBoxCollider)
    {
        GameObject floatingVoxelChunkGameObject = new GameObject("FloatingVoxelChunk");
        floatingVoxelChunkGameObject.layer = LayerMask.NameToLayer("FloatingVoxel");
        floatingVoxelChunkGameObject.AddComponent<MeshRenderer>().material = _parentMaterial;

        FloatingVoxelChunk floatingVoxelChunk = floatingVoxelChunkGameObject.AddComponent<FloatingVoxelChunk>();
        floatingVoxelChunk.floatingVoxelChunkTransform = floatingVoxelChunkGameObject.transform;
        floatingVoxelChunk.floatingVoxelChunkTransform.position = _parentTransform.position;
        floatingVoxelChunk.floatingVoxelChunkTransform.rotation = _parentTransform.rotation;

        floatingVoxelChunk.rigidBody = floatingVoxelChunkGameObject.AddComponent<Rigidbody>();
        floatingVoxelChunk.boxCollider = floatingVoxelChunkGameObject.AddComponent<BoxCollider>();
        floatingVoxelChunk.voxelStructs = _floatingVoxelStructs;
        ScaleBoxColliderBoundsToAllExposedVoxelStructs(floatingVoxelChunk.boxCollider, _floatingVoxelStructs, floatingVoxelChunk.floatingVoxelChunkTransform);
        floatingVoxelChunk.parentBoxCollider = _parentBoxCollider;
        floatingVoxelChunk.mesh = new Mesh();
        floatingVoxelChunk.mesh.vertices = _parentMesh.vertices;
        floatingVoxelChunk.meshTriangles = new List<int>(_parentMesh.triangles);
        floatingVoxelChunk.mesh.normals = _parentMesh.normals;
        floatingVoxelChunk.mesh.uv = _parentMesh.uv;
        floatingVoxelChunkGameObject.AddComponent<MeshFilter>().mesh = floatingVoxelChunk.mesh;

        floatingVoxelChunk.UpdateMesh();
        floatingVoxelChunk.LaunchChunk(_fallDirection);

        return floatingVoxelChunk;
    }

    private static void ScaleBoxColliderBoundsToAllExposedVoxelStructs(BoxCollider boxCollider, HashSet<VoxelStruct> voxelStructs, Transform parentTransform)
    {
        // Find Voxels On Edge To Encapsulate Smallest Number Of Voxels
        Vector3 voxelLocalPosition;
        Vector3 smallestX = new Vector3(float.MaxValue, 0, 0);
        Vector3 largestX = new Vector3(float.MinValue, 0, 0);
        Vector3 smallestY = new Vector3(0, float.MaxValue, 0);
        Vector3 largestY = new Vector3(0, float.MinValue, 0);
        Vector3 smallestZ = new Vector3(0, 0, float.MaxValue);
        Vector3 largestZ = new Vector3(0, 0, float.MinValue);

        foreach (VoxelStruct voxelStruct in voxelStructs)
        {
            if (!voxelStruct.isExposed)
                continue;

            voxelLocalPosition = voxelStruct.localPosition;
            if (voxelLocalPosition.x < smallestX.x)
                smallestX = voxelLocalPosition;

            if (voxelLocalPosition.x > largestX.x)
                largestX = voxelLocalPosition;

            if (voxelLocalPosition.y > largestY.y)
                largestY = voxelLocalPosition;

            if (voxelLocalPosition.y < smallestY.y)
                smallestY = voxelLocalPosition;

            if (voxelLocalPosition.z > largestZ.z)
                largestZ = voxelLocalPosition;

            if (voxelLocalPosition.z < smallestZ.z)
                smallestZ = voxelLocalPosition;
        }

        // Encapsulate edge voxels' Bounds
        Vector3 up = parentTransform.up;
        Vector3 right = parentTransform.right;
        Vector3 forward = parentTransform.forward;
        Vector3 voxelSize = new Vector3(Voxel.SIZE, Voxel.SIZE, Voxel.SIZE);
        Vector3[] encapsulateLocalPositions = new Vector3[] { smallestX, largestX, smallestY, largestY, smallestZ, largestZ };
        Bounds newBounds = new Bounds(encapsulateLocalPositions[0], voxelSize);
        Bounds voxelBounds = new Bounds();
        for (int index = 0; index < encapsulateLocalPositions.Length; index++)
        {
            voxelBounds.size = voxelSize;
            voxelBounds.center = encapsulateLocalPositions[index] + up * Voxel.HALF_SIZE + right * Voxel.HALF_SIZE + forward * Voxel.HALF_SIZE;

            newBounds.Encapsulate(voxelBounds);
        }

        boxCollider.center = newBounds.center;
        boxCollider.size = new Vector3(newBounds.size.x, newBounds.size.y, newBounds.size.z);
    }

    private void Start()
    {
        game = Game.GetGame();
        seperatedVoxels = game.seperatedVoxels;
    }

    public void LaunchChunk(Vector3 launchDirection)
    {
        rigidBody.velocity = launchDirection * launchMagnitude;
        rigidBody.angularVelocity = new Vector3(5, 0, 0);
    }

    public void UpdateMesh()
    {
        meshTriangles.Clear();

        // Draw Remaining Floating Voxel Face Triangles
        foreach (VoxelStruct voxelStruct in voxelStructs)
        {
            int triangleRootVerticeIndex = 0;
            bool[] drawFaces = voxelStruct.drawFaces;
            int[] faceTriangleStartIndexes = voxelStruct.faceTriangleStartIndexes;
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

    public void ConvertThisGameObjectIntoSeperatedVoxels()
    {
        int seperatedVoxelIndex = 0;
        foreach (VoxelStruct voxelStruct in voxelStructs)
        {
            // Teleport and launch first available seperated voxel
            while (seperatedVoxelIndex < seperatedVoxels.Count)
            {
                SeperatedVoxel seperatedVoxel = seperatedVoxels[seperatedVoxelIndex];
                seperatedVoxelIndex++;

                if (seperatedVoxel.active)
                    continue;

                seperatedVoxel.SetActive(voxelStruct, floatingVoxelChunkTransform);
                break;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        ConvertThisGameObjectIntoSeperatedVoxels();

        Destroy(this.gameObject);
    }
}
