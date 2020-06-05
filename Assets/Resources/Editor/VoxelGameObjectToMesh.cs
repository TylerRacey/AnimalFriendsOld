using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;

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

        Transform[] allChildren = selectedGameObject.GetComponentsInChildren<Transform>();
        List<GameObject> voxels = new List<GameObject>();
        List<VoxelRender> voxelRenders = new List<VoxelRender>();
        List<VoxelRender> exposedVoxelRenders = new List<VoxelRender>();
        List<VoxelRender> anchorVoxelRenders = new List<VoxelRender>();

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
            Vector2 voxelMeshUVs = new Vector2(voxelColors.IndexOf(voxelColor) / 4.0f, 0);
            VoxelRender voxelRender = VoxelRender.CreateInstance(voxel.transform.localPosition, voxel.transform.forward, voxel.transform.right, voxel.transform.up, voxel.transform.localRotation, voxelColor, drawFaces, null, null, false, isAnchor, isExposed, false, null, voxelMeshUVs);

            // Create Temp GameObject Voxel
            GameObject generatedVoxel = Utility.GenerateVoxelGameObjectFromVoxelRender(voxelRender, true, selectedGameObject.transform);

            voxelRender.gameObject = generatedVoxel;
            voxelRenders.Add(voxelRender);

            if (isExposed)
            {
                exposedVoxelRenders.Add(voxelRender);
            }

            if (isAnchor)
            {
                anchorVoxelRenders.Add(voxelRender);
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
        for (int voxelIndex = 0; voxelIndex < voxelRenders.Count; voxelIndex++)
        {
            VoxelRender voxelRender = voxelRenders[voxelIndex];
            GameObject generatedVoxel = voxelRender.gameObject;

            Vector3[] faceDirectionChecks = Utility.GetVoxelFaceDirections(generatedVoxel);
            for (int directionIndex = 0; directionIndex < (int)Common.VoxelFaces.SIZE; directionIndex++)
            {
                RaycastHit raycastHit;
                if (Physics.Raycast(generatedVoxel.GetComponent<BoxCollider>().bounds.center, faceDirectionChecks[directionIndex], out raycastHit, Common.VOXEL_SIZE))
                {
                    GameObject hitObject = raycastHit.collider.gameObject;

                    if (!generatedVoxels.Contains(hitObject))
                        continue;

                    if(voxelRender.adjacentVoxelRenderIndexes == null)
                    {
                        voxelRender.adjacentVoxelRenderIndexes = new int[(int)Common.VoxelFaces.SIZE];
                    }

                    foreach(VoxelRender checkedVoxelRender in voxelRenders)
                    {
                        if (checkedVoxelRender.gameObject != hitObject)
                            continue;

                        voxelRender.adjacentVoxelRenderIndexes[directionIndex] = voxelRenders.IndexOf(checkedVoxelRender);
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
        CombineInstance[] combineInstances = new CombineInstance[exposedVoxelRenders.Count];
        for (int voxelIndex = 0; voxelIndex < exposedVoxelRenders.Count; voxelIndex++)
        {
            GameObject exposedVoxel = exposedVoxelRenders[voxelIndex].gameObject;
            MeshFilter meshFilter = exposedVoxel.GetComponent<MeshFilter>();

            combineInstances[voxelIndex].mesh = meshFilter.sharedMesh;
            combineInstances[voxelIndex].transform = Matrix4x4.TRS(exposedVoxel.transform.localPosition, exposedVoxel.transform.localRotation, Vector3.one);
            DestroyImmediate(meshFilter);
        }
        MeshFilter newMeshFilter = newGameObject.AddComponent<MeshFilter>();
        newMeshFilter.mesh = new Mesh();
        newMeshFilter.sharedMesh.CombineMeshes(combineInstances, true, true, true);

        // Delete all generated voxel game objects
        for (int voxelIndex = 0; voxelIndex < voxelRenders.Count; voxelIndex++)
        {
            DestroyImmediate(voxelRenders[voxelIndex].gameObject);
        }

        // Make Material Out of Voxel Colors
        MeshRenderer meshRenderer = newGameObject.AddComponent<MeshRenderer>();
        Texture2D texture = new Texture2D(voxelColors.Count, 1);
        for (int colorIndex = 0; colorIndex < voxelColors.Count; colorIndex++)
        {
            texture.SetPixel(colorIndex, 0, voxelColors[colorIndex]);
            texture.Apply();
        }

        FileUtil.DeleteFileOrDirectory(assetsFolderPath + "/" + meshName);
        AssetDatabase.CreateFolder(assetsFolderPath, meshName);
        AssetDatabase.CreateFolder(assetsFolderPath + "/" + meshName, "Textures");
        AssetDatabase.CreateFolder(assetsFolderPath + "/" + meshName, "Meshes");
        AssetDatabase.CreateFolder(assetsFolderPath + "/" + meshName, "Materials");
        AssetDatabase.CreateFolder(assetsFolderPath + "/" + meshName, "VoxelRenders");

        for (int index = 0; index < voxelRenders.Count; index++)
        {
            AssetDatabase.CreateAsset(voxelRenders[index], AssetDatabase.GenerateUniqueAssetPath(assetsFolderPath + "/" + meshName + "/VoxelRenders/" + meshName + index + ".asset" ));
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();

        Destructible destructible = newGameObject.AddComponent<Destructible>();
        destructible.voxelRenderImports = voxelRenders;

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
}