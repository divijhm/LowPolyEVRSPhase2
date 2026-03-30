using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SceneConfig
{
    public string scenename;
    public string sid;
    public string slabel;
    public PlayArea playarea;
    public CameraSettings camera;
    public InitialCameraPos initialcamerapos;
    public Viewport viewport;
    public ClippingPlane clippingplane;
    public bool horizon;
    public int dof;
    public int skybox;
    public Controllers controllers;
    public Gravity gravity;
    public bool interaction;
    public NestedScene nestedscene;
    public bool audio;
    public bool timeline;
    public string Opttxt1;
    public string context_mock;
    public List<UserType> usertype;
}

[System.Serializable]
public class PlayArea
{
    public string pid;
    public float length;
    public float breadth;
    public float height;
    public string comment;
    public float x_scenecenter;
    public float y_scenecenter;
    public float z_scenecenter;
}

[System.Serializable]
public class CameraSettings
{
    public bool IsSceneObject;
    public string trackingorigin;
}

[System.Serializable]
public class InitialCameraPos
{
    public float x_initialcamerapos;
    public float y_initialcamerapos;
    public float z_initialcamerapos;
}

[System.Serializable]
public class Viewport
{
    public float x_viewport;
    public float y_viewport;
    public float w_viewport;
    public float h_viewport;
}

[System.Serializable]
public class ClippingPlane
{
    public float near_cp;
    public float far_cp;
}

[System.Serializable]
public class Controllers
{
    public string type;
    public bool raycast;
    public float raydistance;
    public float raythinkness;
    public string raycolor;
    public string raytype;
}

[System.Serializable]
public class Gravity
{
    public float value;
}

[System.Serializable]
public class NestedScene
{
    public bool value;
    public int scenecount;
    public int sid_order;
}

[System.Serializable]
public class UserType
{
    public string type;
    public UPlayArea uplayarea;
    public InitialUPos initialupos;
    public UPlayAreaCenter uplayareacenter;
}

[System.Serializable]
public class UPlayArea
{
    public float length_uplayarea;
    public float breadth_uplayarea;
    public float height_uplayarea;
}

[System.Serializable]
public class InitialUPos
{
    public float x_initialupos;
    public float y_initialupos;
    public float z_initialupos;
}

[System.Serializable]
public class UPlayAreaCenter
{
    public float x_uplayareacenter;
    public float y_uplayareacenter;
    public float z_uplayareacenter;
}
