using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Resolves an <see cref="InputAction"/> by name from the action map a <see cref="PlayerInput"/>
/// currently has active, and keeps that result until the map changes.
///
/// Why this exists (T-046 B4): three gameplay scripts - <c>NpcPlayerDetection</c>,
/// <c>DialogueUI</c> and <c>QuestItemInteractable</c> - called
/// <c>playerInput.currentActionMap.FindAction("Interact")</c> from <c>Update</c>, so each of them ran a
/// name lookup over the map's actions once per frame. With a crowd of interactable NPCs that is
/// thousands of string lookups a second for an answer that only changes when the player switches
/// action maps (dialogue switches to "UI" and back). The map reference is the invalidation key, so a
/// map switch re-resolves on the next read and nothing can go stale.
///
/// A null player or a null map is not cached: the lookup is simply attempted again next frame, so a
/// late-assigned <c>PlayerInput</c> starts working on its own. The lookup itself is still
/// <see cref="InputActionMap.FindAction(string)"/> with its default <c>throwIfNotFound</c>, so an action
/// name that does not exist is still as loud as it always was - once per map instead of once per frame.
/// </summary>
public class CachedInputAction
{
    private readonly string actionName;
    private InputActionMap resolvedFromMap;
    private InputAction resolvedAction;

    public CachedInputAction(string actionName)
    {
        this.actionName = actionName;
    }

    /// <summary>The action from the player's current map, or null while there is no map to read it from.</summary>
    public InputAction Get(PlayerInput input)
    {
        if (input == null) return null;

        InputActionMap map = input.currentActionMap;
        if (map == null) return null;

        if (!ReferenceEquals(map, resolvedFromMap))
        {
            resolvedFromMap = map;
            resolvedAction = map.FindAction(actionName);
        }

        return resolvedAction;
    }
}
