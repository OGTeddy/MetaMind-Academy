/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Editor.Inspectors.Game
{
    using Opsive.Shared.Editor.Inspectors;
    using Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Game;
    using UnityEngine;
    using UnityEditor;
    using UnityEditorInternal;

    /// <summary>
    /// Shows a custom inspector for the RuntimePickups.
    /// </summary>
    [CustomEditor(typeof(RuntimePickups))]
    public class RuntimePickupsInspector : InspectorBase
    {
        private ReorderableList m_RuntimeItemsReorderableList;

        /// <summary>
        /// Draws the custom inspector.
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUI.BeginChangeCheck();
            if (m_RuntimeItemsReorderableList == null) {
                var preloadedPrefabsProperty = PropertyFromName("m_RuntimeItems");
                m_RuntimeItemsReorderableList = new ReorderableList(serializedObject, preloadedPrefabsProperty, true, false, true, true);
                m_RuntimeItemsReorderableList.drawHeaderCallback = OnNetworkRuntimeItemsDrawHeader;
                m_RuntimeItemsReorderableList.drawElementCallback = OnNetworkRuntimeItemsElementDraw;
            }
            m_RuntimeItemsReorderableList.DoLayoutList();
            if (EditorGUI.EndChangeCheck()) {
                Shared.Editor.Utility.EditorUtility.RecordUndoDirtyObject(target, "Change Value");
                serializedObject.ApplyModifiedProperties();
                Shared.Editor.Utility.EditorUtility.SetDirty(target);
            }
        }

        /// <summary>
        /// Draws the header for the RuntimeItems list.
        /// </summary>
        private void OnNetworkRuntimeItemsDrawHeader(Rect rect)
        {
            EditorGUI.LabelField(new Rect(rect.x + 12, rect.y, rect.width, EditorGUIUtility.singleLineHeight), "Runtime Items");
        }

        /// <summary>
        /// Draws the RuntimeItems ReordableList element.
        /// </summary>
        private void OnNetworkRuntimeItemsElementDraw(Rect rect, int index, bool isActive, bool isFocused)
        {
            EditorGUI.BeginChangeCheck();

            var runtimeItem = m_RuntimeItemsReorderableList.serializedProperty.GetArrayElementAtIndex(index);
            EditorGUI.ObjectField(new Rect(rect.x, rect.y + 1, rect.width, EditorGUIUtility.singleLineHeight), runtimeItem, new GUIContent());

            if (EditorGUI.EndChangeCheck()) {
                var serializedObject = m_RuntimeItemsReorderableList.serializedProperty.serializedObject;
                Shared.Editor.Utility.EditorUtility.RecordUndoDirtyObject(serializedObject.targetObject, "Change Value");
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}