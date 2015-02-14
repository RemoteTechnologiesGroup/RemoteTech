using RemoteTech.SimpleTypes;

namespace RemoteTech.AddOns
{
    /// <summary>
    /// This class connects to the KSP-Addon ControlLock, created by Diazo
    /// to keep the inputs of Linux users to the focused input fields.
    /// Topic: http://forum.kerbalspaceprogram.com/threads/108561
    /// </summary>
    public class ControlLockAddon : AddOn
    {
        public ControlLockAddon()
            : base("ControlLock", "ControlLock.ControlLock")
        {
        }

        /// <summary>
        /// Is the control lock set by a specific <paramref name="modname"/>?
        /// The string is the same string as passed to SetFullLock. Note
        /// that this method can return false that it is not locked by
        /// a mod, but another mod is enforcing the control lock.
        /// </summary>
        /// <param name="modName">Name of the mod who set the lock</param>
        /// <returns>true if lock is the for the mod</returns>
        public bool IsLockSet(string modName)
        {
            var result = this.invoke(new System.Object[] { modName });

            if (result != null) return (bool)result;
            return false;
        }

        /// <summary>
        /// Check if the control lock is set, yes or no.
        /// </summary>
        /// <returns>true for lock is set, otherwise false</returns>
        public bool IsLockSet()
        {
            var result = this.invoke(null);

            if (result!=null) return (bool)result;
            return false;
        }

        /// <summary>
        /// Pass this method a string, I recommend the name of your
        /// <paramref name="modname"/>, to engage the lock. Note you
        /// can pass the same lock multiple times, this mod will
        /// detect that and only lock controls the first time the
        /// mod is locked after being unlocked.
        /// </summary>
        /// <param name="modName">Name of the mod to set the lock</param>
        public void SetFullLock(string modName)
        {
            this.invoke(new System.Object[] { modName });
        }

        /// <summary>
        /// Pass the <paramref name="modname"/> of your control lock
        /// to unset the lock. This is the same string (capitilization
        /// matters) that you passed in the SetFullLock command.
        /// It is safe to pass this with an invalid string,
        /// nothing will happen.
        /// </summary>
        /// <param name="modName">Name of the mod to unlock</param>
        public void UnsetFullLock(string modName)
        {
            this.invoke(new System.Object[] { modName });
        }
    }
}
