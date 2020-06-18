using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Permissions;
using UnityEngine;

public class Destructible : MonoBehaviour
{
    private Game game;
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

    public string binaryFilePath = null;
    private List<VoxelStruct> voxelStructs = new List<VoxelStruct>();
    private HashSet<VoxelStruct> anchorVoxelStructs = new HashSet<VoxelStruct>();
    private List<VoxelStruct> exposedVoxelStructs = new List<VoxelStruct>();
    private List<VoxelStruct> floatingVoxelStructs = new List<VoxelStruct>();

    private List<int> meshTriangles = new List<int>();
    private List<Vector3> meshVertices = new List<Vector3>();
    private List<Vector3> meshNormals = new List<Vector3>();
    private List<Vector2> meshUVs = new List<Vector2>();

    private int minVoxelCount = 0;
    private int seperatedVoxelIndex = 0;
    private const float minVoxelCountAnchorScalar = 2.25f;

    private const float findHitVoxelPositionDepth = 4.0f;
    private const float floatingDestructibleStartVelocityMagnitude = 1.0f;
    private const float floatingDestructibleStartLaunchMagnitude = 30.0f;
    private const float floatingTakeDamageAddForceMagnitude = 20000.0f;
    private const float floatingFallDamageMinVelocitySquared = 0.7f * 0.7f;
    private const float floatingFallDamageMinAngularVelocitySquared = 0.7f * 0.7f;
    private const float floatingDropVoxelRadiusSquared = 0.6f * 0.6f;

    private float nextFallDamageTime;
    private const float fallDamageFrequencyMin = 0.5f;
    private const float fallDamageRadius = 0.5f;
    private bool wasFloating;

    private static readonly int[] VoxelAdjacentFaces = Utility.GetVoxelAdjacentFaces();
    private static readonly int[][] VertexIndexFaceTriangleAdditions = Voxel.VertexIndexFaceTriangleAdditions;
    private static readonly Vector3[][] VertexVectorAdditions = Voxel.VertexVectorAdditions;
    private static readonly Vector3[] VertexNormals = Voxel.VertexNormals;

    void Awake()
    {
        game = Game.GetGame();
        destructibleVoxelDictionaries = game.destructibleVoxelDictionaries;
        seperatedVoxelDictionaries = game.seperatedVoxelDictionaries;
        destructibleVoxels = game.destructibleVoxels;
        seperatedVoxels = game.seperatedVoxels;

        destructibleTransform = transform;

        meshCollider = GetComponent<MeshCollider>();
        mesh = GetComponent<MeshFilter>().mesh;
        material = GetComponent<MeshRenderer>().material;
        thisDestructible = this;

        if (binaryFilePath != null)
        {
            ConvertVoxelExportsIntoVoxelStructs();

            meshTriangles = new List<int>(mesh.triangles.Length);
            meshVertices = new List<Vector3>(mesh.vertexCount);
            meshNormals = new List<Vector3>(mesh.vertexCount);
            meshUVs = new List<Vector2>(mesh.vertexCount);
        }
    }

