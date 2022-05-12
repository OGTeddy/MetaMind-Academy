/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

using UnityEngine;
using Opsive.UltimateCharacterController.Inventory;
using Opsive.UltimateCharacterController.Objects.CharacterAssist;
using UMA;

namespace Opsive.UltimateCharacterController.Integrations.UMA
{
    /// <summary>
    /// Picks up the specified items. These items should first be created so they can be picked up at runtime:
    /// https://opsive.com/support/documentation/ultimate-character-controller/items/runtime-pickup/.
    /// </summary>
    public class UMAItemPickup : MonoBehaviour
    {
        [Tooltip("A reference to the items that should be picked up.")]
        [SerializeField] protected ItemPickup[] m_ItemPickups;

        /// <summary>
        /// UMA has created the character.
        /// </summary>
        /// <param name="data">The UMAData for the character.</param>
        public void OnCharacterCreated(UMAData data)
        {
            if (m_ItemPickups == null || m_ItemPickups.Length == 0) {
                return;
            }

            var inventory = data.gameObject.GetComponent<InventoryBase>();
            if (inventory == null) {
                return;
            }

            for (int i = 0; i < m_ItemPickups.Length; ++i) {
                if (m_ItemPickups[i] == null) {
                    continue;
                }

                m_ItemPickups[i].DoItemPickup(data.gameObject, inventory, -1, true, true);
            }

            // The script is no longer required.
            Destroy(this);
        }
    }
}