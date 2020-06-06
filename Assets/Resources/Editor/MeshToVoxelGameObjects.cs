using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;
using GitHub.Unity;
using UnityEngine.ParticleSystemJobs;

public class MeshToVoxelGameObjects : EditorWindow
{
    const string assetsFolderPath = "Assets/Generated Meshes";
    const string prefabsFolderPath = "Assets/Resources/Prefabs/Destructible";

    string meshName = "Default";

    [MenuItem("Tools/Mesh To Voxel Game Objects")]
    static void CreateMeshToVoxelGameObjects()
    {
        EditorWindow.GetWindow<MeshToVoxelGameObjects>();
    }

    private void OnGUI()
    {
        meshName = EditorGUILayout.TextField("Mesh Name: ", meshName);

        if (GUILayout.Button("Create Mesh"))
        {
            GameObject[] selection = Selection.gameObjects;

            for (int selectionIndex = selection.Length - 1; selectionIndex >= 0; --selectionIndex)
            {
                GameObject selected = selection[selectionIndex];

                Selection.activeGameObject = TurnMeshIntoVoxelGameObjects(selected, meshName);

                //FileUtil.DeleteFileOrDirectory(prefabsFolderPath + "/" + meshName);
                //GameObject prefab = PrefabUtility.SaveAsPrefabAsset(newGameObject, prefabsFolderPath + "/" + meshName + ".prefab");

                //Selection.activeObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

                //DestroyImmediate(newGameObject);
                //UnityEditor.AssetDatabase.Refresh();
                //EditorApplication.RepaintHierarchyWindow();

                //AssetDatabase.SaveAssets();
            }
        }

        GUI.enabled = false;
        EditorGUILayout.LabelField("Selection count: " + Selection.objects.Length);
    }

