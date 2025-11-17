using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public ItemData[] items;

    public ItemData GetItemById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        foreach (var it in items)
        {
            if (it != null && it.id == id)
                return it;
        }
        return null;
    }
}
