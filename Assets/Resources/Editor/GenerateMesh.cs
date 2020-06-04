//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine;
//using UnityEngine.UIElements;

//public struct VoxelStruct
//{
//    public bool active;
//    public bool launched;
//    public Vector3 position;
//    public Color color;
//    public GameObject gameObject;

//    public VoxelStruct(bool _active, bool _launched, Vector3 _position, Color _color, GameObject _gameObject)
//    {
//        active = _active;
//        launched = _launched;
//        position = _position;
//        color = _color;
//        gameObject = _gameObject;
//    }
//}

//public class GenerateMesh : MonoBehaviour
//{
//    private Dictionary<Vector3, VoxelStruct> allVoxelData = new Dictionary<Vector3, VoxelStruct>();

//    float treeHeight = 30;

//    private static Color barkDarkColor = new Color(0.37f, 0.22f, 0.0f);
//    private static Color barkMediumColor = new Color(0.49f, 0.28f, 0.0f);
//    private static Color barkLightColor = new Color(0.72f, 0.41f, 0.0f);
//    private static Color woodColor = new Color(0.84f, 0.57f, 0.19f);
//    private static Color coreColor = new Color(1.00f, 0.88f, 0.63f);

//    // 0 = empty
//    // 1 = dark bark
//    // 2 = medium bark
//    // 3 = light bark
//    // 4 = wood
//    // 5 = core color

//    Color[] voxelColors = new Color[] {Color.clear, barkDarkColor, barkMediumColor, barkLightColor, woodColor, coreColor};
//    int[][] template = new int [][]
//    {
//        new int[] {0, 0, 1, 1, 0, 0},
//        new int[] {0, 2, 2, 2, 1, 0},
//        new int[] {1, 2, 4, 4, 2, 1},
//        new int[] {1, 4, 5, 5, 4, 1},
//        new int[] {1, 4, 5, 5, 4, 1},
//        new int[] {1, 3, 4, 4, 3, 1},
//        new int[] {0, 1, 2, 2, 1, 0},
//        new int[] {0, 0, 1, 2, 0, 0}
//    };

//    private void Start()
//    {
//        for (int height = 0; height < treeHeight; height++)
//        {
//            for (int row = 0; row < template.Length; row++)
//            {
//                for (int column = 0; column < template[0].Length; column++)
//                {
//                    int colorIndex = template[row][column];
//                    if (colorIndex == 0)
//                        continue;

//                    Vector3 position = transform.position + (-transform.forward * row * Common.VOXEL_SIZE) + (transform.right * column * Common.VOXEL_SIZE) + (transform.up * height * Common.VOXEL_SIZE);
//                    Color color = voxelColors[colorIndex];

//                    VoxelStruct voxelData = new VoxelStruct(false, false, position, color, default(GameObject));
//                    if (TemplateDataHasEmptySide(row, column))
//                    {
//                        voxelData.active = true;
//                        voxelData.gameObject = GenerateVoxel(position, transform.up, transform.right, transform.forward, color);
//                    }
//                    allVoxelData.Add(position, voxelData);
//                }
//            }
//        }
//    }

//    private bool TemplateDataHasEmptySide(int row, int column)
//    {
//        // Forward Edge of Template
//        if (row == 0)
//            return true;

//        // Backwards Edge of Template
//        if (row == template.Length - 1)
//            return true;

//        // Left Edge of Template
//        if (column == 0)
//            return true;

//        // Right Edge of Template
//        if (column == template[0].Length - 1)
//            return true;

//        // Left Data Is Empty
//        if (template[row - 1][column] == 0)
//            return true;

//        // Right Data Is Empty
//        if (template[row + 1][column] == 0)
//            return true;

//        // Forward Data Is Empty
//        if (template[row][column - 1] == 0)
//            return true;

//        // Back Data Is Empty
//        if (template[row][column + 1] == 0)
//            return true;

//        return false;
//    }

//    private bool VoxelDataHasEmptySide(VoxelStruct voxelData)
//    {
//        Vector3[] voxelKeys =
//        {
//            voxelData.position - (transform.right * Common.VOXEL_SIZE),
//            voxelData.position + (transform.right * Common.VOXEL_SIZE),
//            voxelData.position - (transform.up * Common.VOXEL_SIZE),
//            voxelData.position + (transform.up * Common.VOXEL_SIZE),
//            voxelData.position - (transform.forward * Common.VOXEL_SIZE),
//            voxelData.position + (transform.forward * Common.VOXEL_SIZE)
//        };

