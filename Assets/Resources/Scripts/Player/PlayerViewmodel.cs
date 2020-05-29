﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerViewmodel : MonoBehaviour
{
    private GameObject player;
    private PlayerInput playerInput;
    private Transform playerEye;
    private PlayerInventory playerInventory;

    private InventoryItem previousSelectedInventoryItem;

    public Viewmodel currentViewmodel;
    private Mesh defaultMesh;
    private Material defaultMaterial;
    private Animator animator;
    private bool swinging;

    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindWithTag("Player");
        playerInput = player.GetComponent<PlayerInput>();
        playerEye = player.transform.Find("Eye");
        playerInventory = player.GetComponent<PlayerInventory>();

        animator = GetComponent<Animator>();
        defaultMesh = gameObject.GetComponent<MeshFilter>().mesh;
        defaultMaterial = gameObject.GetComponent<MeshRenderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateCurrentViewmodel();

        UpdateSwinging();

        previousSelectedInventoryItem = playerInventory.toolbarItemSlots[playerInventory.selectedSlotIndex].inventoryItem; ;
    }

    private void UpdateCurrentViewmodel()
    {
        bool changedSelectedInventoryItem = !playerInventory.selectedInventoryItem.viewmodel.Equals(currentViewmodel);
        if (changedSelectedInventoryItem)
        {
            if (playerInventory.PlayerCurrentlySelectingAnItem())
            {
                SetViewModel(playerInventory.selectedInventoryItem.viewmodel);
            }
            else
            {
                ClearViewModel();
            }
        }
    }

    void UpdateSwinging()
    {
        if (playerInput.SwingPressed())
        {
            animator.CrossFade(currentViewmodel.viewmodelName + "_swing", 0.10f, -1, 0.0f);
            swinging = true;
        }
    }

    public void SetViewModel(Viewmodel viewmodel)
    {
        gameObject.GetComponent<MeshFilter>().mesh = viewmodel.mesh;
        gameObject.GetComponent<MeshRenderer>().material = viewmodel.material;

        animator.Play(viewmodel.viewmodelName + "_idle");

        currentViewmodel = viewmodel;
    }

    public void ClearViewModel()
    {
        gameObject.GetComponent<MeshFilter>().mesh = defaultMesh;
        gameObject.GetComponent<MeshRenderer>().material = defaultMaterial;

        currentViewmodel = new Viewmodel();
    }

    public void PlayerHideViewmodel()
    {
        gameObject.GetComponent<MeshRenderer>().enabled = false;
    }

    public void PlayerShowViewmodel()
    {
        gameObject.GetComponent<MeshRenderer>().enabled = true;
    }

    public void SwingingAnimationComplete()
    {
        swinging = false;
    }


    public void AxeSwingImpact()
    {
        //GameObject[] destructibleTrees = Utility.GetAllDestructibleTrees();
        //destructibleTrees = Utility.SortByDistance(destructibleTrees, player.transform.position);
        //destructibleTrees[0].GetComponent<DestructibleTree>().TakeDamage(playerEye.position);
    }
}

public struct Viewmodel
{
    public string viewmodelName;
    public Mesh mesh;
    public Material material;

    public Viewmodel(string _viewmodelName, Mesh _mesh, Material _material)
    {
        viewmodelName = _viewmodelName;
        mesh = _mesh;
        material = _material;
    }
}
