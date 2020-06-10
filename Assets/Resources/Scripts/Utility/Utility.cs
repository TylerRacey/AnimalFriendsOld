using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Utility;

public static class Utility
{
    public static GameObject[] GetAllItems()
    {
        return GameObject.FindGameObjectsWithTag("Item");
    }

    public static List<GameObject> GetAllDestructibleTrees()
    {
        return new List<GameObject>(GameObject.FindGameObjectsWithTag("Destructible_Tree"));
    }

    public static List<GameObject> GetAllDestructibleTreesWithinRadius(float radius, Vector3 position)
    {
        List<GameObject> destructibleTrees = Utility.GetAllDestructibleTrees();
        List<GameObject> destructibleTreesWithinRadius = new List<GameObject>();

        foreach (GameObject destructibleTree in destructibleTrees)
        {
            if (Vector3.SqrMagnitude(destructibleTree.transform.position - position) > (radius * radius))
                continue;

            destructibleTreesWithinRadius.Add(destructibleTree);
        }

        return destructibleTreesWithinRadius;
    }

    public static GameObject[] SortByDistance(GameObject[] gameObjects, Vector3 position)
    {
        for (int primary = 0; primary <= gameObjects.Length - 2; primary++)
        {
            for (int secondary = 0; secondary <= gameObjects.Length - 2; secondary++)
            {
                if (Vector3.SqrMagnitude(gameObjects[secondary].transform.position - position) > Vector3.SqrMagnitude(gameObjects[secondary + 1].transform.position - position))
                {
                    GameObject tempGameObject = gameObjects[secondary + 1];
                    gameObjects[secondary + 1] = gameObjects[secondary];
                    gameObjects[secondary] = tempGameObject;
                }
            }
        }

        return gameObjects;
    }

    public static GameObject[] SortByHeight(GameObject[] gameObjects)
    {
        for (int primary = 0; primary <= gameObjects.Length - 2; primary++)
        {
            for (int secondary = 0; secondary <= gameObjects.Length - 2; secondary++)
            {
                if (gameObjects[secondary].transform.position.y > gameObjects[secondary + 1].transform.position.y)
                {
                    GameObject tempGameObject = gameObjects[secondary + 1];
                    gameObjects[secondary + 1] = gameObjects[secondary];
                    gameObjects[secondary] = tempGameObject;
                }
            }
        }

        return gameObjects;
    }

    public static List<GameObject> SortByDistance(List<GameObject> gameObjects, Vector3 position)
    {
        for (int primary = 0; primary <= gameObjects.Count - 2; primary++)
        {
            for (int secondary = 0; secondary <= gameObjects.Count - 2; secondary++)
            {
                if (Vector3.SqrMagnitude(gameObjects[secondary].transform.position - position) > Vector3.SqrMagnitude(gameObjects[secondary + 1].transform.position - position))
                {
                    GameObject tempGameObject = gameObjects[secondary + 1];
                    gameObjects[secondary + 1] = gameObjects[secondary];
                    gameObjects[secondary] = tempGameObject;
                }
            }
        }

        return gameObjects;
    }

    public static List<GameObject> GetChildrenWithTag(GameObject parent, string tag)
    {
        List<GameObject> children = new List<GameObject>();

        for (int index = 0; index < parent.transform.childCount; index++)
        {
            GameObject child = parent.transform.GetChild(index).gameObject;

            if (child.tag == tag)
            {
                children.Add(child);
            }
        }

        return children;
    }

    public static float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    public static GameObject DebugDrawSphere(Vector3 position, float radius, Color color, float duration)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.localScale = new Vector3(radius * 2, radius * 2, radius * 2);
        sphere.transform.position = position;
        Renderer renderer = sphere.GetComponent<Renderer>();
        renderer.material.shader = Shader.Find("Transparent/Diffuse");
        renderer.material.color = color;
        GameObject.Destroy(sphere.GetComponent<SphereCollider>());

        GameObject.Destroy(sphere, duration);

