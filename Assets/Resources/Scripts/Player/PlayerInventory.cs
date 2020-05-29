using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInventory : MonoBehaviour
{
    private Dictionary<string, InventoryItem> inventoryItemTypes = new Dictionary<string, InventoryItem>();

    [HideInInspector]
    public InventoryItem selectedInventoryItem;

    [HideInInspector]
    public int selectedSlotIndex;

    private GameObject player;
    private PlayerInput playerInput;
    private Transform playerEye;
    private Transform mainCamera;
    private Transform viewmodel;
    private PlayerViewmodel viewmodelScript;

    private const int slotCount = 5;
    private const int oddNumberOfSlots = (slotCount % 2);
    private const int slotBorderThickness = 5;
    private const int slotIconWidth = 70;
    private const int slotScreenEdgeBuffer = 10;
    private const int totalBorderThickness = 5;
    private const int slotBorderRectangleWidth = (int)(slotIconWidth + (slotBorderThickness * 2.0f));
    private const int slotsFullyLeftOfCenter = (int)(slotCount * 0.5f);
    private const float slotsLeftOfCenter = ((slotsFullyLeftOfCenter + ((oddNumberOfSlots) * 0.5f)));
    private const float leftOfCenterTotalIconWidth = (slotsLeftOfCenter * slotIconWidth);
    private const float leftOfCenterTotalBorderWidth = (slotsLeftOfCenter * slotIconWidth);

    public ToolbarItemSlot[] toolbarItemSlots = new ToolbarItemSlot[slotCount];

    Texture2D inventoryTexture;

    private static Texture2D slotBackgroundTexture;
    private static Texture2D slotBorderTexture;
    private static Texture2D totalBorderTexture;
    private static Texture2D slotSelectedBorderTexture;


    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindWithTag("Player");
        playerInput = player.GetComponent<PlayerInput>();
        playerEye = player.transform.Find("Eye");

        mainCamera = playerEye.Find("MainCamera");
        viewmodel = mainCamera.Find("Viewmodel");
        viewmodelScript = viewmodel.GetComponent<PlayerViewmodel>();

        slotBackgroundTexture = new Texture2D(1, 1);
        slotBackgroundTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.5f));
        slotBackgroundTexture.Apply();

        slotBorderTexture = new Texture2D(1, 1);
        slotBorderTexture.SetPixel(0, 0, new Color(0.25f, 0.25f, 0.25f));
        slotBorderTexture.Apply();

        totalBorderTexture = new Texture2D(1, 1);
        totalBorderTexture.SetPixel(0, 0, Color.black);
        totalBorderTexture.Apply();

        slotSelectedBorderTexture = new Texture2D(1, 1);
        slotSelectedBorderTexture.SetPixel(0, 0, Color.white);
        slotSelectedBorderTexture.Apply();

        InitializeIventoryItemTypes();
    }

    void InitializeIventoryItemTypes()
    {
        GameObject appleModel = GameObject.Find("viewmodel_apple");
        Viewmodel appleViewmodel = new Viewmodel("apple", appleModel.GetComponent<MeshFilter>().mesh, appleModel.GetComponent<MeshRenderer>().material);
        InventoryItem applyInventoryItem = new InventoryItem("apple", Resources.Load("Textures/icon_apple") as Texture2D, appleViewmodel);
        inventoryItemTypes.Add("apple", applyInventoryItem);
        Destroy(appleModel);

        GameObject orangeModel = GameObject.Find("viewmodel_orange");
        Viewmodel orangeViewmodel = new Viewmodel("orange", orangeModel.GetComponent<MeshFilter>().mesh, orangeModel.GetComponent<MeshRenderer>().material);
        InventoryItem orangeInventoryItem = new InventoryItem("orange", Resources.Load("Textures/icon_orange") as Texture2D, orangeViewmodel);
        inventoryItemTypes.Add("orange", orangeInventoryItem);
        Destroy(orangeModel);

        GameObject axeModel = GameObject.Find("viewmodel_axe");
        Viewmodel axeViewmodel = new Viewmodel("axe", axeModel.GetComponent<MeshFilter>().mesh, axeModel.GetComponent<MeshRenderer>().material);
        InventoryItem axeInventoryItem = new InventoryItem("axe", Resources.Load("Textures/icon_axe") as Texture2D, axeViewmodel);
        inventoryItemTypes.Add("axe", axeInventoryItem);
        Destroy(axeModel);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateSelectedSlotIndex();

        UpdatePlayerTriggerItem();
    }

    private void UpdateSelectedSlotIndex()
    {
        if (playerInput.ScrollToolbarRightPressed())
        {
            selectedSlotIndex = (int)Mathf.Repeat(selectedSlotIndex + 1, slotCount);
        }
        else if (playerInput.ScrollToolbarLeftPressed())
        {
            selectedSlotIndex = (int)Mathf.Repeat(selectedSlotIndex - 1, slotCount);
        }

        selectedInventoryItem = toolbarItemSlots[selectedSlotIndex].inventoryItem;
    }

    void OnGUI()
    {
        OnGUIToolbar();

        //GameObject[] allItems = Utility.GetAllItems();
        //foreach (GameObject item in allItems)
        //{
        //    Item itemScript = item.GetComponent<Item>();

        //    if (itemScript.triggered)
        //        continue;

        //    bool playerWithinDistance = (Vector3.Distance(playerEye.transform.position, item.transform.position) <= itemScript.triggerDistance);
        //    if (!playerWithinDistance)
        //        continue;

        //    Vector3 playerToItem = (item.transform.position - playerEye.position);
        //    Vector3 playerForward = playerEye.transform.forward;
        //    float dot = Vector3.Dot(Vector3.Normalize(playerToItem), Vector3.Normalize(playerForward));

        //    bool playerLookingAtItem = (dot >= Math.COS_30);
        //    if (!playerLookingAtItem)
        //        continue;

        //    dot = Mathf.Acos(dot) * Mathf.Rad2Deg;

        //    Handles.Label(item.transform.position, dot.ToString() + "Degrees");
        //}
    }

    void OnGUIToolbar()
    {
        float centerScreenX = (Screen.width * 0.5f);

        float slotsStartX = (centerScreenX - leftOfCenterTotalIconWidth - leftOfCenterTotalBorderWidth);
        float slotsStartY = (float)(Screen.height - slotScreenEdgeBuffer - totalBorderThickness - slotBorderRectangleWidth);

        for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
        {
            float borderXStart = (slotsStartX + (slotIndex * slotBorderThickness * 2.0f) + (slotIndex * slotIconWidth));
            DrawRectangleBorder(borderXStart, slotsStartY, slotBorderRectangleWidth, slotBorderRectangleWidth, slotBorderThickness, slotBorderTexture);

            float iconXStart = (borderXStart + slotBorderThickness);
            float iconYStart = (slotsStartY + slotBorderThickness);
            GUI.DrawTexture(new Rect(iconXStart, iconYStart, slotIconWidth, slotIconWidth), slotBackgroundTexture);

            if (!ToolbarSlotEmpty(slotIndex))
            {
                GUI.DrawTexture(new Rect(iconXStart, iconYStart, slotIconWidth, slotIconWidth), toolbarItemSlots[slotIndex].inventoryItem.iconTexture);

                GUI.Label(new Rect(iconXStart + 3, iconYStart, slotIconWidth, slotIconWidth), toolbarItemSlots[slotIndex].amount.ToString());
            }
        }

        // Selected Slot Border
        float selectedBorderXStart = (slotsStartX + (selectedSlotIndex * slotBorderThickness * 2.0f) + (selectedSlotIndex * slotIconWidth));
        DrawRectangleBorder(selectedBorderXStart, slotsStartY, slotBorderRectangleWidth, slotBorderRectangleWidth, slotBorderThickness, slotSelectedBorderTexture);

        // Total Inventory Border
        float totalBorderWidth = (slotCount * (slotBorderThickness * 2 + slotIconWidth)) + (totalBorderThickness * 2);
        float totalBorderHeight = (slotBorderThickness * 2 + slotIconWidth) + (totalBorderThickness * 2);
        DrawRectangleBorder(slotsStartX - totalBorderThickness, slotsStartY - totalBorderThickness, totalBorderWidth, totalBorderHeight, totalBorderThickness, totalBorderTexture);
    }

    void DrawRectangleBorder(float x, float y, float width, float height, float thickness, Texture2D texture)
    {
        // Top Border
        GUI.DrawTexture(new Rect(x, y, width, thickness), texture);

        // Bottom Border
        GUI.DrawTexture(new Rect(x, y + height - thickness, width, thickness), texture);

        // Left Border
        GUI.DrawTexture(new Rect(x, y + thickness, thickness, height - (thickness * 2)), texture);

        // Right Border
        GUI.DrawTexture(new Rect(x + width - thickness, y + thickness, thickness, height - (thickness * 2)), texture);
    }

    void UpdatePlayerTriggerItem()
    {
        if (!playerInput.UsePressed())
            return;

        GameObject[] allItems = Utility.GetAllItems();

        List<GameObject> viableItems = new List<GameObject>();

        foreach (GameObject item in allItems)
        {
            Item itemScript = item.GetComponent<Item>();

            if (itemScript.triggered)
                continue;

            bool playerWithinDistance = (Vector3.Distance(playerEye.transform.position, item.transform.position) <= itemScript.triggerDistance);
            if (!playerWithinDistance)
                continue;

            viableItems.Add(item);
        }

        float highestDot = float.MinValue;
        GameObject closestItem = null;
        foreach (GameObject item in viableItems)
        {
            Vector3 playerToItem = Vector3.Normalize((item.transform.position - playerEye.position));
            Vector3 playerForward = playerEye.transform.forward;
            float dot = Vector3.Dot(playerToItem, Vector3.Normalize(playerForward));

            bool playerLookingAtItem = (dot >= Math.COS_30);
            if (!playerLookingAtItem)
                continue;

            if (dot > highestDot)
            {
                closestItem = item;
                highestDot = dot;
            }
        }

        if (closestItem != null)
        {
            Item itemScript = closestItem.GetComponent<Item>();
            itemScript.triggered = true;
        }
    }

    public void GivePlayerInventoryItem(InventoryItem inventoryItem)
    {
        int inventorySlotIndex;
        if (PlayerHasInventoryItem(inventoryItem))
        {
            inventorySlotIndex = PlayerGetInventoryItemSlotIndex(inventoryItem);
        }
        else if (PlayerCurrentlySelectingAnItem())
        {
            inventorySlotIndex = GetPlayerFirstEmptyToolbarSlotIndex();
        }
        else
        {
            inventorySlotIndex = selectedSlotIndex;
        }

        toolbarItemSlots[inventorySlotIndex].inventoryItem = inventoryItem;
        toolbarItemSlots[inventorySlotIndex].amount++;
    }

    public void TakePlayerInventoryItem(InventoryItem inventoryItem)
    {
        for (int slotIndex = 0; slotIndex < toolbarItemSlots.Length; slotIndex++)
        {
            if (!toolbarItemSlots[slotIndex].inventoryItem.Equals(inventoryItem))
                continue;

            toolbarItemSlots[slotIndex].amount--;

            if (toolbarItemSlots[slotIndex].amount <= 0)
            {
                toolbarItemSlots[slotIndex] = new ToolbarItemSlot();
            }

            break;
        }
    }

    public bool PlayerHasInventoryItem(InventoryItem inventoryItem)
    {
        return (PlayerGetInventoryItemSlotIndex(inventoryItem) >= 0);
    }

    public int PlayerGetInventoryItemSlotIndex(InventoryItem inventoryItem)
    {
        for (int slotIndex = 0; slotIndex < toolbarItemSlots.Length; slotIndex++)
        {
            if (toolbarItemSlots[slotIndex].inventoryItem.Equals(inventoryItem))
                return slotIndex;
        }

        return -1;
    }

    public int GetPlayerFirstEmptyToolbarSlotIndex()
    {
        for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
        {
            if (ToolbarSlotEmpty(slotIndex))
                return slotIndex;
        }

        return -1;
    }

    public bool ToolbarSlotEmpty(int slotIndex)
    {
        return toolbarItemSlots[slotIndex].Equals(default(ToolbarItemSlot));
    }

    public bool PlayerCurrentlySelectingAnItem()
    {
        return !ToolbarSlotEmpty(selectedSlotIndex);
    }

    public InventoryItem BuildInventoryItemFromName(string itemName)
    {
        InventoryItem inventoryItem;
        inventoryItemTypes.TryGetValue(itemName, out inventoryItem);

        return inventoryItem;
    }
}

public struct ToolbarItemSlot
{
    public InventoryItem inventoryItem;
    public int amount;

    public ToolbarItemSlot(InventoryItem _inventoryItem, int _amount)
    {
        inventoryItem = _inventoryItem;
        amount = _amount;
    }
}

public struct InventoryItem
{
    public string itemName;
    public Texture2D iconTexture;
    public Viewmodel viewmodel;

    public InventoryItem(string _itemName, Texture2D _iconTexture, Viewmodel _viewmodel)
    {
        itemName = _itemName;
        iconTexture = _iconTexture;
        viewmodel = _viewmodel;
    }
}
