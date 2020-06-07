using System.Collections.Generic;
using UnityEngine;

public class Destructible : MonoBehaviour
{
    private Transform seperatedVoxelsTransform;
    private GameObject  player;
    private Transform playerEye;
    private BoxCollider boxCollider;
    private MeshFilter meshFilter;
    private Mesh voxelMesh;
    private Material material;

    public List<VoxelExport> voxelExports = new List<VoxelExport>();
    private List<VoxelStruct> voxelStructs = new List<VoxelStruct>();
    private List<VoxelStruct> exposedVoxelStructs = new List<VoxelStruct>();

    private Vector3 destructibleCenterFlattened;
    private int minVoxelCount = 0;
    private const float minVoxelCountAnchorScalar = 2.0f;

    //// Start is called before the first frame update
    void Start()
    {
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Destructible"), LayerMask.NameToLayer("SeperatedVoxel"));
        // Physics.IgnoreLayerCollision(LayerMask.NameToLayer("DestructibleVoxel"), LayerMask.NameToLayer("SeperatedVoxel"));

        seperatedVoxelsTransform = GameObject.Find("SeperatedVoxels").transform;
        player = GameObject.FindWithTag("Player");
        playerEye = player.transform.Find("Eye");
        boxCollider = GetComponent<BoxCollider>();
        meshFilter = GetComponent<MeshFilter>();
        material = GetComponent<MeshRenderer>().material;
        voxelMesh = Resources.Load("GeneratedMeshes/voxel/Meshes/voxel", typeof(Mesh)) as Mesh;

        destructibleCenterFlattened = new Vector3(transform.position.x, 0, transform.position.z);

        // Copy Voxel Render Imports And Setup Lists
        for(int index = 0; index < voxelExports.Count; index++)
        {
            VoxelExport voxelExport = voxelExports[index];
            GameObject gameObject = Utility.GenerateVoxelGameObjectFromVoxelExport(voxelExport, transform);
            BoxCollider boxCollider = Utility.VoxelCreateBoxCollider(gameObject);
            VoxelStruct voxelStruct = new VoxelStruct(voxelExport.localPosition, voxelExport.drawFaces, voxelExport.isSeperated, voxelExport.isAnchor, voxelExport.isExposed, false, gameObject, voxelExport.meshUV, new VoxelStruct[(int)Common.VoxelFaces.SIZE], boxCollider);
 
            if (voxelStruct.isExposed)
            {
                exposedVoxelStructs.Add(voxelStruct);
            }
            else
            {
                boxCollider.enabled = false;
            }

            if(voxelStruct.isAnchor)
            {
                minVoxelCount++;
            }

            VoxelData voxelData = gameObject.AddComponent<VoxelData>();
            voxelData.voxelStruct = voxelStruct;

            voxelStructs.Add(voxelStruct);
        }

        // Assign Adjacent Voxel Renders Directly
        for (int index = 0; index < voxelStructs.Count; index++)
        {
            VoxelStruct voxelStruct = voxelStructs[index];
            for (int faceIndex = 0; faceIndex < (int)Common.VoxelFaces.SIZE; faceIndex++)
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

    public void TakeDamage(Vector3 hitPosition, float damageRadius)
    {
        Vector3 voxelHitPosition = FindHitVoxelContactPosition(hitPosition);
        if (voxelHitPosition == default(Vector3))
            return;

        Collider[] hitColliders = Physics.OverlapCapsule(playerEye.position, voxelHitPosition, damageRadius, LayerMask.GetMask("DestructibleVoxel"));
        for (int index = 0; index < hitColliders.Length; index++)
        {
            VoxelStruct hitVoxelStruct = hitColliders[index].gameObject.GetComponent<VoxelData>().voxelStruct;

            if (!exposedVoxelStructs.Contains(hitVoxelStruct))
                continue;

            // Draw Adjacent Voxel Faces
            for (int removedFaceIndex = 0; removedFaceIndex < (int)Common.VoxelFaces.SIZE; removedFaceIndex++)
            {
                VoxelStruct adjacentVoxelStruct = hitVoxelStruct.adjacentVoxelStructs[removedFaceIndex];
                if (adjacentVoxelStruct == null)
                    continue;

                if (adjacentVoxelStruct.isSeperated)
                    continue;

                adjacentVoxelStruct.drawFaces[Utility.GetVoxelAdjacentFaces()[removedFaceIndex]] = true;

                if (!adjacentVoxelStruct.isExposed)
                {
                    ExposeVoxelStruct(adjacentVoxelStruct);
                }
            }

            Vector3 voxelHitCenterFlattened = new Vector3(hitVoxelStruct.boxCollider.bounds.center.x, 0, hitVoxelStruct.boxCollider.bounds.center.z);

            Vector3 launchVector = Vector3.Normalize(voxelHitCenterFlattened - destructibleCenterFlattened);
            LaunchVoxelStruct(hitVoxelStruct, launchVector);
        }

        UpdateFloatingVoxels();

        UpdateMesh();
    }

    private Vector3 FindHitVoxelContactPosition(Vector3 hitPosition)
    {
        RaycastHit raycastHit;
        Vector3 voxelHitPosition = default(Vector3);
        if (Physics.Raycast(hitPosition, playerEye.forward, out raycastHit, 2.0f, LayerMask.GetMask("DestructibleVoxel")))
        {
            voxelHitPosition = raycastHit.point;
        }

        return voxelHitPosition;
    }

    private void LaunchVoxelStruct(VoxelStruct voxelStruct, Vector3 launchForward)
    {
        SeperateVoxelStruct(voxelStruct);

        Vector3 launchRight = Vector3.Cross(launchForward, Vector3.up);

        Vector3 launchVector = new Vector3(0, 0, 0);
        launchVector += launchForward * Random.Range(200, 250);
        launchVector += launchRight * Random.Range(-150, 150);
        launchVector += Vector3.up * Random.Range(-150, 150);

        voxelStruct.gameObject.GetComponent<Rigidbody>().AddForce(launchVector);
    }

    private void ExposeVoxelStruct(VoxelStruct voxelStruct)
    {
        voxelStruct.boxCollider.enabled = true;

        voxelStruct.isExposed = true;
        exposedVoxelStructs.Add(voxelStruct);
    }

    private void SeperateVoxelStruct(VoxelStruct voxelStruct)
    {
        if(!voxelStruct.isExposed)
        {
            ExposeVoxelStruct(voxelStruct);
        }

        voxelStructs.Remove(voxelStruct);
        exposedVoxelStructs.Remove(voxelStruct);

        voxelStruct.isSeperated = true;

        Utility.VoxelStructMakeSeperateMesh(voxelStruct, voxelMesh, material);

        voxelStruct.gameObject.AddComponent<Rigidbody>();
        voxelStruct.gameObject.AddComponent<SeperatedVoxel>();
        voxelStruct.gameObject.transform.SetParent(seperatedVoxelsTransform);
    }

    private void UpdateMesh()
    {
        // Recreate Parent Mesh From Voxel Face Changes
        CombineInstance[] combineInstances = new CombineInstance[exposedVoxelStructs.Count];
        for (int voxelIndex = 0; voxelIndex < exposedVoxelStructs.Count; voxelIndex++)
        {
            VoxelStruct exposedVoxelStruct = exposedVoxelStructs[voxelIndex];
            Mesh mesh = Utility.CreateMeshFromVoxelStruct(exposedVoxelStruct);

            combineInstances[voxelIndex].mesh = mesh;
            combineInstances[voxelIndex].transform = Matrix4x4.TRS(exposedVoxelStruct.localPosition, Quaternion.identity, Vector3.one);
        }
        meshFilter.mesh = new Mesh();
        meshFilter.sharedMesh.CombineMeshes(combineInstances, true, true, true);
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

        if ((voxelStructs.Count - floatingVoxelStructs.Count) < minVoxelCount)
        {
            for (int index = 0; index < voxelStructs.Count; index++)
            {
                LaunchVoxelStruct(voxelStructs[index], player.transform.forward);
            }

            Destroy(gameObject);
        }
        else
        {
            for (int index = 0; index < floatingVoxelStructs.Count; index++)
            {
                VoxelStruct voxelStruct = floatingVoxelStructs[index];

                if (!voxelStruct.isExposed)
                {
                    voxelStruct.boxCollider.enabled = true;
                    voxelStruct.isExposed = true;
                }

                if (!voxelStruct.isSeperated)
                {
                    SeperateVoxelStruct(voxelStruct);
                }

                voxelStruct.gameObject.GetComponent<Rigidbody>().velocity = player.transform.forward * 2.0f;
            }

            if (voxelStructs.Count == 0)
                Destroy(gameObject);
        }

        Utility.ScaleBoxColliderBoundsToVoxelStructs(boxCollider, exposedVoxelStructs);
    }

    private void FloodFillVoxelStruct(VoxelStruct mainVoxelStruct)
    {
        if (mainVoxelStruct.isSeperated)
            return;

        if (mainVoxelStruct.checkedForFloatingThisFrame)
            return;

        mainVoxelStruct.checkedForFloatingThisFrame = true;

        for (int directionIndex = 0; directionIndex < (int)Common.VoxelFaces.SIZE; directionIndex++)
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
