using UnityEngine;

[CreateAssetMenu(menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Kimlik")]
    public string id;              // "key_basement", "gun_pistol" vs.
    public string displayName;     // Oyunda gözükecek isim

    [Header("Görsel")]
    public Sprite icon;            // Envanter ikonu
    public GameObject worldPrefab; // Dünyaya spawn etmek istersen kullanýlýr

    [Header("Stack Ayarlarý")]
    public bool isStackable = false;
    public int maxStack = 1;
    [Header("Kullaným Tipi")]
    public bool canEquip = true;          // eline alýnabilir mi?
    public bool canDrop = true;           // yere býrakýlabilir mi?

    [Header("Equip Prefab")]
    public GameObject equippedPrefab;     // elde görünecek model (boþsa worldPrefab kullanýlacak)

    [Header("Yerleþtirilebilir mi?")]
    public bool isPlaceable = false;

}
