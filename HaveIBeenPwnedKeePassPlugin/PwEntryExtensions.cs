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

        public static bool HasInheritedTag(this PwEntry pwEntry, string tag)
        {
            PwGroup group = pwEntry.ParentGroup;
            while (group != null)
            {
                if (group.Tags.Contains(tag))
                {
                    return true;
                }

                group = group.ParentGroup;
            }

            return false;
        }
    }
}
