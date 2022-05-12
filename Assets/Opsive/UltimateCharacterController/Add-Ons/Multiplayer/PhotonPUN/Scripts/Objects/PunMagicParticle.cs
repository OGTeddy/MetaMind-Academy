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
    using Opsive.UltimateCharacterController.Items.Actions.Magic;
    using Opsive.UltimateCharacterController.Objects.ItemAssist;
    using Opsive.UltimateCharacterController.Networking.Objects;
    using Photon.Pun;
    using UnityEngine;

    /// <summary>
    /// Initializes the magic particle over the network.
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class PunMagicParticle : MonoBehaviour, INetworkMagicObject, ISpawnDataObject
    {
        private GameObject m_Character;
        private MagicItem m_MagicItem;
        private int m_ActionIndex;
        private uint m_CastID;

        private object[] m_SpawnData;
        private object[] m_InstantiationData;
        public object[] InstantiationData { get => m_InstantiationData; set => m_InstantiationData = value; }

        /// <summary>
        /// Sets the spawn data.
        /// </summary>
        /// <param name="character">The character that is instantiating the object.</param>
        /// <param name="magicItem">The MagicItem that the object belongs to.</param>
        /// <param name="actionIndex">The index of the action that is instantiating the object.</param>
        /// <param name="castID">The ID of the cast that is instantiating the object.</param>
        public void Instantiate(GameObject character, MagicItem magicItem, int actionIndex, uint castID)
        {
            m_Character = character;
            m_MagicItem = magicItem;
            m_ActionIndex = actionIndex;
            m_CastID = castID;
        }

        /// <summary>
        /// Returns the initialization data that is required when the object spawns. This allows the remote players to initialize the object correctly.
        /// </summary>
        /// <returns>The initialization data that is required when the object spawns.</returns>
        public object[] SpawnData()
        {
            if (m_SpawnData == null) {
                m_SpawnData = new object[5];
            }
            m_SpawnData[0] = m_Character.GetCachedComponent<PhotonView>().ViewID;
            m_SpawnData[1] = m_MagicItem.Item.SlotID;
            m_SpawnData[2] = m_MagicItem.ID;
            m_SpawnData[3] = m_ActionIndex;
            m_SpawnData[4] = m_CastID;
            return m_SpawnData;
        }

        /// <summary>
        /// The object has been spawned. Initialize the particle.
        /// </summary>
        public void ObjectSpawned()
        {
            if (m_InstantiationData == null) {
                return;
            }

            var characterView = PhotonNetwork.GetPhotonView((int)m_InstantiationData[0]);
            if (characterView == null) {
                return;
            }
            var inventory = characterView.gameObject.GetCachedComponent<Inventory>();
            if (inventory == null) {
                return;
            }
            var item = inventory.GetActiveItem((int)m_InstantiationData[1]);
            if (item == null) {
                return;
            }
            var magicItem = item.GetItemAction((int)m_InstantiationData[2]) as MagicItem;
            if (magicItem == null) {
                return;
            }
            var particleAction = magicItem.CastActions[(int)m_InstantiationData[3]] as IMagicObjectAction;
            if (particleAction == null) {
                return;
            }
            particleAction.SpawnedGameObject = gameObject;
            particleAction.CastID = (uint)m_InstantiationData[4];
            var magicParticle = gameObject.GetCachedComponent<MagicParticle>();
            if (magicParticle != null) {
                magicParticle.Initialize(magicItem, (uint)m_InstantiationData[4]);
            }
        }
    }
}
