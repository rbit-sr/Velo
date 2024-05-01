using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace Velo
{
    public class SteamCache
    {
        private static Dictionary<ulong, string> names = new Dictionary<ulong, string>();
        private static Dictionary<ulong, Texture2D> avatars = new Dictionary<ulong, Texture2D>();

        public static string GetName(ulong id)
        {
            if (names.ContainsKey(id))
            {
                if (names[id] == "[unknown]")
                    names[id] = Steamworks.SteamFriends.GetFriendPersonaName(new Steamworks.CSteamID(id));
                return names[id];
            }

            string name = Steamworks.SteamFriends.GetFriendPersonaName(new Steamworks.CSteamID(id));
            names.Add(id, name);

            if (name == "[unknown]")
                Steamworks.SteamFriends.RequestUserInformation(new Steamworks.CSteamID(id), false);
            return name;
        }

        private static Texture2D AvatarToTexture(int id)
        {
            Texture2D texture;
            if (id != 0)
            {
                uint width, height;
                Steamworks.SteamUtils.GetImageSize(id, out width, out height);
                byte[] avatar = new byte[4 * (int)width * (int)height];
                Steamworks.SteamUtils.GetImageRGBA(id, avatar, avatar.Length);
                texture = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, (int)width, (int)height);
                texture.SetData<byte>(avatar);
                return texture;
            }
            else
                return CEngine.CEngine.Instance.WhitePixel;
        }

        public static Texture2D GetAvatar(ulong id)
        {
            if (avatars.ContainsKey(id))
            {
                if (avatars[id] == CEngine.CEngine.Instance.WhitePixel)
                    avatars[id] = AvatarToTexture(Steamworks.SteamFriends.GetMediumFriendAvatar(new Steamworks.CSteamID(id)));
                return avatars[id];
            }

            Texture2D avatar = AvatarToTexture(Steamworks.SteamFriends.GetMediumFriendAvatar(new Steamworks.CSteamID(id)));
            avatars.Add(id, avatar);

            if (avatar == CEngine.CEngine.Instance.WhitePixel)
                Steamworks.SteamFriends.RequestUserInformation(new Steamworks.CSteamID(id), false);
            return avatar;
        }
    }
}
