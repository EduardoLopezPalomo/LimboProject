using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Quest))]
public class QuestEditor : Editor
{
    public override void OnInspectorGUI()
    {
        
        DrawDefaultInspector();

        Quest data = (Quest)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Create quest Id"))
        {
            data.CreateId();
            EditorUtility.SetDirty(data); 
        }
    }
}
