// GENERATED FILE — DO NOT EDIT
using UnityEngine;

namespace Version_3
{
    public class ControllerAngleDisplayInitializer : MonoBehaviour
    {
        public ControllerAngleDisplayStateEnum initialState = ControllerAngleDisplayStateEnum.Waiting_for_input;

        void Awake()
        {
            ControllerAngleDisplayStateStorage.Register(gameObject, initialState);
        }
    }
}
