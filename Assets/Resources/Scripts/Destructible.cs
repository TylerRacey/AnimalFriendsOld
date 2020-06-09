using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using UnityEngine;

public class Destructible : MonoBehaviour
{
    private Game game;
    private Transform playerEye;
    private Transform playerTransform;
    private Dictionary<GameObject, DestructibleVoxel> destructibleVoxelDictionaries;
    private Dictionary<GameObject, SeperatedVoxel> seperatedVoxelDictionaries;
    private List<DestructibleVoxel> destructibleVoxels;
    private List<SeperatedVoxel> seperatedVoxels;

    private BoxCollider boxCollider;
    private Mesh mesh;
    private Material material;

    public List<VoxelExport> voxelExports = new List<VoxelExport>();
    private List<VoxelStruct> voxelStructs = new List<VoxelStruct>();
    private HashSet<VoxelStruct> anchorVoxelStructs = new HashSet<VoxelStruct>();
    private HashSet<VoxelStruct> exposedVoxelStructs = new HashSet<VoxelStruct>();
    private HashSet<VoxelStruct> floatingVoxelStructs = new HashSet<VoxelStruct>();

    private List<int> meshTriangles = new List<int>();

    private Vector3 destructibleCenterFlattened;
    private int minVoxelCount = 0;
    private const float minVoxelCountAnchorScalar = 2.0f;

    private int[] VoxelAdjacentFaces = Utility.GetVoxelAdjacentFaces();
    private int[][] VertexIndexFaceTriangleAdditions = Voxel.VertexIndexFaceTriangleAdditions;

    void Start()
    {
        game = Game.GetGame();
        playerEye = game.playerEye;
        playerTransform = game.player.transform;
        destructibleVoxelDictionaries = game.destructibleVoxelDictionaries;
        seperatedVoxelDictionaries = game.seperatedVoxelDictionaries;
        destructibleVoxels = game.destructibleVoxels;
        seperatedVoxels = game.seperatedVoxels;

        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Destructible"), LayerMask.NameToLayer("SeperatedVoxel"));
   
        boxCollider = GetComponent<BoxCollider>();
        mesh = GetComponent<MeshFilter>().mesh;
        material = GetComponent<MeshRenderer>().material;

        destructibleCenterFlattened = new Vector3(transform.position.x, 0, transform.position.z);

        // Copy Voxel Render Imports And Setup Lists
        for (int index = 0; index < voxelExports.Count; index++)
        {
            VoxelExport voxelExport = voxelExports[index];
            VoxelStruct voxelStruct = new VoxelStruct(voxelExport.localPosition, voxelExport.drawFaces, voxelExport.isSeperated, voxelExport.isAnchor, voxelExport.isExposed, false, voxelExport.meshUV, new VoxelStruct[(int)Voxel.Faces.SIZE], voxelExport.color, voxelExport.faceTriangleStartIndexes, null);

            if (voxelStruct.isExposed)
            {
                exposedVoxelStructs.Add(voxelStruct);
            }

            if (voxelStruct.isAnchor)
            {
                anchorVoxelStructs.Add(voxelStruct);
                minVoxelCount++;
            }

            voxelStructs.Add(voxelStruct);
        }

        // Assign Adjacent Voxel Renders Directly
        for (int index = 0; index < voxelStructs.Count; index++)
        {
            VoxelStruct voxelStruct = voxelStructs[index];
            for (int faceIndex = 0; faceIndex < (int)Voxel.Faces.SIZE; faceIndex++)
            {
                int[] voxelExportIndexes = voxelExports[index].adjacentVoxelExportIndexes;
                if (voxelExportIndexes.Length == 0)
                    continue;

                // Voxel Struct List is a Mirror of Voxel Exports, their adjacent indexes line up
                int adjacentIndex = voxelExportIndexes[faceIndex];
                if (adjacentIndex == -1)
                {
                    voxelStruct.adjacentVoxelStructs[faceIndex] = default(VoxelStruct);
                }
                else
                {
                    voxelStruct.adjacentVoxelStructs[faceIndex] = voxelStructs[adjacentIndex];
                }
            }
        }

        minVoxelCount = (int)(minVoxelCount * minVoxelCountAnchorScalar);

        voxelExports = null;
    }

