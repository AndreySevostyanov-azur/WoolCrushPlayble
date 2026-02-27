using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Azur.PlayableTemplate.Logger
{
    [CustomEditor(typeof(EventLogger))]
    public class LoggerEditor : Editor
    {
        private const string _enumFile = "LogName";

        private string _logsCount = "0";
        private string _pathToEnumFile;
        private List<Log> _newLogs = new List<Log>();
        private EventLogger _logger;

        private void OnEnable()
        {
            LogDrawer.DeletePressed += RemoveLog;
            _pathToEnumFile = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets(_enumFile)[0]);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            _logger = (EventLogger)target;

            var logs = _logger.Logs;
            logs = RefreshLogs(logs);
            for (var i = 0; i < logs.Count; i++)
            {
                var log = logs[i];
                log.Name = (LogName)i;
                log.CachedName = ((LogName)i).ToString();
            }

            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Clear All Logs", GUILayout.Width(150f)))
            {
                RemoveAllLogs();
            }
            GUILayout.EndHorizontal();
            
            LogDrawer.Draw(logs);

            NewLogSelection();

            if (GUILayout.Button("Add"))
            {
                AddLogs();
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(_logger.gameObject);
                EditorSceneManager.MarkSceneDirty(_logger.gameObject.scene);
            }

            _logger.Logs = logs;
        }

        private List<Log> RefreshLogs(List<Log> oldLogs)
        {
            int countLog = Enum.GetNames(typeof(LogName)).Length;
            List<Log> logs = new List<Log>(countLog);

            for (int i = 0; i < countLog; i++)
            {
                var enumName = (LogName)i;
                Log log = TryRestoreLog(oldLogs, enumName);
                if (log == null)
                {
                    log = CreateNewLog(enumName);
                }

                logs.Add(log);
            }

            return logs;
        }

        private void NewLogSelection()
        {
            _logsCount = EditorGUILayout.TextField("Logs Count", _logsCount);

            if (string.IsNullOrEmpty(_logsCount))
            {
                _newLogs.Clear();
                return;
            }

            var countClipsField = 0;

            if (int.TryParse(_logsCount, out countClipsField))
            {
                for (int i = _newLogs.Count; i < countClipsField; i++)
                {
                    _newLogs.Add(new Log());
                }

                for (int i = 0; i < _newLogs.Count; i++)
                {
                    if (countClipsField <= i)
                        continue;

                    var log = _newLogs[i];

                    log.CachedName = EditorGUILayout.TextField("Name", log.CachedName);
                    
                    if (CheckLogName(log.CachedName) == false)
                    {
                        EditorGUILayout.HelpBox($"Invalid log name {log.CachedName}!", MessageType.Error);
                        continue;
                    }

                    log.Message = EditorGUILayout.TextField("Message", log.Message);
                    log.IsAutoCountable = EditorGUILayout.Toggle("Is Auto Countable", log.IsAutoCountable);
                    
                    EditorGUILayout.Space();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("ТОЛЬКО ЦЕЛЫЕ ЧИСЛА!", MessageType.Error);
            }
        }

        private bool CheckLogName(string name)
        {
            if (name == null)
            {
                return false;
            }

            return Regex.IsMatch(name, @"^[a-zA-Z][a-zA-Z0-9_]*$");
        }

        private static Log TryRestoreLog(List<Log> oldLogs, LogName name)
        {
            return oldLogs.FirstOrDefault(o => o.CachedName == name.ToString());
        }

        private void AddLogs()
        {
            if (_newLogs.Count == 0)
                return;

            var countClipsField = 0;

            for (int i = 0; i < _newLogs.Count; i++)
            {
                if (!Regex.IsMatch(_newLogs[i].CachedName, @"^[a-zA-Z][a-zA-Z0-9_]*$"))
                {
                    EditorGUILayout.HelpBox($"Invalid log name {_newLogs[i].CachedName}!", MessageType.Error);
                    return;
                }

                if (int.TryParse(_logsCount, out countClipsField))
                {
                    if (countClipsField <= i)
                        break;
                }

                EnumEditor.WriteToFile(_newLogs[i].CachedName, _pathToEnumFile);
            }

            Refresh();
        }

        private void RemoveLog(Log log)
        {
            if (!EnumEditor.TryRemoveFromFile(log.Name.ToString(), _pathToEnumFile))
                return;

            Refresh();
        }

        private void RemoveAllLogs()
        {
            foreach (var log in _logger.Logs)
            {
                if (!EnumEditor.TryRemoveFromFile(log.Name.ToString(), _pathToEnumFile))
                    return;
            }
            
            Refresh();
        }

        private void Refresh()
        {
            Debug.Log("WAIT");
            var relativePath = _pathToEnumFile.Substring(_pathToEnumFile.IndexOf("Assets"));
            AssetDatabase.ImportAsset(relativePath);
        }

        private Log CreateNewLog(LogName enumName)
        {
            var isLogsEmpty = _newLogs.Count == 0;
            var log = new Log(
                enumName, 
                isLogsEmpty ? null : _newLogs.First().Message,
                isLogsEmpty ? false :_newLogs.First().IsAutoCountable);
            
            if (!isLogsEmpty)
                _newLogs.Remove(_newLogs.First());

            _logsCount = "";

            return log;
        }

        private void OnDisable()
        {
            LogDrawer.DeletePressed -= RemoveLog;
        }
    }
}