    private GameObject TurnMeshIntoVoxelGameObjects(GameObject selectedGameObject, string meshName)
    {
        GameObject newGameObject = new GameObject(meshName);
        newGameObject.transform.localPosition = selectedGameObject.transform.localPosition;
        newGameObject.transform.localRotation = selectedGameObject.transform.localRotation;
        newGameObject.transform.localScale = selectedGameObject.transform.localScale;
        //newGameObject.layer = LayerMask.NameToLayer("Destructible");
        //newGameObject.AddComponent<Destructible>();

        //FileUtil.DeleteFileOrDirectory(assetsFolderPath + "/" + meshName);
        //UnityEditor.AssetDatabase.Refresh();
        //AssetDatabase.CreateFolder(assetsFolderPath, meshName);
        //AssetDatabase.CreateFolder(assetsFolderPath + "/" + meshName, "Meshes");
        //AssetDatabase.CreateFolder(assetsFolderPath + "/" + meshName, "Materials");

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
            Vector3 vertice1 = vertices[indices[i * 3]];
            Vector3 vertice2 = vertices[indices[i * 3 + 1]];
            Vector3 vertice3 = vertices[indices[i * 3 + 2]];

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
        Vector3 bottomLeftPointOfColliderVoxelCentered = bottomLeftPointOfCollider + (right * 0.5f * Common.VOXEL_SIZE) + (up * 0.5f * Common.VOXEL_SIZE) - (forward * 0.5f * Common.VOXEL_SIZE);

        int rightVoxelCount = (int)Mathf.Max((rightSize / Common.VOXEL_SIZE), 1);
        int upVoxelCount = (int)Mathf.Max((upSize / Common.VOXEL_SIZE), 1);
        int forwardVoxelCount = (int)Mathf.Max((forwardSize / Common.VOXEL_SIZE), 1);

        bool insideMesh = false;

        List<VoxelStruct> voxelStructs = new List<VoxelStruct>();
        for (int rightIndex = 0; rightIndex < rightVoxelCount; rightIndex++)
        {
            for (int upIndex = 0; upIndex < upVoxelCount; upIndex++)
            {
                for (int forwardIndex = 0; forwardIndex < forwardVoxelCount + 1; forwardIndex++)
                {
                    Vector3 pointA = bottomLeftPointOfColliderVoxelCentered + (right * rightIndex * Common.VOXEL_SIZE) + (up * upIndex * Common.VOXEL_SIZE) + (forward * forwardIndex * Common.VOXEL_SIZE);
                    Vector3 pointB = pointA + forward * Common.VOXEL_SIZE;

                    RaycastHit raycastHit;
                    if (Physics.Raycast(pointA, pointB - pointA, out raycastHit, Common.VOXEL_SIZE))
                    {
                        GameObject hitObject = raycastHit.collider.gameObject;

                        if (!hitObject.transform.IsChildOf(newGameObject.transform))
                            continue;

                        insideMesh = !insideMesh;

                        Vector3 voxelPosition = pointA - (right * Common.VOXEL_SIZE * 0.5f) - (up * Common.VOXEL_SIZE * 0.5f) + (forward * 0.5f * Common.VOXEL_SIZE);
                        // GameObject sphere = Utility.DebugDrawSphere(raycastHit.point, 0.01f, new Color(1, 0, 0), 10);
                        //GameObject sphere = Utility.DebugDrawSphere(voxelPosition, 0.01f, new Color(1, 0, 0), 10);
                        //sphere.transform.SetParent(newGameObject.transform);
                    }

                    if (insideMesh)
                    {
                        Vector3 voxelPosition = pointA - (right * Common.VOXEL_SIZE * 0.5f) - (up * Common.VOXEL_SIZE * 0.5f);
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

        // FRONT FACE
        int triangleVerticeStartIndex = 0;

        triangles.Add(triangleVerticeStartIndex);
        triangles.Add(triangleVerticeStartIndex + 1);
        triangles.Add(triangleVerticeStartIndex + 2);
        triangles.Add(triangleVerticeStartIndex);
        triangles.Add(triangleVerticeStartIndex + 2);
        triangles.Add(triangleVerticeStartIndex + 3);

        vertices.Add(Vector3.zero);
        vertices.Add((Vector3.up * Common.VOXEL_SIZE));
        vertices.Add((Vector3.up * Common.VOXEL_SIZE) + (Vector3.right * Common.VOXEL_SIZE));
        vertices.Add((Vector3.right * Common.VOXEL_SIZE));

        normals.Add(-Vector3.forward);
        normals.Add(-Vector3.forward);
        normals.Add(-Vector3.forward);
        normals.Add(-Vector3.forward);

        // RIGHT FACE
        triangleVerticeStartIndex = vertices.Count;

        triangles.Add(triangleVerticeStartIndex);
        triangles.Add(triangleVerticeStartIndex + 1);
        triangles.Add(triangleVerticeStartIndex + 2);
        triangles.Add(triangleVerticeStartIndex);
        triangles.Add(triangleVerticeStartIndex + 2);
        triangles.Add(triangleVerticeStartIndex + 3);

        vertices.Add((Vector3.right * Common.VOXEL_SIZE));
        vertices.Add((Vector3.right * Common.VOXEL_SIZE) + (Vector3.up * Common.VOXEL_SIZE));
        vertices.Add((Vector3.right * Common.VOXEL_SIZE) + (Vector3.up * Common.VOXEL_SIZE) + (Vector3.forward * Common.VOXEL_SIZE));
        vertices.Add((Vector3.right * Common.VOXEL_SIZE) + (Vector3.forward * Common.VOXEL_SIZE));

        normals.Add(Vector3.right);
        normals.Add(Vector3.right);
        normals.Add(Vector3.right);
        normals.Add(Vector3.right);


        // TOP FACE
        triangleVerticeStartIndex = vertices.Count;

        triangles.Add(triangleVerticeStartIndex);
        triangles.Add(triangleVerticeStartIndex + 1);
        triangles.Add(triangleVerticeStartIndex + 2);
        triangles.Add(triangleVerticeStartIndex);
        triangles.Add(triangleVerticeStartIndex + 2);
        triangles.Add(triangleVerticeStartIndex + 3);

        vertices.Add((Vector3.up * Common.VOXEL_SIZE));
        vertices.Add((Vector3.up * Common.VOXEL_SIZE) + (Vector3.forward * Common.VOXEL_SIZE));
        vertices.Add((Vector3.up * Common.VOXEL_SIZE) + (Vector3.forward * Common.VOXEL_SIZE) + (Vector3.right * Common.VOXEL_SIZE));
        vertices.Add((Vector3.up * Common.VOXEL_SIZE) + (Vector3.right * Common.VOXEL_SIZE));

        normals.Add(Vector3.up);
        normals.Add(Vector3.up);
        normals.Add(Vector3.up);
        normals.Add(Vector3.up);

        // LEFT FACE
        triangleVerticeStartIndex = vertices.Count;

        triangles.Add(triangleVerticeStartIndex);
        triangles.Add(triangleVerticeStartIndex + 2);
        triangles.Add(triangleVerticeStartIndex + 1);
        triangles.Add(triangleVerticeStartIndex);
        triangles.Add(triangleVerticeStartIndex + 3);
        triangles.Add(triangleVerticeStartIndex + 2);

        vertices.Add(Vector3.zero);
        vertices.Add((Vector3.up * Common.VOXEL_SIZE));
        vertices.Add((Vector3.up * Common.VOXEL_SIZE) + (Vector3.forward * Common.VOXEL_SIZE));
        vertices.Add((Vector3.forward * Common.VOXEL_SIZE));

        normals.Add(-Vector3.right);
        normals.Add(-Vector3.right);
        normals.Add(-Vector3.right);
        normals.Add(-Vector3.right);

        // BOTTOM FACE
        triangleVerticeStartIndex = vertices.Count;

        triangles.Add(triangleVerticeStartIndex);
        triangles.Add(triangleVerticeStartIndex + 2);
        triangles.Add(triangleVerticeStartIndex + 1);
        triangles.Add(triangleVerticeStartIndex);
        triangles.Add(triangleVerticeStartIndex + 3);
        triangles.Add(triangleVerticeStartIndex + 2);

        vertices.Add(Vector3.zero);
        vertices.Add(Vector3.zero + (Vector3.forward * Common.VOXEL_SIZE));
        vertices.Add(Vector3.zero + (Vector3.forward * Common.VOXEL_SIZE) + (Vector3.right * Common.VOXEL_SIZE));
        vertices.Add(Vector3.zero + (Vector3.right * Common.VOXEL_SIZE));

        normals.Add(-Vector3.up);
        normals.Add(-Vector3.up);
        normals.Add(-Vector3.up);
        normals.Add(-Vector3.up);

        // BACK FACE
        triangleVerticeStartIndex = vertices.Count;

        triangles.Add(triangleVerticeStartIndex);
        triangles.Add(triangleVerticeStartIndex + 2);
        triangles.Add(triangleVerticeStartIndex + 1);
        triangles.Add(triangleVerticeStartIndex);
        triangles.Add(triangleVerticeStartIndex + 3);
        triangles.Add(triangleVerticeStartIndex + 2);

        vertices.Add(Vector3.zero + (Vector3.forward * Common.VOXEL_SIZE));
        vertices.Add(Vector3.zero + (Vector3.forward * Common.VOXEL_SIZE) + (Vector3.up * Common.VOXEL_SIZE));
        vertices.Add(Vector3.zero + (Vector3.forward * Common.VOXEL_SIZE) + (Vector3.up * Common.VOXEL_SIZE) + (Vector3.right * Common.VOXEL_SIZE));
        vertices.Add(Vector3.zero + (Vector3.forward * Common.VOXEL_SIZE) + (Vector3.right * Common.VOXEL_SIZE));

        normals.Add(Vector3.forward);
        normals.Add(Vector3.forward);
        normals.Add(Vector3.forward);
        normals.Add(Vector3.forward);

        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = voxelStruct.material;

        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();

        meshFilter.mesh = mesh;

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