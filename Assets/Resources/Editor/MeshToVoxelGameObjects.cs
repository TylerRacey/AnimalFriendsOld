using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;
using GitHub.Unity;
using UnityEngine.ParticleSystemJobs;

public class MeshToVoxelGameObjects : EditorWindow
{
    Mesh voxelMesh;
    const string prefabsFolderPath = "Assets/Resources/Prefabs/Destructible";

    string objectName = "Default";

    [MenuItem("Tools/Mesh To Voxel Game Objects")]
    static void CreateMeshToVoxelGameObjects()
    {
        EditorWindow.GetWindow<MeshToVoxelGameObjects>();
    }

    private void OnGUI()
    {
        objectName = EditorGUILayout.TextField("Mesh Name: ", objectName);

        if (GUILayout.Button("Create Game Objects"))
        {
            voxelMesh = Resources.Load(Voxel.defaultMeshPath, typeof(Mesh)) as Mesh;

            GameObject[] selection = Selection.gameObjects;

            for (int selectionIndex = selection.Length - 1; selectionIndex >= 0; --selectionIndex)
            {
                GameObject selected = selection[selectionIndex];

                GameObject newGameObject = TurnMeshIntoVoxelGameObjects(selected, objectName);

                if (!AssetDatabase.IsValidFolder(prefabsFolderPath + "/" + objectName))
                {
                    AssetDatabase.CreateFolder(prefabsFolderPath, objectName);
                }
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(newGameObject, prefabsFolderPath + "/" + objectName + "/" + objectName + "_voxels.prefab");

                Selection.activeObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

                DestroyImmediate(newGameObject);
                UnityEditor.AssetDatabase.Refresh();
                EditorApplication.RepaintHierarchyWindow();

                AssetDatabase.SaveAssets();
            }
        }

        GUI.enabled = false;
        EditorGUILayout.LabelField("Selection count: " + Selection.objects.Length);
    }

    private GameObject TurnMeshIntoVoxelGameObjects(GameObject selectedGameObject, string objectName)
    {
        GameObject newGameObject = new GameObject(objectName + "_voxels");
        newGameObject.transform.localPosition = selectedGameObject.transform.localPosition;
        newGameObject.transform.localRotation = selectedGameObject.transform.localRotation;
        newGameObject.transform.localScale = selectedGameObject.transform.localScale;

        Material material = selectedGameObject.GetComponent<MeshRenderer>().sharedMaterial;

        BoxCollider boxCollider = selectedGameObject.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = selectedGameObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
        }

        // CREATE TEMP COLLIDERS
        Mesh mesh = selectedGameObject.GetComponent<MeshFilter>().sharedMesh;
        Vector3[] vertices = mesh.vertices;
        int[] indices = mesh.triangles;
        int triangleCount = indices.Length / 3;
        List<GameObject> tempGameObjects = new List<GameObject>();

        for (int i = 0; i < triangleCount; i++)
        {
            List<int> tempTriangles = new List<int>();
            List<Vector3> tempVertices = new List<Vector3>();

            tempVertices.Add(vertices[indices[i * 3]]);
            tempVertices.Add(vertices[indices[i * 3 + 1]]);
            tempVertices.Add(vertices[indices[i * 3 + 2]]);

            tempTriangles.Add(0);
            tempTriangles.Add(1);
            tempTriangles.Add(2);

            GameObject tempGameObject = new GameObject();
            tempGameObject.transform.position = selectedGameObject.transform.position;

            MeshFilter tempMeshFilter = tempGameObject.AddComponent<MeshFilter>();

            Mesh tempMesh = new Mesh();
            tempMesh.vertices = tempVertices.ToArray();
            tempMesh.triangles = tempTriangles.ToArray();

            tempMeshFilter.mesh = tempMesh;

            MeshRenderer meshRenderer = tempGameObject.AddComponent<MeshRenderer>();
            meshRenderer.material = material;

            tempGameObject.AddComponent<BoxCollider>();
            tempGameObject.transform.SetParent(newGameObject.transform);

            tempGameObjects.Add(tempGameObject);
        }

