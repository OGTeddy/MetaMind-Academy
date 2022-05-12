/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

using UnityEngine;
using UnityEditor;
using Opsive.Shared.Editor.Inspectors;
using Opsive.Shared.Editor.Inspectors.Utility;

namespace Opsive.UltimateCharacterController.Integrations.UMA.Editor
{
    /// <summary>
    /// Draws the inspector for the UMA Character Builder component.
    /// </summary>
    [CustomEditor(typeof(UMACharacterBuilder))]
    public class UMACharacterBuilderInspector : InspectorBase
    {
        /// <summary>
        /// Draws the custom inspector.
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUI.BeginChangeCheck();
#if FIRST_PERSON_CONTROLLER
            EditorGUILayout.LabelField("First Person", InspectorStyles.BoldLabel);
            var addFirstPersonPerspective = PropertyFromName("m_AddFirstPersonPerspective");
            EditorGUILayout.PropertyField(addFirstPersonPerspective);
            if (addFirstPersonPerspective.boolValue) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(PropertyFromName("m_FirstPersonMovementType"));
                EditorGUILayout.PropertyField(PropertyFromName("m_FirstPersonHiddenObjectNames"), true);
                EditorGUILayout.PropertyField(PropertyFromName("m_InvisibleShadowCastorMaterial"));
                EditorGUILayout.PropertyField(PropertyFromName("m_FirstPersonBaseObjects"), true);
                EditorGUI.indentLevel--;
            }
            GUILayout.Space(5);
#endif
            EditorGUILayout.LabelField("Third Person", InspectorStyles.BoldLabel);
            var addThirdPersonPerspective = PropertyFromName("m_AddThirdPersonPerspective");
            EditorGUILayout.PropertyField(addThirdPersonPerspective);
            if (addThirdPersonPerspective.boolValue) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(PropertyFromName("m_ThirdPersonMovementType"));
                EditorGUI.indentLevel--;
            }
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Objects", InspectorStyles.BoldLabel);
            EditorGUILayout.PropertyField(PropertyFromName("m_AnimatorController"));
            var addItems = PropertyFromName("m_AddItems");
            EditorGUILayout.PropertyField(addItems);
            if (addItems.boolValue) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(PropertyFromName("m_ItemCollection"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(PropertyFromName("m_AddHealth"));
            EditorGUILayout.PropertyField(PropertyFromName("m_AddUnityIK"));
            EditorGUILayout.PropertyField(PropertyFromName("m_AddFootEffects"));
            EditorGUILayout.PropertyField(PropertyFromName("m_AddStandardAbilities"));
            var aiAgent = PropertyFromName("m_AIAgent");
            EditorGUILayout.PropertyField(aiAgent);
            if (aiAgent.boolValue) {
                EditorGUILayout.PropertyField(PropertyFromName("m_AddNavMeshAgent"));
            }
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Start", InspectorStyles.BoldLabel);
#if FIRST_PERSON_CONTROLLER
            if (addFirstPersonPerspective.boolValue && addThirdPersonPerspective.boolValue) {
                EditorGUILayout.PropertyField(PropertyFromName("m_StartFirstPersonPerspective"));
            }
#endif
            EditorGUILayout.PropertyField(PropertyFromName("m_AssignCamera"));
            if (EditorGUI.EndChangeCheck()) {
                serializedObject.ApplyModifiedProperties();
            }
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Events", InspectorStyles.BoldLabel);
            EditorGUILayout.PropertyField(PropertyFromName("m_CharacterCreated"));
        }
    }
}