        return sphere;
    }

    public static void DebugDrawCube(Vector3 position, float size, Quaternion orientation, Color color, float duration)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.localScale = new Vector3(size, size, size);
        cube.transform.position = position;
        cube.transform.rotation = orientation;
        Renderer renderer = cube.GetComponent<Renderer>();
        renderer.material.shader = Shader.Find("Transparent/Diffuse");
        renderer.material.color = color;
        GameObject.Destroy(cube.GetComponent<BoxCollider>());

        GameObject.Destroy(cube, duration);
    }

    public static void DebugDrawQuad(Vector3 centerPosition, Vector3 scale, Quaternion orientation, Color color, float duration)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.transform.localScale = scale;
        quad.transform.position = centerPosition;
        quad.transform.rotation = orientation;
        Renderer renderer = quad.GetComponent<Renderer>();
        renderer.material.shader = Shader.Find("Transparent/Diffuse");
        renderer.material.color = color;
        GameObject.Destroy(quad.GetComponent<BoxCollider>());

        GameObject.Destroy(quad, duration);
    }

    public static Collider SortByDistance(Vector3 position, Collider colliderA, Collider colliderB)
    {
        if (Vector3.SqrMagnitude(colliderA.transform.position - position) < Vector3.SqrMagnitude(colliderB.transform.position - position))
            return colliderA;

        return colliderB;
    }

    public static Vector3 FlattenVector(Vector3 vector)
    {
        return new Vector3(vector.x, 0, vector.z);
    }

    public static Vector3[] GetVoxelFaceDirections(GameObject voxel)
    {
        return new Vector3[] { -voxel.transform.forward, voxel.transform.right, voxel.transform.up, -voxel.transform.right, -voxel.transform.up, voxel.transform.forward };
    }

    public static int[] GetVoxelAdjacentFaces()
    {
        return new int[] { (int)Voxel.Faces.BACK, (int)Voxel.Faces.LEFT, (int)Voxel.Faces.BOTTOM, (int)Voxel.Faces.RIGHT, (int)Voxel.Faces.TOP, (int)Voxel.Faces.FRONT };
    }  

    public static void ScaleBoxColliderBoundsToVoxelStructs(BoxCollider boxCollider, HashSet<VoxelStruct> voxelStructs, Transform parentTransform)
    {
        // Find Voxels On Edge To Encapsulate Smallest Number Of Voxels
        Vector3 voxelLocalPosition;
        Vector3 smallestX = new Vector3(float.MaxValue, 0, 0);
        Vector3 largestX = new Vector3(float.MinValue, 0, 0);
        Vector3 smallestY = new Vector3(0, float.MaxValue, 0);
        Vector3 largestY = new Vector3(0, float.MinValue, 0);
        Vector3 smallestZ = new Vector3(0, 0, float.MaxValue);
        Vector3 largestZ = new Vector3(0, 0, float.MinValue);

        foreach (VoxelStruct voxelStruct in voxelStructs)
        {
            voxelLocalPosition = voxelStruct.localPosition;
            if (voxelLocalPosition.x < smallestX.x)
                smallestX = voxelLocalPosition;

            if (voxelLocalPosition.x > largestX.x)
                largestX = voxelLocalPosition;

            if (voxelLocalPosition.y > largestY.y)
                largestY = voxelLocalPosition;

            if (voxelLocalPosition.y < smallestY.y)
                smallestY = voxelLocalPosition;

            if (voxelLocalPosition.z > largestZ.z)
                largestZ = voxelLocalPosition;

            if (voxelLocalPosition.z < smallestZ.z)
                smallestZ = voxelLocalPosition;
        }

        // Encapsulate edge voxels' Bounds
        Bounds newBounds = new Bounds(Vector3.zero, Vector3.zero);
        Bounds voxelBounds = new Bounds();
        Vector3 up = parentTransform.up;
        Vector3 right = parentTransform.right;
        Vector3 forward = parentTransform.forward;
        Vector3 voxelSize = new Vector3(Voxel.SIZE, Voxel.SIZE, Voxel.SIZE);
        Vector3[] encapsulateLocalPositions = new Vector3[] { smallestX, largestX, smallestY, largestY, smallestZ, largestZ };
        for(int index = 0; index < encapsulateLocalPositions.Length; index++)
        {
            voxelBounds.size = voxelSize;
            voxelBounds.center = encapsulateLocalPositions[index] + up * Voxel.HALF_SIZE + right * Voxel.HALF_SIZE + forward * Voxel.HALF_SIZE;

            newBounds.Encapsulate(voxelBounds);
        }

        boxCollider.center = newBounds.center;
        boxCollider.size = newBounds.size;
    }

    public static BoxCollider VoxelCreateBoxCollider(GameObject voxel)
    {
        BoxCollider boxCollider = voxel.AddComponent<BoxCollider>();
        boxCollider.center = new Vector3(Voxel.HALF_SIZE, + Voxel.HALF_SIZE, + Voxel.HALF_SIZE);
        boxCollider.size = new Vector3(Voxel.SIZE, Voxel.SIZE, Voxel.SIZE);

        return boxCollider;
    }

    public static Mesh CreateMeshFromVoxelStruct(VoxelStruct voxelStruct)
    {
        List<int> triangles = new List<int>();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();

        if (voxelStruct.drawFaces[(int)Voxel.Faces.FRONT])
        {
            int triangleVerticeStartIndex = vertices.Count;

            triangles.Add(triangleVerticeStartIndex);
            triangles.Add(triangleVerticeStartIndex + 1);
            triangles.Add(triangleVerticeStartIndex + 2);
            triangles.Add(triangleVerticeStartIndex);
            triangles.Add(triangleVerticeStartIndex + 2);
            triangles.Add(triangleVerticeStartIndex + 3);

            vertices.Add(Vector3.zero);
            vertices.Add((Vector3.up * Voxel.SIZE));
            vertices.Add((Vector3.up * Voxel.SIZE) + (Vector3.right * Voxel.SIZE));
            vertices.Add((Vector3.right * Voxel.SIZE));

            normals.Add(-Vector3.forward);
            normals.Add(-Vector3.forward);
            normals.Add(-Vector3.forward);
            normals.Add(-Vector3.forward);
        }

        if (voxelStruct.drawFaces[(int)Voxel.Faces.RIGHT])
        {
            int triangleVerticeStartIndex = vertices.Count;

            triangles.Add(triangleVerticeStartIndex);
            triangles.Add(triangleVerticeStartIndex + 1);
            triangles.Add(triangleVerticeStartIndex + 2);
            triangles.Add(triangleVerticeStartIndex);
            triangles.Add(triangleVerticeStartIndex + 2);
            triangles.Add(triangleVerticeStartIndex + 3);

            vertices.Add((Vector3.right * Voxel.SIZE));
            vertices.Add((Vector3.right * Voxel.SIZE) + (Vector3.up * Voxel.SIZE));
            vertices.Add((Vector3.right * Voxel.SIZE) + (Vector3.up * Voxel.SIZE) + (Vector3.forward * Voxel.SIZE));
            vertices.Add((Vector3.right * Voxel.SIZE) + (Vector3.forward * Voxel.SIZE));

            normals.Add(Vector3.right);
            normals.Add(Vector3.right);
            normals.Add(Vector3.right);
            normals.Add(Vector3.right);
        }

        if (voxelStruct.drawFaces[(int)Voxel.Faces.TOP])
        {
            int triangleVerticeStartIndex = vertices.Count;

            triangles.Add(triangleVerticeStartIndex);
            triangles.Add(triangleVerticeStartIndex + 1);
            triangles.Add(triangleVerticeStartIndex + 2);
            triangles.Add(triangleVerticeStartIndex);
            triangles.Add(triangleVerticeStartIndex + 2);
            triangles.Add(triangleVerticeStartIndex + 3);

            vertices.Add((Vector3.up * Voxel.SIZE));
            vertices.Add((Vector3.up * Voxel.SIZE) + (Vector3.forward * Voxel.SIZE));
            vertices.Add((Vector3.up * Voxel.SIZE) + (Vector3.forward * Voxel.SIZE) + (Vector3.right * Voxel.SIZE));
            vertices.Add((Vector3.up * Voxel.SIZE) + (Vector3.right * Voxel.SIZE));

            normals.Add(Vector3.up);
            normals.Add(Vector3.up);
            normals.Add(Vector3.up);
            normals.Add(Vector3.up);
        }

        if (voxelStruct.drawFaces[(int)Voxel.Faces.LEFT])
        {
            int triangleVerticeStartIndex = vertices.Count;

            triangles.Add(triangleVerticeStartIndex);
            triangles.Add(triangleVerticeStartIndex + 2);
            triangles.Add(triangleVerticeStartIndex + 1);
            triangles.Add(triangleVerticeStartIndex);
            triangles.Add(triangleVerticeStartIndex + 3);
            triangles.Add(triangleVerticeStartIndex + 2);

            vertices.Add(Vector3.zero);
            vertices.Add((Vector3.up * Voxel.SIZE));
            vertices.Add((Vector3.up * Voxel.SIZE) + (Vector3.forward * Voxel.SIZE));
            vertices.Add((Vector3.forward * Voxel.SIZE));

            normals.Add(-Vector3.right);
            normals.Add(-Vector3.right);
            normals.Add(-Vector3.right);
            normals.Add(-Vector3.right);
        }

        if (voxelStruct.drawFaces[(int)Voxel.Faces.BOTTOM])
        {
            int triangleVerticeStartIndex = vertices.Count;

            triangles.Add(triangleVerticeStartIndex);
            triangles.Add(triangleVerticeStartIndex + 2);
            triangles.Add(triangleVerticeStartIndex + 1);
            triangles.Add(triangleVerticeStartIndex);
            triangles.Add(triangleVerticeStartIndex + 3);
            triangles.Add(triangleVerticeStartIndex + 2);

            vertices.Add(Vector3.zero);
            vertices.Add(Vector3.zero + (Vector3.forward * Voxel.SIZE));
            vertices.Add(Vector3.zero + (Vector3.forward * Voxel.SIZE) + (Vector3.right * Voxel.SIZE));
            vertices.Add(Vector3.zero + (Vector3.right * Voxel.SIZE));

            normals.Add(-Vector3.up);
            normals.Add(-Vector3.up);
            normals.Add(-Vector3.up);
            normals.Add(-Vector3.up);
        }

        if (voxelStruct.drawFaces[(int)Voxel.Faces.BACK])
        {
            int triangleVerticeStartIndex = vertices.Count;

            triangles.Add(triangleVerticeStartIndex);
            triangles.Add(triangleVerticeStartIndex + 2);
            triangles.Add(triangleVerticeStartIndex + 1);
            triangles.Add(triangleVerticeStartIndex);
            triangles.Add(triangleVerticeStartIndex + 3);
            triangles.Add(triangleVerticeStartIndex + 2);

            vertices.Add(Vector3.zero + (Vector3.forward * Voxel.SIZE));
            vertices.Add(Vector3.zero + (Vector3.forward * Voxel.SIZE) + (Vector3.up * Voxel.SIZE));
            vertices.Add(Vector3.zero + (Vector3.forward * Voxel.SIZE) + (Vector3.up * Voxel.SIZE) + (Vector3.right * Voxel.SIZE));
            vertices.Add(Vector3.zero + (Vector3.forward * Voxel.SIZE) + (Vector3.right * Voxel.SIZE));

            normals.Add(Vector3.forward);
            normals.Add(Vector3.forward);
            normals.Add(Vector3.forward);
            normals.Add(Vector3.forward);
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();

        Vector2[] uvs = new Vector2[vertices.Count];
        for (int index = 0; index < uvs.Length; index++)
        {
            uvs[index] = voxelStruct.meshUV;
        }
        mesh.uv = uvs;

        return mesh;
    }

    public static int RandomSign()
    {
        if (UnityEngine.Random.Range(0, 2) == 0)
        {
            return -1;
        }
        return 1;
    }
}
