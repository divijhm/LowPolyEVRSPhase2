using UnityEngine;
using System;
using System.Reflection;

namespace VReqDV
{
    public static class StateAccessor
    {
        /// <summary>
        /// Sets the state of an object dynamically using Reflection.
        /// Searches for {objectName}StateAPI.Set{stateName}(obj).
        /// </summary>
        /// <param name="objectName">The name of the object type (e.g. "Ball", "Pin_1")</param>
        /// <param name="stateName">The state to set (e.g. "Rolling", "Fallen")</param>
        /// <param name="obj">The target GameObject</param>
        /// <param name="versionNamespace">The namespace where the API resides (e.g. "Version_1")</param>
        public static void SetState(string objectName, string stateName, GameObject obj, string versionNamespace = "Version_1")
        {
            string typeName = $"{versionNamespace}.{objectName}StateAPI";
            Type type = Type.GetType(typeName);

            if (type == null)
            {
                // Fallback: Try searching all assemblies if namespace lookup fails or if assembly is not default
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(typeName);
                    if (type != null) break;
                }
            }

            if (type == null)
            {
                Debug.LogWarning($"[StateAccessor] Could not find State API type: {typeName}. Mock-up generation might be required.");
                return;
            }

            string methodName = $"Set{Pascal(stateName)}";
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);

            if (method != null)
            {
                method.Invoke(null, new object[] { obj });
            }
            else
            {
                Debug.LogError($"[StateAccessor] Method {methodName} not found in {typeName}");
            }
        }

        /// <summary>
        /// Checks the state of an object dynamically.
        /// </summary>
        public static bool IsState(string objectName, string stateName, GameObject obj, string versionNamespace = "Version_1")
        {
            string typeName = $"{versionNamespace}.{objectName}StateAPI";
            Type type = Type.GetType(typeName);

            if (type == null)
            {
                 foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(typeName);
                    if (type != null) break;
                }
            }

            if (type == null)
            {
                // If the API doesn't exist yet, we can't check state, defaulting to false or handling gracefully
                return false; 
            }

            string methodName = Pascal(stateName); // The API usually has bool Rolling(GameObject) => ...
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);

            if (method != null)
            {
                object result = method.Invoke(null, new object[] { obj });
                return (bool)result;
            }
            
            return false;
        }

        private static string Pascal(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return char.ToUpper(value[0]) + value.Substring(1);
        }
    }
}
