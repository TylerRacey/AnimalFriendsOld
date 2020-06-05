using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class VoxelGameObjectToDestructiblePrefab : EditorWindow
{
    const string prefabsFolderPath = "Assets/Resources/Prefabs/Destructible";
    const string assetsFolderPath = "Assets/Resources/GeneratedMeshes";

    string meshName = "Default";

    [MenuItem("Tools/Voxel Game Objects To Destructible Prefab")]
    static void CreateVoxelGameObjectToDestructiblePrefab()
    {
        EditorWindow.GetWindow<VoxelGameObjectToDestructiblePrefab>();
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

                GameObject newGameObject = TurnVoxelsIntoGameObject(selected, meshName);

                FileUtil.DeleteFileOrDirectory(prefabsFolderPath + "/" + meshName + "/" + meshName + ".prefab");
                AssetDatabase.CreateFolder(prefabsFolderPath, meshName);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(newGameObject, prefabsFolderPath + "/" + meshName + "/" + meshName + ".prefab");

                Selection.activeObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

                DestroyImmediate(newGameObject);
                AssetDatabase.Refresh();
                EditorApplication.RepaintHierarchyWindow();

                AssetDatabase.SaveAssets();
            }
        }

        GUI.enabled = false;
        EditorGUILayout.LabelField("Selection count: " + Selection.objects.Length);
    }

    private GameObject TurnVoxelsIntoGameObject(GameObject selectedGameObject, string meshName)
    {
        GameObject newGameObject = new GameObject(meshName);
        newGameObject.transform.localPosition = selectedGameObject.transform.localPosition;
        newGameObject.transform.localRotation = selectedGameObject.transform.localRotation;
        newGameObject.transform.localScale = selectedGameObject.transform.localScale;
        newGameObject.layer = LayerMask.NameToLayer("Destructible");
        Destructible destructible = newGameObject.AddComponent<Destructible>();

        Transform[] allChildren = selectedGameObject.GetComponentsInChildren<Transform>();
        List<GameObject> voxels = new List<GameObject>();

        // Assign Box Collider To Each Voxel
        for (int index = 0; index < allChildren.Length; index++)
        {
            GameObject voxel = allChildren[index].gameObject;
            if (voxel.GetComponents(typeof(MeshRenderer)).Length <= 0)
            {
                continue;
            }

            if (voxel.GetComponents(typeof(BoxCollider)).Length <= 0)
            {
                voxel.AddComponent<BoxCollider>();
            }

            voxels.Add(voxel);
        }

        List<Color> voxelColors = new List<Color>();

        List<GameObject> generatedVoxels = new List<GameObject>();
        List<GameObject> exposedVoxels = new List<GameObject>();
        List<GameObject> anchorVoxels = new List<GameObject>();
        foreach (GameObject voxel in voxels)
        {
            BoxCollider boxCollider = voxel.GetComponent<BoxCollider>();
            Vector3 voxelCenter = boxCollider.bounds.center;

            bool[] drawFaces = new bool[(int)Common.VoxelFaces.SIZE];

            Vector3[] faceDirectionChecks = new Vector3[] { -voxel.transform.forward, voxel.transform.right, voxel.transform.up, -voxel.transform.right, -voxel.transform.up, voxel.transform.forward };

            bool isAnchor = false;
            bool isExposed = false;
            for (int directionIndex = 0; directionIndex < (int)Common.VoxelFaces.SIZE; directionIndex++)
            {
                RaycastHit raycastHit;
                if (Physics.Raycast(voxelCenter, faceDirectionChecks[directionIndex], out raycastHit, Common.VOXEL_SIZE))
                {
                    GameObject hitObject = raycastHit.collider.gameObject;

                    if (voxels.Contains(hitObject))
                        continue;

                    isAnchor = true;

                    drawFaces[directionIndex] = false;
                }
                else
                {
                    isExposed = true;
                    drawFaces[directionIndex] = true;
                }
            }

            Color voxelColor = voxel.GetComponent<MeshRenderer>().material.color;
            if (!voxelColors.Contains(voxelColor))
            {
                voxelColors.Add(voxelColor);
            }

            VoxelStruct voxelStruct = new VoxelStruct(voxel.transform.position, voxel.transform.forward, voxel.transform.right, voxel.transform.up, voxel.transform.rotation, voxelColor, drawFaces);

            GameObject generatedVoxel = GenerateVoxelFromVoxelStruct(voxelStruct, voxelColor, voxelColors);

            if(!voxelColors.Contains(voxelColor))
            {
                voxelColors.Add(voxelColor);
            }

            VoxelData voxelData = generatedVoxel.AddComponent<VoxelData>();
            voxelData.drawFaces = drawFaces;

            if (isExposed)
            {
                exposedVoxels.Add(generatedVoxel);
                voxelData.isExposed = true;
            }

            if (isAnchor)
            {
                anchorVoxels.Add(generatedVoxel);
                voxelData.isAnchor = true;
            }

            generatedVoxel.transform.SetParent(newGameObject.transform, true);
            generatedVoxels.Add(generatedVoxel);
        }

        // Remove Box Colliders to Original Voxels
        for (int voxelIndex = 0; voxelIndex < voxels.Count; voxelIndex++)
        {
            DestroyImmediate(voxels[voxelIndex].GetComponent<BoxCollider>());
        }

        // Assign Box Colliders to Generated Voxels
        for (int voxelIndex = 0; voxelIndex < generatedVoxels.Count; voxelIndex++)
        {
            GameObject generatedVoxel = generatedVoxels[voxelIndex];
            Utility.VoxelCreateBoxCollider(generatedVoxel);
        }

        // Assign Voxel Adjacent Voxels
        for (int voxelIndex = 0; voxelIndex < generatedVoxels.Count; voxelIndex++)
        {
            GameObject generatedVoxel = generatedVoxels[voxelIndex];
            VoxelData voxelData = generatedVoxel.GetComponent<VoxelData>();

            Vector3[] faceDirectionChecks = Utility.GetVoxelFaceDirections(generatedVoxel);
            for (int directionIndex = 0; directionIndex < (int)Common.VoxelFaces.SIZE; directionIndex++)
            {
                RaycastHit raycastHit;
                if (Physics.Raycast(generatedVoxel.GetComponent<BoxCollider>().bounds.center, faceDirectionChecks[directionIndex], out raycastHit, Common.VOXEL_SIZE))
                {
                    GameObject hitObject = raycastHit.collider.gameObject;

                    if (!generatedVoxels.Contains(hitObject))
                        continue;

                    voxelData.adjacentVoxels[directionIndex] = hitObject;
                }
            }
        }

        // Build Parent Box Collider From Voxel Colliders
        Bounds newGameObjectBounds = new Bounds(Vector3.zero, Vector3.zero);
        for (int voxelIndex = 0; voxelIndex < generatedVoxels.Count; voxelIndex++)
        {
            GameObject voxel = generatedVoxels[voxelIndex];

            Vector3 voxelCenter = voxel.transform.localPosition + voxel.transform.up * Common.VOXEL_SIZE * 0.5f + voxel.transform.right * Common.VOXEL_SIZE * 0.5f + voxel.transform.forward * Common.VOXEL_SIZE * 0.5f;

            Bounds voxelBounds = new Bounds();
            voxelBounds.size = new Vector3(Common.VOXEL_SIZE, Common.VOXEL_SIZE, Common.VOXEL_SIZE);
            voxelBounds.center = voxelCenter;

            newGameObjectBounds.Encapsulate(voxelBounds);
        }
        BoxCollider newGameObjectBoxCollider = newGameObject.AddComponent<BoxCollider>();
        newGameObjectBoxCollider.center = newGameObjectBounds.center;
        newGameObjectBoxCollider.size = newGameObjectBounds.size;

        // Remove Box Colliders to Generated Voxels
        for (int voxelIndex = 0; voxelIndex < generatedVoxels.Count; voxelIndex++)
        {
            DestroyImmediate(generatedVoxels[voxelIndex].GetComponent<BoxCollider>());
        }

        // Combine All Meshes Into Parent Mesh
        CombineInstance[] combineInstances = new CombineInstance[exposedVoxels.Count];
        
        for (int voxelIndex = 0; voxelIndex < exposedVoxels.Count; voxelIndex++)
        {
            GameObject exposedVoxel = exposedVoxels[voxelIndex];
            MeshFilter meshFilter = exposedVoxel.GetComponent<MeshFilter>();

            combineInstances[voxelIndex].mesh = meshFilter.sharedMesh;
            combineInstances[voxelIndex].transform =  Matrix4x4.TRS(exposedVoxel.transform.localPosition, exposedVoxel.transform.localRotation, Vector3.one);
            DestroyImmediate(meshFilter);
        }

        MeshFilter newMeshFilter = newGameObject.AddComponent<MeshFilter>();
        newMeshFilter.mesh = new Mesh();
        newMeshFilter.sharedMesh.CombineMeshes(combineInstances, true, true, true);

        // Make Material Out of Voxel Colors
        MeshRenderer meshRenderer = newGameObject.AddComponent<MeshRenderer>();
        Texture2D texture = new Texture2D(voxelColors.Count, 1);
        for (int colorIndex = 0; colorIndex < voxelColors.Count; colorIndex++)
        {
            texture.SetPixel(colorIndex, 0, voxelColors[colorIndex]);
            texture.Apply();
        }

        destructible.destructibleVoxels = generatedVoxels;
        destructible.exposedVoxels = exposedVoxels;
        destructible.anchorVoxels = anchorVoxels;

        FileUtil.DeleteFileOrDirectory(assetsFolderPath + "/" + meshName);
        AssetDatabase.CreateFolder(assetsFolderPath, meshName);
        AssetDatabase.CreateFolder(assetsFolderPath + "/" + meshName, "Textures");
        AssetDatabase.CreateFolder(assetsFolderPath + "/" + meshName, "Meshes");
        AssetDatabase.CreateFolder(assetsFolderPath + "/" + meshName, "Materials");

        string assetFolderPath = assetsFolderPath + "/" + meshName + "/";
        byte[] textureBytes = texture.EncodeToPNG();
        FileUtil.DeleteFileOrDirectory(Application.dataPath + "/Resources/GeneratedMeshes/" + meshName + "/Textures/" + meshName + ".png");
        File.WriteAllBytes(Application.dataPath + "/Resources/GeneratedMeshes/" + meshName + "/Textures/" + meshName + ".png", textureBytes);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        texture = Resources.Load("GeneratedMeshes/" + meshName + "/Textures/" + meshName) as Texture2D;

        Material material = new Material(Shader.Find("Standard"));
        material.mainTexture = texture;
        material.SetFloat("_Glossiness", 0.0f);
        meshRenderer.sharedMaterial = material;
        Repaint();

        AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath(assetFolderPath + "Materials/" + meshName + ".mat"));
        AssetDatabase.CreateAsset(newMeshFilter.sharedMesh, AssetDatabase.GenerateUniqueAssetPath(assetFolderPath + "Meshes/" + meshName + ".asset"));

        return newGameObject;
    }

    private GameObject GenerateVoxelFromVoxelStruct(VoxelStruct voxelStruct, Color voxelColor, List<Color> voxelColors)
    {
        GameObject voxel = new GameObject("DestructibleVoxel");
        voxel.transform.position = voxelStruct.position;
        voxel.transform.rotation = voxelStruct.rotation;
        voxel.layer = LayerMask.NameToLayer("DestructibleVoxel");

        bool[] drawFaces = voxelStruct.drawFaces;

        bool willRenderVoxel = false;
        foreach (bool drawFace in drawFaces)
        {
            if (!drawFace)
                continue;

            willRenderVoxel = true;
            break;
        }
        if (!willRenderVoxel)
            return voxel;

        List<int> triangles = new List<int>();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();

        if (drawFaces[(int)Common.VoxelFaces.FRONT])
        {
            int triangleVerticeStartIndex = vertices.Count;

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
        }

        if (drawFaces[(int)Common.VoxelFaces.RIGHT])
        {
            int triangleVerticeStartIndex = vertices.Count;

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
        }

        if (drawFaces[(int)Common.VoxelFaces.TOP])
        {
            int triangleVerticeStartIndex = vertices.Count;

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
        }

        if (drawFaces[(int)Common.VoxelFaces.LEFT])
        {
            int triangleVerticeStartIndex = vertices.Count;

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
        }

        if (drawFaces[(int)Common.VoxelFaces.BOTTOM])
        {
            int triangleVerticeStartIndex = vertices.Count;

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
        }

        if (drawFaces[(int)Common.VoxelFaces.BACK])
        {
            int triangleVerticeStartIndex = vertices.Count;

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
        }

        MeshFilter meshFilter = voxel.AddComponent<MeshFilter>();

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();

        float u = voxelColors.IndexOf(voxelColor) / 4.0f;
        Vector2[] uvs = new Vector2[vertices.Count];
        for (int index = 0; index < uvs.Length; index++)
        {
            uvs[index] = new Vector2(u, 0);
        }

        mesh.uv = uvs;

        meshFilter.mesh = mesh;

        return voxel;
    }

    private struct VoxelStruct
    {
        public Vector3 position;
        public Vector3 forward;
        public Vector3 right;
        public Vector3 up;
        public Quaternion rotation;
        public Color color;
        public bool[] drawFaces;

        public VoxelStruct(Vector3 _position, Vector3 _forward, Vector3 _right, Vector3 _up, Quaternion _rotation, Color _color, bool[] _drawFaces)
        {
            position = _position;
            forward = _forward;
            right = _right;
            up = _up;
            rotation = _rotation;
            color = _color;
            drawFaces = _drawFaces;
        }
    }
}