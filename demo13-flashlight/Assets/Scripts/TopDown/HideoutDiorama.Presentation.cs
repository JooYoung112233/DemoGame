using UnityEngine;

public partial class HideoutDiorama
{
    [Header("Hideout presentation")]
    [SerializeField, Range(.5f, 1f)] float characterDisplayScale = .84f;
    Vector3 _originalDisplayScale;
    bool _displayScaleApplied;
    ChibiPlayerVisual _characterVisual;
    WornLamp _wornLamp;
    MeleeWeaponVisual _fallbackWeapon;
    Vector3 _fallbackScale;
    Renderer _fallbackBlade;
    MaterialPropertyBlock _originalBladeProperties;

    void ApplyPresentation()
    {
        if (_view == null || _displayScaleApplied) return;
        _originalDisplayScale = _view.localScale;
        _view.localScale = _originalDisplayScale * characterDisplayScale;
        _displayScaleApplied = true;
        _characterVisual = _player.GetComponent<ChibiPlayerVisual>();
    }

    void LateUpdate()
    {
        if (_wornLamp == null)
        {
            _wornLamp = FindFirstObjectByType<WornLamp>();
            if (_wornLamp != null) _wornLamp.SetIndoorPresentation(true);
        }
        if (_fallbackWeapon == null && _player != null)
        {
            _fallbackWeapon = _player.GetComponentInChildren<MeleeWeaponVisual>(true);
            if (_fallbackWeapon != null && !_fallbackWeapon.transform.IsChildOf(_view))
            {
                _fallbackScale = _fallbackWeapon.transform.localScale;
                _fallbackWeapon.transform.localScale = _fallbackScale * characterDisplayScale;
                var blade = _fallbackWeapon.transform.Find("Pivot/Blade");
                _fallbackBlade = blade != null ? blade.GetComponent<Renderer>() : null;
                if (_fallbackBlade != null && _fallbackBlade.sharedMaterial.HasProperty("_BaseColor"))
                {
                    _originalBladeProperties = new MaterialPropertyBlock();
                    _fallbackBlade.GetPropertyBlock(_originalBladeProperties);
                    var properties = new MaterialPropertyBlock();
                    _fallbackBlade.GetPropertyBlock(properties);
                    var color = _fallbackBlade.sharedMaterial.GetColor("_BaseColor");
                    properties.SetColor("_BaseColor", new Color(color.r * .65f, color.g * .65f, color.b * .65f, color.a));
                    _fallbackBlade.SetPropertyBlock(properties);
                }
            }
        }
        if (_fallbackWeapon != null && _view != null)
            _fallbackWeapon.SetFacing(Plan3D.ToPlan(_view.forward));
        // Player movement is disabled in the diorama, but its equipped idle pose still needs updating.
        // Keep equipment and sprint-only stowing rules intact; do not hide or unequip weapons.
        if (_characterVisual != null)
            _characterVisual.UpdateMotion(Vector2.zero, 0f, false, false, 1f, Time.unscaledDeltaTime);
    }

    void RestorePresentation()
    {
        if (_displayScaleApplied && _view != null) _view.localScale = _originalDisplayScale;
        if (_wornLamp != null) _wornLamp.SetIndoorPresentation(false);
        if (_fallbackWeapon != null && _fallbackScale != Vector3.zero) _fallbackWeapon.transform.localScale = _fallbackScale;
        if (_fallbackBlade != null && _originalBladeProperties != null) _fallbackBlade.SetPropertyBlock(_originalBladeProperties);
        _displayScaleApplied = false;
    }
}
