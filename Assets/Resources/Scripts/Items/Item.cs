using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    public string itemName;
    public float triggerDistance = 4.0f;

    private GameObject player;
    private Transform playerEye;
    private Transform mainCamera;
    private Transform viewmodel;
    private PlayerInput playerInput;
    private PlayerViewmodel viewmodelScript;
    private PlayerInventory playerInventory;

    [HideInInspector]
    public bool triggered;

    private float floatX;
    private const float floatRate = 0.01f;
    private const float floatHeight = 0.1f;
    private const float floatBaseHeight = 0.2f;
    private const float rotateSpeed = 45.0f;

    // Start is called before the first frame update
    void Awake()
    {
        player = GameObject.FindWithTag("Player");

        playerInput = player.GetComponent<PlayerInput>();
        playerEye = player.transform.Find("Eye");
        mainCamera = playerEye.Find("MainCamera");
        viewmodel = mainCamera.Find("Viewmodel");
        viewmodelScript = viewmodel.GetComponent<PlayerViewmodel>();

        playerInventory = player.GetComponent<PlayerInventory>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateTriggeredMovement();
    }

    void UpdateTriggeredMovement()
    {
        if (!triggered)
            return;

        InventoryItem inventoryItem = playerInventory.BuildInventoryItemFromName(itemName);

        if (playerInventory.PlayerHasInventoryItem(inventoryItem) || playerInventory.PlayerCurrentlySelectingAnItem())
        {
            StartCoroutine(TriggeredMovementToInventory(inventoryItem));
        }
        else
        {
            StartCoroutine(TriggeredMovementToViewmodel(inventoryItem));
        }

        triggered = true;
    }

    IEnumerator TriggeredMovementToInventory(InventoryItem inventoryItem)
    {
        float durationSeconds = 0.35f;

        Vector3 originalPosition = transform.position;
        Quaternion originalRotation = transform.rotation;

        for (float fraction = 0.0f; fraction < 1.0f; fraction += Time.deltaTime / durationSeconds)
        {
            transform.position = Vector3.Lerp(originalPosition, player.transform.position, fraction);

            transform.rotation = Quaternion.Lerp(originalRotation, Quaternion.Euler(player.transform.eulerAngles), fraction);

            yield return null;
        }

        playerInventory.GivePlayerInventoryItem(inventoryItem);

        Destroy(this.gameObject);

        yield return null;
    }

    IEnumerator TriggeredMovementToViewmodel(InventoryItem inventoryItem)
    {
        float durationSeconds = 0.25f;

        Vector3 originalPosition = transform.position;
        Quaternion originalRotation = transform.rotation;

        playerInventory.GivePlayerInventoryItem(inventoryItem);
        viewmodelScript.PlayerHideViewmodel();

        for (float fraction = 0.0f; fraction < 1.0f; fraction += Time.deltaTime / durationSeconds)
        {
            transform.position = Vector3.Lerp(originalPosition, viewmodel.position, fraction);

            transform.rotation = Quaternion.Lerp(originalRotation, Quaternion.Euler(viewmodel.eulerAngles), fraction);

            yield return null;
        }

        viewmodelScript.PlayerShowViewmodel();
        Destroy(this.gameObject);

        yield return null;
    }

    void IdleMovement()
    {
        float previousFloatHeight = (Mathf.Sin(floatX) * floatHeight);

        floatX = Mathf.Repeat(floatX + floatRate, 360);

        float nextFloatHeight = (Mathf.Sin(floatX) * floatHeight);

        float additionalFloatHeight = (nextFloatHeight - previousFloatHeight);

        transform.position += new Vector3(0.0f, additionalFloatHeight, 0.0f);

        float rotateStep = (rotateSpeed * Time.deltaTime);
        transform.Rotate(0.0f, rotateStep, 0.0f);
    }
}
