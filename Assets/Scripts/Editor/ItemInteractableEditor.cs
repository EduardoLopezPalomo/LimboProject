using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(QuestItemInteractable))]
public class ItemInteractableEditor : Editor
{
    public override void OnInspectorGUI()
    {
        
        DrawDefaultInspector();

        QuestItemInteractable data = (QuestItemInteractable)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Create Item Id"))
        {
            data.CreateId();
            EditorUtility.SetDirty(data); 
        }
    }
}
