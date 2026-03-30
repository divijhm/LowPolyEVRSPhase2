// GENERATED FILE — DO NOT EDIT
using UnityEngine;

namespace Version_5
{
    public class UpdateAngleAlignment_ControllerAngleDisplay : MonoBehaviour
    {
        void Update()
        {
            if (ControllerAngleDisplayStateStorage.Get(GameObject.Find("ControllerAngleDisplay")) == ControllerAngleDisplayStateEnum.Waiting_for_input)
            {
                UserAlgorithms.UpdateAngleAlignment(GameObject.Find("ControllerAngleDisplay"));
            }
        }
    }
}
