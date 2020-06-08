using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerViewmodel : MonoBehaviour
{
    private Game game;

    private InventoryItem previousSelectedInventoryItem;

    public Viewmodel currentViewmodel;
    private Mesh defaultMesh;
    private Material defaultMaterial;
    private Animator animator;
    private bool swinging;

    private const float axeSwingTrunkDistance = 2.0f;
    private const float axeSwingDamageRadius = 0.25f;

    // Start is called before the first frame update
    void Start()
    {
        game = Game.GetGame();

        animator = GetComponent<Animator>();
        defaultMesh = gameObject.GetComponent<MeshFilter>().mesh;
        defaultMaterial = gameObject.GetComponent<MeshRenderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateCurrentViewmodel();

        UpdateSwinging();

        previousSelectedInventoryItem = game.playerInventory.toolbarItemSlots[game.playerInventory.selectedSlotIndex].inventoryItem;
    }

    private void UpdateCurrentViewmodel()
    {
        bool changedSelectedInventoryItem = !game.playerInventory.selectedInventoryItem.viewmodel.Equals(currentViewmodel);
        if (changedSelectedInventoryItem)
        {
            if (game.playerInventory.PlayerCurrentlySelectingAnItem())
            {
                SetViewModel(game.playerInventory.selectedInventoryItem.viewmodel);
            }
            else
            {
                ClearViewModel();
            }
        }
    }

    void UpdateSwinging()
    {
        if (game.playerInput.SwingPressed())
        {
            animator.CrossFade(currentViewmodel.viewmodelName + "_swing", 0.10f, -1, 0.0f);
            
            RaycastHit hit;
            if (Physics.Raycast(game.playerEye.position, game.playerEye.forward, out hit, axeSwingTrunkDistance, LayerMask.GetMask("Destructible")))
            {
                hit.collider.gameObject.GetComponent<Destructible>().AssignDestructibleVoxels();
            }

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
        RaycastHit hit;
        if (Physics.Raycast(game.playerEye.position, game.playerEye.forward, out hit, axeSwingTrunkDistance, LayerMask.GetMask("Destructible")))
        {
            hit.collider.gameObject.GetComponent<Destructible>().TakeDamage(hit.point, axeSwingDamageRadius);
        }
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
