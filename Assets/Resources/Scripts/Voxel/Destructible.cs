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
    private MeshCollider meshCollider;
    private Mesh mesh;
    private Material material;
    private Destructible thisDestructible;
    private Rigidbody rigidBody;

    public List<VoxelExport> voxelExports = new List<VoxelExport>();
    private List<VoxelStruct> voxelStructs = new List<VoxelStruct>();
    private HashSet<VoxelStruct> anchorVoxelStructs = new HashSet<VoxelStruct>();
    private HashSet<VoxelStruct> floatingVoxelStructs = new HashSet<VoxelStruct>();

    private int remainingVoxelStructCount;
    private int floatingVoxelStructCount;

    private List<int> meshTriangles = new List<int>();
    private List<Vector3> meshVertices = new List<Vector3>();
    private List<Vector3> meshNormals = new List<Vector3>();
    private List<Vector2> meshUVs = new List<Vector2>();

    private Vector3 destructibleCenterFlattened;
    private int minVoxelCount = 0;
    private const float minVoxelCountAnchorScalar = 2.25f;

    private const float findHitVoxelPositionDepth = 4.0f;
    private const float floatingDestructibleStartVelocityMagnitude = 1.0f;
    private const float floatingDestructibleStartLaunchMagnitude = 30.0f;
    private const float wasFloatingDamageForceMagnitude = 75.0f;

    private bool wasFloating;
    private Vector3 lastPosition;
    private Quaternion lastRotation;

    private static readonly int[] VoxelAdjacentFaces = Utility.GetVoxelAdjacentFaces();
    private static readonly int[][] VertexIndexFaceTriangleAdditions = Voxel.VertexIndexFaceTriangleAdditions;
    private static readonly Vector3[][] VertexVectorAdditions = Voxel.VertexVectorAdditions;
    private static readonly Vector3[] VertexNormals = Voxel.VertexNormals;

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

        meshCollider = GetComponent<MeshCollider>();
        mesh = GetComponent<MeshFilter>().mesh;
        material = GetComponent<MeshRenderer>().material;
        thisDestructible = this;
        destructibleCenterFlattened = new Vector3(destructibleTransform.position.x, 0, destructibleTransform.position.z);

        if (voxelExports.Count > 0)
        {
            ConvertVoxelExportsIntoVoxelStructs();

            meshTriangles = new List<int>(mesh.triangles.Length);
            meshVertices = new List<Vector3>(mesh.vertexCount);
            meshNormals = new List<Vector3>(mesh.vertexCount);
            meshUVs = new List<Vector2>(mesh.vertexCount);
        }
        else
        {
            UpdateMesh();
        }
    }

    private void ConvertVoxelExportsIntoVoxelStructs()
    {
        for (int index = 0; index < voxelExports.Count; index++)
        {
            VoxelExport voxelExport = voxelExports[index];
            VoxelStruct voxelStruct = new VoxelStruct(voxelExport.localPosition, voxelExport.drawFaces, voxelExport.isSeperated, voxelExport.isAnchor, voxelExport.isExposed, false, voxelExport.meshUV, voxelExport.color, thisDestructible, voxelExport.adjacentVoxelExportIndexes);

            if (voxelStruct.isAnchor)
            {
                anchorVoxelStructs.Add(voxelStruct);
                minVoxelCount++;
            }

            Vector3 voxelStructCenterPosition = destructibleTransform.TransformPoint(voxelExport.localPosition) + destructibleTransform.up * Voxel.HALF_SIZE + destructibleTransform.right * Voxel.HALF_SIZE + destructibleTransform.forward * Voxel.HALF_SIZE;
            voxelStruct.launchDirection = Vector3.Normalize(new Vector3(voxelStructCenterPosition.x - destructibleCenterFlattened.x, 0, voxelStructCenterPosition.z - destructibleCenterFlattened.z));

            voxelStructs.Add(voxelStruct);
        }

        // Assign Adjacent Voxel Structs by adjacent Indexes
        for(int voxelIndex = 0; voxelIndex < voxelStructs.Count; voxelIndex++)
        {
            VoxelStruct voxelStruct = voxelStructs[voxelIndex];
            int adjacentVoxelIndex;
            for (int faceIndex = 0; faceIndex < (int)Voxel.Faces.SIZE; faceIndex++)
            {
                adjacentVoxelIndex = voxelStructs[voxelIndex].adjacentVoxelIndexes[faceIndex];
                if (adjacentVoxelIndex == -1)
                    continue;

                voxelStruct.adjacentVoxelStructs[faceIndex] = voxelStructs[adjacentVoxelIndex];
            }
        }

        remainingVoxelStructCount = voxelStructs.Count;
        minVoxelCount = (int)(minVoxelCount * minVoxelCountAnchorScalar);
    }

    public void AssignDestructibleVoxels(Vector3 hitPosition)
    {
        // Assign DestructibleVoxel to ExposedStructs
        int destructibleVoxelIndex = 0;
        for (int voxelIndex = 0; voxelIndex < voxelStructs.Count; voxelIndex++)
        {
            VoxelStruct voxelStruct = voxelStructs[voxelIndex];
            if (voxelStruct.isSeperated)
                continue;

            if (!voxelStruct.isExposed)
                continue;

            // If we have already assigned a destructible voxel for this struct and we haven't moved, continue
            if (voxelStruct.destructibleVoxel != null && voxelStruct.destructibleVoxel.destructible == thisDestructible && lastPosition == destructibleTransform.position && lastRotation == destructibleTransform.rotation)
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

        lastPosition = destructibleTransform.position;
        lastRotation = destructibleTransform.rotation;

        if (wasFloating)
        {
            rigidBody.AddForce(playerTransform.forward * wasFloatingDamageForceMagnitude);
        }
    }

    private Vector3 FindHitVoxelContactPosition(Vector3 hitPosition)
    {
        RaycastHit raycastHit;
        Vector3 voxelHitPosition = Vector3.negativeInfinity;
        if (Physics.Raycast(hitPosition, playerEye.forward, out raycastHit, findHitVoxelPositionDepth, LayerMask.GetMask("DestructibleVoxel")))
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
                CreateDestructibleFromFloatingVoxels(floatingVoxelStructCount);

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
            VoxelStruct adjacentVoxelStruct = mainVoxelStruct.adjacentVoxelStructs[adjacentFaceIndex];
            if (adjacentVoxelStruct == null)
                continue;

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
        meshVertices.Clear();
        meshNormals.Clear();
        meshUVs.Clear();

        int vertexCount = 0;
        bool[] drawFaces;

        // Draw Exposed Voxel Face Triangles
        for (int voxelIndex = 0; voxelIndex < voxelStructs.Count; voxelIndex++)
        {
            VoxelStruct voxelStruct = voxelStructs[voxelIndex];
            if (voxelStruct.isSeperated)
                continue;

            if (!voxelStruct.isExposed)
                continue;

            drawFaces = voxelStruct.drawFaces;
            for (int faceIndex = 0; faceIndex < (int)Voxel.Faces.SIZE; faceIndex++)
            {
                if (!drawFaces[faceIndex])
                    continue;

                for (int triangleIndex = 0; triangleIndex < Voxel.FACE_TRIANGLES_VERTICES; triangleIndex++)
                {
                    meshTriangles.Add(vertexCount + VertexIndexFaceTriangleAdditions[faceIndex][triangleIndex]);
                }

                for (int vertexIndex = 0; vertexIndex < Voxel.FACE_QUAD_VERTICES; vertexIndex++)
                {
                    meshVertices.Add(voxelStruct.localPosition + VertexVectorAdditions[faceIndex][vertexIndex]);
                    
                    meshNormals.Add(VertexNormals[faceIndex]);
                    meshUVs.Add(voxelStruct.meshUV);
                    
                    vertexCount++;
                }
            }
        }

        mesh.Clear();
        mesh.vertices = meshVertices.ToArray();
        mesh.triangles = meshTriangles.ToArray();
        mesh.normals = meshNormals.ToArray();
        mesh.uv = meshUVs.ToArray();

        meshCollider.sharedMesh = mesh;
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

    private void CreateDestructibleFromFloatingVoxels(int floatingVoxelStructCount)
    {
        // Create destructible gameobject
        GameObject newDestructibleGameObject = new GameObject(destructibleTransform.name);
        newDestructibleGameObject.layer = LayerMask.NameToLayer("Destructible");
        newDestructibleGameObject.transform.position = destructibleTransform.position;
        newDestructibleGameObject.transform.rotation = destructibleTransform.rotation;

        // Initialized destructible script
        Destructible newDestructible = newDestructibleGameObject.AddComponent<Destructible>();
        newDestructible.wasFloating = true;
        newDestructible.remainingVoxelStructCount = floatingVoxelStructCount;
        newDestructible.minVoxelCount = minVoxelCount;

        // Add Physics to destructible
        newDestructible.rigidBody = newDestructibleGameObject.AddComponent<Rigidbody>();
        newDestructible.meshCollider = newDestructibleGameObject.AddComponent<MeshCollider>();
        newDestructible.meshCollider.convex = true;

        // Set Mesh Of destructible
        newDestructible.mesh = new Mesh();
        newDestructible.mesh.vertices = mesh.vertices;
        newDestructible.mesh.normals = mesh.normals;
        newDestructible.mesh.uv = mesh.uv;
        newDestructibleGameObject.AddComponent<MeshFilter>().mesh = newDestructible.mesh;

        // Assign Material
        newDestructibleGameObject.AddComponent<MeshRenderer>().material = material;
        newDestructible.material = material;
        
        // Copy over VoxelStructs
        for (int voxelIndex = 0; voxelIndex < voxelStructs.Count; voxelIndex++)
        {
            VoxelStruct parentVoxelStruct = voxelStructs[voxelIndex];

            bool isNewVoxelSeperated = parentVoxelStruct.isSeperated || !parentVoxelStruct.isFloating;
            bool isNewVoxelExposed = parentVoxelStruct.isExposed && parentVoxelStruct.isFloating;

            VoxelStruct newVoxelStruct = new VoxelStruct(parentVoxelStruct.localPosition, Utility.CopyArray(parentVoxelStruct.drawFaces), isNewVoxelSeperated, false, isNewVoxelExposed, false, parentVoxelStruct.meshUV, parentVoxelStruct.color, newDestructible, parentVoxelStruct.adjacentVoxelIndexes);
            newDestructible.voxelStructs.Add(newVoxelStruct);
        }

        // Assign adjacent indexes
        for (int voxelIndex = 0; voxelIndex < voxelStructs.Count; voxelIndex++)
        {
            int adjacentVoxelIndex;
            for (int faceIndex = 0; faceIndex < (int)Voxel.Faces.SIZE; faceIndex++)
            {
                adjacentVoxelIndex = voxelStructs[voxelIndex].adjacentVoxelIndexes[faceIndex];
                if (adjacentVoxelIndex == -1)
                    continue;

                newDestructible.voxelStructs[voxelIndex].adjacentVoxelStructs[faceIndex] = newDestructible.voxelStructs[adjacentVoxelIndex];
            }
        }

        // Add force at top of new destructible away from player
        Vector3 launchPosition = destructibleTransform.TransformPoint(mesh.bounds.center) + destructibleTransform.up * mesh.bounds.extents.y;
        newDestructible.rigidBody.AddForceAtPosition(playerTransform.forward * floatingDestructibleStartLaunchMagnitude, launchPosition);
        newDestructible.rigidBody.velocity = playerTransform.forward * floatingDestructibleStartVelocityMagnitude;
    }
}
