using ArabicSupport;
using UnityEditor;
using UnityEngine;

public class LanguageFixerMenu : EditorWindow
{
	private FixableLanguages _language;
	private string _initialText = "";
	private string _fixedText = "";
	private Vector2 _scroll;


	[MenuItem("Window/Language Fixer ")]

	public static void ShowWindow()
	{
		EditorWindow.GetWindow(typeof(LanguageFixerMenu));
	}

	void OnGUI()
	{
		GUIStyle style = new GUIStyle(EditorStyles.textArea);
		style.wordWrap = true;

		_language = (FixableLanguages)EditorGUILayout.EnumPopup(_language);
		_scroll = EditorGUILayout.BeginScrollView(_scroll);
		GUILayout.Label("Input Initial Text", EditorStyles.boldLabel);
		_initialText = EditorGUILayout.TextArea(_initialText, style);

		GUILayout.Label("Fixed Text Output", EditorStyles.boldLabel);

		_fixedText = EditorGUILayout.TextArea(_fixedText, style);

		EditorGUILayout.EndScrollView();


		if (GUILayout.Button("Fix"))
		{
			switch (_language)
			{
				case FixableLanguages.Arabic:
					_fixedText = ArabicFixer.Fix(_initialText);
					break;
				case FixableLanguages.Hindi:
					_fixedText = HindiSupport.Fix(_initialText);
					break;

			}
		}

	}


}

public enum FixableLanguages
{
	Arabic,
	Hindi
}
