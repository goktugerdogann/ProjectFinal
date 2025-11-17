using UnityEngine;

public class EquipManager : MonoBehaviour
{
    public static EquipManager Instance;

    [Header("Refs")]
    public Inventory inventory;
    public Transform handAnchor;
    public Camera playerCamera;
    public LayerMask groundMask;

    [Header("Drop Settings")]
    public float dropForward = 1.2f;
    public float dropRayHeight = 1.0f;
    public float maxDropDownDistance = 3f;

    [Header("State")]
    public int currentSlotIndex = -1;
    public ItemData currentItem;
    GameObject equippedInstance;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (inventory == null)
            inventory = Inventory.Instance;

        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
            DropEquipped();
    }

    public void EquipFromSlot(int slotIndex)
    {
        // cancel any active placement preview first
        InteractionRaycaster ray = FindObjectOfType<InteractionRaycaster>();
        if (ray != null)
            ray.CancelPlacementPreview(true);   // return inventory item if needed

        if (inventory == null) return;
        if (slotIndex < 0 || slotIndex >= inventory.slots.Count) return;

        var slot = inventory.slots[slotIndex];
        if (slot.IsEmpty) return;

        ItemData data = slot.item;
        if (data == null) return;

        currentItem = data;
        currentSlotIndex = slotIndex;

        // PLACEABLE ITEM -> preview mode
        if (currentItem.isPlaceable)
        {
            ClearEquippedVisual();

            if (ray != null)
                ray.BeginPlaceFromInventory(currentItem);

            inventory.RemoveItem(slotIndex, 1);

            var ui = InventoryUI.Instance != null ? InventoryUI.Instance : FindObjectOfType<InventoryUI>();
            if (ui != null)
            {
                ui.UpdateUI();
                if (ui.IsOpen)
                    ui.CloseInventory();
            }

            return;
        }

        // NORMAL EQUIP (weapon, tool, etc.)
        ClearEquippedVisual();

        GameObject prefabToUse = currentItem.equippedPrefab != null
            ? currentItem.equippedPrefab
            : currentItem.worldPrefab;

        if (prefabToUse == null)
        {
            Debug.LogError("EquipManager: prefabToUse is null for " + currentItem.displayName);
            return;
        }

        equippedInstance = Instantiate(prefabToUse, handAnchor);
        equippedInstance.name = "EQUIPPED_" + prefabToUse.name;
        equippedInstance.transform.localPosition = Vector3.zero;
        equippedInstance.transform.localRotation = Quaternion.identity;
        // eldeki görsel SaveManager tarafýndan world item sanýlmasýn
        PickupItem pickupOnEquipped = equippedInstance.GetComponent<PickupItem>();
        if (pickupOnEquipped != null)
        {
            Destroy(pickupOnEquipped);
        }

        var rb = equippedInstance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        foreach (var col in equippedInstance.GetComponentsInChildren<Collider>())
            col.enabled = false;

        // close inventory when equipped
        if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen)
            InventoryUI.Instance.CloseInventory();
    }

    public void ClearEquippedVisual()
    {
        if (equippedInstance != null)
        {
            Destroy(equippedInstance);
            equippedInstance = null;
        }
    }

    public void DropEquipped()
    {
        if (currentItem == null) return;
        if (!currentItem.canDrop) return;
        if (inventory == null) return;

        if (currentSlotIndex < 0 || currentSlotIndex >= inventory.slots.Count)
            return;

        var slot = inventory.slots[currentSlotIndex];
        if (slot.IsEmpty)
        {
            ClearEquippedVisual();
            currentItem = null;
            currentSlotIndex = -1;
            return;
        }

        Vector3 origin = playerCamera.transform.position;
        Vector3 forward = playerCamera.transform.forward;
        Vector3 dropPos = origin + forward * 1.5f;

        RaycastHit hit;
        if (Physics.Raycast(dropPos + Vector3.up * 2f, Vector3.down, out hit, 5f, groundMask))
            dropPos = hit.point + Vector3.up * 0.05f;

        GameObject worldPrefab = currentItem.worldPrefab != null
            ? currentItem.worldPrefab
            : currentItem.equippedPrefab;

        if (worldPrefab != null)
        {
            GameObject obj = Instantiate(worldPrefab, dropPos, Quaternion.identity);
            obj.name = "DROPPED_" + worldPrefab.name;

            var rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        inventory.RemoveItem(currentSlotIndex, 1);

        var ui = InventoryUI.Instance != null ? InventoryUI.Instance : FindObjectOfType<InventoryUI>();
        if (ui != null)
            ui.UpdateUI();

        slot = inventory.slots[currentSlotIndex];
        if (slot.IsEmpty)
        {
            ClearEquippedVisual();
            currentItem = null;
            currentSlotIndex = -1;
        }
        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();

    }
}
