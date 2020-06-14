using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Security.Permissions;
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
    private Destructible thisDestructible;

    public List<VoxelExport> voxelExports = new List<VoxelExport>();
    public List<VoxelStruct> voxelStructs = new List<VoxelStruct>();
    private HashSet<VoxelStruct> anchorVoxelStructs = new HashSet<VoxelStruct>();
    private HashSet<VoxelStruct> floatingVoxelStructs = new HashSet<VoxelStruct>();

    public int remainingVoxelStructCount;
    public int floatingVoxelStructCount;

    private List<int> meshTriangles = new List<int>();

    private Vector3 destructibleCenterFlattened;
    public int minVoxelCount = 0;
    private const float minVoxelCountAnchorScalar = 2.25f;

    private bool wasFloating;
    private Vector3 lastPosition;
    private Quaternion lastRotation;

    private static readonly int[] VoxelAdjacentFaces = Utility.GetVoxelAdjacentFaces();
    private static readonly int[][] VertexIndexFaceTriangleAdditions = Voxel.VertexIndexFaceTriangleAdditions;

    private Vector3 smallestXPosition;
    private Vector3 largestXPosition;
    private Vector3 smallestYPosition;
    private Vector3 largestYPosition;
    private Vector3 smallestZPosition;
    private Vector3 largestZPosition;

    private float smallestXPrevious;
    private float largestXPrevious;
    private float smallestYPrevious;
    private float largestYPrevious;
    private float smallestZPrevious;
    private float largestZPrevious;

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
        lastPosition = destructibleTransform.position;
        lastRotation = destructibleTransform.rotation;

        boxCollider = GetComponent<BoxCollider>();
        mesh = GetComponent<MeshFilter>().mesh;
        material = GetComponent<MeshRenderer>().material;
        thisDestructible = this;
        destructibleCenterFlattened = new Vector3(destructibleTransform.position.x, 0, destructibleTransform.position.z);

        if (voxelExports.Count > 0)
        {
            ConvertVoxelExportsIntoVoxelStructs();
        }
        else
        {
            UpdateMesh();
        }

        ScaleBoxColliderBoundsToVoxelStructs();
    }

    public void WasFloatingInit(List<VoxelStruct> parentVoxelStructs, int meshTriangleCount, int _remainingVoxelStructCount, int _minVoxelCount)
    {
        wasFloating = true;
        thisDestructible = this;

        for (int voxelIndex = 0; voxelIndex < parentVoxelStructs.Count; voxelIndex++)
        {
            VoxelStruct parentVoxelStruct = parentVoxelStructs[voxelIndex];

            bool isNewVoxelSeperated = parentVoxelStruct.isSeperated || !parentVoxelStruct.isFloating;
            bool isNewVoxelExposed = parentVoxelStruct.isExposed && parentVoxelStruct.isFloating;

            VoxelStruct newVoxelStruct = new VoxelStruct(parentVoxelStruct.localPosition, Utility.CopyArray(parentVoxelStruct.drawFaces), isNewVoxelSeperated, false, isNewVoxelExposed, false, parentVoxelStruct.meshUV, Utility.CopyArray(parentVoxelStruct.adjacentVoxelIndexes), parentVoxelStruct.color, parentVoxelStruct.faceTriangleStartIndexes, thisDestructible);
            voxelStructs.Add(newVoxelStruct);
        }

        remainingVoxelStructCount = _remainingVoxelStructCount;
        minVoxelCount = _minVoxelCount;
        meshTriangles = new List<int>(meshTriangleCount);
    }

    private void ConvertVoxelExportsIntoVoxelStructs()
    {
        for (int index = 0; index < voxelExports.Count; index++)
        {
            VoxelExport voxelExport = voxelExports[index];
            VoxelStruct voxelStruct = new VoxelStruct(voxelExport.localPosition, voxelExport.drawFaces, voxelExport.isSeperated, voxelExport.isAnchor, voxelExport.isExposed, false, voxelExport.meshUV, voxelExport.adjacentVoxelExportIndexes, voxelExport.color, voxelExport.faceTriangleStartIndexes, thisDestructible);

            if (voxelStruct.isAnchor)
            {
                anchorVoxelStructs.Add(voxelStruct);
                minVoxelCount++;
            }

            Vector3 voxelStructCenterPosition = destructibleTransform.TransformPoint(voxelExport.localPosition) + destructibleTransform.up * Voxel.HALF_SIZE + destructibleTransform.right * Voxel.HALF_SIZE + destructibleTransform.forward * Voxel.HALF_SIZE;
            voxelStruct.launchDirection = Vector3.Normalize(new Vector3(voxelStructCenterPosition.x - destructibleCenterFlattened.x, 0, voxelStructCenterPosition.z - destructibleCenterFlattened.z));

            voxelStructs.Add(voxelStruct);
        }
        remainingVoxelStructCount = voxelStructs.Count;
        minVoxelCount = (int)(minVoxelCount * minVoxelCountAnchorScalar);
    }

    public void AssignDestructibleVoxels()
    {
        // Assign DestructibleVoxel to ExposedStructs
        int destructibleVoxelIndex = 0;
        bool hasDestructibleMoved = lastPosition != destructibleTransform.position || lastRotation != destructibleTransform.rotation;
        for (int voxelIndex = 0; voxelIndex < voxelStructs.Count; voxelIndex++)
        {
            VoxelStruct voxelStruct = voxelStructs[voxelIndex];
            if (voxelStruct.isSeperated)
                continue;

            if (!voxelStruct.isExposed)
                continue;

            // If we have already assigned a destructible voxel for this struct and we haven't moved, continue
            if (voxelStruct.destructibleVoxel != null && voxelStruct.destructibleVoxel.destructible == thisDestructible && !hasDestructibleMoved)
                continue;

            while (destructibleVoxelIndex < destructibleVoxels.Count)
            {
                DestructibleVoxel destructibleVoxel = destructibleVoxels[destructibleVoxelIndex];
                destructibleVoxelIndex++;

                if (destructibleVoxel.active && destructibleVoxel.destructible == thisDestructible)
                    continue;

                destructibleVoxel.SetActive(voxelStruct, thisDestructible);

                break;
            }
        }
    }

    public void TakeDamage(Vector3 hitPosition, float damageRadius)
    {
        // Find Accurate Voxel Contact Position
        Vector3 voxelHitPosition = FindHitVoxelContactPosition(hitPosition);
        if (voxelHitPosition == Vector3.negativeInfinity)
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
            if (hitVoxelStruct == null)
                continue;

            if (hitVoxelStruct.parentDestructible != thisDestructible)
                continue;

            if (hitVoxelStruct.isSeperated)
                continue;

            // Set Adjacent Voxel Faces To Draw
            for (int adjacentFaceIndex = 0; adjacentFaceIndex < (int)Voxel.Faces.SIZE; adjacentFaceIndex++)
            {
                int adjacentVoxelIndex = hitVoxelStruct.adjacentVoxelIndexes[adjacentFaceIndex];
                if (adjacentVoxelIndex == -1)
                    continue;

                VoxelStruct adjacentVoxelStruct = voxelStructs[adjacentVoxelIndex];
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

                        if (destructibleVoxel.active && destructibleVoxel.destructible == thisDestructible)
                            continue;

                        destructibleVoxel.SetActive(adjacentVoxelStruct, thisDestructible);

                        break;
                    }
                }
            }

            SeperateVoxelStruct(hitVoxelStruct);

            // Teleport and launch first available seperated voxel
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

        if (remainingVoxelStructCount == 0)
            return;

        UpdateMesh();

        ScaleBoxColliderBoundsToVoxelStructs();

        lastPosition = destructibleTransform.position;
        lastRotation = destructibleTransform.rotation;
    }

    private Vector3 FindHitVoxelContactPosition(Vector3 hitPosition)
    {
        RaycastHit raycastHit;
        Vector3 voxelHitPosition = Vector3.negativeInfinity;
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
        if (wasFloating)
        {
            // Flood Fill From First Attached Voxel
            for (int voxelIndex = 0; voxelIndex < voxelStructs.Count; voxelIndex++)
            {
                if (voxelStructs[voxelIndex].isSeperated)
                    continue;

                FloodFillVoxelStruct(voxelStructs[voxelIndex]);
                break;
            }
        }
        else
        {
            // Flood Fill Each AnchorVoxelStruct
            foreach (VoxelStruct anchorVoxelStruct in anchorVoxelStructs)
            {
                FloodFillVoxelStruct(anchorVoxelStruct);
            }
        }

        floatingVoxelStructs.Clear();
        floatingVoxelStructCount = 0;
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

            voxelStruct.isFloating = true;

            floatingVoxelStructs.Add(voxelStruct);

            floatingVoxelStructCount++;
        }

        // Launch All Voxel Structs if we are at minVoxelCount
        int seperatedVoxelIndex = 0;
        if ((remainingVoxelStructCount - floatingVoxelStructCount) < minVoxelCount)
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
            if (floatingVoxelStructCount >= minVoxelCount)
            {
                FloatingVoxelChunk.CreateFloatingVoxelChunk(destructibleTransform, mesh, material, voxelStructs, minVoxelCount, floatingVoxelStructCount, playerTransform.forward);

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
                }
            }
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

                    if (floatingVoxelStructCount < minVoxelCount)
                    {
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
                }
            }

            if (remainingVoxelStructCount == 0)
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

        for (int adjacentFaceIndex = 0; adjacentFaceIndex < (int)Voxel.Faces.SIZE; adjacentFaceIndex++)
        {
            int adjacentVoxelIndex = mainVoxelStruct.adjacentVoxelIndexes[adjacentFaceIndex];
            if (adjacentVoxelIndex == -1)
                continue;

            VoxelStruct adjacentVoxelStruct = voxelStructs[adjacentVoxelIndex];
            if (adjacentVoxelStruct.isSeperated)
                continue;

            if (adjacentVoxelStruct.checkedForFloatingThisFrame)
                continue;

            FloodFillVoxelStruct(adjacentVoxelStruct);
        }
    }

    public void UpdateMesh()
    {
        meshTriangles.Clear();

        int triangleRootVerticeIndex = 0;
        bool[] drawFaces;
        int[] faceTriangleStartIndexes;

        // Draw Exposed Voxel Face Triangles
        for (int voxelIndex = 0; voxelIndex < voxelStructs.Count; voxelIndex++)
        {
            VoxelStruct voxelStruct = voxelStructs[voxelIndex];
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

    private void ScaleBoxColliderBoundsToVoxelStructs()
    {
        // Find Voxels On Edge To Encapsulate Smallest Number Of Voxels
        Vector3 voxelLocalPosition;
        smallestXPosition = Math.smallestXPositionBase;
        largestXPosition = Math.largestXPositionBase;
        smallestYPosition = Math.smallestYPositionBase;
        largestYPosition = Math.largestYPositionBase;
        smallestZPosition = Math.smallestZPositionBase;
        largestZPosition = Math.largestZPositionBase;

        foreach (VoxelStruct voxelStruct in voxelStructs)
        {
            if (voxelStruct.isSeperated)
                continue;

            if (!voxelStruct.isExposed)
                continue;

            voxelLocalPosition = voxelStruct.localPosition;

            if (voxelLocalPosition.x < smallestXPosition.x)
            {
                smallestXPosition = voxelLocalPosition;
            }
            else if (voxelLocalPosition.x > largestXPosition.x)
            {
                largestXPosition = voxelLocalPosition;
            }

            if (voxelLocalPosition.y < smallestYPosition.y)
            {
                smallestYPosition = voxelLocalPosition;
            }
            else if (voxelLocalPosition.y > largestYPosition.y)
            {
                largestYPosition = voxelLocalPosition;
            }

            if (voxelLocalPosition.z < smallestZPosition.z)
            {
                smallestZPosition = voxelLocalPosition;
            }
            else if (voxelLocalPosition.z > largestZPosition.z)
            {
                largestZPosition = voxelLocalPosition;
            }
        }

        // Don't recaculate if edges have not changed
        if (smallestXPosition.x >= smallestXPrevious && largestXPosition.x <= largestXPrevious && smallestYPosition.y >= smallestYPrevious && largestYPosition.y <= smallestZPrevious && smallestZPosition.z >= smallestZPrevious && largestZPosition.z <= largestZPrevious)
            return;

        // Encapsulate edge voxels' Bounds
        Vector3 up = destructibleTransform.up;
        Vector3 right = destructibleTransform.right;
        Vector3 forward = destructibleTransform.forward;
        Vector3[] encapsulateLocalPositions = new Vector3[] { smallestXPosition, largestXPosition, smallestYPosition, largestYPosition, smallestZPosition, largestZPosition };
        Bounds newBounds = new Bounds(encapsulateLocalPositions[0], Voxel.EXTENTS);
        Bounds voxelBounds = new Bounds();
        for (int index = 0; index < encapsulateLocalPositions.Length; index++)
        {
            voxelBounds.size = Voxel.EXTENTS;
            voxelBounds.center = encapsulateLocalPositions[index] + up * Voxel.HALF_SIZE + right * Voxel.HALF_SIZE + forward * Voxel.HALF_SIZE;
            newBounds.Encapsulate(voxelBounds);
        }

        boxCollider.center = newBounds.center;
        boxCollider.size = new Vector3(newBounds.size.x, newBounds.size.y, newBounds.size.z);

        smallestXPrevious = smallestXPosition.x;
        largestXPrevious = largestXPosition.x;
        smallestYPrevious = smallestYPosition.y;
        largestYPrevious = largestYPosition.y;
        smallestZPrevious = smallestZPosition.y;
        largestZPrevious = largestZPosition.z;
    }
}