    private void ConvertVoxelExportsIntoVoxelStructs()
    {
        BinaryFormatter binaryFormatter = new BinaryFormatter();
        FileStream voxelExportBinaryFile = File.Open(binaryFilePath, FileMode.Open);
        List<VoxelExport> voxelExports = binaryFormatter.Deserialize(voxelExportBinaryFile) as List<VoxelExport>;
        voxelExportBinaryFile.Close();

        for (int index = 0; index < voxelExports.Count; index++)
        {
            VoxelExport voxelExport = voxelExports[index];
            VoxelStruct voxelStruct = new VoxelStruct(new Vector3(voxelExport.localPositionX, voxelExport.localPositionY, voxelExport.localPositionZ), voxelExport.drawFaces, voxelExport.isAnchor, voxelExport.isExposed, false, new Vector2(voxelExport.meshU, voxelExport.meshV), new Color(voxelExport.colorR, voxelExport.colorG, voxelExport.colorB), thisDestructible, new Vector3(voxelExport.localNormalX, voxelExport.localNormalY, voxelExport.localNormalZ));

            if (voxelStruct.isAnchor)
            {
                anchorVoxelStructs.Add(voxelStruct);
                minVoxelCount++;
            }

            if(voxelStruct.isExposed)
            {
                ExposeVoxelStruct(voxelStruct);
            }

            voxelStruct.listIndex = voxelStructs.Count;

            voxelStructs.Add(voxelStruct);
        }

        // Assign Adjacent Voxel Structs by adjacent Indexes
        for(int voxelIndex = 0; voxelIndex < voxelStructs.Count; voxelIndex++)
        {
            int adjacentVoxelIndex;
            for (int faceIndex = 0; faceIndex < (int)Voxel.Faces.SIZE; faceIndex++)
            {
                adjacentVoxelIndex = voxelExports[voxelIndex].adjacentVoxelExportIndexes[faceIndex];
                if (adjacentVoxelIndex == -1)
                    continue;

                voxelStructs[voxelIndex].adjacentVoxelStructs[faceIndex] = voxelStructs[adjacentVoxelIndex];
            }
        }

        minVoxelCount = (int)(minVoxelCount * minVoxelCountAnchorScalar);
    }

    public void AssignDestructibleVoxels()
    {
        // Assign DestructibleVoxel to ExposedStructs
        int destructibleVoxelIndex = 0;
        for (int voxelIndex = 0; voxelIndex < exposedVoxelStructs.Count; voxelIndex++)
        {
            VoxelStruct exposedVoxelStruct = exposedVoxelStructs[voxelIndex];
            if (exposedVoxelStruct.destructibleVoxel != null)
                continue;

            while (destructibleVoxelIndex < destructibleVoxels.Count)
            {
                DestructibleVoxel destructibleVoxel = destructibleVoxels[destructibleVoxelIndex];
                destructibleVoxelIndex++;

                if (destructibleVoxel.active && destructibleVoxel.destructible == thisDestructible)
                    continue;

                destructibleVoxel.SetActive(exposedVoxelStruct, thisDestructible, destructibleTransform);

                break;
            }
        }
    }

    public void TakeDamage(Vector3 hitPosition, float damageRadius, Vector3 hitDirection)
    {
        // Find Accurate Voxel Contact Position
        Vector3 voxelHitPosition = FindHitVoxelContactPosition(hitPosition, hitDirection);
        if (voxelHitPosition == Vector3.negativeInfinity)
            return;

        int destructibleVoxelIndex = 0;
        seperatedVoxelIndex = 0;
        Collider[] hitColliders = Physics.OverlapCapsule(hitPosition, voxelHitPosition, damageRadius, LayerMask.GetMask("DestructibleVoxel"));
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

            // Set Adjacent Voxel Faces To Draw
            for (int adjacentFaceIndex = 0; adjacentFaceIndex < (int)Voxel.Faces.SIZE; adjacentFaceIndex++)
            {
                VoxelStruct adjacentVoxelStruct = hitVoxelStruct.adjacentVoxelStructs[adjacentFaceIndex];
                if (adjacentVoxelStruct == null)
                    continue;

                adjacentVoxelStruct.drawFaces[VoxelAdjacentFaces[adjacentFaceIndex]] = true;

                // Expose Adjacent Voxel And Move Destructible Voxel Into Place
                if (!adjacentVoxelStruct.isExposed)
                {
                    ExposeVoxelStruct(adjacentVoxelStruct);

                    while (destructibleVoxelIndex < destructibleVoxels.Count)
                    {
                        DestructibleVoxel destructibleVoxel = destructibleVoxels[destructibleVoxelIndex];
                        destructibleVoxelIndex++;

                        if (destructibleVoxel.active && destructibleVoxel.destructible == thisDestructible)
                            continue;

                        destructibleVoxel.SetActive(adjacentVoxelStruct, thisDestructible, destructibleTransform);

                        break;
                    }
                }
            }

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

            SeperateVoxelStruct(hitVoxelStruct);
        }

