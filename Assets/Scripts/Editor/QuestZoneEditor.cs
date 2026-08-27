using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(QuestZoneTrigger))]
public class QuestZoneEditor : Editor
{
    public override void OnInspectorGUI()
    {
        
        DrawDefaultInspector();

        QuestZoneTrigger data = (QuestZoneTrigger)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Create Zone Id"))
        {
            data.CreateId();
            EditorUtility.SetDirty(data); 
        }
    }
}
