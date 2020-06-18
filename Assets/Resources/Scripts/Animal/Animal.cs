using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions;

public class Animal : MonoBehaviour
{
    public string[] edibleItems;
    public Mesh happyMesh;
    public Material happyMaterial;

    private GameObject player;
    private PlayerInput playerInput;
    private PlayerInventory playerInventory;
    private Transform playerEye;
    private Transform mainCamera;
    private Transform viewmodel;
    private PlayerViewmodel viewmodelScript;

    private NavMeshAgent navMeshAgent;
    private Transform mesh;
    private Transform mouth;
    private Transform food;
    private Animator animator;

    private bool eating;
    private bool happy;
    private bool tamed;
    private const float tamedGoalRadius = 3.0f;

    private bool lookingAtPlayer;

    private const float lookAtPlayerDistance = 4.0f;
    private const float rotateToPlayerSpeed = 4.0f;

    private Vector3 goalPosition;
    private const float defaultGoalSpeedMin = 0.5f;
    private const float defaultGoalSpeedMax = 4.0f;
    private const float defaultGoalDistanceMin = 1.0f;
    private const float defaultGoalDistanceMax = 10.0f;
    private const float defaultGoalRadius = 2.0f;
    private const float rotateToGoalPositionSpeed = 4.0f;
    private const float walkAnimationRateMin = 0.5f;
    private const float walkAnimationRateMax = 1.5f;
    private float goalRadius = defaultGoalRadius;
    private bool movingTowardsGoal;

    // Start is called before the first frame update
    void Awake()
    {
        player = GameObject.FindWithTag("Player");
        playerInput = player.GetComponent<PlayerInput>();
        playerInventory = player.GetComponent<PlayerInventory>();
        playerEye = player.transform.Find("Eye");
        mainCamera = playerEye.Find("MainCamera");
        viewmodel = mainCamera.Find("Viewmodel");
        viewmodelScript = viewmodel.GetComponent<PlayerViewmodel>();

        navMeshAgent = GetComponent<NavMeshAgent>();
        mesh = transform.Find("mesh");
        animator = GetComponent<Animator>();
        mouth = mesh.Find("mouth");
        food = mouth.Find("food");

        Destroy(food.gameObject.GetComponent<MeshFilter>());
        Destroy(food.gameObject.GetComponent<MeshRenderer>());

        goalPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateGoalPosition();
        UpdateGoalMovement();
        UpdateGoalRotation();
        UpdatePlayerFeeding();
    }

    void UpdateGoalPosition()
    {
        if(tamed)
        {
            goalPosition = player.transform.position;
            goalRadius = tamedGoalRadius;
        }
        //else if()
        //{

        //}
        else
        {
            goalPosition = transform.position;
            goalRadius = defaultGoalRadius;
        }
    }

    void UpdateGoalMovement()
    {
        if (eating)
            return;

        float distanceToGoal = Vector3.Distance(transform.position, goalPosition);
        if (distanceToGoal > goalRadius)
        {
            if( !movingTowardsGoal)
            {
                animator.Play("walk");
                animator.Update(Time.deltaTime);
            }

            movingTowardsGoal = true;

            float goalSpeed = Utility.Remap(distanceToGoal, defaultGoalDistanceMin, defaultGoalDistanceMax, defaultGoalSpeedMin, defaultGoalSpeedMax);
            float animationRate = Utility.Remap(goalSpeed, defaultGoalSpeedMin, defaultGoalSpeedMax, walkAnimationRateMin, walkAnimationRateMax);

            animator.speed = animationRate;
            navMeshAgent.speed = goalSpeed;
            navMeshAgent.SetDestination(goalPosition);
        }
        else
        {
            if (movingTowardsGoal)
            {
                animator.CrossFadeInFixedTime("Idle", 0.5f);
                animator.speed = 1.0f;
                animator.Update(Time.deltaTime);
            }

            movingTowardsGoal = false;
        }
    }