        CheckFloatingVoxels(hitDirection);

        CheckMinVoxelThreshold();

        if (voxelStructs.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        UpdateMesh();

        UpdateRigidBody(hitDirection);
    }

    private Vector3 FindHitVoxelContactPosition(Vector3 hitPosition, Vector3 hitDirection)
    {
        RaycastHit raycastHit;
        Vector3 voxelHitPosition = Vector3.negativeInfinity;
        if (Physics.Raycast(hitPosition, hitDirection, out raycastHit, findHitVoxelPositionDepth, LayerMask.GetMask("DestructibleVoxel")))
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

        if (voxelStruct.isAnchor)
        {
            voxelStruct.isAnchor = false;
            anchorVoxelStructs.Remove(voxelStruct);
        }

        if (voxelStruct.isExposed)
        {
            // Swap this exposedVoxelStruct list placement with last index in array
            exposedVoxelStructs[voxelStruct.exposedListIndex] = exposedVoxelStructs[exposedVoxelStructs.Count - 1];
            exposedVoxelStructs[voxelStruct.exposedListIndex].exposedListIndex = voxelStruct.exposedListIndex;

            // Remove off end since we moved this one into removed struct index
            exposedVoxelStructs.RemoveAt(exposedVoxelStructs.Count - 1);
        }

        // Set adjacent voxels references to seperated voxel null
        for (int adjacentFaceIndex = 0; adjacentFaceIndex < (int)Voxel.Faces.SIZE; adjacentFaceIndex++)
        {
            VoxelStruct adjacentVoxelStruct = voxelStruct.adjacentVoxelStructs[adjacentFaceIndex];
            if (adjacentVoxelStruct == null)
                continue;

            adjacentVoxelStruct.adjacentVoxelStructs[VoxelAdjacentFaces[adjacentFaceIndex]] = null;
        }

        // Swap this voxelStruct list placement with last index in array
        voxelStructs[voxelStruct.listIndex] = voxelStructs[voxelStructs.Count - 1];
        voxelStructs[voxelStruct.listIndex].listIndex = voxelStruct.listIndex;

        // Remove off end since we moved this one into removed struct index
        voxelStructs.RemoveAt(voxelStructs.Count - 1);
    }

    private void ExposeVoxelStruct(VoxelStruct voxelStruct)
    {
        voxelStruct.exposedListIndex = exposedVoxelStructs.Count;
        exposedVoxelStructs.Add(voxelStruct);

        voxelStruct.isExposed = true;
    }

