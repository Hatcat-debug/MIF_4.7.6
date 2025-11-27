using UnityEditor;
using UnityEngine;

namespace InvertedWorldAssets.Scripts.Tube.Editor
{
    [CustomEditor(typeof(EventSignal))]
    public class TubeEventEditor : UnityEditor.Editor
    {
        private SerializedProperty _cmdProp;
        private SerializedProperty _angleProp;
        private SerializedProperty _swapMatProp;
        private SerializedProperty _nextMatProp;
        private SerializedProperty _genRefProp;

        private void OnEnable()
        {
            _cmdProp = serializedObject.FindProperty("Command");
            _angleProp = serializedObject.FindProperty("DeltaAngle");
            _swapMatProp = serializedObject.FindProperty("SwapMaterial");
            _nextMatProp = serializedObject.FindProperty("NextMaterial");
            _genRefProp = serializedObject.FindProperty("TargetGenerator");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_cmdProp);

            TubeCommand type = (TubeCommand)_cmdProp.enumValueIndex;

            if (type == TubeCommand.ShiftOrbit)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(5);
                
                EditorGUILayout.LabelField("Orbit Params", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_angleProp, new GUIContent("Delta Angle"));
                EditorGUILayout.PropertyField(_swapMatProp, new GUIContent("Switch Material?"));

                if (_swapMatProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.HelpBox("Will switch material on the target generator.", MessageType.None);
                    EditorGUILayout.PropertyField(_genRefProp, new GUIContent("Generator"));
                    EditorGUILayout.PropertyField(_nextMatProp, new GUIContent("Material Asset"));
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }
            else if (type == TubeCommand.Land)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("Wait for Normal Up alignment, then release.", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}