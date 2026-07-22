// Editor/GameManagerEditor.cs
using UnityEngine;
using UnityEditor;
using System.IO;

namespace GMTK.PlatformerToolkit {

    [CustomEditor(typeof(GameManager))]
    public class GameManagerEditor : Editor {

        public override void OnInspectorGUI() {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Save File Tools", EditorStyles.boldLabel);

            if (GUILayout.Button("Delete Save File")) {
                string savePath = GetSavePath();
                if (File.Exists(savePath)) {
                    File.Delete(savePath);
                    Debug.Log($"Deleted save file at: {savePath}");
                } else {
                    Debug.Log("No save file found to delete.");
                }
            }

            if (GUILayout.Button("Open Save File Location")) {
                string savePath = GetSavePath();
                string folder = Path.GetDirectoryName(savePath);
                if (Directory.Exists(folder)) {
                    EditorUtility.RevealInFinder(folder);
                } else {
                    Debug.Log("Save folder doesn't exist yet.");
                }
            }

            if (Application.isPlaying) {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Runtime Tools", EditorStyles.boldLabel);

                if (GUILayout.Button("Reset Save & Reload")) {
                    string savePath = GetSavePath();
                    if (File.Exists(savePath)) File.Delete(savePath);
                    GameManager.Instance.LoadGame();
                    Debug.Log("Save reset and reloaded.");
                }

                if (GUILayout.Button("Print Current Save Data")) {
                    Debug.Log(JsonUtility.ToJson(GameManager.Instance.SaveData, prettyPrint: true));
                }
            }
        }

        private string GetSavePath() {
            // Mirror the same logic as GameManager.SavePath
            string editorSavePath = serializedObject
                .FindProperty("editorSavePath").stringValue;
            string folder = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                editorSavePath
            );
            return Path.Combine(folder, "save.json");
        }
    }
}
