/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Utility
{
    using ExitGames.Client.Photon;
    using UnityEngine;
    using System;

    /// <summary>
    /// Serializes and deserializes a uint so it can work with PUN.
    /// </summary>
    public class PunUIntType
    {
        private static byte[] s_Bytes = new byte[4];
        private static byte[] s_BigEndianFourByteArray;
        private static byte[] BigEndianFourByteArray { get { if (s_BigEndianFourByteArray == null) { s_BigEndianFourByteArray = new byte[4]; } return s_BigEndianFourByteArray; } set { s_BigEndianFourByteArray = value; } }

        /// <summary>
        /// Reset the static variables for domain reloading.
        /// </summary>
        [RuntimeInitializeOnLoadMethod]
        private static void RegisterType()
        {
            PhotonPeer.RegisterType(typeof(uint), 220, SerializeUInt, DeserializeUInt);
        }

        /// <summary>
        /// Serializes the custom uint type.
        /// </summary>
        /// <param name="outStream">The stream that the type should be written to.</param>
        /// <param name="value">The uint object.</param>
        /// <returns>The legnth of the type.</returns>
        private static short SerializeUInt(StreamBuffer outStream, object value)
        {
            var bytes = BitConverter.GetBytes((uint)value);
            outStream.Write(bytes, 0, 4);
            return 4;
        }

        /// <summary>
        /// Deserializes the custom uint type.
        /// </summary>
        /// <param name="inStream">The stream that should be read from.</param>
        /// <param name="length">The length of the custom types.</param>
        /// <returns>The coverted value.</returns>
        private static object DeserializeUInt(StreamBuffer inStream, short length)
        {
            uint value;
            lock (s_Bytes) {
                inStream.Read(s_Bytes, 0, 4);
                if (!BitConverter.IsLittleEndian) {
                    Array.Copy(s_Bytes, 0, BigEndianFourByteArray, 0, 4);
                    Array.Reverse(BigEndianFourByteArray);
                    value = BitConverter.ToUInt32(BigEndianFourByteArray, 0);
                } else {
                    value = BitConverter.ToUInt32(s_Bytes, 0);
                }
            }
            return value;
        }
    }
}