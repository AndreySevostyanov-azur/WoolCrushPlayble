using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Azur.PlayableTemplate.Logger
{
    public static class LogDrawer
    {
        public static event Action<Log> DeletePressed;

        public static void Draw(List<Log> logs)
        {
            foreach (var log in logs)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(log.Name.ToString());
                DeleteButton(log);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginVertical(GUI.skin.window);
                log.Message = EditorGUILayout.TextField("Log Message", log.Message);
                log.IsAutoCountable = EditorGUILayout.Toggle("Is Auto Countable", log.IsAutoCountable);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
            }
        }

        private static void DeleteButton(Log log)
        {
            if (GUILayout.Button("X", GUILayout.Width(40), GUILayout.Height(40)))
            {
                DeletePressed?.Invoke(log);
            }
        }
    }
}
