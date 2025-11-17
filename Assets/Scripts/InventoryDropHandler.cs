using UnityEngine;

public class InventoryDropHandler : MonoBehaviour
{
    public Camera playerCamera;      // player cam
    public float dropForward = 1.2f; // item’i öne spawn mesafesi
    public float dropUp = 0.2f;      // yere gömülmesin diye
    public LayerMask groundMask;     // yere hizalama istersen

    public void DropItem(int slotIndex)
    {
        InventorySlot slot = Inventory.Instance.GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty || slot.item.worldPrefab == null)
            return;

        // 1) Oyuncunun biraz önünden aþaðýya doðru ray at
        Vector3 origin = playerCamera.transform.position +
                         playerCamera.transform.forward * dropForward +
                         Vector3.up * 1.0f; // biraz yukarýdan

        RaycastHit hit;
        Vector3 spawnPos;

        if (Physics.Raycast(origin, Vector3.down, out hit, 3f, groundMask))
        {
            // zemini buldu: tam zeminin üstüne koy
            spawnPos = hit.point;
        }
        else
        {
            // garanti olsun diye, ray bulamazsa eski sisteme düþ
            spawnPos = origin + Vector3.down * 1.0f;
        }

        GameObject obj = Instantiate(slot.item.worldPrefab, spawnPos, Quaternion.identity);

        // Hafif yukarý al, gömülmesin
        obj.transform.position += Vector3.up * 0.05f;

        // fizik
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // envanterden eksilt
        Inventory.Instance.RemoveItem(slotIndex, 1);
        FindObjectOfType<InventoryUI>()?.UpdateUI();
    }

}
