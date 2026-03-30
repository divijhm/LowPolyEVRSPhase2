using System.IO;
using System.Text;
using UnityEngine;
using System.Collections.Generic;

public static class StateCodeGenerator
{
    // Entry point
    public static void GenerateStateSystem(
        string objectName,
        List<string> states,
        string version,
        string outputFolder = "Assets/VReqDV/Generated"
    )
    {
        string versionedFolder = Path.Combine(outputFolder, "States/" + version);
        Directory.CreateDirectory(versionedFolder);

        WriteFile(versionedFolder, $"{objectName}StateEnum.cs",
            GenerateEnum(objectName, states, version));

        WriteFile(versionedFolder, $"{objectName}StateStorage.cs",
            GenerateStateStorage(objectName, states, version));

        WriteFile(versionedFolder, $"{objectName}Initializer.cs",
            GenerateInitializer(objectName, states, version));

        WriteFile(versionedFolder, $"{objectName}StateAPI.cs",
            GenerateStateAPI(objectName, states, version));

        Debug.Log($"State system generated for {objectName} (Version: {version})");
    }

    // ------------------------------------------------------------
    // ENUM
    // ------------------------------------------------------------
    private static string GenerateEnum(string objectName, List<string> states, string version)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// GENERATED FILE — DO NOT EDIT");
        sb.AppendLine($"namespace {version}");
        sb.AppendLine("{");
        sb.AppendLine($"    public enum {objectName}StateEnum");
        sb.AppendLine("    {");

        for (int i = 0; i < states.Count; i++)
        {
            sb.AppendLine($"        {Pascal(states[i])},");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    // ------------------------------------------------------------
    // STATE STORAGE
    // ------------------------------------------------------------
    private static string GenerateStateStorage(string objectName, List<string> states, string version)
    {
        string enumName = $"{objectName}StateEnum";
        string className = $"{objectName}StateStorage";

        var sb = new StringBuilder();

        sb.AppendLine("// GENERATED FILE — DO NOT EDIT");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();
        sb.AppendLine($"namespace {version}");
        sb.AppendLine("{");
        sb.AppendLine($"    public static class {className}");
        sb.AppendLine("    {");
        sb.AppendLine($"        private static Dictionary<GameObject, {enumName}> stateTable = new();");
        sb.AppendLine();
        sb.AppendLine($"        public static event Action<GameObject, {enumName}> OnStateChanged;");
        sb.AppendLine();
        sb.AppendLine($"        public static void Register(GameObject obj, {enumName} initialState)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (!stateTable.ContainsKey(obj))");
        sb.AppendLine("                stateTable.Add(obj, initialState);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        public static {enumName} Get(GameObject obj) => stateTable[obj];");
        sb.AppendLine();

        // IsX methods
        foreach (var state in states)
        {
            sb.AppendLine(
                $"        public static bool Is{Pascal(state)}(GameObject obj) => stateTable[obj] == {enumName}.{Pascal(state)};");
        }

        sb.AppendLine();

        // SetX methods
        foreach (var state in states)
        {
            sb.AppendLine(
                $"        public static void Set{Pascal(state)}(GameObject obj) => SetState(obj, {enumName}.{Pascal(state)});");
        }

        sb.AppendLine();
        sb.AppendLine($"        private static void SetState(GameObject obj, {enumName} newState)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (stateTable[obj] != newState)");
        sb.AppendLine("            {");
        sb.AppendLine("                stateTable[obj] = newState;");
        sb.AppendLine("                OnStateChanged?.Invoke(obj, newState);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    // ------------------------------------------------------------
    // INITIALIZER
    // ------------------------------------------------------------
    private static string GenerateInitializer(string objectName, List<string> states, string version)
    {
        string enumName = $"{objectName}StateEnum";
        string storageName = $"{objectName}StateStorage";
        string initClass = $"{objectName}Initializer";

        var sb = new StringBuilder();

        sb.AppendLine("// GENERATED FILE — DO NOT EDIT");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine();
        sb.AppendLine($"namespace {version}");
        sb.AppendLine("{");
        sb.AppendLine($"    public class {initClass} : MonoBehaviour");
        sb.AppendLine("    {");
        sb.AppendLine($"        public {enumName} initialState = {enumName}.{Pascal(states[0])};");
        sb.AppendLine();
        sb.AppendLine("        void Awake()");
        sb.AppendLine("        {");
        sb.AppendLine($"            {storageName}.Register(gameObject, initialState);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    // ------------------------------------------------------------
    // API WRAPPER
    // ------------------------------------------------------------
    private static string GenerateStateAPI(string objectName, List<string> states, string version)
    {
        string storageName = $"{objectName}StateStorage";
        string apiName = $"{objectName}StateAPI";

        var sb = new StringBuilder();

        sb.AppendLine("// GENERATED FILE — DO NOT EDIT");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine();
        sb.AppendLine($"namespace {version}");
        sb.AppendLine("{");
        sb.AppendLine($"    public static class {apiName}");
        sb.AppendLine("    {");

        foreach (var state in states)
        {
            sb.AppendLine(
                $"        public static bool {Pascal(state)}(GameObject obj) => {storageName}.Is{Pascal(state)}(obj);");
        }

        sb.AppendLine();

        foreach (var state in states)
        {
            sb.AppendLine(
                $"        public static void Set{Pascal(state)}(GameObject obj) => {storageName}.Set{Pascal(state)}(obj);");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    // ------------------------------------------------------------
    // UTILITIES
    // ------------------------------------------------------------
    private static void WriteFile(string folder, string fileName, string content)
    {
        File.WriteAllText(Path.Combine(folder, fileName), content);
    }

    private static string Pascal(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return char.ToUpper(value[0]) + value.Substring(1);
    }
}