//        foreach( Vector3 voxelKey in voxelKeys )
//        {
//            if (!allVoxelData.ContainsKey(voxelKey))
//                return true;

//            if (allVoxelData[voxelKey].active == false)
//                return true;
//        }

//        return false;
//    }

//    public GameObject GenerateVoxel(Vector3 position, Vector3 up, Vector3 right, Vector3 forward, Color color)
//    {
//        GameObject voxel = new GameObject();

//        Vector3[] vertices =
//        {
//            // Front Face
//            position,
//            position + (up * Common.VOXEL_SIZE),
//            position + (up * Common.VOXEL_SIZE) + (right * Common.VOXEL_SIZE),
//            position + (right * Common.VOXEL_SIZE),

//            // Right Face
//            position + (right * Common.VOXEL_SIZE),
//            position + (right * Common.VOXEL_SIZE) + (up * Common.VOXEL_SIZE),
//            position + (right * Common.VOXEL_SIZE) + (up * Common.VOXEL_SIZE) + (forward * Common.VOXEL_SIZE),
//            position + (right * Common.VOXEL_SIZE) + (forward * Common.VOXEL_SIZE),

//            // Top Face
//            position + (up * Common.VOXEL_SIZE),
//            position + (up * Common.VOXEL_SIZE) + (forward * Common.VOXEL_SIZE),
//            position + (up * Common.VOXEL_SIZE) + (forward * Common.VOXEL_SIZE) + (right * Common.VOXEL_SIZE),
//            position + (up * Common.VOXEL_SIZE) + (right * Common.VOXEL_SIZE),

//            // Left Face
//            position,
//            position + (up * Common.VOXEL_SIZE),
//            position + (up * Common.VOXEL_SIZE) + (forward * Common.VOXEL_SIZE),
//            position + (forward * Common.VOXEL_SIZE),

//            // Bottom Face
//            position,
//            position + (forward * Common.VOXEL_SIZE),
//            position + (forward * Common.VOXEL_SIZE) + (right * Common.VOXEL_SIZE),
//            position + (right * Common.VOXEL_SIZE),

//            // Bottom Back
//            position + (forward * Common.VOXEL_SIZE),
//            position + (forward * Common.VOXEL_SIZE) + (up * Common.VOXEL_SIZE),
//            position + (forward * Common.VOXEL_SIZE) + (up * Common.VOXEL_SIZE) + (right * Common.VOXEL_SIZE),
//            position + (forward * Common.VOXEL_SIZE) + (right * Common.VOXEL_SIZE)
//        };

//        MeshRenderer meshRenderer = voxel.AddComponent<MeshRenderer>();

//        Material material = new Material(Shader.Find("Standard"));
//        Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
//        texture.SetPixel(0, 0, color);
//        texture.Apply();
//        material.mainTexture = texture;
//        meshRenderer.material = material;

//        MeshFilter meshFilter = voxel.AddComponent<MeshFilter>();

//        Mesh mesh = new Mesh();
//        mesh.vertices = vertices;

//        int[] triangles = {
//            0, 1, 2, // Front Face
//			0, 2, 3,
//            4, 5, 6, // Right Face
//			4, 6, 7,
//            8, 9, 10, // Up Face
//			8, 10, 11,
//            12, 14, 13, // Left Face
//			12, 15, 14,
//            16, 18, 17, //Bottom Face
//			16, 19, 18,
//            22, 21, 20, // Back Face
//			22, 20, 23
//        };
//        mesh.triangles = triangles;

//        Vector3[] normals =
//        {
//            -forward,
//            -forward,
//            -forward,
//            -forward,
//            right,
//            right,
//            right,
//            right,
//            up,
//            up,
//            up,
//            up,
//            -right,
//            -right,
//            -right,
//            -right,
//            -up,
//            -up,
//            -up,
//            -up,
//            forward,
//            forward,
//            forward,
//            forward
//        };
//        mesh.normals = normals;

//        meshFilter.mesh = mesh;

//        return voxel;
//    }
//}
