using UnityEngine;
using UnityEngine.UI;

public class InteractionRaycaster : MonoBehaviour
{
    [Header("General")]
    public Camera playerCamera;
    public float interactDistance = 3f;
    public LayerMask interactionLayer;
    public InteractionUIManager uiManager;

    [Header("Place Settings")]
    public float placeDistance = 4f;
    public LayerMask groundLayer;

    [Header("Snap Settings")]
    public bool useGridSnap = true;
    public float gridSize = 0.5f;
    public bool freezeOnPlace = true;

    [Header("Preview Settings")]
    public Material previewMaterial;
    public float placeForwardOffset = 0f;
    public float rotationSpeed = 200f;

    [Header("Preview Colors")]
    public Color validColor = new Color(0.2f, 1f, 0.2f, 0.4f);
    public Color invalidColor = new Color(1f, 0.2f, 0.2f, 0.4f);

    [Header("Overlap Check")]
    public LayerMask overlapCheckMask;

    [Header("Pickup Settings")]
    public KeyCode pickupKey = KeyCode.Q;
    public float pickupHoldTime = 1f;

    [Header("Pickup Progress UI")]
    public Image pickupProgressImage;
    public bool usePickupProgress = true;

    [Header("Crosshair")]
    public GameObject crosshairObject;
    public bool hideCrosshairWhileCharging = true;

    // internal state
    float pickupTimer = 0f;
    bool isHoldingPickup = false;

    GameObject heldObject;
    Rigidbody heldRigidbody;

    float heldHeightOffset;
    Quaternion baseRotation;
    Quaternion currentRotation;
    float rotationAngleY = 0f;

    GameObject previewObject;
    Renderer[] previewRenderers;
    Collider[] previewColliders;

    // placement state
    bool isPlacementMode = false;
    bool isPlacingFromInventory = false;
    ItemData placingFromInventoryItem = null;

    void Start()
    {
        Debug.Log(Application.persistentDataPath);

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (uiManager == null)
            uiManager = FindObjectOfType<InteractionUIManager>();

        if (pickupProgressImage != null)
        {
            pickupProgressImage.fillAmount = 0f;
            pickupProgressImage.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // if inventory is open, do not interact or place
        if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen)
        {
            if (isHoldingPickup)
            {
                isHoldingPickup = false;
                pickupTimer = 0f;
                UpdatePickupProgressUI(0f, true);
            }

            if (uiManager != null)
                uiManager.HideInteractText();

            return;
        }

        if (heldObject == null)
        {
            HandlePickup();
        }
        else
        {
            HandlePlacementMode();
        }
    }

