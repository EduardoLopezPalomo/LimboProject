/*
 * AbilityNameAttribute.cs
 *
 * Marks a string field that holds an ability RUNTIME NAME (see AbilityIds) so the
 * Inspector draws it as a registry dropdown instead of a free-text box
 * (AbilityNamePropertyDrawer, T-008 step 4).
 *
 * The field stays a plain string: nothing serialized changes, existing assets load
 * unchanged. This closes the authoring typo at the source for AttackDefinition.
 * Behavior-graph blackboards are the same class of risk and stay open until the
 * step-3 graph migration (gated behind documenting the graphs).
 */
using UnityEngine;

/// <summary>Draw this string field as a dropdown of AbilityIds.All.</summary>
public sealed class AbilityNameAttribute : PropertyAttribute
{
}
