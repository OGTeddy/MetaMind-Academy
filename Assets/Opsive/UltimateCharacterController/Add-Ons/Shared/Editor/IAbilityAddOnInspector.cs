/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.AddOns.Shared.Editor
{
    using UnityEditor.Animations;
    using UnityEngine;

    /// <summary>
    /// Common interface for any inspectors that are ability add-on packs.
    /// </summary>
    public interface IAbilityAddOnInspector
    {
        GameObject Character { get; set; }
        bool AddAbilities { get; set; }
        bool AddAnimations { get; set; }
        AnimatorController AnimatorController { get; set; }
        AnimatorController FirstPersonAnimatorController { get; set; }
        string AddOnName { get; }
        string AbilityNamespace { get; }
        bool ShowFirstPersonAnimatorController { get; }
    }
}