using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Security.AccessControl;

public class Game : MonoBehaviour
{
    [HideInInspector]
    public GameObject player;
    [HideInInspector]
    public Transform playerEye;
    [HideInInspector]
    public PlayerInput playerInput;
    [HideInInspector]
    public PlayerInventory playerInventory;
    [HideInInspector]
    public Transform mainCamera;
    [HideInInspector]
    public Transform viewmodelTransform;
    [HideInInspector]
    public PlayerViewmodel playerViewmodel;
    [HideInInspector]
    public CharacterController characterController;
    [HideInInspector]
    public PlayerMovement playerMovement;

    [HideInInspector]
    public Transform seperatedVoxelsParentTransform;
    [HideInInspector]
    public Transform destructibleVoxelsParentTransform;
    [HideInInspector]
    public Dictionary<GameObject, SeperatedVoxel> seperatedVoxelDictionaries = new Dictionary<GameObject, SeperatedVoxel>();
    [HideInInspector]
    public Dictionary<GameObject, DestructibleVoxel> destructibleVoxelDictionaries = new Dictionary<GameObject, DestructibleVoxel>();
    [HideInInspector]
    public List<SeperatedVoxel> seperatedVoxels = new List<SeperatedVoxel>();
    [HideInInspector]
    public List<DestructibleVoxel> destructibleVoxels = new List<DestructibleVoxel>();

    private const int seperatedVoxelMaxCount = 1000;
    private Mesh seperatedVoxelMesh;
    private const int destructibleVoxelMaxCount = 1000;

    public void Awake()
    {
        player = GameObject.FindWithTag("Player");
        playerEye = player.transform.Find("Eye");
        playerInventory = player.GetComponent<PlayerInventory>();
        playerInput = player.GetComponent<PlayerInput>();
        mainCamera = playerEye.Find("MainCamera");
        viewmodelTransform = mainCamera.Find("Viewmodel");
        playerViewmodel = viewmodelTransform.GetComponent<PlayerViewmodel>();
        characterController = player.GetComponent<CharacterController>();
        playerMovement = player.GetComponent<PlayerMovement>();

        seperatedVoxelMesh = Resources.Load("GeneratedMeshes/voxel/Meshes/voxel", typeof(Mesh)) as Mesh;
        seperatedVoxelsParentTransform = GameObject.Find("SeperatedVoxelsParent").transform;
        for (int index = 0; index < seperatedVoxelMaxCount; index++)
        {
            GameObject seperatedVoxelGameObject = GenerateSeperatedVoxel();
            SeperatedVoxel seperatedVoxel = seperatedVoxelGameObject.AddComponent<SeperatedVoxel>();
            seperatedVoxelDictionaries.Add(seperatedVoxelGameObject, seperatedVoxel);
            seperatedVoxels.Add(seperatedVoxel);
        }

        destructibleVoxelsParentTransform = GameObject.Find("DestructibleVoxelsParent").transform;
        for (int index = 0; index < destructibleVoxelMaxCount; index++)
        {
            GameObject destructibleVoxelGameObject = GenerateDestructibleVoxel();
            DestructibleVoxel destructibleVoxel = destructibleVoxelGameObject.AddComponent<DestructibleVoxel>();
            destructibleVoxelDictionaries.Add(destructibleVoxelGameObject, destructibleVoxel);
            destructibleVoxels.Add(destructibleVoxel);
        }

        GameObject[] gameObjectsToDestroy = GameObject.FindGameObjectsWithTag("Destroy");
        foreach (GameObject gameObjectToDestroy in gameObjectsToDestroy)
        {
            Destroy(gameObjectToDestroy);
        }

        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Destructible"), LayerMask.NameToLayer("SeperatedVoxel"));
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("DestructibleVoxel"));
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("SeperatedVoxel"));
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("SeperatedVoxel"), LayerMask.NameToLayer("FloatingVoxel"));
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("DestructibleVoxel"), LayerMask.NameToLayer("FloatingVoxel"));
    }

    public static Game GetGame()
    {
        return GameObject.Find("Game").GetComponent<Game>();
    }

    private GameObject GenerateSeperatedVoxel()
    {
        GameObject seperatedVoxel = new GameObject("SeperatedVoxel");
        seperatedVoxel.layer = LayerMask.NameToLayer("SeperatedVoxel");
        seperatedVoxel.transform.SetParent(seperatedVoxelsParentTransform, false);

        MeshFilter meshFilter = seperatedVoxel.AddComponent<MeshFilter>();
        meshFilter.mesh = seperatedVoxelMesh;

        Material material = new Material(Shader.Find("Standard"));
        material.SetFloat("_Glossiness", 0.0f);
        MeshRenderer meshRenderer = seperatedVoxel.AddComponent<MeshRenderer>();
        meshRenderer.material = material;

        return seperatedVoxel;
    }

    private GameObject GenerateDestructibleVoxel()
    {
        GameObject destructibleVoxel = new GameObject("DestructibleVoxel");
        destructibleVoxel.layer = LayerMask.NameToLayer("DestructibleVoxel");
        destructibleVoxel.transform.SetParent(destructibleVoxelsParentTransform, false);

        return destructibleVoxel;
    }
}
