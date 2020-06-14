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

    public Mesh mesh;

    public List<int> meshTriangles = new List<int>();

    public const float launchMagnitude = 2.0f;

    public static void CreateFloatingVoxelChunk(Transform parentTransform, Mesh parentMesh, Material parentMaterial, List<VoxelStruct> parentVoxelStructs, int minVoxelCount, int remainingVoxelStructCount, Vector3 launchDirection)
    {
        // Make New Parent For All Voxels
        GameObject floatingVoxelChunkGameObject = new GameObject(parentTransform.name);
        floatingVoxelChunkGameObject.layer = LayerMask.NameToLayer("Destructible");
        floatingVoxelChunkGameObject.AddComponent<MeshRenderer>().material = parentMaterial;

        // Move Parent Into Exact Parent Destructible Transform
        FloatingVoxelChunk floatingVoxelChunk = floatingVoxelChunkGameObject.AddComponent<FloatingVoxelChunk>();
        floatingVoxelChunk.floatingVoxelChunkTransform = floatingVoxelChunkGameObject.transform;
        floatingVoxelChunk.floatingVoxelChunkTransform.position = parentTransform.position;
        floatingVoxelChunk.floatingVoxelChunkTransform.rotation = parentTransform.rotation;

        // Add Physics to Voxel Chunk
        floatingVoxelChunk.rigidBody = floatingVoxelChunkGameObject.AddComponent<Rigidbody>();
        floatingVoxelChunk.boxCollider = floatingVoxelChunkGameObject.AddComponent<BoxCollider>();

        // Set Mesh Of Voxel Chunk
        floatingVoxelChunk.mesh = new Mesh();
        floatingVoxelChunk.mesh.vertices = parentMesh.vertices;
        floatingVoxelChunk.mesh.normals = parentMesh.normals;
        floatingVoxelChunk.mesh.uv = parentMesh.uv;
        floatingVoxelChunkGameObject.AddComponent<MeshFilter>().mesh = floatingVoxelChunk.mesh;

        // Turn Floating Chunk Into Destructible
        Destructible floatingChunkDestructible = floatingVoxelChunkGameObject.AddComponent<Destructible>();

        floatingChunkDestructible.WasFloatingInit(parentVoxelStructs, floatingVoxelChunk.floatingVoxelChunkTransform, parentMesh.triangles.Length, remainingVoxelStructCount, minVoxelCount, floatingVoxelChunk.boxCollider, floatingVoxelChunk.mesh);

        floatingVoxelChunk.LaunchChunk(launchDirection);
    }

    private void Start()
    {
        game = Game.GetGame();
        seperatedVoxels = game.seperatedVoxels;
    }

    public void Update()
    {
        
    }

    public void LaunchChunk(Vector3 launchDirection)
    {
        rigidBody.velocity = launchDirection * launchMagnitude;
        // rigidBody.angularVelocity = new Vector3(0, 0, 0);
    }
}
