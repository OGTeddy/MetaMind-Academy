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

    /// <summary>
    /// Shows a custom inspector for the SpawnManagerBase.
    /// </summary>
    [CustomEditor(typeof(SpawnManagerBase))]
    public class SpawnManagerBaseInspector : InspectorBase
    {
        /// <summary>
        /// Draws the custom inspector.
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DrawInspectedFields();
        }

        /// <summary>
        /// Draws the inspected fields.
        /// </summary>
        protected virtual void DrawInspectedFields()
        {
            EditorGUI.BeginChangeCheck();
            var modeProperty = PropertyFromName("m_Mode");
            EditorGUILayout.PropertyField(modeProperty);
            if (modeProperty.enumValueIndex == (int)SpawnManagerBase.SpawnMode.FixedLocation) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(PropertyFromName("m_SpawnLocation"));
                EditorGUILayout.PropertyField(PropertyFromName("m_SpawnLocationOffset"));
                EditorGUI.indentLevel--;
            } else {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(PropertyFromName("m_SpawnPointGrouping"));
                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck()) {
                Shared.Editor.Utility.EditorUtility.RecordUndoDirtyObject(target, "Change Value");
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}