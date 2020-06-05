using System.Collections.Generic;
using UnityEngine;

public class Destructible : MonoBehaviour
{
    private GameObject  player;
    private Transform playerEye;

    public List<VoxelRender> voxelRenderImports = new List<VoxelRender>();
    private List<VoxelRender> voxelRenders = new List<VoxelRender>();
    private List<VoxelRender> anchorVoxelRenders = new List<VoxelRender>();
    private List<VoxelRender> exposedVoxelRenders = new List<VoxelRender>();

    private Vector3 destructibleCenterFlattened;
    private int minDestructibleVoxelCount;

    //// Start is called before the first frame update
    void Start()
    {
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Destructible"), LayerMask.NameToLayer("SeperatedVoxel"));
        // Physics.IgnoreLayerCollision(LayerMask.NameToLayer("DestructibleVoxel"), LayerMask.NameToLayer("SeperatedVoxel"));

        player = GameObject.FindWithTag("Player");
        playerEye = player.transform.Find("Eye");

        destructibleCenterFlattened = new Vector3(transform.position.x, 0, transform.position.z);

        // Copy Voxel Render Imports And Setup Lists
        for(int index = 0; index < voxelRenderImports.Count; index++)
        {
            VoxelRender voxelRender = Instantiate(voxelRenderImports[index]);
            
            if (voxelRender.isExposed)
            {
                voxelRender.gameObject = Utility.GenerateVoxelGameObjectFromVoxelRender(voxelRender, false, transform);

                exposedVoxelRenders.Add(voxelRender);
            }

            if (voxelRender.isAnchor)
            {
                anchorVoxelRenders.Add(voxelRender);
            }

            voxelRenders.Add(voxelRender);
        }

        // Assign Adjacent Voxel Renders Directly
        for (int index = 0; index < voxelRenders.Count; index++)
        {
            VoxelRender voxelRender = voxelRenders[index];
            voxelRender.adjacentVoxelRenders = new VoxelRender[(int)Common.VoxelFaces.SIZE];
            for (int faceIndex = 0; faceIndex < (int)Common.VoxelFaces.SIZE; faceIndex++)
            {
                VoxelRender adjacentVoxelRender = voxelRenders[voxelRender.adjacentVoxelRenderIndexes[faceIndex]];
                if (adjacentVoxelRender.Equals(null))
                    continue;

                if (adjacentVoxelRender.isSeperated)
                    continue;

                voxelRender.adjacentVoxelRenders[faceIndex] = adjacentVoxelRender;
            }
        }

        minDestructibleVoxelCount = (int)(anchorVoxelRenders.Count * 2.0f);
    }

