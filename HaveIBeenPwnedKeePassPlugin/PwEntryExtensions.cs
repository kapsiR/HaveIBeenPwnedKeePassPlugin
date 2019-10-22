using System;
using KeePassLib;

namespace HaveIBeenPwnedPlugin
{
    public static class PwEntryExtensions
    {
        public static bool IsExpired(this PwEntry pwEntry)
        {
            if (pwEntry.Expires)
            {
                return DateTime.UtcNow >= pwEntry.ExpiryTime;
            }

            return false;
        }
    }
}
