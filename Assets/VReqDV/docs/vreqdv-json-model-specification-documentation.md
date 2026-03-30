# JSON Model Specification Documentation

This document explains how to write `article.json`, `behavior.json` and `UserAlgorithms.cs` files to define objects and behaviors in the application.

## Introduction: VReqDV System Overview

VReqDV is a tool for **model-based prototype generation and versioning** in Unity. It allows you to rapidly prototype interactions by defining them in JSON specifications and writing minimal C# logic.

### 1. How it Works (Workflow)
1.  **Define Specs (JSON)**: You describe *what* objects exist (`article.json`) and *when* things happen (`behavior.json`).
2.  **Implement Logic (C#)**: You write *how* specific actions or checks are performed in `UserAlgorithms.cs`.
3.  **Generate**: Click **"Display Mock-up"** in the VReqDV Editor Window. This generates C# scripts, state machines, and attaches them to your objects.
4.  **Verify**: Play the scene to test interactions.
5.  **Iterate**: If you need changes, click "Save New Version" to create a fresh snapshot, then modify the new JSON/C# files.

### 2. Logic Separation
*   **behavior.json**: Defines the **High-Level Rules**. (e.g., "IF the Ball hits the Pin AND the Pin is standing, THEN run `SetPinFallen`").
*   **UserAlgorithms.cs**: Defines the **Low-Level Implementation**. (e.g., "Apply force to the rigidbody", "Play a sound", "Increase score"). wrapper functions that are called by the generated behavior scripts.

### 3. State Machines (For Beginners)
VReqDV uses **State Machines** to track object status. Think of a "State" as a **mode** or **tag** that an object is currently in.
*   *Example:* A Pin can be in the **"Standing"** state or the **"Fallen"** state.
*   *Usage:* You can check `IsState("Pin", "Standing")` to ensure a pin only falls if it's currently standing.
*   *Advantage:* This prevents bugs like a pin falling twice or a ball rolling while it's already moving.

### 4. Generated Code & Debugging
*   **Where is it?**: All generated logic lives in `Assets/VReqDV/Generated/`.
*   **Do NOT Edit**: These files are overwritten every time you click "Display Mock-up". Make changes in `UserAlgorithms.cs` or the JSON files instead.
*   **Debugging**:
    *   If an action doesn't run, check the **Console** for errors.
    *   Verify that **Object Names** in JSON match exactly with `UserAlgorithms.cs`.

---

## 1. article.json

The `article.json` file defines the GameObjects to be instantiated or managed in the scene. It contains a list of `articles`.

### Structure
```json
{
  "articles": [
    {
      "_objectname": "ObjectName",
      ... properties ...
    }
  ]
}
```

### Fields

| Field | Type | Description |
| :--- | :--- | :--- |
| `_objectname` | `string` | **Required.** The unique name of the GameObject. |
| `source` | `string` | The name of another article to inherit properties from. If defined, this object copies all fields from the source, which can then be overridden. |
| `shape` | `string` | The primitive shape or type. Valid values: `"Cube"`, `"Sphere"`, `"Capsule"`, `"Cylinder"`, `"Plane"`, `"Quad"`, `"empty"`. |
| `Transform_initialpos` | `object` | Position coordinates `{ "x": "0.0", "y": "0.0", "z": "0.0" }`. |
| `Transform_initialrotation` | `object` | Rotation Euler angles `{ "x": "0.0", "y": "0.0", "z": "0.0" }`. |
| `Transform_objectscale` | `object` | Scale `{ "x": "1.0", "y": "1.0", "z": "1.0" }`. |
| `states` | `string[]` | A list of state names (e.g., `["ready", "rolling"]`). These generate a state machine enum for the object. **Note:** The first state in this list is automatically assigned as the default initial state. |
| `context_img_source` | `string` | (Optional) Path to a prefab asset (e.g., `Assets/Prefabs/MyObject.prefab`). If provided, this prefab is instantiated instead of a primitive shape. |
<!-- | `HasChild` | `int` | Set to `1` to indicate this object is a group parent. Set to `0` otherwise. |
| `Children` | `string[]` | If `HasChild` is `1`, this list contains names of *existing* objects to group under this parent. The system will calculate the center and reparent them. | -->
| `XRRigidObject` | `object` | Configuration for the Rigidbody component. See below. |
| `Interaction` | `object` | Configuration for XR interaction capabilities (e.g., grab interactable). See below. |

### XRRigidObject Properties

| Field | Type | Description |
| :--- | :--- | :--- |
| `value` | `string` | "1" to add a Rigidbody, "0" to skip. |
| `mass` | `string` | Mass of the object (float). |
| `dragfriction` | `string` | Linear drag (float). |
| `angulardrag` | `string` | Angular drag (float). |
| `Isgravityenable` | `string` | "true" or "false". |
| `IsKinematic` | `string` | "true" or "false". |
| `CollisionPolling` | `string` | Collision detection mode. <br>Values: `"discrete"`, `"continuous"`, `"continuous-dynamic"`, `"continuous-speculative"`. |
| `CanInterpolate` | `string` | Interpolation mode. <br>`"0"`: None<br>`"1"`: Interpolate<br>`"2"`: Extrapolate |

### Interaction Properties

| Field | Type | Description |
| :--- | :--- | :--- |
| `XRGrabInteractable` | `string` | `"true"` to attach an `XRGrabInteractable` component, allowing the object to be picked up in VR. |
| `XRInteractionMaskLayer` | `string[]` | List of layer indices or names (e.g., `["Default", "Grab"]`) to set the interaction layer mask. |
| `TrackPosition` | `string` | `"true"` to track position when grabbed. |
| `TrackRotation` | `string` | `"true"` to track rotation when grabbed. |
| `Throw_Detach` | `string` | `"true"` to enable throwing physics upon detach. |

### Inheritance Logic
If `source` is specified:
1. The system finds the source article.
2. It copies `shape`, `Transform_*`, `XRRigidObject`, `Interaction`, `states`, `context_img_source`, from the source.
3. Any fields explicitly defined in the current article override the copied values.

---

## 2. behavior.json

The `behavior.json` file defines the logic rules, event triggers, and actions for the objects.

### Structure
```json
{
  "behaviors": [
    {
      "id": "BehaviorID",
      "event": "OnCondition",
      "actors": ["Obj1", "Obj2"],
      ...
    }
  ]
}
```

### Fields

| Field | Type | Description |
| :--- | :--- | :--- |
| `id` | `string` | **Required.** Unique Identifier for this behavior. Used as the base for the generated class name (e.g., `BehaviorID_Obj1`). |
| `event` | `string` | The trigger type.<br>`"OnCondition"`: Checks `precondition` every frame (Update loop).<br>`"OnStateChange"`: Triggered when the actor's state changes.<br>`"OnXRInteraction"`: Triggered by user XR interaction. Auto-attaches an `XRSimpleInteractable` (and a `BoxCollider` if missing) if no interactable is present on the actor. |
| `actors` | `string[]` | A list of explicit objects (actors) this behavior applies to. |
| `precondition` | `ConditionNode` | Logic tree that must evaluate to `true` for the action to run. |
| `action` | `ActionNode` | The action to execute if the event and precondition are met. |
| `postcondition` | `ConditionNode` | (Optional) Defines the expected state after the action. (Currently for documentation/verification). |

### ConditionNode

A condition node (precondition or postcondition) defines a logic check. It supports recursive nesting, meaning `all` and `any` can contain further `ConditionNode` objects.

#### Fields (Strict Usage)
You must use **exactly one** of the following logic types per condition node (or, one per recursive level of the condition node):

1.  **`all`** (Recursive AND)
    *   **Type**: `ConditionNode[]` (Array)
    *   **Behavior**: Returns `true` only if **ALL** child nodes evaluate to true.
    *   **Example**: `{ "all": [ { "equals": ... }, { "runAlgorithm": ... } ] }`

2.  **`any`** (Recursive OR)
    *   **Type**: `ConditionNode[]` (Array)
    *   **Behavior**: Returns `true` if **ANY** child node evaluates to true.
    *   **Example**: `{ "any": [ { "equals": ... }, { "equals": ... } ] }`

3.  **`equals`** (Comparison)
    *   **Type**: `string[]` (Array of exactly 2 strings)
    *   **Format**: `["LeftOperand", "RightOperand"]`
    *   **Supported Patterns**:
        *   **State Check**: `["ObjectName.state", "StateName"]`
            *   *Left*: Must be an object name followed by `.state` (e.g., `Ball.state`, `Self.state`).
            *   *Right*: A valid state name defined in `article.json` (e.g., `Rolling`).
            *   *Generates*: `ObjectTypeStateStorage.Get(obj) == ObjectTypeStateEnum.StateName`
        *   **Raw C# Comparison**: `["ExpressionA", "ExpressionB"]`
            *   *Behavior*: Injects raw C# code as `ExpressionA == ExpressionB`.
            *   *Warning*: Ensure expressions are valid C# (e.g., `["1", "1"]`).

4.  **`runAlgorithm`** (Custom Method)
    *   **Type**: `string` (Method Name)
    *   **Behavior**: Calls a `public static bool` method in `UserAlgorithms`.
    *   **Optional Field**: `params` (Dictionary)
        *   Maps arguments for the method.
        *   **Special Key**: `"obj": "Name"` converts to `GameObject.Find("Name")` in the generated code.

5.  **`XRinteraction`** (XR Event Check)
    *   **Type**: `string`
    *   **Behavior**: Used as a precondition check when the behavior event is `"OnXRInteraction"`. 
    *   **Valid Values**: Commonly `"select"` (fired when the object is selected/grabbed).
    *   **Example**: `{ "XRinteraction": "select" }`

### ActionNode
Defines what happens.

*   `"runAlgorithm": "MethodName"` - Calls a static void method in `UserAlgorithms` class.
*   `"params": { ... }` - Arguments passed to the method.

### Example Behavior
```json
{
    "id": "PinFall",
    "event": "OnCondition",
    "actors": ["Pin_1"],
    "precondition": {
        "all": [
            { "equals": ["Self.state", "standing"] },
            { "runAlgorithm": "IsPinFallen", "params": { "obj": "Self" } }
        ]
    },
    "action": {
        "runAlgorithm": "SetPinFallen",
        "params": { "obj": "Self" }
    }
}
```
This behavior applies to `Pin_1`. If `Self` (Pin_1) is standing AND `IsPinFallen(Pin_1)` is true, it runs `SetPinFallen(Pin_1)`.

### Inheritance in Behaviors (Hybrid Approach)
Behaviors are assigned to an object if:
1.  The object is explicitly listed in `actors`.
2.  **OR** The object inherits from an object in `actors` (via `source` in `article.json`).

**Example:**
- `behavior.json`: `actors: ["Pin_1"]`
- `article.json`: `Pin_2` has `"source": "Pin_1"`
- **Result:** Both `Pin_1` and `Pin_2` will get the behavior.

**Substitution Logic:**
The system generates a unique script for each actor (e.g., `PinFall_Pin_1.cs`, `PinFall_Pin_2.cs`). Inside these scripts:
- `Self` is replaced by the actual actor name (`Pin_1` or `Pin_2`).
- References to the *primary actor* (e.g., `Pin_1` in the logic) are also contextually replaced by the current actor to verify generic logic.

---

## 3. UserAlgorithms.cs

The `UserAlgorithms.cs` file contains the custom C# logic referenced by `behavior.json`.

### Rules

1.  **One File Per Version**: Each version folder (`specifications/version_XV/`, e.g., `version_1`) must contain its own `UserAlgorithms.cs`.
2.  **Namespace**: The class must be wrapped in a namespace matching the folder version name exactly (case-sensitive for consistency, though convention is PascalCase).
    *   Example: `Version_1`, `Version_2`.
3.  **Class Definition**: The class must be defined as `public static class UserAlgorithms`.
4.  **Function Rules**:
    *   Must be `public static`.
    *   **Conditions**: Return `bool`.
    *   **Actions**: Return `void`.
    *   **Arguments**: Can simply take standard types or `GameObject`. Use params in JSON to map inputs.

### State Access & Changes

To access or change object states, use the **`StateAccessor`** helper.

#### Accessing State
Use `VReqDV.StateAccessor.IsState` to check if an object is in a specific state.

```csharp
// bool IsState(string objectName, string stateName, GameObject obj, string versionNamespace)
if (VReqDV.StateAccessor.IsState("Ball", "Rolling", obj, "Version_1")) 
{
    // ...
}
```

#### Changing State
Use `VReqDV.StateAccessor.SetState` to transition an object to a new state.

```csharp
// void SetState(string objectName, string stateName, GameObject obj, string versionNamespace)
VReqDV.StateAccessor.SetState("Ball", "Stopped", obj, "Version_1");
```

**Note**: The `versionNamespace` parameter defaults to `"Version_1"` if omitted. For other versions, you **must** specify it (e.g., `"Version_2"`).

### Workflow & Troubleshooting

1.  **Write Logic**: Create `UserAlgorithms.cs` with your methods.
2.  **Define Behavior**: Use `runAlgorithm` tags in `behavior.json` to call these methods. Ensure names match exactly.
3.  **Compile**: Ensure there are no C# syntax errors.
4.  **Display Mock-up**: Click this button in the VReqDV Editor Window.
    *   This generates the actual `StateAPI` classes and behavior scripts.
    *   It attaches the scripts to scene objects.
5.  **Runtime Errors**:
    *   *State Accessor Warning:* Check console. You might have misspelled the object name or state name in your string.
    *   *KeyNotFound:* Ensure the object exists in `article.json` and has `states` defined.
