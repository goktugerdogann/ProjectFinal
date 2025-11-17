using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    [Header("Refs")]
    public ItemDatabase itemDatabase;

    [System.Serializable]
    public class InventorySlotData
    {
        public string itemId;
        public int amount;
    }

    [System.Serializable]
    public class WorldItemData
    {
        public string itemId;
        public Vector3 position;
        public Quaternion rotation;
    }

    [System.Serializable]
    public class SaveData
    {
        public List<InventorySlotData> inventory = new List<InventorySlotData>();
        public List<WorldItemData> worldItems = new List<WorldItemData>();
    }

    string SavePath => Path.Combine(Application.persistentDataPath, "placements.json");

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        LoadGame();
    }

    // ------------- SAVE -------------
    public void SaveGame()
    {
        SaveData data = new SaveData();

        // 1) ENVANTER
        if (Inventory.Instance != null)
        {
            var inv = Inventory.Instance;

            for (int i = 0; i < inv.slots.Count; i++)
            {
                var slot = inv.slots[i];
                InventorySlotData s = new InventorySlotData();

                if (slot != null && slot.item != null && slot.amount > 0)
                {
                    s.itemId = slot.item.id;
                    s.amount = slot.amount;
                }
                else
                {
                    s.itemId = "";
                    s.amount = 0;
                }

                data.inventory.Add(s);
            }
        }

        // 2) DÜNYADAKÝ TÜM PICKUPLAR
        PickupItem[] worldItems = FindObjectsOfType<PickupItem>();
        foreach (var p in worldItems)
        {
            if (p.itemData == null) continue;

            // sadece sahnede aktif olanlarý kaydet
            if (!p.gameObject.activeInHierarchy) continue;

            WorldItemData w = new WorldItemData
            {
                itemId = p.itemData.id,
                position = p.transform.position,
                rotation = p.transform.rotation
            };
            data.worldItems.Add(w);
        }






        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);

        Debug.Log($"SaveManager: saved {data.inventory.Count} inventory slots, {data.worldItems.Count} world items. Path: {SavePath}");
    }

    // ------------- LOAD -------------
    public void LoadGame()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("SaveManager: no save file yet, starting fresh.");
            return;
        }

        string json = File.ReadAllText(SavePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        if (data == null)
        {
            Debug.LogWarning("SaveManager: save file invalid.");
            return;
        }

        // 1) ENVANTERÝ GERÝ YÜKLE
        if (Inventory.Instance != null && data.inventory != null && data.inventory.Count > 0)
        {
            var inv = Inventory.Instance;

            while (inv.slots.Count < data.inventory.Count)
                inv.slots.Add(new InventorySlot());

            for (int i = 0; i < data.inventory.Count && i < inv.slots.Count; i++)
            {
                var s = data.inventory[i];
                var slot = inv.slots[i];

                if (string.IsNullOrEmpty(s.itemId) || s.amount <= 0)
                {
                    slot.item = null;
                    slot.amount = 0;
                }
                else
                {
                    ItemData item = itemDatabase != null ? itemDatabase.GetItemById(s.itemId) : null;
                    if (item == null)
                    {
                        slot.item = null;
                        slot.amount = 0;
                        Debug.LogWarning("SaveManager: itemId not found in database: " + s.itemId);
                    }
                    else
                    {
                        slot.item = item;
                        slot.amount = s.amount;
                    }
                }
            }

            InventoryUI.Instance?.UpdateUI();
        }

        // 2) DÜNYADAKÝ TÜM ESKÝ PICKUPLARI SÝL
        var existing = FindObjectsOfType<PickupItem>();
        foreach (var p in existing)
        {
            if (!p.saveToWorld) continue; //  EKLEDÝÐÝMÝZ SATIR
            Destroy(p.gameObject);
        }


        // 3) KAYITTAN TEKRAR SPAWN ET
        if (data.worldItems != null)
        {
            foreach (var w in data.worldItems)
            {
                if (string.IsNullOrEmpty(w.itemId)) continue;

                ItemData item = itemDatabase != null ? itemDatabase.GetItemById(w.itemId) : null;
                if (item == null || item.worldPrefab == null)
                {
                    Debug.LogWarning("SaveManager: cannot spawn world item, missing ItemData or prefab for id: " + w.itemId);
                    continue;
                }

                GameObject obj = Object.Instantiate(item.worldPrefab, w.position, w.rotation);

                PickupItem pi = obj.GetComponent<PickupItem>();
                if (pi == null)
                    pi = obj.AddComponent<PickupItem>();

                pi.itemData = item;
                if (pi.amount <= 0) pi.amount = 1;

                // bu spawn edilen, gerçek dünya objesi  kaydedilsin
                pi.saveToWorld = true;

            }
        }

        Debug.Log($"SaveManager: loaded {data.inventory?.Count ?? 0} inventory slots, {data.worldItems?.Count ?? 0} world items.");
    }
}
