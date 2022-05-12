/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Editor.Inspectors.Character
{
    using Opsive.Shared.Editor.Inspectors;
    using Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Character;
    using Photon.Pun;
    using UnityEditor;

    /// <summary>
    /// Shows a custom inspector for the PunCharacterTransformMonitor.
    /// </summary>
    [CustomEditor(typeof(PunCharacterTransformMonitor))]
    public class PunCharacterTransformMonitorInspector : InspectorBase
    {
        /// <summary>
        /// Draws the custom inspector.
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.PropertyField(PropertyFromName("m_SynchronizeScale"));
            EditorGUILayout.PropertyField(PropertyFromName("m_RemoteInterpolationMultiplayer"));

            var transformMonitor = target as PunCharacterTransformMonitor;
            // Ensure the character has a value of 0 for the scene ID.
            if (PrefabUtility.IsPartOfAnyPrefab(target)) {
                var photonView = transformMonitor.GetComponent<PhotonView>();
                if (photonView == null) {
                    return;
                }
                if (photonView.sceneViewId != 0) {
                    photonView.sceneViewId = 0;
                    EditorUtility.SetDirty(photonView);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}