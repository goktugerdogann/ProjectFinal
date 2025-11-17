using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InventorySlot
{
    public ItemData item;
    public int amount;

    public bool IsEmpty => item == null || amount <= 0;
}

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    public int slotCount = 16;
    public List<InventorySlot> slots = new List<InventorySlot>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // slotlarý burada oluþtur
            slots.Clear();
            for (int i = 0; i < slotCount; i++)
                slots.Add(new InventorySlot());

            // istersen:
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject); // ikinci kopyayý yok et
        }
    }

    public bool AddItem(ItemData data, int amount = 1)
    {
        if (data == null || amount <= 0)
            return false;

        // 1) Stacklenebilir ise önce var olan stack'lere ekle
        if (data.isStackable)
        {
            for (int i = 0; i < slots.Count && amount > 0; i++)
            {
                var slot = slots[i];
                if (slot.item == data && slot.amount < data.maxStack)
                {
                    int canAdd = data.maxStack - slot.amount;
                    int add = Mathf.Min(canAdd, amount);
                    slot.amount += add;
                    amount -= add;
                }
            }
        }

        // 2) Kalan miktarý boþ slotlara daðýt
        for (int i = 0; i < slots.Count && amount > 0; i++)
        {
            var slot = slots[i];
            if (slot.IsEmpty)
            {
                int add = data.isStackable
                    ? Mathf.Min(data.maxStack, amount)
                    : 1;

                slot.item = data;
                slot.amount = add;
                amount -= add;
            }
        }

        // Eðer hala amount > 0 ise envanter dolu demektir
      
        bool success = amount <= 0;
        if (success && SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();

        return success;
    }
    public void ClearAllSlots()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].item = null;
            slots[i].amount = 0;
        }
    }

    public void RemoveItem(int slotIndex, int amount = 1)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count) return;

        var slot = slots[slotIndex];
        if (slot.IsEmpty) return;

        slot.amount -= amount;
        if (slot.amount <= 0)
        {
            slot.item = null;
            slot.amount = 0;
        }

        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();
    }


    public InventorySlot GetSlot(int index)
    {
        if (index < 0 || index >= slots.Count) return null;
        return slots[index];
    }

}
