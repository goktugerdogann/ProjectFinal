using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractionUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject crosshair;
    public Image interactionDot;
    public TextMeshProUGUI interactText; // Ýstersen null býrak, zorunlu deðil

    private void Start()
    {
        // Baþlangýç durumu: silah yok varsayalým
        ShowInteractionDot();
        HideCrosshair();
        HideInteractText();
    }

    // Silah varken
    public void ShowCrosshair()
    {
        if (crosshair != null) crosshair.SetActive(true);
    }

    public void HideCrosshair()
    {
        if (crosshair != null) crosshair.SetActive(false);
    }

    // Silah yokken
    public void ShowInteractionDot()
    {
        if (interactionDot != null) interactionDot.enabled = true;
    }

    public void HideInteractionDot()
    {
        if (interactionDot != null) interactionDot.enabled = false;
    }

    // Objeye bakýnca E yazýsý gösterme
    public void ShowInteractText(string text)
    {
        if (interactText == null) return;

        interactText.text = text;
        interactText.gameObject.SetActive(true);
    }

    public void HideInteractText()
    {
        if (interactText == null) return;

        interactText.gameObject.SetActive(false);
    }

    // Dýþarýdan tek fonksiyonla kontrol etmek için:
    public void SetWeaponEquipped(bool hasWeapon)
    {
        if (hasWeapon)
        {
            ShowCrosshair();
            HideInteractionDot();
        }
        else
        {
            HideCrosshair();
            ShowInteractionDot();
        }
    }
}
