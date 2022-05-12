/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Objects
{
    using Opsive.Shared.Game;
    using Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Game;
    using Opsive.UltimateCharacterController.Objects;
    using Photon.Pun;
    using UnityEngine;

    /// <summary>
    /// Initializes the projectile over the network.
    /// </summary>
    public class PunProjectile : Projectile, ISpawnDataObject
    {
        private object[] m_SpawnData;
        private object[] m_InstantiationData;
        public object[] InstantiationData { get => m_InstantiationData; set => m_InstantiationData = value; }

        /// <summary>
        /// Returns the initialization data that is required when the object spawns. This allows the remote players to initialize the object correctly.
        /// </summary>
        /// <returns>The initialization data that is required when the object spawns.</returns>
        public object[] SpawnData()
        {
            if (m_SpawnData == null) {
                m_SpawnData = new object[9];
            }
            m_SpawnData[0] = m_Velocity;
            m_SpawnData[1] = m_Torque;
            m_SpawnData[2] = m_DamageAmount;
            m_SpawnData[3] = m_ImpactForce;
            m_SpawnData[4] = m_ImpactForceFrames;
            m_SpawnData[5] = m_ImpactLayers.value;
            m_SpawnData[6] = m_ImpactStateName;
            m_SpawnData[7] = m_ImpactStateDisableTimer;
            m_SpawnData[8] = m_Originator != null ? m_Originator.GetCachedComponent<PhotonView>().ViewID : -1;
            return m_SpawnData;
        }

        /// <summary>
        /// The object has been spawned. Initialize the projectile.
        /// </summary>
        public void ObjectSpawned()
        {
            if (m_InstantiationData == null) {
                return;
            }

            // Initialize the projectile from the data within the InstantiationData field.
            var originator = PhotonNetwork.GetPhotonView((int)m_InstantiationData[8]);
            Initialize((Vector3)m_InstantiationData[0], (Vector3)m_InstantiationData[1], m_DamageProcessor, (float)m_InstantiationData[2],
                            (float)m_InstantiationData[3], (int)m_InstantiationData[4], (int)m_InstantiationData[5],
                            (string)m_InstantiationData[6], (float)m_InstantiationData[7], null, originator != null ? originator.gameObject : null);
        }
    }
}
