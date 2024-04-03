using Lidgren.Network;
using System.Collections.Generic;

namespace Velo
{
    public class Performance : ToggleModule
    {
        public BoolSetting DisableBubbles;
        public BoolSetting DisableSteamInputApi;
        public IntSetting EnableControllerId;
        public BoolSetting LimitFramerateAfterRender;
        public BoolSetting PollNetworkPacketsInSeparateThread;

        private Performance() : base("Performance")
        {
            Enabled.SetValueAndDefault(new Toggle(true));

            DisableBubbles = AddBool("disable bubbles", false);
            DisableSteamInputApi = AddBool("disable Steam input API", false);
            EnableControllerId = AddInt("enable controller ID", -1, -2, 16);
            LimitFramerateAfterRender = AddBool("limit framerate after render", false);
            PollNetworkPacketsInSeparateThread = AddBool("poll network packets in separate thread", true);
        }

        public static Performance Instance = new Performance();

        public class MessagePoller
        {
            public MessagePools pools;
            public int channel;
            public List<Velo.Message> messages;

            public MessagePoller(MessagePools pools, int channel)
            {
                this.pools = pools;
                this.channel = channel;
                this.messages = new List<Velo.Message>();
            }
        }

        public List<MessagePoller> pollers = new List<MessagePoller>();

        public Velo.Message PollPacket(MessagePools pools, int channel)
        {
            if (
                !(Enabled.Value.Enabled &&
                PollNetworkPacketsInSeparateThread.Value)
                )
            {
                if (pollers.Count > 0)
                {
                    lock (pollers)
                    {
                        pollers.Clear();
                    }
                }
                uint num;
                if (Steamworks.SteamNetworking.IsP2PPacketAvailable(out num, channel))
                {
                    NetIncomingMessage msg = pools.CreateIncomingMessage(NetIncomingMessageType.Data, (int)num);
                    Steamworks.CSteamID identifier;
                    Steamworks.SteamNetworking.ReadP2PPacket(msg.PeekDataBuffer(), num, out num, out identifier, channel);
                    msg.LengthBits = (int)(num * 8u);
                    return new Velo.Message(msg, identifier);
                }
                else
                    return null;
            }

            foreach (MessagePoller poller in pollers)
            {
                if (poller.pools == pools && poller.channel == channel)
                {
                    lock (poller.messages)
                    {
                        if (poller.messages.Count >= 1)
                        {
                            Velo.Message msg = poller.messages[0];
                            poller.messages.RemoveAt(0);
                            return msg;
                        }
                        else
                            return null;
                    }
                }
            }

            MessagePoller newPoller = new MessagePoller(pools, channel);
            lock (pollers)
            {
                pollers.Add(newPoller);
            }
            System.Threading.Thread thread = new System.Threading.Thread(PollPacketsLoop);
            thread.Start(newPoller);
            return null;
        }

        public void RecyclePacket(NetIncomingMessage message, MessagePools pools)
        {
            if (!(Enabled.Value.Enabled &&
                PollNetworkPacketsInSeparateThread.Value)
                )
            {
                pools.Recycle(message);
                return;
            }

            lock (pools)
            {
                pools.Recycle(message);
            }
        }

        public void PollPacketsLoop(object pollerObj)
        {
            MessagePoller poller = (MessagePoller)pollerObj;
            while (
                Enabled.Value.Enabled &&
                PollNetworkPacketsInSeparateThread.Value
                )
            {
                uint num;
                if (Steamworks.SteamNetworking.IsP2PPacketAvailable(out num, poller.channel))
                {
                    NetIncomingMessage msg;
                    lock (poller.pools)
                    {
                        msg = poller.pools.CreateIncomingMessage(NetIncomingMessageType.Data, (int)num);
                    }
                    Steamworks.CSteamID identifier;
                    Steamworks.SteamNetworking.ReadP2PPacket(msg.PeekDataBuffer(), num, out num, out identifier, poller.channel);
                    msg.LengthBits = (int)(num * 8u);
                    lock (poller.messages)
                    {
                        poller.messages.Add(new Velo.Message(msg, identifier));
                    }
                }

                System.Threading.Thread.Sleep(1);
            }
        }
    }
}
