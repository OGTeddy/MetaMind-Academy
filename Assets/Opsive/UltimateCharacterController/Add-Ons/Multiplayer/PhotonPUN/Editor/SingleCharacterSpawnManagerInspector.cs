/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Editor.Inspectors.Game
{
    using Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Game;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Shows a custom inspector for the SingleCharacterSpawnManager.
    /// </summary>
    [CustomEditor(typeof(SingleCharacterSpawnManager), true)]
    public class SingleCharacterSpawnManagerInspector : SpawnManagerBaseInspector
    {
        /// <summary>
        /// Draws the inspected fields.
        /// </summary>
        protected override void DrawInspectedFields()
        {
            EditorGUI.BeginChangeCheck();
            var characterProperty = PropertyFromName("m_Character");
            EditorGUILayout.PropertyField(characterProperty);
            if (characterProperty.objectReferenceValue != null) {
                var photonView = (characterProperty.objectReferenceValue as UnityEngine.GameObject).GetComponent<Photon.Pun.PhotonView>();
                if (photonView == null) {
                    EditorGUILayout.HelpBox("The character must have a PhotonView.", MessageType.Error);
                } else {
                    if (photonView.sceneViewId != 0) {
                        photonView.sceneViewId = 0;
                        EditorUtility.SetDirty(photonView);
                    }
                }
            }
            if (EditorGUI.EndChangeCheck()) {
                Shared.Editor.Utility.EditorUtility.RecordUndoDirtyObject(target, "Change Value");
                serializedObject.ApplyModifiedProperties();
            }

            base.DrawInspectedFields();
        }
    }
}