    public void AssignDestructibleVoxels()
    {
        // Assign DestructibleVoxel to ExposedStructs
        int destructibleVoxelIndex = 0;
        foreach (VoxelStruct exposedVoxelStruct in exposedVoxelStructs)
        {
            if (exposedVoxelStruct.destructibleVoxel != null && exposedVoxelStruct.destructibleVoxel.destructible == this)
                continue;

            while (destructibleVoxelIndex < destructibleVoxels.Count)
            {
                DestructibleVoxel destructibleVoxel = destructibleVoxels[destructibleVoxelIndex];
                destructibleVoxelIndex++;

                if (destructibleVoxel.active && destructibleVoxel.destructible == this)
                    continue;

                destructibleVoxel.SetActive(exposedVoxelStruct, this);

                break;
            }
        }
    }

    public void TakeDamage(Vector3 hitPosition, float damageRadius)
    {
        // Find Accurate Voxel Contact Position
        Vector3 voxelHitPosition = FindHitVoxelContactPosition(hitPosition);
        if (voxelHitPosition == default(Vector3))
            return;

        int destructibleVoxelIndex = 0;
        int seperatedVoxelIndex = 0;
        Collider[] hitColliders = Physics.OverlapCapsule(playerEye.position, voxelHitPosition, damageRadius, LayerMask.GetMask("DestructibleVoxel"));
        for (int index = 0; index < hitColliders.Length; index++)
        {
            DestructibleVoxel hitDestructibleVoxel;
            if (!destructibleVoxelDictionaries.TryGetValue(hitColliders[index].gameObject, out hitDestructibleVoxel))
                continue;

            VoxelStruct hitVoxelStruct = hitDestructibleVoxel.voxelStruct;
            if (!exposedVoxelStructs.Contains(hitVoxelStruct))
                continue;

            // Set Adjacent Voxel Faces To Draw
            for (int removedFaceIndex = 0; removedFaceIndex < (int)Voxel.Faces.SIZE; removedFaceIndex++)
            {
                VoxelStruct adjacentVoxelStruct = hitVoxelStruct.adjacentVoxelStructs[removedFaceIndex];
                if (adjacentVoxelStruct == null)
                    continue;

                if (adjacentVoxelStruct.isSeperated)
                    continue;

                adjacentVoxelStruct.drawFaces[VoxelAdjacentFaces[removedFaceIndex]] = true;

                // Expose Adjacent Voxel And Move Destructible Voxel Into Place
                if (!adjacentVoxelStruct.isExposed)
                {
                    SetVoxelStructExposed(adjacentVoxelStruct);

                    while (destructibleVoxelIndex < destructibleVoxels.Count)
                    {
                        DestructibleVoxel destructibleVoxel = destructibleVoxels[destructibleVoxelIndex];
                        destructibleVoxelIndex++;

                        if (destructibleVoxel.active && destructibleVoxel.destructible == this)
                            continue;

                        destructibleVoxel.SetActive(adjacentVoxelStruct, this);

                        break;
                    }
                }
            }

            Vector3 launchForward = Vector3.Normalize(new Vector3(hitDestructibleVoxel.boxCollider.bounds.center.x - destructibleCenterFlattened.x, 0, hitDestructibleVoxel.boxCollider.bounds.center.z - destructibleCenterFlattened.z));

            SeperateVoxelStruct(hitVoxelStruct);

            // Teleport and launch first available seperated voxel
            Vector3 launchRight = Vector3.Cross(launchForward, Vector3.up);
            Vector3 launchVector = Vector3.zero;
            launchVector += launchForward * Random.Range(200, 250);
            launchVector += launchRight * Random.Range(-150, 150);
            launchVector += Vector3.up * Random.Range(-150, 150);
            while (seperatedVoxelIndex < seperatedVoxels.Count)
            {
                SeperatedVoxel seperatedVoxel = seperatedVoxels[seperatedVoxelIndex];

                seperatedVoxelIndex++;

                if (seperatedVoxel.active)
                    continue;

                seperatedVoxel.SetActive(hitVoxelStruct, this);
                seperatedVoxel.rigidBody.AddForce(launchVector);

                break;
            }
        }

        UpdateFloatingVoxels();

        UpdateMesh();

        Utility.ScaleBoxColliderBoundsToVoxelStructs(boxCollider, exposedVoxelStructs);
    }

    private Vector3 FindHitVoxelContactPosition(Vector3 hitPosition)
    {
        RaycastHit raycastHit;
        Vector3 voxelHitPosition = hitPosition;
        if (Physics.Raycast(hitPosition, playerEye.forward, out raycastHit, 2.0f, LayerMask.GetMask("DestructibleVoxel")))
        {
            voxelHitPosition = raycastHit.point;
        }

        return voxelHitPosition;
    }

