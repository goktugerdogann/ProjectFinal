using UnityEngine;

public class InventorySlotButton : MonoBehaviour
{
    public int slotIndex;   // 0,1,2,... 15 gibi

    public void OnClick()
    {
        Debug.Log("InventorySlotButton.OnClick -> slot: " + slotIndex);

        // En basit yol: sahnedeki EquipManager'ý bul
        EquipManager equip = FindObjectOfType<EquipManager>();

        if (equip != null)
        {
            equip.EquipFromSlot(slotIndex);
        }
        else
        {
            Debug.LogWarning("EquipManager bulunamadý!");
        }
    }
}
