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

        foreach (VoxelRender voxelRenderImport in voxelRenderImports)
        {
            VoxelRender voxelRender = Instantiate(voxelRenderImport);
            
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

        minDestructibleVoxelCount = (int)(anchorVoxelRenders.Count * 2.0f);
    }

    public void TakeDamage(Vector3 hitPosition, float damageRadius)
    {
        foreach (VoxelRender exposedVoxelRender in exposedVoxelRenders)
        {
            BoxCollider boxCollider = Utility.VoxelCreateBoxCollider(exposedVoxelRender.gameObject);
            boxCollider.isTrigger = true;
        }

        Vector3 voxelHitPosition = FindHitVoxelContactPosition(hitPosition);
        if (voxelHitPosition == default(Vector3))
        {
            foreach (VoxelRender exposedVoxelRender in exposedVoxelRenders)
            {
                Destroy(exposedVoxelRender.gameObject.GetComponent<BoxCollider>());
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

        DrawAdjacentFacesFromLaunchedVoxelRenders(launchedVoxelRenders);

        //UpdateFloatingVoxels();

        foreach (VoxelRender exposedVoxelRender in exposedVoxelRenders)
        {
            Destroy(exposedVoxelRender.gameObject.GetComponent<BoxCollider>());
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
        exposedVoxelRenders.Remove(voxelRender);
        anchorVoxelRenders.Remove(voxelRender);

        voxelRender.isExposed = true;
        voxelRender.isSeperated = true;

        Destroy(voxelRender.gameObject);
        voxelRender.gameObject = Utility.ConvertVoxelRenderIntoSeperatedVoxelGameObject(voxelRender, transform.gameObject);
    }

    private void DrawAdjacentFacesFromLaunchedVoxelRenders(List<VoxelRender> launchedVoxelRenders)
    {
        foreach (VoxelRender launchedVoxelRender in launchedVoxelRenders)
        {
            for (int removedFaceIndex = 0; removedFaceIndex < (int)Common.VoxelFaces.SIZE; removedFaceIndex++)
            {
                VoxelRender adjacentVoxelRender = voxelRenders[launchedVoxelRender.adjacentVoxelRenderIndexes[removedFaceIndex]];
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

    //private void UpdateFloatingVoxels()
    //{
    //    for (int index = 0; index < destructibleVoxels.Count; index++)
    //    {
    //        destructibleVoxels[index].GetComponent<VoxelData>().checkedForFloatingThisFrame = false;
    //    }

    //    for (int index = 0; index < anchorVoxels.Count; index++)
    //    {
    //        FloodFillVoxel(anchorVoxels[index]);
    //    }

    //    List<GameObject> floatingVoxels = new List<GameObject>();
    //    for (int index = 0; index < destructibleVoxels.Count; index++)
    //    {
    //        GameObject destructibleVoxel = destructibleVoxels[index];
    //        if (destructibleVoxels[index].GetComponent<VoxelData>().checkedForFloatingThisFrame)
    //            continue;

    //        floatingVoxels.Add(destructibleVoxel);
    //    }

    //    LaunchFloatingVoxels(floatingVoxels);
    //}

    //private void FloodFillVoxel(GameObject mainVoxel)
    //{
    //    VoxelData mainVoxelData = mainVoxel.GetComponent<VoxelData>();

    //    if (mainVoxelData.isSeperated)
    //        return;

    //    if (mainVoxelData.checkedForFloatingThisFrame)
    //        return;

    //    mainVoxelData.checkedForFloatingThisFrame = true;

    //    for (int directionIndex = 0; directionIndex < (int)Common.VoxelFaces.SIZE; directionIndex++)
    //    {
    //        GameObject adjacentVoxel = mainVoxelData.adjacentVoxels[directionIndex];

    //        if (adjacentVoxel == null)
    //            continue;

    //        VoxelData adjacentVoxelData = adjacentVoxel.GetComponent<VoxelData>();

    //        if (adjacentVoxelData.isSeperated)
    //            continue;

    //        if (adjacentVoxelData.checkedForFloatingThisFrame)
    //            continue;

    //        FloodFillVoxel(adjacentVoxel);
    //    }
    //}

    //private void LaunchFloatingVoxels(List<GameObject> floatingVoxels)
    //{
    //    for (int index = 0; index < floatingVoxels.Count; index++)
    //    {
    //        GameObject floatingVoxel = floatingVoxels[index];
    //        if (!floatingVoxel.GetComponent<VoxelData>().isSeperated)
    //        {
    //            Utility.VoxelCreateBoxCollider(floatingVoxel);
    //            SeperateVoxel(floatingVoxel);
    //        }

    //        Rigidbody rigidbody = floatingVoxel.GetComponent<Rigidbody>();
    //        rigidbody.velocity = player.transform.forward * 2.0f;
    //    }

    //    if (destructibleVoxels.Count == 0)
    //        Destroy(gameObject);

    //    Utility.ScaleBoxColliderBoundsToVoxels(GetComponent<BoxCollider>(), destructibleVoxels);
    //}
}
