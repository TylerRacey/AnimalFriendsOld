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
        return new int[] { (int)Common.VoxelFaces.BACK, (int)Common.VoxelFaces.LEFT, (int)Common.VoxelFaces.BOTTOM, (int)Common.VoxelFaces.RIGHT, (int)Common.VoxelFaces.TOP, (int)Common.VoxelFaces.FRONT };
    }  

    public static void ScaleBoxColliderBoundsToVoxels(BoxCollider boxCollider, List<GameObject> voxels)
    {
        // Build Parent Box Collider From Voxel Colliders
        Bounds newBounds = new Bounds(Vector3.zero, Vector3.zero);
        for (int voxelIndex = 0; voxelIndex < voxels.Count; voxelIndex++)
        {
            GameObject voxel = voxels[voxelIndex];

            Vector3 voxelCenter = voxel.transform.localPosition + voxel.transform.up * Common.VOXEL_SIZE * 0.5f + voxel.transform.right * Common.VOXEL_SIZE * 0.5f + voxel.transform.forward * Common.VOXEL_SIZE * 0.5f;

            Bounds voxelBounds = new Bounds();
            voxelBounds.size = new Vector3(Common.VOXEL_SIZE, Common.VOXEL_SIZE, Common.VOXEL_SIZE);
            voxelBounds.center = voxelCenter;

            newBounds.Encapsulate(voxelBounds);
        }
        boxCollider.center = newBounds.center;
        boxCollider.size = newBounds.size;
    }

    public static BoxCollider VoxelCreateBoxCollider(GameObject voxel)
    {
        BoxCollider boxCollider = voxel.AddComponent<BoxCollider>();
        boxCollider.center = new Vector3(Common.VOXEL_SIZE * 0.5f, + Common.VOXEL_SIZE * 0.5f, + Common.VOXEL_SIZE * 0.5f);
        boxCollider.size = new Vector3(Common.VOXEL_SIZE, Common.VOXEL_SIZE, Common.VOXEL_SIZE);

        return boxCollider;
    }

    public static GameObject ConvertVoxelRenderIntoSeperatedVoxelGameObject(VoxelRender voxelRender, GameObject parentGameObject)
    {
        GameObject voxel = new GameObject("SeperatedVoxel");
        VoxelCreateBoxCollider(voxel);
        voxel.layer = LayerMask.NameToLayer("SeperatedVoxel");
        voxel.AddComponent<Rigidbody>();
        voxel.transform.SetParent(parentGameObject.transform);
        voxel.transform.localPosition = voxelRender.localPosition;
        voxel.transform.localRotation = voxelRender.localRotation;

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

        MeshFilter meshFilter = voxel.AddComponent<MeshFilter>();

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();

        meshFilter.mesh = mesh;

        Vector2[] uvs = new Vector2[vertices.Count];
        for (int index = 0; index < uvs.Length; index++)
        {
            uvs[index] = voxelRender.meshUVs;
        }
        mesh.uv = uvs;

        MeshRenderer meshRenderer = voxel.AddComponent<MeshRenderer>();
        meshRenderer.material = parentGameObject.GetComponent<MeshRenderer>().material;

        return voxel;
    }

    public static GameObject GenerateVoxelGameObjectFromVoxelRender(VoxelRender voxelRender, bool createMesh, Transform parentTransform)
    {
        GameObject voxel = new GameObject("DestructibleVoxel");
        voxel.layer = LayerMask.NameToLayer("DestructibleVoxel");

        if (parentTransform == null)
        {
            voxel.transform.position = voxelRender.localPosition;
            voxel.transform.rotation = voxelRender.localRotation;
        }
        else
        {
            voxel.transform.SetParent(parentTransform);
            voxel.transform.localPosition = voxelRender.localPosition;
            voxel.transform.localRotation = voxelRender.localRotation;
        }

        VoxelData voxelData = voxel.AddComponent<VoxelData>();
        voxelData.voxelRender = voxelRender;

        if (!createMesh)
            return voxel;

        bool willRenderVoxel = false;
        foreach (bool drawFace in voxelRender.drawFaces)
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

        if (voxelRender.drawFaces[(int)Common.VoxelFaces.FRONT])
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

        if (voxelRender.drawFaces[(int)Common.VoxelFaces.RIGHT])
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

        if (voxelRender.drawFaces[(int)Common.VoxelFaces.TOP])
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

        if (voxelRender.drawFaces[(int)Common.VoxelFaces.LEFT])
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

        if (voxelRender.drawFaces[(int)Common.VoxelFaces.BOTTOM])
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

        if (voxelRender.drawFaces[(int)Common.VoxelFaces.BACK])
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
            uvs[index] = voxelRender.meshUVs;
        }
        mesh.uv = uvs;

        meshFilter.mesh = mesh;

        return voxel;
    }

    public static Mesh CreateMeshFromVoxelRender(VoxelRender voxelRender)
    {
        List<int> triangles = new List<int>();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();

        if (voxelRender.drawFaces[(int)Common.VoxelFaces.FRONT])
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

        if (voxelRender.drawFaces[(int)Common.VoxelFaces.RIGHT])
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

        if (voxelRender.drawFaces[(int)Common.VoxelFaces.TOP])
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

        if (voxelRender.drawFaces[(int)Common.VoxelFaces.LEFT])
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

        if (voxelRender.drawFaces[(int)Common.VoxelFaces.BOTTOM])
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

        if (voxelRender.drawFaces[(int)Common.VoxelFaces.BACK])
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

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();

        Vector2[] uvs = new Vector2[vertices.Count];
        for (int index = 0; index < uvs.Length; index++)
        {
            uvs[index] = voxelRender.meshUVs;
        }
        mesh.uv = uvs;

        return mesh;
    }
}
