using System.Collections.Generic;

namespace Velo
{
    public class Cursor
    {
        private static readonly HashSet<object> cursorRequests = new HashSet<object>();

        public static void EnableCursor(object requester)
        {
            cursorRequests.Add(requester);
            if (cursorRequests.Count >= 1)
                Velo.CEngineInst.Game.IsMouseVisible = true;
        }

        public static void DisableCursor(object requester)
        {
            cursorRequests.Remove(requester);
            if (cursorRequests.Count == 0)
                Velo.CEngineInst.Game.IsMouseVisible = false;
        }
    }
}
