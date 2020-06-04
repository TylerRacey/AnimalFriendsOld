using System.Collections.Generic;
using UnityEngine;

public class Destructible : MonoBehaviour
{
    private GameObject player;
    private Transform playerEye;

    public List<GameObject> destructibleVoxels = new List<GameObject>();
    public List<GameObject> anchorVoxels = new List<GameObject>();
    public List<GameObject> exposedVoxels = new List<GameObject>();

    private Vector3 destructibleCenterFlattened;
    private int minDestructibleVoxelCount;

    // Start is called before the first frame update
    void Start()
    {
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Destructible"), LayerMask.NameToLayer("SeperatedVoxel"));
        // Physics.IgnoreLayerCollision(LayerMask.NameToLayer("DestructibleVoxel"), LayerMask.NameToLayer("SeperatedVoxel"));

        player = GameObject.FindWithTag("Player");
        playerEye = player.transform.Find("Eye");

        destructibleCenterFlattened = new Vector3(transform.position.x, 0, transform.position.z);

        minDestructibleVoxelCount = (int)(anchorVoxels.Count * 2.0f);
    }

    public void TakeDamage(Vector3 hitPosition, float damageRadius)
    {
        foreach(GameObject exposedVoxel in exposedVoxels)
        {
            BoxCollider boxCollider = Utility.VoxelCreateBoxCollider(exposedVoxel);
            boxCollider.isTrigger = true;
        }

        Vector3 voxelHitPosition = FindHitVoxelContactPosition(hitPosition);
        if (voxelHitPosition == default(Vector3))
        {
            foreach (GameObject exposedVoxel in exposedVoxels)
            {
                Destroy(exposedVoxel.GetComponent<BoxCollider>());
            }

            return;
        }

        Collider[] hitColliders = Physics.OverlapCapsule(playerEye.position, voxelHitPosition, damageRadius, LayerMask.GetMask("DestructibleVoxel"));

        List<GameObject> launchedVoxels = new List<GameObject>();
        for (int index = 0; index < hitColliders.Length; index++)
        {
            GameObject hitVoxel = hitColliders[index].gameObject;

            if (!exposedVoxels.Contains(hitVoxel))
                continue;

            Vector3 voxelHitCenterFlattened = new Vector3(hitVoxel.GetComponent<BoxCollider>().bounds.center.x, 0, hitVoxel.GetComponent<BoxCollider>().bounds.center.z);

            Vector3 launchVector = Vector3.Normalize(voxelHitCenterFlattened - destructibleCenterFlattened);
            LaunchVoxel(hitVoxel, launchVector);
            launchedVoxels.Add(hitVoxel);
        }

        DrawAdjacentFacesFromLaunchedVoxels(launchedVoxels);

        UpdateFloatingVoxels();

        foreach (GameObject exposedVoxel in exposedVoxels)
        {
            Destroy(exposedVoxel.GetComponent<BoxCollider>());
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

    private void LaunchVoxel(GameObject voxel, Vector3 launchForward)
    {
        SeperateVoxel(voxel);

        Vector3 launchRight = Vector3.Cross(launchForward, Vector3.up);

        Vector3 launchVector = new Vector3(0, 0, 0);
        launchVector += launchForward * Random.Range(200, 250);
        launchVector += launchRight * Random.Range(-150, 150);
        launchVector += Vector3.up * Random.Range(-150, 150);

        voxel.GetComponent<Rigidbody>().AddForce(launchVector);
    }

    private void SeperateVoxel(GameObject voxel)
    {
        destructibleVoxels.Remove(voxel);
        exposedVoxels.Remove(voxel);
        anchorVoxels.Remove(voxel);

        Utility.ConvertGameObjectIntoFullVoxel(voxel);

        voxel.GetComponent<VoxelData>().isExposed = true;
        voxel.GetComponent<VoxelData>().isSeperated = true;
        voxel.layer = LayerMask.NameToLayer("SeperatedVoxel");

        voxel.GetComponent<BoxCollider>().isTrigger = false;
        voxel.AddComponent<Rigidbody>();
    }

    private void DrawAdjacentFacesFromLaunchedVoxels(List<GameObject> launchedVoxels)
    {
        foreach (GameObject launchedVoxel in launchedVoxels)
        {
            VoxelData voxelData = launchedVoxel.GetComponent<VoxelData>();

            for (int removedFaceIndex = 0; removedFaceIndex < (int)Common.VoxelFaces.SIZE; removedFaceIndex++)
            {
                GameObject adjacentVoxel = voxelData.adjacentVoxels[removedFaceIndex];
                if (adjacentVoxel == null)
                    continue;

                VoxelData adjacentVoxelData = adjacentVoxel.GetComponent<VoxelData>();
                if (adjacentVoxelData.isSeperated)
                    continue;

                int faceToAdd = Utility.GetVoxelAdjacentFaces()[removedFaceIndex];
                adjacentVoxelData.drawFaces[faceToAdd] = true;

                MeshRenderer meshRenderer = adjacentVoxel.GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                {
                    meshRenderer = adjacentVoxel.AddComponent<MeshRenderer>();
                }
                meshRenderer.material = adjacentVoxelData.material;

                Utility.UpdateGameObjectVoxelFaces(adjacentVoxel, adjacentVoxelData.drawFaces);

                if (!adjacentVoxelData.isExposed)
                {
                    adjacentVoxelData.isExposed = true;
                    exposedVoxels.Add(adjacentVoxel);
                }
            }
        }
    }

    private void UpdateFloatingVoxels()
    {
        for (int index = 0; index < destructibleVoxels.Count; index++)
        {
            destructibleVoxels[index].GetComponent<VoxelData>().checkedForFloatingThisFrame = false;
        }

        for (int index = 0; index < anchorVoxels.Count; index++)
        {
            FloodFillVoxel(anchorVoxels[index]);
        }

        List<GameObject> floatingVoxels = new List<GameObject>();
        for (int index = 0; index < destructibleVoxels.Count; index++)
        {
            GameObject destructibleVoxel = destructibleVoxels[index];
            if (destructibleVoxels[index].GetComponent<VoxelData>().checkedForFloatingThisFrame)
                continue;

            floatingVoxels.Add(destructibleVoxel);
        }

        LaunchFloatingVoxels(floatingVoxels);
    }

    private void FloodFillVoxel(GameObject mainVoxel)
    {
        VoxelData mainVoxelData = mainVoxel.GetComponent<VoxelData>();

        if (mainVoxelData.isSeperated)
            return;

        if (mainVoxelData.checkedForFloatingThisFrame)
            return;

        mainVoxelData.checkedForFloatingThisFrame = true;

        for (int directionIndex = 0; directionIndex < (int)Common.VoxelFaces.SIZE; directionIndex++)
        {
            GameObject adjacentVoxel = mainVoxelData.adjacentVoxels[directionIndex];

            if (adjacentVoxel == null)
                continue;

            VoxelData adjacentVoxelData = adjacentVoxel.GetComponent<VoxelData>();

            if (adjacentVoxelData.isSeperated)
                continue;

            if (adjacentVoxelData.checkedForFloatingThisFrame)
                continue;

            FloodFillVoxel(adjacentVoxel);
        }
    }

    private void LaunchFloatingVoxels(List<GameObject> floatingVoxels)
    {
        for (int index = 0; index < floatingVoxels.Count; index++)
        {
            GameObject floatingVoxel = floatingVoxels[index];
            if(!floatingVoxel.GetComponent<VoxelData>().isSeperated)
            {
                Utility.VoxelCreateBoxCollider(floatingVoxel);
                SeperateVoxel(floatingVoxel);
            }

            Rigidbody rigidbody = floatingVoxel.GetComponent<Rigidbody>();
            rigidbody.velocity = player.transform.forward * 2.0f;
        }

        if (destructibleVoxels.Count == 0)
            Destroy(gameObject);

        Utility.ScaleBoxColliderBoundsToVoxels(GetComponent<BoxCollider>(), destructibleVoxels);
    }
}