    void UpdateGoalRotation()
    {
        lookingAtPlayer = (Vector3.Distance(player.transform.position, transform.position) <= lookAtPlayerDistance);

        if (lookingAtPlayer)
        {
            Vector3 animalToPlayer = (player.transform.position - transform.position);
            Quaternion goalRotation = Quaternion.LookRotation(animalToPlayer);

            float rotationStep = (Time.deltaTime * rotateToPlayerSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, goalRotation, rotationStep);
        }
        else if(movingTowardsGoal)
        {
            Vector3 animalToGoalPosition = (goalPosition - transform.position);
            Quaternion goalRotation = Quaternion.LookRotation(animalToGoalPosition);

            float rotationStep = (Time.deltaTime * rotateToGoalPositionSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, goalRotation, rotationStep);
        }
    }

    void UpdatePlayerFeeding()
    {
        if (!ShouldBeFedByPlayer())
            return;

        playerInventory.TakePlayerInventoryItem(playerInventory.selectedInventoryItem);

        MeshFilter foodMeshFilter = food.gameObject.AddComponent<MeshFilter>();
        foodMeshFilter.mesh = viewmodelScript.currentViewmodel.mesh;

        MeshRenderer foodMeshRenderer = food.gameObject.AddComponent<MeshRenderer>();
        foodMeshRenderer.material = viewmodelScript.currentViewmodel.material;

        animator.Play("eating_" + playerInventory.selectedInventoryItem.itemName);
        animator.Update(Time.deltaTime);

        eating = true;

        //StartCoroutine(FoodMovementFromPlayerViewmodel(playerInventory.selectedInventoryItem));
    }

    private bool ShouldBeFedByPlayer()
    {
        if (!lookingAtPlayer)
            return false;

        if (eating)
            return false;

        if (!playerInput.UsePressed() && !playerInput.SwingPressed())
            return false;

        bool playerHoldingEdibleItem = (System.Array.IndexOf(edibleItems, playerInventory.selectedInventoryItem.itemName) != -1);
        if (!playerHoldingEdibleItem)
            return false;

        return true;
    }

    //IEnumerator FoodMovementFromPlayerViewmodel(InventoryItem inventoryItem)
    //{
    //    playerInventory.TakePlayerInventoryItem(playerInventory.selectedInventoryItem);

    //    animator.speed = 1.0f;
    //    animator.Play("eating_idle_" + inventoryItem.itemName);

    //    float durationSeconds = 0.25f;

    //    Vector3 originalPosition = viewmodel.position;
    //    Quaternion originalRotation = viewmodel.rotation;

    //    GameObject movingFood = new GameObject();
    //    movingFood.AddComponent<MeshRenderer>().material = inventoryItem.viewmodel.material;
    //    movingFood.AddComponent<MeshFilter>().mesh = inventoryItem.viewmodel.mesh;

    //    for (float fraction = 0.0f; fraction < 1.0f; fraction += Time.deltaTime / durationSeconds)
    //    {
    //        movingFood.transform.position = Vector3.Lerp(originalPosition, mouth.position, fraction);

    //        movingFood.transform.rotation = Quaternion.Lerp(originalRotation, Quaternion.Euler(mouth.eulerAngles), fraction);

    //        yield return null;
    //    }

    //    Destroy(movingFood);

    //    MeshFilter foodMeshFilter = food.gameObject.AddComponent<MeshFilter>();
    //    foodMeshFilter.mesh = viewmodelScript.currentViewmodel.mesh;

    //    MeshRenderer foodMeshRenderer = food.gameObject.AddComponent<MeshRenderer>();
    //    foodMeshRenderer.material = viewmodelScript.currentViewmodel.material;

    //    animator.Play("eating_" + inventoryItem.itemName);
    //    animator.Update(Time.deltaTime);

    //    eating = true;

    //    yield return null;
    //}

    public void EatingFoodModelAnimationComplete()
    {
        Destroy(food.gameObject.GetComponent<MeshFilter>());
        Destroy(food.gameObject.GetComponent<MeshRenderer>());
    }

    public void EatingHappyModelSwap()
    {
        mesh.gameObject.GetComponent<MeshFilter>().mesh = happyMesh;
        mesh.gameObject.GetComponent<MeshRenderer>().material = happyMaterial;

        happy = true;
    }

    public void EatingAnimationComplete()
    {
        eating = false;
        tamed = true;
    }
}
