using System;
using System.Collections.Generic;
using System.Reflection;

namespace GrayWolf.Gaze.Editor
{
    internal static class ButtonCache
    {
        private const BindingFlags MemberFlags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly;

        private static readonly Dictionary<Type, List<(MethodInfo Method, ButtonAttribute Attribute)>> Cache = new();

        public static List<(MethodInfo Method, ButtonAttribute Attribute)> GetButtons(Type type)
        {
            if (Cache.TryGetValue(type, out var cached))
                return cached;

            var chain = new List<Type>();

            for (var t = type; t != null && t != typeof(object) && t.Assembly != typeof(UnityEngine.Object).Assembly; t = t.BaseType)
                chain.Add(t);

            chain.Reverse();

            var buttons = new List<(MethodInfo, ButtonAttribute)>();

            foreach (var t in chain)
            {
                foreach (var method in t.GetMethods(MemberFlags))
                {
                    if (method.GetParameters().Length != 0)
                        continue;

                    var attribute = method.GetCustomAttribute<ButtonAttribute>();

                    if (attribute != null)
                        buttons.Add((method, attribute));
                }
            }

            Cache[type] = buttons;
            return buttons;
        }
    }
}
