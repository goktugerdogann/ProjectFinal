using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance;

    [Header("References")]
    public Inventory inventory;
    public GameObject panel;
    public TextMeshProUGUI[] slotTexts;
    public Button[] slotButtons;
    public EquipManager equipManager;

    [Header("Control")]
    public KeyCode toggleKey = KeyCode.Tab;

    bool isOpen = false;
    public bool IsOpen => isOpen;   // EquipManager uses this

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (inventory == null)
            inventory = Inventory.Instance;

        if (panel != null)
            panel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (isOpen)
                CloseInventory();
            else
                OpenInventory();
        }
    }

    public void OpenInventory()
    {
        isOpen = true;

        if (panel != null)
            panel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // YENÝ: Herhangi bir yerleþtirme preview’i varsa iptal et
        var ray = FindObjectOfType<InteractionRaycaster>();
        if (ray != null)
        {
            // true = eðer item envanterden geldiyse geri envantere koy
            ray.CancelPlacementPreview(true);
        }

        UpdateUI();
    }


    public void CloseInventory()
    {
        isOpen = false;

        if (panel != null)
            panel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void UpdateUI()
    {
        if (inventory == null || slotTexts == null) return;

        for (int i = 0; i < slotTexts.Length; i++)
        {
            if (i >= inventory.slots.Count) break;

            var slot = inventory.slots[i];

            if (slot.IsEmpty)
                slotTexts[i].text = "";
            else
                slotTexts[i].text = $"{slot.item.displayName} x{slot.amount}";
        }
    }

    // Only needed if you still use InventoryUI.OnSlotClicked from Button OnClick.
    // If you switched to InventorySlotButton.OnClick, you can delete this.
    public void OnSlotClicked(int slotIndex)
    {
        Debug.Log("OnSlotClicked: " + slotIndex);

        if (equipManager != null)
        {
            equipManager.EquipFromSlot(slotIndex);
        }
        else
        {
            Debug.LogWarning("EquipManager reference is missing!");
        }
    }
}
