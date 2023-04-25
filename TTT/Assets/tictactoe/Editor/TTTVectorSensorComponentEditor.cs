using UnityEditor;
using Unity.MLAgents.Sensors;

namespace TTT
{
    
    [CustomEditor(typeof(TTTVectorSensorComponent), editorForChildClasses: true)]
    [CanEditMultipleObjects]
    internal class TTTVectorSensorComponentEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var so = serializedObject;
            so.Update();

            EditorGUILayout.PropertyField(so.FindProperty("m_SensorName"), true);
            EditorGUILayout.PropertyField(so.FindProperty("m_ObservationSize"), true);
            EditorGUILayout.PropertyField(so.FindProperty("m_ObservationType"), true);

            so.ApplyModifiedProperties();
        }
    }
}