    private void CheckFloatingVoxels(Vector3 hitDirection)
    {
        if (wasFloating)
        {
            // Flood Fill From First Attached Voxel
            FloodFillVoxelStruct(voxelStructs[0]);
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
        for (int voxelIndex = 0; voxelIndex < voxelStructs.Count; voxelIndex++)
        {
            VoxelStruct voxelStruct = voxelStructs[voxelIndex];

            if (voxelStruct.checkedForFloatingThisFrame)
            {
                voxelStruct.checkedForFloatingThisFrame = false;
                continue;
            }

            voxelStruct.isFloating = true;

            floatingVoxelStructs.Add(voxelStruct);
        }

        if (floatingVoxelStructs.Count == 0)
            return;

        // Launch All Voxel Structs if we are at minVoxelCount
        if ((voxelStructs.Count - floatingVoxelStructs.Count) < minVoxelCount)
        {
            if (floatingVoxelStructs.Count >= minVoxelCount)
            {
                CreateDestructibleFromFloatingVoxels(hitDirection);

                for (int voxelIndex = 0; voxelIndex < voxelStructs.Count; voxelIndex++)
                {
                    VoxelStruct voxelStruct = voxelStructs[voxelIndex];
                    if (voxelStruct == null)
                        continue;

                    if (voxelStruct.isFloating)
                        continue;

                    SeperateVoxelStruct(voxelStruct);

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
            }
            else
            {
                for (int voxelIndex = 0; voxelIndex < voxelStructs.Count; voxelIndex++)
                {
                    VoxelStruct voxelStruct = voxelStructs[voxelIndex];
                    if (voxelStruct == null)
                        continue;

                    SeperateVoxelStruct(voxelStruct);

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
            }
        }
        // Launch All Floating Voxels
        else if (floatingVoxelStructs.Count >= minVoxelCount)
        {
            CreateDestructibleFromFloatingVoxels(hitDirection);
        }
    }

    private void FloodFillVoxelStruct(VoxelStruct mainVoxelStruct)
    {
        if (mainVoxelStruct.checkedForFloatingThisFrame)
            return;

        mainVoxelStruct.checkedForFloatingThisFrame = true;

        for (int adjacentFaceIndex = 0; adjacentFaceIndex < (int)Voxel.Faces.SIZE; adjacentFaceIndex++)
        {
            VoxelStruct adjacentVoxelStruct = mainVoxelStruct.adjacentVoxelStructs[adjacentFaceIndex];
            if (adjacentVoxelStruct == null)
                continue;

            if (adjacentVoxelStruct.checkedForFloatingThisFrame)
                continue;

            FloodFillVoxelStruct(adjacentVoxelStruct);
        }
    }

    private void CheckMinVoxelThreshold()
    {
        if (voxelStructs.Count >= minVoxelCount)
            return;

        for (int voxelIndex = voxelStructs.Count - 1; voxelIndex >= 0; voxelIndex--)
        {
            if (voxelStructs[voxelIndex].destructibleVoxel != null)
            {
                voxelStructs[voxelIndex].destructibleVoxel.SetInactive();
            }

            // Teleport and launch first available seperated voxel
            while (seperatedVoxelIndex < seperatedVoxels.Count)
            {
                SeperatedVoxel seperatedVoxel = seperatedVoxels[seperatedVoxelIndex];
                seperatedVoxelIndex++;

                if (seperatedVoxel.active)
                    continue;

                seperatedVoxel.SetActive(voxelStructs[voxelIndex], destructibleTransform);

                break;
            }

            voxelStructs.RemoveAt(voxelIndex);
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
        for (int voxelIndex = 0; voxelIndex < exposedVoxelStructs.Count; voxelIndex++)
        {
            VoxelStruct exposedVoxelStruct = exposedVoxelStructs[voxelIndex];

            drawFaces = exposedVoxelStruct.drawFaces;
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
                    meshVertices.Add(exposedVoxelStruct.localPosition + VertexVectorAdditions[faceIndex][vertexIndex]);
                    
                    meshNormals.Add(VertexNormals[faceIndex]);
                    meshUVs.Add(exposedVoxelStruct.meshUV);
                    
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

    private void UpdateRigidBody(Vector3 hitDirection)
    {
        if (!wasFloating)
            return;

        rigidBody.mass = voxelStructs.Count * Voxel.MASS;
        nextFallDamageTime = Time.time + fallDamageFrequencyMin;
        rigidBody.AddForce(hitDirection * floatingTakeDamageAddForceMagnitude);
    }

    private Vector3 GetVoxelStructLaunchForce(VoxelStruct voxelStruct)
    {
        Vector3 launchForward = destructibleTransform.TransformDirection(voxelStruct.localNormal);
        Vector3 launchRight = Vector3.Cross(launchForward, Vector3.up);
        Vector3 launchVector = Vector3.zero;
        launchVector += launchForward * Random.Range(500, 550);
        launchVector += launchRight * Random.Range(150, 200) * Utility.RandomSign();
        launchVector += Vector3.up * Random.Range(100, 150) * Utility.RandomSign();

        return launchVector;
    }

    private void CreateDestructibleFromFloatingVoxels(Vector3 hitDirection)
    {
        // Create destructible gameobject
        GameObject newDestructibleGameObject = new GameObject(destructibleTransform.name);
        newDestructibleGameObject.layer = LayerMask.NameToLayer("Destructible");
        newDestructibleGameObject.transform.position = destructibleTransform.position;
        newDestructibleGameObject.transform.rotation = destructibleTransform.rotation;

        // Add Collider to destructible
        MeshCollider newMeshCollider = newDestructibleGameObject.AddComponent<MeshCollider>();
        newMeshCollider.convex = true;

        // Set Mesh Of destructible
        Mesh newMesh = new Mesh();
        newMesh.vertices = mesh.vertices;
        newMesh.normals = mesh.normals;
        newMesh.uv = mesh.uv;
        newDestructibleGameObject.AddComponent<MeshFilter>().mesh = newMesh;

        // Assign Material
        newDestructibleGameObject.AddComponent<MeshRenderer>().material = material;

        // Initialized destructible script
        Destructible newDestructible = newDestructibleGameObject.AddComponent<Destructible>();
        newDestructible.wasFloating = true;
        newDestructible.minVoxelCount = minVoxelCount;
        newDestructible.rigidBody = newDestructibleGameObject.AddComponent<Rigidbody>();

        // Remove Voxels From Parent Voxel Lists
        for (int voxelIndex = 0; voxelIndex < floatingVoxelStructs.Count; voxelIndex++)
        {
            VoxelStruct voxelStruct = floatingVoxelStructs[voxelIndex];

            if (voxelStruct.isAnchor)
            {
                anchorVoxelStructs.Remove(voxelStruct);
            }

            if (voxelStruct.isExposed)
            {
                // Swap this exposedVoxelStruct list placement with last index in array
                exposedVoxelStructs[voxelStruct.exposedListIndex] = exposedVoxelStructs[exposedVoxelStructs.Count - 1];
                exposedVoxelStructs[voxelStruct.exposedListIndex].exposedListIndex = voxelStruct.exposedListIndex;

                // Remove off end since we moved this one into removed struct index
                exposedVoxelStructs.RemoveAt(exposedVoxelStructs.Count - 1);
            }

            // Swap this voxelStruct list placement with last index in array
            voxelStructs[voxelStruct.listIndex] = voxelStructs[voxelStructs.Count - 1];
            voxelStructs[voxelStruct.listIndex].listIndex = voxelStruct.listIndex;

            // Remove off end since we moved this one into removed struct index
            voxelStructs.RemoveAt(voxelStructs.Count - 1);
        }

        // Take Floating VoxelsStructs and Build New Destructible Voxel Lists
        for (int voxelIndex = 0; voxelIndex < floatingVoxelStructs.Count; voxelIndex++)
        {
            VoxelStruct voxelStruct = floatingVoxelStructs[voxelIndex];
            voxelStruct.parentDestructible = newDestructible;
            voxelStruct.checkedForFloatingThisFrame = false;

            if (voxelStruct.destructibleVoxel != null)
            {
                voxelStruct.destructibleVoxel.SetInactive();
            }

            if (voxelStruct.isAnchor)
            {
                voxelStruct.isAnchor = false;
            }

            if (voxelStruct.isExposed)
            {
                voxelStruct.exposedListIndex = newDestructible.exposedVoxelStructs.Count;
                newDestructible.exposedVoxelStructs.Add(voxelStruct);
            }

            // Clear references if they are not floating in this chunk
            for (int adjacentFaceIndex = 0; adjacentFaceIndex < (int)Voxel.Faces.SIZE; adjacentFaceIndex++)
            {
                VoxelStruct adjacentVoxelStruct = voxelStruct.adjacentVoxelStructs[adjacentFaceIndex];
                if (adjacentVoxelStruct == null)
                    continue;

                if (adjacentVoxelStruct.isFloating)
                    continue;

                adjacentVoxelStruct.adjacentVoxelStructs[VoxelAdjacentFaces[adjacentFaceIndex]] = null;
                voxelStruct.adjacentVoxelStructs[adjacentFaceIndex] = null;
            }

            voxelStruct.listIndex = voxelIndex;
            newDestructible.voxelStructs.Add(voxelStruct);
        }

        newDestructible.UpdateMesh();

        // Add force at top of new destructible
        Utility.FlattenVector(hitDirection);
        Vector3 launchPosition = destructibleTransform.TransformPoint(mesh.bounds.center) + destructibleTransform.up * mesh.bounds.extents.y;
        newDestructible.rigidBody.mass = newDestructible.voxelStructs.Count * Voxel.MASS;
        newDestructible.rigidBody.AddForceAtPosition(hitDirection * floatingDestructibleStartLaunchMagnitude, launchPosition);
        newDestructible.rigidBody.velocity = hitDirection * floatingDestructibleStartVelocityMagnitude;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // FallDamage(collision.contacts[0].point);
    }

    private void FallDamage(Vector3 position)
    {
        if (!wasFloating)
            return;

        if (rigidBody.velocity.sqrMagnitude < floatingFallDamageMinVelocitySquared)
            return;

        if (Time.time < nextFallDamageTime)
            return;

        nextFallDamageTime = Time.time + fallDamageFrequencyMin;

        TakeCollisionDamage(position, fallDamageRadius);
    }

    public void TakeCollisionDamage(Vector3 hitPosition, float damageRadius)
    {
        Vector3 localHitPosition = destructibleTransform.InverseTransformPoint(hitPosition);

        int destructibleVoxelIndex = 0;
        seperatedVoxelIndex = 0;
        for (int voxelIndex = 0; voxelIndex < voxelStructs.Count; voxelIndex++)
        {
            VoxelStruct voxelStruct = voxelStructs[voxelIndex];

            if (Vector3.SqrMagnitude(voxelStruct.localPosition + Voxel.HALF_EXTENTS - localHitPosition) > damageRadius * damageRadius)
                continue;

            // Set Adjacent Voxel Faces To Draw
            for (int adjacentFaceIndex = 0; adjacentFaceIndex < (int)Voxel.Faces.SIZE; adjacentFaceIndex++)
            {
                VoxelStruct adjacentVoxelStruct = voxelStruct.adjacentVoxelStructs[adjacentFaceIndex];
                if (adjacentVoxelStruct == null)
                    continue;

                adjacentVoxelStruct.drawFaces[VoxelAdjacentFaces[adjacentFaceIndex]] = true;

                // Expose Adjacent Voxel And Move Destructible Voxel Into Place
                if (!adjacentVoxelStruct.isExposed)
                {
                    ExposeVoxelStruct(adjacentVoxelStruct);

                    while (destructibleVoxelIndex < destructibleVoxels.Count)
                    {
                        DestructibleVoxel destructibleVoxel = destructibleVoxels[destructibleVoxelIndex];
                        destructibleVoxelIndex++;

                        if (destructibleVoxel.active && destructibleVoxel.destructible == thisDestructible)
                            continue;

                        destructibleVoxel.SetActive(adjacentVoxelStruct, thisDestructible, destructibleTransform);

                        break;
                    }
                }
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

            SeperateVoxelStruct(voxelStruct);
        }

        //CheckFloatingVoxels(new Vector3( 0, 0, 1));

        //CheckMinVoxelThreshold();

        //if (voxelStructs.Count == 0)
        //{
        //    Destroy(gameObject);
        //    return;
        //}

        UpdateMesh();
    }
}
