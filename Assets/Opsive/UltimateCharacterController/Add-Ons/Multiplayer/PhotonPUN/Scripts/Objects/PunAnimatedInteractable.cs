/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Objects
{
    using ExitGames.Client.Photon;
    using Opsive.Shared.Game;
    using Opsive.UltimateCharacterController.Objects.CharacterAssist;
    using Photon.Pun;
    using Photon.Realtime;
    using UnityEngine;

    /// <summary>
    /// Syncronizes the animated interactable when a new player joins the room.
    /// </summary>
    public class PunAnimatedInteractable : AnimatedInteractable, IOnEventCallback
    {
        private PhotonView m_PhotonView;

        /// <summary>
        /// Initialize the default values.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            m_PhotonView = gameObject.GetCachedComponent<PhotonView>();
            if (m_PhotonView == null) {
                Debug.LogError("Error: A PhotonView must be added to " + gameObject.name + ".");
                enabled = false;
            }
        }

        /// <summary>
        /// The object has been enabled.
        /// </summary>
        private void OnEnable()
        {
            PhotonNetwork.AddCallbackTarget(this);
        }

        /// <summary>
        /// A event from Photon has been sent.
        /// </summary>
        /// <param name="photonEvent">The Photon event.</param>
        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code == PhotonEventIDs.RemotePlayerInstantiationComplete) {
                if (!PhotonNetwork.IsMasterClient || !m_HasInteracted) {
                    return;
                }

                var player = PhotonNetwork.CurrentRoom.GetPlayer(photonEvent.Sender);
                m_PhotonView.RPC("InteractedRPC", player);
            }
        }

        /// <summary>
        /// Indicates that the GameObject has been interacted with.
        /// </summary>
        [PunRPC]
        private void InteractedRPC(PhotonMessageInfo info)
        {
            m_HasInteracted = true;
        }

        /// <summary>
        /// The object has been disabled.
        /// </summary>
        private void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
        }
    }
}