        Vector3 center = selectedGameObject.transform.TransformPoint(boxCollider.center);
        float rightSize = boxCollider.size.x;
        float forwardSize = boxCollider.size.z;
        float upSize = boxCollider.size.y;
        Vector3 right = selectedGameObject.transform.right;
        Vector3 up = selectedGameObject.transform.up;
        Vector3 forward = selectedGameObject.transform.forward;
        Vector3 bottomLeftPointOfCollider = center + (right * -rightSize * 0.5f) + (up * -upSize * 0.5f) + (forward * -forwardSize * 0.5f);
        Vector3 bottomLeftPointOfColliderVoxelCentered = bottomLeftPointOfCollider + (right * Voxel.HALF_SIZE) + (up * Voxel.HALF_SIZE) - (forward * Voxel.HALF_SIZE);

        int rightVoxelCount = (int)Mathf.Max((rightSize / Voxel.SIZE), 1);
        int upVoxelCount = (int)Mathf.Max((upSize / Voxel.SIZE), 1);
        int forwardVoxelCount = (int)Mathf.Max((forwardSize / Voxel.SIZE), 1);

        bool insideMesh = false;

        List<VoxelStruct> voxelStructs = new List<VoxelStruct>();
        for (int rightIndex = 0; rightIndex < rightVoxelCount; rightIndex++)
        {
            for (int upIndex = 0; upIndex < upVoxelCount; upIndex++)
            {
                for (int forwardIndex = 0; forwardIndex < forwardVoxelCount + 1; forwardIndex++)
                {
                    Vector3 pointA = bottomLeftPointOfColliderVoxelCentered + (right * rightIndex * Voxel.SIZE) + (up * upIndex * Voxel.SIZE) + (forward * forwardIndex * Voxel.SIZE);
                    Vector3 pointB = pointA + forward * Voxel.SIZE;

                    RaycastHit raycastHit;
                    if (Physics.Raycast(pointA, pointB - pointA, out raycastHit, Voxel.SIZE))
                    {
                        GameObject hitObject = raycastHit.collider.gameObject;

                        if (!hitObject.transform.IsChildOf(newGameObject.transform))
                            continue;

                        insideMesh = !insideMesh;
                    }

                    if (insideMesh)
                    {
                        Vector3 voxelPosition = pointA - (right * Voxel.HALF_SIZE) - (up * Voxel.HALF_SIZE);
                        VoxelStruct voxelStruct = new VoxelStruct(voxelPosition, forward, right, up, material);
                        voxelStructs.Add(voxelStruct);
                    }
                }
                insideMesh = false;
            }
        }

        List<GameObject> generatedVoxels = new List<GameObject>();
        foreach (VoxelStruct voxelStruct in voxelStructs)
        {
            GameObject generatedVoxel = GenerateVoxelFromVoxelStruct(voxelStruct);

            generatedVoxel.transform.SetParent(newGameObject.transform, true);
            generatedVoxels.Add(generatedVoxel);
        }

        // Temp Game Objects
        for (int index = 0; index < tempGameObjects.Count; index++)
        {
            DestroyImmediate(tempGameObjects[index]);
        }

        return newGameObject;
    }

    private GameObject GenerateVoxelFromVoxelStruct(VoxelStruct voxelStruct)
    {
        GameObject gameObject = new GameObject("DestructibleVoxel");
        gameObject.transform.position = voxelStruct.position;
        gameObject.layer = LayerMask.NameToLayer("DestructibleVoxel");

        List<int> triangles = new List<int>();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();

        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = voxelStruct.material;

        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = voxelMesh;

        return gameObject;
    }

    private struct VoxelStruct
    {
        public Vector3 position;
        public Vector3 forward;
        public Vector3 right;
        public Vector3 up;
        public Material material;

        public VoxelStruct(Vector3 _position, Vector3 _forward, Vector3 _right, Vector3 _up, Material _material)
        {
            position = _position;
            forward = _forward;
            right = _right;
            up = _up;
            material = _material;
        }
    }
}