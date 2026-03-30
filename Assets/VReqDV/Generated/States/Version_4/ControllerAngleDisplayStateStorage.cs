// GENERATED FILE — DO NOT EDIT
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Version_4
{
    public static class ControllerAngleDisplayStateStorage
    {
        private static Dictionary<GameObject, ControllerAngleDisplayStateEnum> stateTable = new();

        public static event Action<GameObject, ControllerAngleDisplayStateEnum> OnStateChanged;

        public static void Register(GameObject obj, ControllerAngleDisplayStateEnum initialState)
        {
            if (!stateTable.ContainsKey(obj))
                stateTable.Add(obj, initialState);
        }

        public static ControllerAngleDisplayStateEnum Get(GameObject obj) => stateTable[obj];

        public static bool IsWaiting_for_input(GameObject obj) => stateTable[obj] == ControllerAngleDisplayStateEnum.Waiting_for_input;

        public static void SetWaiting_for_input(GameObject obj) => SetState(obj, ControllerAngleDisplayStateEnum.Waiting_for_input);

        private static void SetState(GameObject obj, ControllerAngleDisplayStateEnum newState)
        {
            if (stateTable[obj] != newState)
            {
                stateTable[obj] = newState;
                OnStateChanged?.Invoke(obj, newState);
            }
        }
    }
}
