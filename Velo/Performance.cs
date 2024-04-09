using Lidgren.Network;
using System.Collections.Generic;

namespace Velo
{
    public class Performance : ToggleModule
    {
        public IntSetting Framelimit;
        public IntSetting FramelimitMethod;
        public BoolSetting DisableBubbles;
        public BoolSetting DisableSteamInputApi;
        public IntSetting EnableControllerId;
        public BoolSetting LimitFramerateAfterRender;
        public BoolSetting MultithreadedNetwork;

        private Performance() : base("Performance")
        {
            Enabled.SetValueAndDefault(new Toggle(true));

            NewCategory("framerate");
            Framelimit = AddInt("framelimit", -1, -1, 2500);
            FramelimitMethod = AddInt("framelimit method", -1, -1, 3);
            LimitFramerateAfterRender = AddBool("limit framerate after render", false);

            Framelimit.Tooltip =
                "framelimit, as in the +framelimit launch argument (-1 for default, below 30 and above 300 cannot be used in multiplayer.)";
            FramelimitMethod.Tooltip =
                "framelimit method, as in the +framelimitmethod launch argument:\n" +
                "-1: default\n" +
                "0: calls Thread.Yield() repeatedly\n" +
                "1: calls Thread.Sleep(0) repeatedly\n" +
                "2: calls nothing, basically a spin wait\n" +
                "3: calls Thread.Sleep(1) repeatedly";
            LimitFramerateAfterRender.Tooltip =
                "Makes the game render its current frame before waiting to limit the framerate. " +
                "Might reduce input lag.";

            NewCategory("particles");
            DisableBubbles = AddBool("disable bubbles", false);

            DisableBubbles.Tooltip =
                "Disabling bubbles gives a good performance boost on maps like SpeedCity Nights. " +
                "The game does a collision detection for each bubble on every frame.";

            NewCategory("input");
            DisableSteamInputApi = AddBool("disable Steam input API", false);
            EnableControllerId = AddInt("enable controller ID", -1, -2, 16);

            DisableSteamInputApi.Tooltip =
                "Disables the Steam input API. Might break controller inputs.";
            EnableControllerId.Tooltip =
                "Enables only the specified controller ID. " +
                "Put -1 to enable all and -2 to enable none (-2 for best performance). " +
                "Might break controller inputs.";

            NewCategory("other");
            MultithreadedNetwork = AddBool("multithreaded network", true);
            
            MultithreadedNetwork.Tooltip =
                "[WARNING: Experimental] Starts a new thread to poll network packets. " +
                "If you experience more crashes than usual, then disable this setting again.";
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
                MultithreadedNetwork.Value)
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
                MultithreadedNetwork.Value)
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
                MultithreadedNetwork.Value
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
