using System;
using UnityEngine;

public class PlayerHUDMenu : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public RectTransform rectTransformLife;
    public RectTransform rectTransformWeapon;
    public RectTransform rectTransformAbilities;
    public RectTransform rectTransformMinimap;
    public RectTransform rectTransformNotification;

    private bool ShowHUD = true;

    private Vector3 scaleLifeBase;
    private Vector3 scaleWeaponBase;
    private Vector3 scaleAbilitiesBase;
    private Vector3 scaleMinimapBase;
    private Vector3 scaleNotificationBase;

    private void Awake()
    {
        scaleLifeBase = rectTransformLife.localScale;
        scaleWeaponBase = rectTransformWeapon.localScale;
        scaleAbilitiesBase = rectTransformAbilities.localScale;
        scaleMinimapBase = rectTransformMinimap.localScale;
        scaleNotificationBase = rectTransformNotification.localScale;
    }

    public void Init()
    {
        SetHUDVisibility(InterfaceManager.Instance.showHUD);
        SetHUDSize(InterfaceManager.Instance.sizeHUD);
        SetHUDOpacity(InterfaceManager.Instance.opacityHUD);
    }

    private void OnEnable()
    {
        InterfaceManager.OnManagerInstance += Init;
        if (InterfaceManager.Instance == null) return;
        
        Init();
    }

    private void OnDisable()
    {
        InterfaceManager.OnManagerInstance -= Init;
    }

    private void SetHUDVisibility(bool showHUD)
    {
        ShowHUD = showHUD;
        canvasGroup.alpha = showHUD ? 1 : 0;
    }

    private void SetHUDSize(float size)
    {
        if (!ShowHUD) return;
        
        rectTransformLife.localScale = scaleLifeBase * Mathf.Clamp(size, 0.5f, 1f);
        rectTransformWeapon.localScale = scaleWeaponBase * Mathf.Clamp(size, 0.5f, 1f);
        rectTransformAbilities.localScale = scaleAbilitiesBase * Mathf.Clamp(size, 0.5f, 1f);
        rectTransformMinimap.localScale = scaleMinimapBase * Mathf.Clamp(size, 0.5f, 1f);
        rectTransformNotification.localScale = scaleNotificationBase * Mathf.Clamp(size, 0.5f, 1f);
    }

    private void SetHUDOpacity(float opacity)
    {
        if (!ShowHUD) return;
        
        canvasGroup.alpha =  Mathf.Clamp(opacity, 0.1f, 1f);
    }
}
