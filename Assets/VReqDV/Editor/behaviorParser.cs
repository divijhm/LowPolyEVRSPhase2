using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;

public static class BehaviorJsonParser
{
    public static BehaviorList Parse(string jsonText)
    {
        return JsonConvert.DeserializeObject<BehaviorList>(jsonText);
    }
}
