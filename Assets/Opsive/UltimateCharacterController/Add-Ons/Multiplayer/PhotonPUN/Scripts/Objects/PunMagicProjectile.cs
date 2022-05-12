/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Objects
{
    using Opsive.Shared.Game;
    using Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Game;
    using Opsive.UltimateCharacterController.Inventory;
    using Opsive.UltimateCharacterController.Items.Actions;
    using Opsive.UltimateCharacterController.Objects.ItemAssist;
    using Photon.Pun;
    using UnityEngine;

    /// <summary>
    /// Initializes the magic projectile over the network.
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class PunMagicProjectile : MagicProjectile, ISpawnDataObject
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
                m_SpawnData = new object[6];
            }
            m_SpawnData[0] = m_Velocity;
            m_SpawnData[1] = m_Torque;
            m_SpawnData[2] = m_Originator.GetCachedComponent<PhotonView>().ViewID;
            m_SpawnData[3] = m_MagicItem.Item.SlotID;
            m_SpawnData[4] = m_MagicItem.ID;
            m_SpawnData[5] = m_CastID;
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

            var originator = PhotonNetwork.GetPhotonView((int)m_InstantiationData[2]);
            if (originator == null) {
                return;
            }
            var inventory = originator.gameObject.GetCachedComponent<Inventory>();
            if (inventory == null) {
                return;
            }
            var item = inventory.GetActiveItem((int)m_InstantiationData[3]);
            if (item == null) {
                return;
            }
            var magicItem = item.GetItemAction((int)m_InstantiationData[4]) as MagicItem;
            if (magicItem == null) {
                return;
            }

            // Initialize the projectile from the data within the InstantiationData field.
            Initialize((Vector3)m_InstantiationData[0], (Vector3)m_InstantiationData[1], originator.gameObject, magicItem, (uint)m_InstantiationData[5]);
        }
    }
}