    private void SetVoxelStructExposed(VoxelStruct voxelStruct)
    {
        voxelStruct.isExposed = true;
        exposedVoxelStructs.Add(voxelStruct);
    }

    private void SeperateVoxelStruct(VoxelStruct voxelStruct)
    {
        if (voxelStruct.isExposed)
        {
            exposedVoxelStructs.Remove(voxelStruct);
        }

        if (voxelStruct.destructibleVoxel != null)
        {
            voxelStruct.destructibleVoxel.SetInactive();
        }

        voxelStructs.Remove(voxelStruct);
        
        voxelStruct.isSeperated = true;
    }

    private void UpdateMesh()
    {
        meshTriangles.Clear();

        // Draw Exposed Voxel Face Triangles
        foreach (VoxelStruct exposedVoxelStruct in exposedVoxelStructs)
        {
            int triangleRootVerticeIndex = 0;
            bool[] drawFaces = exposedVoxelStruct.drawFaces;
            int[] faceTriangleStartIndexes = exposedVoxelStruct.faceTriangleStartIndexes;
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

    private void UpdateFloatingVoxels()
    {
        foreach (VoxelStruct anchorVoxelStruct in anchorVoxelStructs)
        {
            if (anchorVoxelStruct.isSeperated)
                continue;
            
            FloodFillVoxelStruct(anchorVoxelStruct);
        }

        floatingVoxelStructs.Clear();
        for(int voxelIndex = 0; voxelIndex < voxelStructs.Count; voxelIndex++)
        {
            VoxelStruct voxelStruct = voxelStructs[voxelIndex];
            if (voxelStruct.checkedForFloatingThisFrame)
            {
                voxelStruct.checkedForFloatingThisFrame = false;
                continue;
            }

            floatingVoxelStructs.Add(voxelStruct);
        }

        // Launch All Voxel Structs if we are at minVoxelCount
        int seperatedVoxelIndex = 0;
        if ((voxelStructs.Count - floatingVoxelStructs.Count) < minVoxelCount)
        {
            for (int voxelIndex = 0; voxelIndex < voxelStructs.Count; voxelIndex++)
            {
                VoxelStruct voxelStruct = voxelStructs[voxelIndex];
                if (!voxelStruct.isSeperated)
                {
                    SeperateVoxelStruct(voxelStruct);
                }

                // Teleport and launch first available seperated voxel
                while (seperatedVoxelIndex < seperatedVoxels.Count)
                {
                    SeperatedVoxel seperatedVoxel = seperatedVoxels[seperatedVoxelIndex];
                    seperatedVoxelIndex++;

                    if (seperatedVoxel.active)
                        continue;

                    seperatedVoxel.SetActive(voxelStruct, this);
                    seperatedVoxel.rigidBody.velocity = playerTransform.forward * 2.0f;

                    break;
                }
            }

            Destroy(gameObject);
        }
        // Launch All Floating Voxels
        else
        {
            foreach (VoxelStruct voxelStruct in floatingVoxelStructs)
            {
                if (!voxelStruct.isExposed)
                {
                    voxelStruct.isExposed = true;
                }

                if (!voxelStruct.isSeperated)
                {
                    SeperateVoxelStruct(voxelStruct);
                }

                while (seperatedVoxelIndex < seperatedVoxels.Count)
                {
                    SeperatedVoxel seperatedVoxel = seperatedVoxels[seperatedVoxelIndex];
                    seperatedVoxelIndex++;

                    if (seperatedVoxel.active)
                        continue;

                    seperatedVoxel.SetActive(voxelStruct, this);
                    seperatedVoxel.rigidBody.velocity = playerTransform.forward * 2.0f;

                    break;
                }
            }

            if (voxelStructs.Count == 0)
                Destroy(gameObject);
        }
    }

    private void FloodFillVoxelStruct(VoxelStruct mainVoxelStruct)
    {
        if (mainVoxelStruct.isSeperated)
            return;

        if (mainVoxelStruct.checkedForFloatingThisFrame)
            return;

        mainVoxelStruct.checkedForFloatingThisFrame = true;

        for (int directionIndex = 0; directionIndex < (int)Voxel.Faces.SIZE; directionIndex++)
        {
            VoxelStruct adjacentVoxelStruct = mainVoxelStruct.adjacentVoxelStructs[directionIndex];
            if (adjacentVoxelStruct == null)
                continue;

            if (adjacentVoxelStruct.isSeperated)
                continue;

            if (adjacentVoxelStruct.checkedForFloatingThisFrame)
                continue;

            FloodFillVoxelStruct(adjacentVoxelStruct);
        }
    }
}
