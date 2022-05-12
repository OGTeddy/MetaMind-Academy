/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Editor.Inspectors.Game
{
    using Opsive.Shared.Editor.Inspectors;
    using Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Game;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;

    /// <summary>
    /// Shows a custom inspector for the PunObjectPool.
    /// </summary>
    [CustomEditor(typeof(PunObjectPool))]
    public class PunObjectPoolInspector : InspectorBase
    {
        private ReorderableList m_NetworkSpawnableObjectsReorderableList;

        /// <summary>
        /// Draws the custom inspector.
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUI.BeginChangeCheck();
            if (m_NetworkSpawnableObjectsReorderableList == null) {
                var preloadedPrefabsProperty = PropertyFromName("m_NetworkSpawnableObjects");
                m_NetworkSpawnableObjectsReorderableList = new ReorderableList(serializedObject, preloadedPrefabsProperty, true, false, true, true);
                m_NetworkSpawnableObjectsReorderableList.drawHeaderCallback = OnNetworkSpawnableObjectsDrawHeader;
                m_NetworkSpawnableObjectsReorderableList.drawElementCallback = OnNetworkSpawnableObjectsElementDraw;
            }
            m_NetworkSpawnableObjectsReorderableList.DoLayoutList();
            if (EditorGUI.EndChangeCheck()) {
                Shared.Editor.Utility.EditorUtility.RecordUndoDirtyObject(target, "Change Value");
                serializedObject.ApplyModifiedProperties();
                Shared.Editor.Utility.EditorUtility.SetDirty(target);
            }
        }

        /// <summary>
        /// Draws the header for the SpawnableObject list.
        /// </summary>
        private void OnNetworkSpawnableObjectsDrawHeader(Rect rect)
        {
            EditorGUI.LabelField(new Rect(rect.x + 12, rect.y, rect.width, EditorGUIUtility.singleLineHeight), "Spawnable Prefabs");
        }

        /// <summary>
        /// Draws the SpawnableObject ReordableList element.
        /// </summary>
        private void OnNetworkSpawnableObjectsElementDraw(Rect rect, int index, bool isActive, bool isFocused)
        {
            EditorGUI.BeginChangeCheck();

            var spawnableObject = m_NetworkSpawnableObjectsReorderableList.serializedProperty.GetArrayElementAtIndex(index);
            EditorGUI.ObjectField(new Rect(rect.x, rect.y + 1, rect.width, EditorGUIUtility.singleLineHeight), spawnableObject, new GUIContent());

            if (EditorGUI.EndChangeCheck()) {
                var serializedObject = m_NetworkSpawnableObjectsReorderableList.serializedProperty.serializedObject;
                Shared.Editor.Utility.EditorUtility.RecordUndoDirtyObject(serializedObject.targetObject, "Change Value");
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}