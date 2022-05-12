/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Input
{
    using Opsive.Shared.Events;
    using Opsive.Shared.Input.VirtualControls;
    using Photon.Realtime;
    using UnityEngine;

    /// <summary>
    /// Registers the virtual controls with the local PUN character.
    /// </summary>
    public class PunVirtualControlsManager : VirtualControlsManager
    {
        /// <summary>
        /// Initialize the default values.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            if (m_Character == null) {
                EventHandler.RegisterEvent<Player, GameObject>("OnPlayerEnteredRoom", OnPlayerEnteredRoom);
            }
        }

        /// <summary>
        /// A player has entered the room. Connect the virtual controls to the joining local character.
        /// </summary>
        /// <param name="player">The Photon Player that entered the room.</param>
        /// <param name="character">The character that the player controls.</param>
        private void OnPlayerEnteredRoom(Player player, GameObject character)
        {
            if (player.IsLocal) {
                Character = character;
            }
        }

        /// <summary>
        /// The object has been destroyed.
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();

            EventHandler.UnregisterEvent<Player, GameObject>("OnPlayerEnteredRoom", OnPlayerEnteredRoom);
        }
    }
}