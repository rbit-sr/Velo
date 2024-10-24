using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Steamworks;

namespace Velo
{
    public class SteamCache
    {
        private static readonly Dictionary<ulong, string> names = new Dictionary<ulong, string>();
        private static readonly Dictionary<ulong, Texture2D> avatars = new Dictionary<ulong, Texture2D>();

        public static string GetPlayerName(ulong id)
        {
            if (names.ContainsKey(id))
            {
                if (names[id] == "[unknown]")
                    names[id] = SteamFriends.GetFriendPersonaName(new CSteamID(id));

                return names[id];
            }

            string name = SteamFriends.GetFriendPersonaName(new CSteamID(id));
            names.Add(id, name);

            if (name == "[unknown]")
                SteamFriends.RequestUserInformation(new CSteamID(id), false);
            return name;
        }

        private static Texture2D AvatarToTexture(int id)
        {
            Texture2D texture;
            if (id > 5)
            {
                SteamUtils.GetImageSize(id, out uint width, out uint height);
                if (width != 184 || height != 184)
                    return CEngine.CEngine.Instance.WhitePixel;
                byte[] avatar = new byte[4 * (int)width * (int)height];
                SteamUtils.GetImageRGBA(id, avatar, avatar.Length);
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
                    avatars[id] = AvatarToTexture(SteamFriends.GetLargeFriendAvatar(new CSteamID(id)));
                return avatars[id];
            }

            Texture2D avatar = AvatarToTexture(SteamFriends.GetLargeFriendAvatar(new CSteamID(id)));
            avatars.Add(id, avatar);

            if (avatar == CEngine.CEngine.Instance.WhitePixel)
                SteamFriends.RequestUserInformation(new CSteamID(id), false);
            return avatar;
        }

        private static readonly Dictionary<ulong, string> fileNames = new Dictionary<ulong, string>();

        public static string FileIdToName(ulong id)
        {
            if (id == ulong.MaxValue)
                return "[unknown]";

            if (fileNames.ContainsKey(id))
                return fileNames[id];

            CallResult<RemoteStorageGetPublishedFileDetailsResult_t> callback = CallResult<RemoteStorageGetPublishedFileDetailsResult_t>.Create((res, failure) =>
            {
                fileNames[id] = res.m_rgchTitle != "" ? res.m_rgchTitle : "[deleted]";
            });

            fileNames.Add(id, "[" + id + "]");

            var apiCall = SteamRemoteStorage.GetPublishedFileDetails(new PublishedFileId_t(id), 0);
            callback.Set(apiCall, null);

            return fileNames[id];
        }
    }
}
