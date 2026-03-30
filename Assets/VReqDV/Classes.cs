using System.Collections.Generic;
using UnityEngine;

using Newtonsoft.Json;

public class BehaviorList
{
    public List<BehaviorRule> behaviors;
}

public class BehaviorRule
{
    [JsonProperty("id")]
    public string Id;

    [JsonProperty("event")]
    public string Event;

    [JsonProperty("actors")]
    public List<string> Actors;

    // Backward compatibility: Source returns the first actor if available
    [JsonIgnore]
    public string Source 
    {
        get { return (Actors != null && Actors.Count > 0) ? Actors[0] : null; }
        set { if (Actors == null) Actors = new List<string>(); if (!Actors.Contains(value)) Actors.Insert(0, value); }
    }

    [JsonProperty("precondition")]
    public ConditionNode Precondition;

    [JsonProperty("action")]
    public ActionNode Action;

    [JsonProperty("postcondition")]
    public ConditionNode Postcondition;

    // Helper property to maintain compatibility or simple access if needed
    [JsonIgnore]
    public string ActionAlgorithm => Action?.runAlgorithm;
}

public class ActionNode
{
    public string runAlgorithm;
    public Dictionary<string, string> @params;
}

public class ConditionNode
{
    public string XRinteraction;
    public List<ConditionNode> all;
    public List<ConditionNode> any;
    public List<string> equals;
    public string runAlgorithm;
    public Dictionary<string, string> @params;

    [JsonIgnore]
    public string left => (equals != null && equals.Count > 0) ? equals[0] : null;

    [JsonIgnore]
    public string right => (equals != null && equals.Count > 1) ? equals[1] : null;
}


[System.Serializable]
public class ActionResponseList
{
    public List<ActionResponse> ObjAction;
}

[System.Serializable]
public class TriggerEvent
{
    public string sourceObj;
    public string IsCollision;
    public string action;
    public object change_property_by;
    public object force;
    public string disappear;
    public string inputType;
    public string repeatactionfor;
}

[System.Serializable]
public class ResponseEvent
{
    public string targetObj;
    public string IsCollision;
    public string response;
    public object change_property_by;
    public object force;
    public string disappear;
    public string outputType;
    public string repeatactionfor;
}

// [System.Serializable]
// public class Force
// {
//     // Define force properties if needed
// }

[System.Serializable]
public class ActionResponse
{
    public string actresid;
    public TriggerEvent trigger_event;
    public ResponseEvent response_event;
    public string comment;
    public string Syncronous;
}

// public class property_change
// {

// }
public class ArticleList
{
    public List<Article> articles { get; set; }
}

public class Article
{
    public string _objectname { get; set; }
    public string _sid { get; set; }
    public string _slabel { get; set; }
    public string source { get; set; }
    public int _IsHidden { get; set; }
    public int _enumcount { get; set; }
    public int _Is3DObject { get; set; }
    public int HasChild { get; set; }
    public List<string> Children { get; set; }
    public string shape { get; set; }
    [Newtonsoft.Json.JsonIgnore]
    public Dimension dimension { get; set; }
    [Newtonsoft.Json.JsonIgnore]
    public bool IsText { get; set; }
    [Newtonsoft.Json.JsonIgnore]
    public bool IsText3D { get; set; }
    [Newtonsoft.Json.JsonIgnore]
    public Lighting lighting { get; set; }
    [Newtonsoft.Json.JsonIgnore]
    public bool IsIlluminate { get; set; }
    public TransformData Transform_initialpos { get; set; }
    public TransformData Transform_initialrotation { get; set; }
    public TransformData Transform_objectscale { get; set; }
    [Newtonsoft.Json.JsonIgnore]
    public RepeatTransform repeattransfrom { get; set; }
    public Interaction Interaction { get; set; }
    [Newtonsoft.Json.JsonIgnore]
    public string Smoothing { get; set; }
    [Newtonsoft.Json.JsonIgnore]
    public string Smoothing_duration { get; set; }
    [Newtonsoft.Json.JsonIgnore]
    public AttachTransform attachtransform { get; set; }
    public XRRigidObject XRRigidObject { get; set; }
    [Newtonsoft.Json.JsonIgnore]
    public string aud_hasaudio { get; set; }
    [Newtonsoft.Json.JsonIgnore]
    public string aud_type { get; set; }
    [Newtonsoft.Json.JsonIgnore]
    public string aud_src { get; set; }
    [Newtonsoft.Json.JsonIgnore]
    public string aud_volume { get; set; }
    [Newtonsoft.Json.JsonIgnore]
    public string aud_PlayInloop { get; set; }
    [Newtonsoft.Json.JsonIgnore]
    public string aud_IsSurround { get; set; }
    [Newtonsoft.Json.JsonIgnore]
    public string aud_Dopplerlevel { get; set; }
    [Newtonsoft.Json.JsonIgnore]
    public string aud_spread { get; set; }
    [Newtonsoft.Json.JsonIgnore]
    public string aud_mindist { get; set; }
    [Newtonsoft.Json.JsonIgnore]
    public string aud_maxdist { get; set; }
    [Newtonsoft.Json.JsonIgnore]
    public string _Opttxt1 { get; set; }
    public List<string> states { get; set; }
    public string context_img_source { get; set; }
}

public class Dimension
{
    public float dradii { get; set; }
    public string dvolumn { get; set; }
    public string dlength { get; set; }
    public string dbreadth { get; set; }
    public string dheigth { get; set; }
}

public class Lighting
{
    public string CastShadow { get; set; }
    public string ReceiveShadow { get; set; }
    public string ContributeGlobalIlumination { get; set; }
}

public class TransformData
{
    public string x { get; set; }
    public string y { get; set; }
    public string z { get; set; }
}

public class RepeatTransform
{
    public string distfactorx { get; set; }
    public string distfactory { get; set; }
    public string distfactorz { get; set; }
}

public class Interaction
{
    public string XRGrabInteractable { get; set; }
    public List<string> XRInteractionMaskLayer { get; set; }
    public string TrackPosition { get; set; }
    public string TrackRotation { get; set; }
    public string Throw_Detach { get; set; }
    public string forcegravity { get; set; }
    public string velocity { get; set; }
    public string angularvelocity { get; set; }
}

public class AttachTransform
{
    public string rotate_x { get; set; }
    public string rotate_y { get; set; }
    public string rotate_z { get; set; }
    public string pos_x { get; set; }
    public string pos_y { get; set; }
    public string pos_z { get; set; }
}

public class XRRigidObject
{
    public string value { get; set; }
    public string mass { get; set; }
    public string dragfriction { get; set; }
    public string angulardrag { get; set; }
    public string Isgravityenable { get; set; }
    public string IsKinematic { get; set; }
    public string CanInterpolate { get; set; }
    public string CollisionPolling { get; set; }
}
