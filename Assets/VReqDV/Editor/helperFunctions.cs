using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using Newtonsoft.Json;
using UnityEditor.SceneManagement;
using Newtonsoft.Json.Linq;
using UnityEngine.XR.Interaction.Toolkit;

public class HelperFunctions
{
    public static PrimitiveType GetPrimitiveTypeByString(string shape)
    {
        if (Enum.TryParse(shape, true, out PrimitiveType primitiveType))
        {
            return primitiveType;
        }

        return PrimitiveType.Cube;
    }

    public static Dictionary<string, object> GetTransformInitialPosition(GameObject obj)
    {
        Dictionary<string, object> transformInitialPosition = new Dictionary<string, object>();

        if (obj.transform != null)
        {
            transformInitialPosition["x"] = obj.transform.position.x;
            transformInitialPosition["y"] = obj.transform.position.y;
            transformInitialPosition["z"] = obj.transform.position.z;
        }
        else
        {
            transformInitialPosition["x"] = 0;
            transformInitialPosition["y"] = 0;
            transformInitialPosition["z"] = 0;
        }

        return transformInitialPosition;
    }

    public static Dictionary<string, object> GetTransformObjectScale(GameObject obj)
    {
        Dictionary<string, object> transformObjectScale = new Dictionary<string, object>();

        if (obj.transform != null)
        {
            transformObjectScale["x"] = obj.transform.localScale.x;
            transformObjectScale["y"] = obj.transform.localScale.y;
            transformObjectScale["z"] = obj.transform.localScale.z;
        }
        else
        {
            transformObjectScale["x"] = 1;
            transformObjectScale["y"] = 1;
            transformObjectScale["z"] = 1;
        }

        return transformObjectScale;
    }

    public static Dictionary<string, object> GetTransformInitialRotation(GameObject obj)
    {
        Dictionary<string, object> transformInitialRotation = new Dictionary<string, object>();

        if (obj.transform != null)
        {
            transformInitialRotation["x"] = obj.transform.rotation.eulerAngles.x;
            transformInitialRotation["y"] = obj.transform.rotation.eulerAngles.y;
            transformInitialRotation["z"] = obj.transform.rotation.eulerAngles.z;
        }
        else
        {
            transformInitialRotation["x"] = 0;
            transformInitialRotation["y"] = 0;
            transformInitialRotation["z"] = 0;
        }

        return transformInitialRotation;
    }

    public static Dictionary<string, object> GetXRRigidObject(GameObject obj)
    {
        Dictionary<string, object> xrrigidObject = new Dictionary<string, object>();

        if (obj.GetComponent<Rigidbody>() != null)
        {
            Rigidbody rigidObject = obj.GetComponent<Rigidbody>();
            xrrigidObject["value"] = 1;
            xrrigidObject["mass"] = rigidObject.mass;
            xrrigidObject["dragfriction"] = rigidObject.drag;
            xrrigidObject["angulardrag"] = rigidObject.drag;
            xrrigidObject["Isgravityenable"] = rigidObject.useGravity;
            xrrigidObject["IsKinematic"] = rigidObject.isKinematic;
            if (rigidObject.interpolation == RigidbodyInterpolation.Interpolate)
                xrrigidObject["CanInterpolate"] = 1;
            else if (rigidObject.interpolation == RigidbodyInterpolation.Extrapolate)
                xrrigidObject["CanInterpolate"] = 2;
            else
                xrrigidObject["CanInterpolate"] = 0;
            // xrrigidObject["CollisionPolling"] = rigidObject.collisionPolling;
            switch (rigidObject.collisionDetectionMode)
            {
                case CollisionDetectionMode.Discrete:
                    xrrigidObject["CollisionPolling"] = "discrete";
                    break;
                case CollisionDetectionMode.Continuous:
                    xrrigidObject["CollisionPolling"] = "continuous";
                    break;
                case CollisionDetectionMode.ContinuousDynamic:
                    xrrigidObject["CollisionPolling"] = "continuous-dynamic";
                    break;
                case CollisionDetectionMode.ContinuousSpeculative:
                    xrrigidObject["CollisionPolling"] = "continuous-speculative";
                    break;
                default:
                    xrrigidObject["CollisionPolling"] = "none";
                    break;
            }
        }
        else
        {
            xrrigidObject["value"] = 0;
            xrrigidObject["mass"] = 0;
            xrrigidObject["dragfriction"] = 0;
            xrrigidObject["angulardrag"] = 0;
            xrrigidObject["Isgravityenable"] = false;
            xrrigidObject["IsKinematic"] = false;
            xrrigidObject["CanInterpolate"] = 0;
            xrrigidObject["CollisionPolling"] = "none";
        }

        return xrrigidObject;
    }

    public static Interaction GetInteraction(GameObject obj)
    {
        Interaction interaction = new Interaction();
        XRGrabInteractable grabInteractable = obj.GetComponent<XRGrabInteractable>();

        if (grabInteractable != null)
        {
            interaction.XRGrabInteractable = "true";
            interaction.TrackPosition = grabInteractable.trackPosition.ToString().ToLower();
            interaction.TrackRotation = grabInteractable.trackRotation.ToString().ToLower();
            interaction.Throw_Detach = grabInteractable.throwOnDetach.ToString().ToLower();

            // Read interaction layer mask as a list of layer indices
            interaction.XRInteractionMaskLayer = new List<string>();
            InteractionLayerMask mask = grabInteractable.interactionLayers;
            for (int i = 0; i < 32; i++)
            {
                if ((mask.value & (1 << i)) != 0)
                {
                    interaction.XRInteractionMaskLayer.Add(i.ToString());
                }
            }

            interaction.forcegravity = "";
            interaction.velocity = "";
            interaction.angularvelocity = "";
        }
        else
        {
            interaction.XRGrabInteractable = "false";
            interaction.XRInteractionMaskLayer = new List<string>();
            interaction.TrackPosition = "false";
            interaction.TrackRotation = "false";
            interaction.Throw_Detach = "false";
            interaction.forcegravity = "";
            interaction.velocity = "";
            interaction.angularvelocity = "";
        }

        return interaction;
    }
}