using System;
using System.Reflection;

namespace RemoteTech.Common.Utils
{
    public static class ReflectionUtil
    {
        //Note: Keep this method even if it has no reference, it is useful to track some bugs.

        /// <summary>
        /// Get a private field value from an object instance though reflection.
        /// </summary>
        /// <param name="type">The type of the object instance from which to obtain the private field.</param>
        /// <param name="instance">The object instance</param>
        /// <param name="fieldName">The field name in the object instance, from which to obtain the value.</param>
        /// <returns>The value of the <paramref name="fieldName"/> instance or null if no such field exist in the instance.</returns>
        public static object GetInstanceField(Type type, object instance, string fieldName)
        {
            const BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                                           | BindingFlags.Static;
            var field = type.GetField(fieldName, bindFlags);
            return field?.GetValue(instance);
        }
    }
}