    public void TakeDamage(Vector3 hitPosition, float damageRadius)
    {
        for (int index = 0; index < exposedVoxelRenders.Count; index++)
        {
            BoxCollider boxCollider = Utility.VoxelCreateBoxCollider(exposedVoxelRenders[index].gameObject);
            boxCollider.isTrigger = true;
        }

        Vector3 voxelHitPosition = FindHitVoxelContactPosition(hitPosition);
        if (voxelHitPosition == default(Vector3))
        {
            for (int index = 0; index < exposedVoxelRenders.Count; index++)
            {
                Destroy(exposedVoxelRenders[index].gameObject.GetComponent<BoxCollider>());
            }
            return;
        }

        Collider[] hitColliders = Physics.OverlapCapsule(playerEye.position, voxelHitPosition, damageRadius, LayerMask.GetMask("DestructibleVoxel"));
        List<VoxelRender> launchedVoxelRenders = new List<VoxelRender>();
        for (int index = 0; index < hitColliders.Length; index++)
        {
            GameObject hitVoxel = hitColliders[index].gameObject;
            VoxelRender hitVoxelRender = hitVoxel.GetComponent<VoxelData>().voxelRender;

            if (!exposedVoxelRenders.Contains(hitVoxelRender))
                continue;

            Vector3 voxelHitCenterFlattened = new Vector3(hitVoxel.GetComponent<BoxCollider>().bounds.center.x, 0, hitVoxel.GetComponent<BoxCollider>().bounds.center.z);

            Vector3 launchVector = Vector3.Normalize(voxelHitCenterFlattened - destructibleCenterFlattened);
            LaunchVoxelRender(hitVoxelRender, launchVector);
            launchedVoxelRenders.Add(hitVoxelRender);
        }

        UpdateMeshFromLaunchedVoxelRenders(launchedVoxelRenders);

        UpdateFloatingVoxels();

        for (int index = 0; index < exposedVoxelRenders.Count; index++)
        {
            Destroy(exposedVoxelRenders[index].gameObject.GetComponent<BoxCollider>());
        }
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

    private void LaunchVoxelRender(VoxelRender voxelRender, Vector3 launchForward)
    {
        SeperateVoxelRender(voxelRender);

        Vector3 launchRight = Vector3.Cross(launchForward, Vector3.up);

        Vector3 launchVector = new Vector3(0, 0, 0);
        launchVector += launchForward * Random.Range(200, 250);
        launchVector += launchRight * Random.Range(-150, 150);
        launchVector += Vector3.up * Random.Range(-150, 150);

        voxelRender.gameObject.GetComponent<Rigidbody>().AddForce(launchVector);
    }

    private void SeperateVoxelRender(VoxelRender voxelRender)
    {
        voxelRenders.Remove(voxelRender);
        exposedVoxelRenders.Remove(voxelRender);
        anchorVoxelRenders.Remove(voxelRender);

        voxelRender.isExposed = true;
        voxelRender.isSeperated = true;

        Destroy(voxelRender.gameObject);
        voxelRender.gameObject = Utility.ConvertVoxelRenderIntoSeperatedVoxelGameObject(voxelRender, transform.gameObject);
    }

    private void UpdateMeshFromLaunchedVoxelRenders(List<VoxelRender> launchedVoxelRenders)
    {
        for (int index = 0; index < launchedVoxelRenders.Count; index++)
        {
            for (int removedFaceIndex = 0; removedFaceIndex < (int)Common.VoxelFaces.SIZE; removedFaceIndex++)
            {
                VoxelRender adjacentVoxelRender = launchedVoxelRenders[index].adjacentVoxelRenders[removedFaceIndex];
                if (adjacentVoxelRender.Equals(null))
                    continue;

                if (adjacentVoxelRender.isSeperated)
                    continue;

                int voxelFaceToAdd = Utility.GetVoxelAdjacentFaces()[removedFaceIndex];
                adjacentVoxelRender.drawFaces[voxelFaceToAdd] = true;

                if(!adjacentVoxelRender.isExposed)
                {
                    adjacentVoxelRender.gameObject = Utility.GenerateVoxelGameObjectFromVoxelRender(adjacentVoxelRender, false, transform);
                    adjacentVoxelRender.isExposed = true;
                    exposedVoxelRenders.Add(adjacentVoxelRender);
                }
            }
        }

        // Recreate Parent Mesh From Voxel Face Changes
        CombineInstance[] combineInstances = new CombineInstance[exposedVoxelRenders.Count];
        for (int voxelIndex = 0; voxelIndex < exposedVoxelRenders.Count; voxelIndex++)
        {
            VoxelRender exposedVoxelRender = exposedVoxelRenders[voxelIndex];
            Mesh mesh = Utility.CreateMeshFromVoxelRender(exposedVoxelRender);

            combineInstances[voxelIndex].mesh = mesh;
            combineInstances[voxelIndex].transform = Matrix4x4.TRS(exposedVoxelRender.localPosition, exposedVoxelRender.localRotation, Vector3.one);
        }
        MeshFilter newMeshFilter = transform.gameObject.GetComponent<MeshFilter>();
        newMeshFilter.mesh = new Mesh();
        newMeshFilter.sharedMesh.CombineMeshes(combineInstances, true, true, true);
    }

    private void UpdateFloatingVoxels()
    {
        for (int index = 0; index < voxelRenders.Count; index++)
        {
            voxelRenders[index].checkedForFloatingThisFrame = false;
        }

        for (int index = 0; index < anchorVoxelRenders.Count; index++)
        {
            FloodFillVoxelRender(anchorVoxelRenders[index]);
        }

        List<VoxelRender> floatingVoxelRenders = new List<VoxelRender>();
        for (int index = 0; index < voxelRenders.Count; index++)
        {
            VoxelRender voxelRender = voxelRenders[index];
            if (voxelRender.checkedForFloatingThisFrame)
                continue;

            floatingVoxelRenders.Add(voxelRender);
        }

        LaunchFloatingVoxelRenders(floatingVoxelRenders);
    }

    private void FloodFillVoxelRender(VoxelRender mainVoxelRender)
    {
        if (mainVoxelRender.isSeperated)
            return;

        if (mainVoxelRender.checkedForFloatingThisFrame)
            return;

        mainVoxelRender.checkedForFloatingThisFrame = true;

        for (int directionIndex = 0; directionIndex < (int)Common.VoxelFaces.SIZE; directionIndex++)
        {
            VoxelRender adjacentVoxelRender = mainVoxelRender.adjacentVoxelRenders[directionIndex];
            if (adjacentVoxelRender.Equals(null))
                continue;

            if (adjacentVoxelRender.isSeperated)
                continue;

            if (adjacentVoxelRender.checkedForFloatingThisFrame)
                continue;

            FloodFillVoxelRender(adjacentVoxelRender);
        }
    }

    private void LaunchFloatingVoxelRenders(List<VoxelRender> floatingVoxelRenders)
    {
        for (int index = 0; index < floatingVoxelRenders.Count; index++)
        {
            VoxelRender floatingVoxelRender = floatingVoxelRenders[index];
            if (!floatingVoxelRender.isSeperated)
            {
                SeperateVoxelRender(floatingVoxelRender);
                Utility.VoxelCreateBoxCollider(floatingVoxelRender.gameObject);
            }

            Rigidbody rigidbody = floatingVoxelRender.gameObject.GetComponent<Rigidbody>();
            rigidbody.velocity = player.transform.forward * 2.0f;
        }

        if (voxelRenders.Count == 0)
            Destroy(gameObject);

        Utility.ScaleBoxColliderBoundsToVoxelRenders(GetComponent<BoxCollider>(), exposedVoxelRenders);
        UpdateMeshFromLaunchedVoxelRenders(floatingVoxelRenders);
    }
}
