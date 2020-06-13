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

    private Transform destructibleTransform;
    private BoxCollider boxCollider;
    private Mesh mesh;
    private Material material;

    public List<VoxelExport> voxelExports = new List<VoxelExport>();
    private List<VoxelStruct> voxelStructs = new List<VoxelStruct>();
    private HashSet<VoxelStruct> anchorVoxelStructs = new HashSet<VoxelStruct>();
    private HashSet<VoxelStruct> floatingVoxelStructs = new HashSet<VoxelStruct>();
    private int remainingVoxelStructCount;

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

        destructibleTransform = transform;
        boxCollider = GetComponent<BoxCollider>();

        mesh = GetComponent<MeshFilter>().mesh;
        material = GetComponent<MeshRenderer>().material;

        destructibleCenterFlattened = new Vector3(destructibleTransform.position.x, 0, destructibleTransform.position.z);

        // Copy Voxel Render Imports And Setup Lists
        for (int index = 0; index < voxelExports.Count; index++)
        {
            VoxelExport voxelExport = voxelExports[index];
            VoxelStruct voxelStruct = new VoxelStruct(voxelExport.localPosition, voxelExport.drawFaces, voxelExport.isSeperated, voxelExport.isAnchor, voxelExport.isExposed, false, voxelExport.meshUV, new VoxelStruct[(int)Voxel.Faces.SIZE], voxelExport.color, voxelExport.faceTriangleStartIndexes, voxelExport.voxelIndex, this);

            if (voxelStruct.isAnchor)
            {
                anchorVoxelStructs.Add(voxelStruct);
                minVoxelCount++;
            }

            Vector3 voxelStructCenterPosition = destructibleTransform.TransformPoint(voxelExport.localPosition) + destructibleTransform.up * Voxel.HALF_SIZE + destructibleTransform.right * Voxel.HALF_SIZE + destructibleTransform.forward * Voxel.HALF_SIZE;
            voxelStruct.launchDirection = Vector3.Normalize(new Vector3(voxelStructCenterPosition.x - destructibleCenterFlattened.x, 0, voxelStructCenterPosition.z - destructibleCenterFlattened.z));

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
                    voxelStruct.adjacentVoxelStructs[faceIndex] = null;
                }
                else
                {
                    voxelStruct.adjacentVoxelStructs[faceIndex] = voxelStructs[adjacentIndex];
                }
            }
        }

        remainingVoxelStructCount = voxelStructs.Count;
        minVoxelCount = (int)(minVoxelCount * minVoxelCountAnchorScalar);
    }

    public void AssignDestructibleVoxels()
    {
        // Assign DestructibleVoxel to ExposedStructs
        int destructibleVoxelIndex = 0;
        foreach (VoxelStruct voxelStruct in voxelStructs)
        {
            if (voxelStruct.isSeperated)
                continue;

            if (!voxelStruct.isExposed)
                continue;

            if (voxelStruct.destructibleVoxel != null && voxelStruct.destructibleVoxel.destructible == this)
                continue;

            while (destructibleVoxelIndex < destructibleVoxels.Count)
            {
                DestructibleVoxel destructibleVoxel = destructibleVoxels[destructibleVoxelIndex];
                destructibleVoxelIndex++;

                if (destructibleVoxel.active && destructibleVoxel.destructible == this)
                    continue;

                destructibleVoxel.SetActive(voxelStruct, this);

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
            if (hitVoxelStruct.parentDestructible != this)
                continue;

            if (hitVoxelStruct.isSeperated)
                continue;

            // Set Adjacent Voxel Faces To Draw
            for (int adjacentFaceIndex = 0; adjacentFaceIndex < (int)Voxel.Faces.SIZE; adjacentFaceIndex++)
            {
                VoxelStruct adjacentVoxelStruct = hitVoxelStruct.adjacentVoxelStructs[adjacentFaceIndex];
                if (adjacentVoxelStruct == null)
                    continue;

                if (adjacentVoxelStruct.isSeperated)
                    continue;

                adjacentVoxelStruct.drawFaces[VoxelAdjacentFaces[adjacentFaceIndex]] = true;

                // Expose Adjacent Voxel And Move Destructible Voxel Into Place
                if (!adjacentVoxelStruct.isExposed)
                {
                    adjacentVoxelStruct.isExposed = true;

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

            SeperateVoxelStruct(hitVoxelStruct);

            // Teleport and launch first available seperated voxel
            Vector3 launchForward = Vector3.Normalize(hitVoxelStruct.launchDirection + playerTransform.forward);
            Vector3 launchRight = Vector3.Cross(launchForward, Vector3.up);
            Vector3 launchVector = Vector3.zero;
            launchVector += launchForward * Random.Range(200, 250);
            launchVector += launchRight * Random.Range(150, 200) * Utility.RandomSign();
            launchVector += Vector3.up * Random.Range(100, 150) * Utility.RandomSign();
            while (seperatedVoxelIndex < seperatedVoxels.Count)
            {
                SeperatedVoxel seperatedVoxel = seperatedVoxels[seperatedVoxelIndex];

                seperatedVoxelIndex++;

                if (seperatedVoxel.active)
                    continue;

                seperatedVoxel.SetActive(hitVoxelStruct, destructibleTransform);
                seperatedVoxel.rigidBody.AddForce(GetVoxelStructLaunchForce(hitVoxelStruct));

                break;
            }
        }

        UpdateFloatingVoxels();

        UpdateMesh();
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

    private void SeperateVoxelStruct(VoxelStruct voxelStruct)
    {
        if (voxelStruct.destructibleVoxel != null)
        {
            voxelStruct.destructibleVoxel.SetInactive();
        }

        voxelStruct.isSeperated = true;
        remainingVoxelStructCount--;
    }

    private void UpdateFloatingVoxels()
    {
        foreach (VoxelStruct anchorVoxelStruct in anchorVoxelStructs)
        {
            FloodFillVoxelStruct(anchorVoxelStruct);
        }

        floatingVoxelStructs.Clear();
        for(int voxelIndex = 0; voxelIndex < voxelStructs.Count; voxelIndex++)
        {
            VoxelStruct voxelStruct = voxelStructs[voxelIndex];
            
            if (voxelStruct.isSeperated)
                continue;

            if (voxelStruct.checkedForFloatingThisFrame)
            {
                voxelStruct.checkedForFloatingThisFrame = false;
                continue;
            }

            floatingVoxelStructs.Add(voxelStruct);
        }

        // Launch All Voxel Structs if we are at minVoxelCount
        int seperatedVoxelIndex = 0;
        if ((remainingVoxelStructCount - floatingVoxelStructs.Count) < minVoxelCount)
        {
            for (int voxelIndex = 0; voxelIndex < voxelStructs.Count; voxelIndex++)
            {
                VoxelStruct voxelStruct = voxelStructs[voxelIndex];
                if (voxelStruct.isSeperated)
                    continue;

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

                    seperatedVoxel.SetActive(voxelStruct, destructibleTransform);
                    seperatedVoxel.rigidBody.AddForce(GetVoxelStructLaunchForce(voxelStruct));

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

                // Teleport and launch first available seperated voxel
                while (seperatedVoxelIndex < seperatedVoxels.Count)
                {
                    SeperatedVoxel seperatedVoxel = seperatedVoxels[seperatedVoxelIndex];
                    seperatedVoxelIndex++;

                    if (seperatedVoxel.active)
                        continue;

                    seperatedVoxel.SetActive(voxelStruct, destructibleTransform);

                    break;
                }
            }

            if (remainingVoxelStructCount == 0)
                Destroy(gameObject);

            Utility.ScaleBoxColliderBoundsToVoxelStructs(boxCollider, voxelStructs, destructibleTransform);
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
    private void UpdateMesh()
    {
        meshTriangles.Clear();

        int triangleRootVerticeIndex = 0;
        bool[] drawFaces;
        int[] faceTriangleStartIndexes;

        // Draw Exposed Voxel Face Triangles
        foreach (VoxelStruct voxelStruct in voxelStructs)
        {
            if (voxelStruct.isSeperated)
                continue;

            if (!voxelStruct.isExposed)
                continue;

            drawFaces = voxelStruct.drawFaces;
            faceTriangleStartIndexes = voxelStruct.faceTriangleStartIndexes;
            for (int faceIndex = 0; faceIndex < (int)Voxel.Faces.SIZE; faceIndex++)
            {
                if (!drawFaces[faceIndex])
                    continue;

                triangleRootVerticeIndex = faceTriangleStartIndexes[faceIndex];
                for (int triangleIndex = 0; triangleIndex < Voxel.FACE_TRIANGLES_VERTICES; triangleIndex++)
                {
                    meshTriangles.Add(triangleRootVerticeIndex + VertexIndexFaceTriangleAdditions[faceIndex][triangleIndex]);
                }
            }
        }

        mesh.triangles = meshTriangles.ToArray();
    }

    private Vector3 GetVoxelStructLaunchForce(VoxelStruct voxelStruct)
    {
        Vector3 launchForward = Vector3.Normalize(voxelStruct.launchDirection + playerTransform.forward);
        Vector3 launchRight = Vector3.Cross(launchForward, Vector3.up);
        Vector3 launchVector = Vector3.zero;
        launchVector += launchForward * Random.Range(200, 250);
        launchVector += launchRight * Random.Range(150, 200) * Utility.RandomSign();
        launchVector += Vector3.up * Random.Range(100, 150) * Utility.RandomSign();

        return launchVector;
    }
}
