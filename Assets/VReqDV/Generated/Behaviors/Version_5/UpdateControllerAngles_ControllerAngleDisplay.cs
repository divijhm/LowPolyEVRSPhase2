// GENERATED FILE — DO NOT EDIT
using UnityEngine;

namespace Version_5
{
    public class UpdateControllerAngles_ControllerAngleDisplay : MonoBehaviour
    {
        void Update()
        {
            if (ControllerAngleDisplayStateStorage.Get(GameObject.Find("ControllerAngleDisplay")) == ControllerAngleDisplayStateEnum.Waiting_for_input)
            {
                UserAlgorithms.DisplayLeftControllerAngles(GameObject.Find("ControllerAngleDisplay"));
            }
        }
    }
}
