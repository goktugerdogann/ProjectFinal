using UnityEngine;

public class PickupItem : MonoBehaviour
{
    [Header("Item Ayarlarý")]
    public ItemData itemData;
    public int amount = 1;

    [Header("Davranýþ")]
    public bool goesToInventory = true; // true = envantere, false = world placement sistemi

    [Header("Save")]
    public bool saveToWorld = true;     // sadece gerçek dünya objelerinde true olacak
}
    