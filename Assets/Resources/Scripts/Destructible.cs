using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Destructible : MonoBehaviour
{
    private Game game;

    private BoxCollider boxCollider;
    private MeshFilter meshFilter;
    private Material material;

    public List<VoxelExport> voxelExports = new List<VoxelExport>();
    private List<VoxelStruct> voxelStructs = new List<VoxelStruct>();
    private List<VoxelStruct> exposedVoxelStructs = new List<VoxelStruct>();

    private Vector3 destructibleCenterFlattened;
    private int minVoxelCount = 0;
    private const float minVoxelCountAnchorScalar = 2.0f;

    void Start()
    {
        game = Game.GetGame();

        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Destructible"), LayerMask.NameToLayer("SeperatedVoxel"));
        // Physics.IgnoreLayerCollision(LayerMask.NameToLayer("DestructibleVoxel"), LayerMask.NameToLayer("SeperatedVoxel"));

        boxCollider = GetComponent<BoxCollider>();
        meshFilter = GetComponent<MeshFilter>();
        material = GetComponent<MeshRenderer>().material;

        destructibleCenterFlattened = new Vector3(transform.position.x, 0, transform.position.z);

        // Copy Voxel Render Imports And Setup Lists
        for(int index = 0; index < voxelExports.Count; index++)
        {
            VoxelExport voxelExport = voxelExports[index];
            VoxelStruct voxelStruct = new VoxelStruct(voxelExport.localPosition, voxelExport.drawFaces, voxelExport.isSeperated, voxelExport.isAnchor, voxelExport.isExposed, false, voxelExport.meshUV, new VoxelStruct[(int)Voxel.Faces.SIZE], voxelExport.color, voxelExport.faceTriangleStartIndexes, null);

            if (voxelStruct.isExposed)
            {
                exposedVoxelStructs.Add(voxelStruct);
            }

            if(voxelStruct.isAnchor)
            {
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

        voxelExports.Clear();
    }

    public void AssignDestructibleVoxels()
    {
        // Assign DestructibleVoxel to ExposedStructs
        int destructibleVoxelIndex = 0;
        for (int exposedVoxelIndex = 0; exposedVoxelIndex < exposedVoxelStructs.Count; exposedVoxelIndex++)
        {
            VoxelStruct exposedVoxelStruct = exposedVoxelStructs[exposedVoxelIndex];

            if (exposedVoxelStruct.destructibleVoxel != null)
                continue;

            while (destructibleVoxelIndex < game.destructibleVoxels.Count)
            {
                DestructibleVoxel destructibleVoxel = game.destructibleVoxels.ElementAt(destructibleVoxelIndex).Value;

                destructibleVoxelIndex++;

                if (destructibleVoxel.active && destructibleVoxel.destructible == this)
                    continue;

                destructibleVoxel.gameObject.transform.position = transform.TransformPoint(exposedVoxelStruct.localPosition);
                destructibleVoxel.gameObject.transform.rotation = transform.rotation;
                if (destructibleVoxel.voxelStruct != null)
                {
                    destructibleVoxel.voxelStruct.destructibleVoxel = null;
                }
                destructibleVoxel.voxelStruct = exposedVoxelStruct;
                destructibleVoxel.destructible = this;
                destructibleVoxel.active = true;

                exposedVoxelStruct.destructibleVoxel = destructibleVoxel;

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

        int seperatedVoxelIndex = 0;
        Collider[] hitColliders = Physics.OverlapCapsule(game.playerEye.position, voxelHitPosition, damageRadius, LayerMask.GetMask("DestructibleVoxel"));
        for (int index = 0; index < hitColliders.Length; index++)
        {
            DestructibleVoxel hitDestructibleVoxel;
            if (!game.destructibleVoxels.TryGetValue(hitColliders[index].gameObject, out hitDestructibleVoxel))
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

                adjacentVoxelStruct.drawFaces[Utility.GetVoxelAdjacentFaces()[removedFaceIndex]] = true;

                if (!adjacentVoxelStruct.isExposed)
                {
                    SetVoxelStructExposed(adjacentVoxelStruct);
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
            while (seperatedVoxelIndex < game.seperatedVoxels.Count)
            {
                SeperatedVoxel seperatedVoxel = game.seperatedVoxels.ElementAt(seperatedVoxelIndex).Value;

                seperatedVoxelIndex++;

                if (seperatedVoxel.active)
                    continue;

                seperatedVoxel.gameObject.transform.position = transform.TransformPoint(hitVoxelStruct.localPosition);
                seperatedVoxel.gameObject.transform.rotation = transform.rotation;

                seperatedVoxel.meshRenderer.material.color = hitVoxelStruct.color;

                seperatedVoxel.active = true;
                seperatedVoxel.rigidBody.WakeUp();
                seperatedVoxel.rigidBody.AddForce(launchVector);

                break;
            }
        }

        UpdateFloatingVoxels();

        UpdateMesh();

        AssignDestructibleVoxels();

        Utility.ScaleBoxColliderBoundsToVoxelStructs(boxCollider, exposedVoxelStructs);
    }

    private Vector3 FindHitVoxelContactPosition(Vector3 hitPosition)
    {
        RaycastHit raycastHit;
        Vector3 voxelHitPosition = default(Vector3);
        if (Physics.Raycast(hitPosition, game.playerEye.forward, out raycastHit, 2.0f, LayerMask.GetMask("DestructibleVoxel")))
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
        if(!voxelStruct.isExposed)
        {
            SetVoxelStructExposed(voxelStruct);
        }

        voxelStruct.isSeperated = true;

        if (voxelStruct.destructibleVoxel != null)
        {
            voxelStruct.destructibleVoxel.SetInactive();
        }

        voxelStructs.Remove(voxelStruct);
        exposedVoxelStructs.Remove(voxelStruct);
    }

    private void UpdateMesh()
    {
        List<int> triangles = new List<int>();

        // Draw Exposed Voxel Face Triangles
        for (int voxelIndex = 0; voxelIndex < exposedVoxelStructs.Count; voxelIndex++)
        {
            VoxelStruct exposedVoxelStruct = exposedVoxelStructs[voxelIndex];

            int triangleRootVerticeIndex = 0;
            for (int faceIndex = 0; faceIndex < (int)Voxel.Faces.SIZE; faceIndex++)
            {
                if (exposedVoxelStruct.drawFaces[faceIndex])
                {
                    triangleRootVerticeIndex = exposedVoxelStruct.faceTriangleStartIndexes[faceIndex];
                    for (int triangleIndex = 0; triangleIndex < Voxel.FACE_TRIANGLES_VERTICES; triangleIndex++)
                    {
                        triangles.Add(triangleRootVerticeIndex + Voxel.VertexIndexFaceTriangleAdditions[faceIndex][triangleIndex]);
                    }
                }
            }
        }

        meshFilter.mesh.triangles = triangles.ToArray();
    }

    private void UpdateFloatingVoxels()
    {
        for (int index = 0; index < voxelStructs.Count; index++)
        {
            if (voxelStructs[index].isAnchor)
            {
                FloodFillVoxelStruct(voxelStructs[index]);
            }
        }

        List<VoxelStruct> floatingVoxelStructs = new List<VoxelStruct>();
        for (int index = 0; index < voxelStructs.Count; index++)
        {
            VoxelStruct voxelStruct = voxelStructs[index];
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
            for (int index = 0; index < voxelStructs.Count; index++)
            {
                // Teleport and launch first available seperated voxel
                while (seperatedVoxelIndex < game.seperatedVoxels.Count)
                {
                    SeperatedVoxel seperatedVoxel = game.seperatedVoxels.ElementAt(seperatedVoxelIndex).Value;

                    seperatedVoxelIndex++;

                    if (seperatedVoxel.active)
                        continue;

                    seperatedVoxel.gameObject.transform.position = transform.TransformPoint(voxelStructs[index].localPosition);
                    seperatedVoxel.gameObject.transform.rotation = transform.rotation;

                    seperatedVoxel.meshRenderer.material.color = voxelStructs[index].color;

                    seperatedVoxel.active = true;
                    seperatedVoxel.rigidBody.WakeUp();
                    seperatedVoxel.rigidBody.velocity = game.player.transform.forward * 2.0f;

                    break;
                }
            }

            Destroy(gameObject);
        }
        // Launch All Floating Voxels
        else
        {
            for (int index = 0; index < floatingVoxelStructs.Count; index++)
            {
                VoxelStruct voxelStruct = floatingVoxelStructs[index];

                if (!voxelStruct.isExposed)
                {
                    voxelStruct.isExposed = true;
                }

                if (!voxelStruct.isSeperated)
                {
                    SeperateVoxelStruct(voxelStruct);
                }

                while (seperatedVoxelIndex < game.seperatedVoxels.Count)
                {
                    SeperatedVoxel seperatedVoxel = game.seperatedVoxels.ElementAt(seperatedVoxelIndex).Value;

                    seperatedVoxelIndex++;

                    if (seperatedVoxel.active)
                        continue;

                    seperatedVoxel.gameObject.transform.position = transform.TransformPoint(voxelStruct.localPosition);
                    seperatedVoxel.gameObject.transform.rotation = transform.rotation;

                    seperatedVoxel.meshRenderer.material.color = voxelStruct.color;

                    seperatedVoxel.active = true;
                    seperatedVoxel.rigidBody.WakeUp();
                    seperatedVoxel.rigidBody.velocity = game.player.transform.forward * 2.0f;

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
