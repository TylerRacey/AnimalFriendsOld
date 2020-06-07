﻿using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
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

                if (!AssetDatabase.IsValidFolder(prefabsFolderPath + "/" + meshName))
                {
                    AssetDatabase.CreateFolder(prefabsFolderPath, meshName);
                }

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

        Transform[] allChildren = selectedGameObject.GetComponentsInChildren<Transform>();
        List<GameObject> voxels = new List<GameObject>();
        List<VoxelExport> voxelExports = new List<VoxelExport>();
        List<VoxelExport> exposedVoxelExports = new List<VoxelExport>();
        List<VoxelExport> anchorVoxelExports = new List<VoxelExport>();

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

        // Save Off All Voxel Colors
        List<Color> voxelColors = new List<Color>();
        foreach (GameObject voxel in voxels)
        {
            Color voxelColor = voxel.GetComponent<MeshRenderer>().sharedMaterial.color;
            if (!voxelColors.Contains(voxelColor))
            {
                voxelColors.Add(voxelColor);
            }
        }

        List<GameObject> generatedVoxels = new List<GameObject>();
        foreach (GameObject voxel in voxels)
        {
            BoxCollider boxCollider = voxel.GetComponent<BoxCollider>();
            Vector3 voxelCenter = boxCollider.bounds.center;

            bool[] drawFaces = new bool[(int)Common.VoxelFaces.SIZE];

            Vector3[] faceDirectionChecks = new Vector3[] { -voxel.transform.forward, voxel.transform.right, voxel.transform.up, -voxel.transform.right, -voxel.transform.up, voxel.transform.forward };

            // Trace to see which voxel faces to draw, along if it's an anchor or exposed
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

            // Create Voxel Render
            Color voxelColor = voxel.GetComponent<MeshRenderer>().sharedMaterial.color;
            Vector2 voxelMeshUVs = new Vector2((voxelColors.IndexOf(voxelColor) / (float)voxelColors.Count) + (0.5f / voxelColors.Count), 0);
            VoxelExport voxelExport = VoxelExport.CreateInstance(voxel.transform.localPosition, voxelColor, drawFaces, null, false, isAnchor, isExposed, false, null, voxelMeshUVs);

            // Create Temp GameObject Voxel
            GameObject generatedVoxel = GenerateVoxelGameObjectFromVoxelExport(voxelExport, selectedGameObject.transform);

            voxelExport.gameObject = generatedVoxel;
            voxelExports.Add(voxelExport);

            if (isExposed)
            {
                exposedVoxelExports.Add(voxelExport);
            }

            if (isAnchor)
            {
                anchorVoxelExports.Add(voxelExport);
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

        // Assign Voxel Adjacent Voxel Indexes
        for (int voxelIndex = 0; voxelIndex < voxelExports.Count; voxelIndex++)
        {
            VoxelExport voxelExport = voxelExports[voxelIndex];
            GameObject generatedVoxel = voxelExport.gameObject;

            Vector3[] faceDirectionChecks = Utility.GetVoxelFaceDirections(generatedVoxel);
            for (int directionIndex = 0; directionIndex < (int)Common.VoxelFaces.SIZE; directionIndex++)
            {
                RaycastHit raycastHit;
                if (Physics.Raycast(generatedVoxel.GetComponent<BoxCollider>().bounds.center, faceDirectionChecks[directionIndex], out raycastHit, Common.VOXEL_SIZE))
                {
                    GameObject hitObject = raycastHit.collider.gameObject;

                    if (!generatedVoxels.Contains(hitObject))
                        continue;

                    if(voxelExport.adjacentVoxelExportIndexes == null)
                    {
                        voxelExport.adjacentVoxelExportIndexes = new int[(int)Common.VoxelFaces.SIZE];
                        for(int index = 0; index < voxelExport.adjacentVoxelExportIndexes.Length;index++)
                        {
                            voxelExport.adjacentVoxelExportIndexes[index] = -1;
                        }
                    }

                    foreach(VoxelExport checkedVoxelExport in voxelExports)
                    {
                        if (checkedVoxelExport.gameObject != hitObject)
                            continue;

                        voxelExport.adjacentVoxelExportIndexes[directionIndex] = voxelExports.IndexOf(checkedVoxelExport);
                    }
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

        // Combine All Meshes Into Parent Mesh
        CombineInstance[] combineInstances = new CombineInstance[exposedVoxelExports.Count];
        for (int voxelIndex = 0; voxelIndex < exposedVoxelExports.Count; voxelIndex++)
        {
            GameObject exposedVoxel = exposedVoxelExports[voxelIndex].gameObject;
            MeshFilter meshFilter = exposedVoxel.GetComponent<MeshFilter>();

            combineInstances[voxelIndex].mesh = meshFilter.sharedMesh;
            combineInstances[voxelIndex].transform = Matrix4x4.TRS(exposedVoxel.transform.localPosition, exposedVoxel.transform.localRotation, Vector3.one);
            DestroyImmediate(meshFilter);
        }
        MeshFilter newMeshFilter = newGameObject.AddComponent<MeshFilter>();
        newMeshFilter.mesh = new Mesh();
        newMeshFilter.sharedMesh.CombineMeshes(combineInstances, true, true, true);

        // Delete all generated voxel game objects
        for (int voxelIndex = 0; voxelIndex < voxelExports.Count; voxelIndex++)
        {
            DestroyImmediate(voxelExports[voxelIndex].gameObject);
        }

        // Make Material Out of Voxel Colors
        MeshRenderer meshRenderer = newGameObject.AddComponent<MeshRenderer>();
        // float nextPowerOfTwoWidth = pow(2, Mathf.Ceil(Mathf.Log10(x) / log(2)));
        Texture2D texture = new Texture2D(voxelColors.Count * 2, 1);
        int x = 0;
        for (int colorIndex = 0; colorIndex < voxelColors.Count; colorIndex++)
        {
            texture.SetPixel(x, 0, voxelColors[colorIndex]);
            texture.SetPixel(x + 1, 0, voxelColors[colorIndex]);

            x += 2;

            texture.Apply();
        }

        FileUtil.DeleteFileOrDirectory(assetsFolderPath + "/" + meshName);
        AssetDatabase.CreateFolder(assetsFolderPath, meshName);
        AssetDatabase.CreateFolder(assetsFolderPath + "/" + meshName, "Textures");
        AssetDatabase.CreateFolder(assetsFolderPath + "/" + meshName, "Meshes");
        AssetDatabase.CreateFolder(assetsFolderPath + "/" + meshName, "Materials");
        AssetDatabase.CreateFolder(assetsFolderPath + "/" + meshName, "VoxelExports");

        for (int index = 0; index < voxelExports.Count; index++)
        {
            AssetDatabase.CreateAsset(voxelExports[index], AssetDatabase.GenerateUniqueAssetPath(assetsFolderPath + "/" + meshName + "/VoxelExports/" + meshName + index + ".asset" ));
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();

        Destructible destructible = newGameObject.AddComponent<Destructible>();
        destructible.voxelExports = voxelExports;

        string assetFolderPath = assetsFolderPath + "/" + meshName + "/";
        byte[] textureBytes = texture.EncodeToPNG();
        FileUtil.DeleteFileOrDirectory(Application.dataPath + "/Resources/GeneratedMeshes/" + meshName + "/Textures/" + meshName + ".png");
        File.WriteAllBytes(Application.dataPath + "/Resources/GeneratedMeshes/" + meshName + "/Textures/" + meshName + ".png", textureBytes);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();

        texture = Resources.Load("GeneratedMeshes/" + meshName + "/Textures/" + meshName) as Texture2D;

        Material material = new Material(Shader.Find("Standard"));
        material.mainTexture = texture;
        material.SetFloat("_Glossiness", 0.0f);
        meshRenderer.sharedMaterial = material;
        Repaint();

        AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath(assetFolderPath + "Materials/" + meshName + ".mat"));
        AssetDatabase.CreateAsset(newMeshFilter.sharedMesh, AssetDatabase.GenerateUniqueAssetPath(assetFolderPath + "Meshes/" + meshName + ".asset"));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();

        return newGameObject;
    }

    public static GameObject GenerateVoxelGameObjectFromVoxelExport(VoxelExport voxelExport, Transform parentTransform)
    {
        GameObject voxel = new GameObject("DestructibleVoxel");
        voxel.layer = LayerMask.NameToLayer("DestructibleVoxel");
        voxel.transform.SetParent(parentTransform);
        voxel.transform.localPosition = voxelExport.localPosition;

        bool willRenderVoxel = false;
        foreach (bool drawFace in voxelExport.drawFaces)
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

        if (voxelExport.drawFaces[(int)Common.VoxelFaces.FRONT])
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

        if (voxelExport.drawFaces[(int)Common.VoxelFaces.RIGHT])
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

        if (voxelExport.drawFaces[(int)Common.VoxelFaces.TOP])
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

        if (voxelExport.drawFaces[(int)Common.VoxelFaces.LEFT])
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

        if (voxelExport.drawFaces[(int)Common.VoxelFaces.BOTTOM])
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

        if (voxelExport.drawFaces[(int)Common.VoxelFaces.BACK])
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

        Vector2[] uvs = new Vector2[vertices.Count];
        for (int index = 0; index < uvs.Length; index++)
        {
            uvs[index] = voxelExport.meshUV;
        }
        mesh.uv = uvs;

        meshFilter.mesh = mesh;

        return voxel;
    }
}