    // ============= PICKUP (HOLD Q) =============
    void HandlePickup()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance, interactionLayer))
        {
            GameObject target = hit.collider.gameObject;
            PickupItem pickup = target.GetComponent<PickupItem>();

            if (Input.GetKey(pickupKey))
            {
                if (!isHoldingPickup)
                {
                    isHoldingPickup = true;
                    pickupTimer = 0f;
                }

                pickupTimer += Time.deltaTime;
                if (uiManager != null)
                    uiManager.ShowInteractText("Hold Q to pick up");

                UpdatePickupProgressUI(pickupTimer / pickupHoldTime);

                if (pickupTimer >= pickupHoldTime)
                {
                    // time finished, decide inventory or world placement
                    if (pickup != null && pickup.goesToInventory && pickup.itemData != null)
                    {
                        bool added = (Inventory.Instance != null) &&
                                     Inventory.Instance.AddItem(pickup.itemData, pickup.amount);

                        if (added)
                        {
                            // önce sahneden gizle
                            target.SetActive(false);

                            // sonra save al
                            if (SaveManager.Instance != null)
                                SaveManager.Instance.SaveGame();

                            // sonra yok et (artýk save'de görünmeyecek)
                            Destroy(target);
                            

                            if (uiManager != null)
                                uiManager.ShowInteractText(pickup.itemData.displayName + " added to inventory");
                        }

                        else
                        {
                            if (uiManager != null)
                                uiManager.ShowInteractText("Inventory is full");
                        }
                    }
                    else
                    {
                        // normal world object, go into placement mode
                        isPlacingFromInventory = false;
                        placingFromInventoryItem = null;
                        PickUpObject(target);
                    }

                    isHoldingPickup = false;
                    pickupTimer = 0f;
                    UpdatePickupProgressUI(0f, true);
                }
            }
            else
            {
                if (isHoldingPickup)
                {
                    isHoldingPickup = false;
                    pickupTimer = 0f;
                    UpdatePickupProgressUI(0f, true);
                }

                if (uiManager != null)
                    uiManager.ShowInteractText("Press Q to pick up");
            }
        }
        else
        {
            if (isHoldingPickup)
            {
                isHoldingPickup = false;
                pickupTimer = 0f;
            }

            UpdatePickupProgressUI(0f, true);
            if (uiManager != null)
                uiManager.HideInteractText();
        }
    }

    // called from EquipManager when a placeable item is selected from inventory
    public void BeginPlaceFromInventory(ItemData item)
    {
        if (item == null || item.worldPrefab == null)
        {
            Debug.LogWarning("BeginPlaceFromInventory: item or prefab missing");
            return;
        }

        // mark that this placement comes from inventory
        isPlacingFromInventory = true;
        placingFromInventoryItem = item;

        // create a hidden world object just like a normal pickup
        GameObject obj = Instantiate(item.worldPrefab);
        obj.name = "PLACE_FROM_INV_" + item.displayName;

        PickUpObject(obj);
    }

    void PickUpObject(GameObject obj)
    {
        heldObject = obj;
        heldRigidbody = heldObject.GetComponent<Rigidbody>();

        baseRotation = heldObject.transform.rotation;
        rotationAngleY = 0f;
        currentRotation = baseRotation;

        Collider col = heldObject.GetComponent<Collider>();
        if (col != null)
        {
            float bottomY = col.bounds.center.y - col.bounds.extents.y;
            heldHeightOffset = heldObject.transform.position.y - bottomY;
        }
        else
        {
            heldHeightOffset = 0.5f;
        }

        if (heldRigidbody != null)
        {
            heldRigidbody.velocity = Vector3.zero;
            heldRigidbody.angularVelocity = Vector3.zero;
        }

        // destroy previous preview if any
        if (previewObject != null)
            Destroy(previewObject);

        previewObject = Instantiate(heldObject);
        previewObject.name = heldObject.name + "_Preview";

        // GHOST'tan PickupItem'ý SÝL
        PickupItem previewPickup = previewObject.GetComponent<PickupItem>();
        if (previewPickup != null)
        {
            Destroy(previewPickup);
        }


        previewColliders = previewObject.GetComponentsInChildren<Collider>();
        foreach (Collider c in previewColliders)
            c.isTrigger = true;

        Rigidbody prb = previewObject.GetComponent<Rigidbody>();
        if (prb != null)
            Destroy(prb);

        previewRenderers = previewObject.GetComponentsInChildren<Renderer>();

        if (previewMaterial != null)
        {
            foreach (Renderer r in previewRenderers)
                r.material = previewMaterial;
        }

        previewObject.SetActive(true);
        SetPreviewColor(false);

        heldObject.SetActive(false);
        isPlacementMode = true;

        UpdatePickupProgressUI(0f, true);
        if (uiManager != null)
            uiManager.HideInteractText();
    }

    // ============= PLACEMENT MODE =============
    void HandlePlacementMode()
    {
        if (!isPlacementMode)
            isPlacementMode = true;

        // rotation with mouse wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            rotationAngleY += scroll * rotationSpeed;
            currentRotation = Quaternion.Euler(0f, rotationAngleY, 0f) * baseRotation;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        bool hasGround = Physics.Raycast(ray, out hit, placeDistance, groundLayer);

        Vector3 previewPos;
        if (hasGround)
        {
            previewPos = CalculatePlacementPosition(hit);
        }
        else
        {
            previewPos = playerCamera.transform.position +
                         playerCamera.transform.forward * (placeDistance * 0.5f);
        }

        if (previewObject != null)
        {
            previewObject.SetActive(true);
            previewObject.transform.position = previewPos;
            previewObject.transform.rotation = currentRotation;
        }

        bool hasOverlap = hasGround && CheckOverlapAtPreview();
        bool isValidSpot = hasGround && !hasOverlap;

        if (uiManager != null)
        {
            if (!hasGround)
                uiManager.ShowInteractText("Look at the ground to place");
            else if (hasOverlap)
                uiManager.ShowInteractText("Cannot place here");
            else
                uiManager.ShowInteractText("Left click to place");
        }

        SetPreviewColor(isValidSpot);

        // place
        if (Input.GetMouseButtonDown(0) && isValidSpot)
        {
            PlaceObject(previewPos);
        }

        // optional: right click cancels placement and returns item if it came from inventory
        if (Input.GetMouseButtonDown(1))
        {
            CancelPlacementPreview(true);
        }
    }

    Vector3 CalculatePlacementPosition(RaycastHit hit)
    {
        Vector3 placePos = hit.point;

        if (placeForwardOffset != 0f)
        {
            Vector3 flatForward = playerCamera.transform.forward;
            flatForward.y = 0f;
            flatForward.Normalize();
            placePos += flatForward * placeForwardOffset;
        }

        if (useGridSnap && gridSize > 0.0001f)
        {
            placePos.x = Mathf.Round(placePos.x / gridSize) * gridSize;
            placePos.z = Mathf.Round(placePos.z / gridSize) * gridSize;
        }

        placePos.y += heldHeightOffset;
        return placePos;
    }

    void PlaceObject(Vector3 placePos)
    {
        if (heldObject == null)
            return;

        heldObject.transform.position = placePos;
        heldObject.transform.rotation = currentRotation;
        heldObject.SetActive(true);

        if (heldRigidbody != null)
        {
            heldRigidbody.isKinematic = false;
            heldRigidbody.useGravity = true;
            if (freezeOnPlace)
                heldRigidbody.constraints = RigidbodyConstraints.FreezeAll;
        }

        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
            previewRenderers = null;
            previewColliders = null;
        }

        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();

        heldObject = null;
        heldRigidbody = null;
        isPlacementMode = false;
        isPlacingFromInventory = false;
        placingFromInventoryItem = null;

        if (uiManager != null)
            uiManager.HideInteractText();
    }

    // ============= CANCEL PLACEMENT (used from EquipManager) =============
    public void CancelPlacementPreview(bool returnToInventory)
    {
        // no placement active
        if (heldObject == null && previewObject == null)
            return;

        // destroy preview ghost
        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
            previewRenderers = null;
            previewColliders = null;
        }

        // handle the real object
        if (heldObject != null)
        {
            // came from inventory and caller wants to return it
            if (isPlacingFromInventory &&
                returnToInventory &&
                placingFromInventoryItem != null &&
                Inventory.Instance != null)
            {
                Destroy(heldObject);
                Inventory.Instance.AddItem(placingFromInventoryItem, 1);
                InventoryUI.Instance?.UpdateUI();
            }
            else
            {
                // normal world object -> just reactivate at original spot
                heldObject.SetActive(true);
            }
        }

        heldObject = null;
        heldRigidbody = null;
        isPlacementMode = false;
        isPlacingFromInventory = false;
        placingFromInventoryItem = null;

        if (uiManager != null)
            uiManager.HideInteractText();
    }

    // ============= HELPERS =============
    void SetPreviewColor(bool isValid)
    {
        if (previewRenderers == null) return;

        Color targetColor = isValid ? validColor : invalidColor;

        foreach (Renderer r in previewRenderers)
        {
            if (r.material.HasProperty("_Color"))
                r.material.color = targetColor;
        }
    }

    bool CheckOverlapAtPreview()
    {
        if (previewColliders == null || previewColliders.Length == 0)
            return false;

        foreach (Collider c in previewColliders)
        {
            if (!c.enabled) continue;

            Vector3 center = c.bounds.center;
            Vector3 halfExtents = c.bounds.extents;

            Collider[] hits = Physics.OverlapBox(
                center,
                halfExtents,
                c.transform.rotation,
                overlapCheckMask,
                QueryTriggerInteraction.Ignore
            );

            foreach (Collider hit in hits)
            {
                if (hit == null) continue;
                if (hit.transform.root == previewObject.transform.root)
                    continue;

                return true;
            }
        }

        return false;
    }

    void UpdatePickupProgressUI(float normalized, bool hide = false)
    {
        if (!usePickupProgress || pickupProgressImage == null)
            return;

        if (hide)
        {
            pickupProgressImage.fillAmount = 0f;
            pickupProgressImage.gameObject.SetActive(false);

            if (crosshairObject != null && hideCrosshairWhileCharging)
                crosshairObject.SetActive(true);
        }
        else
        {
            pickupProgressImage.gameObject.SetActive(true);
            pickupProgressImage.fillAmount = Mathf.Clamp01(normalized);

            if (crosshairObject != null && hideCrosshairWhileCharging)
                crosshairObject.SetActive(false);
        }
    }
}
