using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NPCDialogueData))]
public class NpcDialogueDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        
        DrawDefaultInspector();

        NPCDialogueData data = (NPCDialogueData)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Create NPC Id"))
        {
            data.CreateNpcId();
            EditorUtility.SetDirty(data); 
        }
    }
}
