/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Demo
{
    using UnityEngine;
    using UnityEngine.UI;
    using Photon.Pun;

    /// <summary>
    /// UI class which displays the amount of traffic the PhotonNetwork is using.
    /// </summary>
    public class TrafficMonitor : MonoBehaviour
    {
        private Text m_OutgoingText;
        private float m_LastCollectionTime = -1;
        private int m_LastIncomingValue = 0;
        private int m_LastOutgoingValue = 0;

        /// <summary>
        /// Initializes the default values.
        /// </summary>
        private void Awake()
        {
            m_OutgoingText = GetComponent<Text>();
        }

        /// <summary>
        /// Enables the traffic stats.
        /// </summary>
        private void Start()
        {
            if (!PhotonNetwork.NetworkingClient.LoadBalancingPeer.TrafficStatsEnabled) {
                PhotonNetwork.NetworkingClient.LoadBalancingPeer.TrafficStatsEnabled = true;
            }
        }

        /// <summary>
        /// Updates the UI with the most recent amount.
        /// </summary>
        private void Update()
        {
            if (m_LastCollectionTime + 1 < Time.realtimeSinceStartup) {
                var incomingDiff = PhotonNetwork.NetworkingClient.LoadBalancingPeer.TrafficStatsIncoming.TotalPacketBytes - m_LastIncomingValue;
                var outgoingDiff = PhotonNetwork.NetworkingClient.LoadBalancingPeer.TrafficStatsOutgoing.TotalPacketBytes - m_LastOutgoingValue;
                m_OutgoingText.text = "Bytes Per Second: " + incomingDiff + " in, " + outgoingDiff + " out";
                m_LastIncomingValue = PhotonNetwork.NetworkingClient.LoadBalancingPeer.TrafficStatsIncoming.TotalPacketBytes;
                m_LastOutgoingValue = PhotonNetwork.NetworkingClient.LoadBalancingPeer.TrafficStatsOutgoing.TotalPacketBytes;
                m_LastCollectionTime = Time.realtimeSinceStartup;
            }
        }
    }
}