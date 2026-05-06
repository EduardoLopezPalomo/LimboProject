using System;
using Unity.Behavior;

[BlackboardEnum]
public enum NPCState
{
	Default,
	GoingToWork,
	Working,
	StartingToRest,
	Resting,
	Talking
}
