﻿using Barotrauma;
using Barotrauma.Networking;

namespace MoreLevelContent.Networking
{
    /// <summary>
    /// Server
    /// </summary>
    public static partial class NetUtil
    {
        /// <summary>
        /// Send a message to the specified client
        /// </summary>
        /// <param name="outMsg"></param>
        /// <param name="connection"></param>
        /// <param name="deliveryMethod"></param>
        public static void SendClient(IWriteMessage outMsg, NetworkConnection connection, DeliveryMethod deliveryMethod = DeliveryMethod.Reliable) => GameMain.LuaCs.Networking.Send(outMsg, connection, deliveryMethod);
        
        /// <summary>
        /// Send message to all connected clients
        /// </summary>
        /// <param name="outMsg"></param>
        /// <param name="deliveryMethod"></param>
        public static void SendAll(IWriteMessage outMsg, DeliveryMethod deliveryMethod = DeliveryMethod.Reliable) => GameMain.LuaCs.Networking.Send(outMsg, null, deliveryMethod);
    }
}
