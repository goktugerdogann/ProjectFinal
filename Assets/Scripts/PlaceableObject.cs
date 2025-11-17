using UnityEngine;

[DisallowMultipleComponent]
public class PlaceableObject : MonoBehaviour
{
    public string itemId;

    private void Reset()
    {
        var pickup = GetComponent<PickupItem>();
        if (pickup != null && pickup.itemData != null)
        {
            itemId = pickup.itemData.id;
        }
    }
}
