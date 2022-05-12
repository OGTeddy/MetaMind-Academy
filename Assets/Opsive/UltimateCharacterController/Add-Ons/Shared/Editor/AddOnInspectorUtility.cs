/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.AddOns.Shared.Editor
{
    using Opsive.Shared.Editor.Inspectors.Utility;
    using Opsive.UltimateCharacterController.Editor.Managers;
    using Opsive.UltimateCharacterController.Editor.Inspectors.Character;
    using Opsive.UltimateCharacterController.Utility.Builders;
    using UnityEditor;
    using UnityEditor.Animations;
    using UnityEngine;

    /// <summary>
    /// Shared methods for drawing the ability animation options.
    /// </summary>
    public static class AddOnInspectorUtility
    {
        private static IAbilityAddOnInspector s_AddOnInspector;

        /// <summary>
        /// Draws the add-on inspector.
        /// </summary>
        public static void DrawInspector(IAbilityAddOnInspector addOnInspector)
        {
            s_AddOnInspector = addOnInspector;

            ManagerUtility.DrawControlBox(s_AddOnInspector.AddOnName + " Abilities & Animations", DrawCharacterSelection, "This option will add the " + s_AddOnInspector.AddOnName.ToLower() + " abilities or animations to your character.",
                            CanSetupCharacter(), "Setup Character", SetupCharacter, "The character was successfully setup." + 
                            (s_AddOnInspector.AddAbilities ? " Refer to the documentation for the steps to configure the abilities." : string.Empty));
        }

        /// <summary>
        /// Draws the additional controls for the animator.
        /// </summary>
        private static void DrawCharacterSelection()
        {
            EditorGUI.BeginChangeCheck();
            s_AddOnInspector.Character = EditorGUILayout.ObjectField("Character", s_AddOnInspector.Character, typeof(GameObject), true) as GameObject;
            if (EditorGUI.EndChangeCheck()) {
                s_AddOnInspector.AnimatorController = null;
                if (s_AddOnInspector.Character != null) {

                    var animator = s_AddOnInspector.Character.GetComponent<Animator>();
                    if (animator != null) {
                        s_AddOnInspector.AnimatorController = (AnimatorController)animator.runtimeAnimatorController;
                    }
#if FIRST_PERSON_CONTROLLER
                    var firstPersonBaseObjects = s_AddOnInspector.Character.GetComponentsInChildren<FirstPersonController.Character.Identifiers.FirstPersonBaseObject>(true);
                    if (firstPersonBaseObjects != null && firstPersonBaseObjects.Length > 0) {
                        var firstPersonBaseObject = firstPersonBaseObjects[0];
                        // Choose the base object with the lowest ID.
                        for (int i = 1; i < firstPersonBaseObjects.Length; ++i) {
                            if (firstPersonBaseObjects[i].ID < firstPersonBaseObject.ID) {
                                firstPersonBaseObject = firstPersonBaseObjects[i];
                            }
                        }

                        animator = firstPersonBaseObject.GetComponent<Animator>();
                        if (animator != null) {
                            s_AddOnInspector.FirstPersonAnimatorController = (AnimatorController)animator.runtimeAnimatorController;
                        }
                    }
#endif
                }
            }

            // The character must first be created by the Character Manager.
            if (s_AddOnInspector.Character != null && s_AddOnInspector.Character.GetComponent<Character.UltimateCharacterLocomotion>() == null) {
                EditorGUILayout.HelpBox("The character must first be setup by the Character Manager.", MessageType.Error);
            }

            s_AddOnInspector.AddAbilities = EditorGUILayout.Toggle("Add Abilities", s_AddOnInspector.AddAbilities);
            s_AddOnInspector.AddAnimations = EditorGUILayout.Toggle("Add Animations", s_AddOnInspector.AddAnimations);

            if (s_AddOnInspector.AddAnimations) {
                EditorGUI.indentLevel++;
                s_AddOnInspector.AnimatorController = ClampAnimatorControllerField("Animator Controller", s_AddOnInspector.AnimatorController, 33);
#if FIRST_PERSON_CONTROLLER
                if (s_AddOnInspector.ShowFirstPersonAnimatorController) {
                    s_AddOnInspector.FirstPersonAnimatorController = ClampAnimatorControllerField("First Person Animator", s_AddOnInspector.FirstPersonAnimatorController, 23);
                }
#endif
                EditorGUI.indentLevel--;
            }
            GUILayout.Space(5);
        }

        /// <summary>
        /// Returns true if the character can be setup.
        /// </summary>
        /// <returns>True if the character can be setup.</returns>
        private static bool CanSetupCharacter()
        {
            if (s_AddOnInspector.Character == null || s_AddOnInspector.Character.GetComponent<Character.UltimateCharacterLocomotion>() == null) {
                return false;
            }

            if (s_AddOnInspector.AddAnimations && s_AddOnInspector.AnimatorController == null) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Adds the abilities and animations to the animator controllers.
        /// </summary>
        private static void SetupCharacter()
        {
            var types = InspectorDrawerUtility.GetAllTypesWithinNamespace(s_AddOnInspector.AbilityNamespace);
            if (types == null) {
                return;
            }

            if (s_AddOnInspector.AddAbilities) {
                var characterLocomotion = s_AddOnInspector.Character.GetComponent<Character.UltimateCharacterLocomotion>();
                var abilities = characterLocomotion.GetSerializedAbilities();
                // Call AbilityBuilder on all of the abilities.
                for (int i = 0; i < types.Count; ++i) {
                    if (!typeof(Character.Abilities.Ability).IsAssignableFrom(types[i])) {
                        continue;
                    }
                    var hasAbility = false;
                    // Do not add duplicates.
                    for (int j = 0; j < abilities.Length; ++j) {
                        if (abilities[j] != null && abilities[j].GetType() == types[i]) {
                            hasAbility = true;
                            break;
                        }
                    }
                    if (hasAbility) {
                        continue;
                    }
                    AbilityBuilder.AddAbility(characterLocomotion, types[i], i);
                }
                Opsive.Shared.Editor.Utility.EditorUtility.SetDirty(characterLocomotion);
            }

            if (s_AddOnInspector.AddAnimations) {
                // Call BuildAnimator on all of the inspector drawers for the abilities.
                for (int i = 0; i < types.Count; ++i) {
                    var inspectorDrawer = InspectorDrawerUtility.InspectorDrawerForType(types[i]) as AbilityInspectorDrawer;
                    if (inspectorDrawer == null || !inspectorDrawer.CanBuildAnimator) {
                        continue;
                    }

                    inspectorDrawer.BuildAnimator(s_AddOnInspector.AnimatorController, s_AddOnInspector.FirstPersonAnimatorController);
                }
            }
        }

        /// <summary>
        /// Prevents the label from being too far away from the object field.
        /// </summary>
        /// <param name="label">The animator controller label.</param>
        /// <param name="animatorController">The animator controller value.</param>
        /// <param name="widthAddition">Any additional width to separate the label and the control.</param>
        /// <returns>The new animator controller.</returns>
        private static AnimatorController ClampAnimatorControllerField(string label, AnimatorController animatorController, int widthAddition)
        {
            var textDimensions = GUI.skin.label.CalcSize(new GUIContent(label));
            var prevLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = textDimensions.x + widthAddition;
            animatorController = EditorGUILayout.ObjectField(label, animatorController, typeof(AnimatorController), true) as AnimatorController;
            EditorGUIUtility.labelWidth = prevLabelWidth;
            return animatorController;
        }
    }
}