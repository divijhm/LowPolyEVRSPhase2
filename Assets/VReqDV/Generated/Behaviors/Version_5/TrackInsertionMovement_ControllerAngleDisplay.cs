// GENERATED FILE — DO NOT EDIT
using UnityEngine;

namespace Version_5
{
    public class TrackInsertionMovement_ControllerAngleDisplay : MonoBehaviour
    {
        void Update()
        {
            if (ControllerAngleDisplayStateStorage.Get(GameObject.Find("ControllerAngleDisplay")) == ControllerAngleDisplayStateEnum.Waiting_for_input)
            {
                UserAlgorithms.TrackNeedleInsertion(GameObject.Find("ControllerAngleDisplay"));
            }
        }
    }
}
