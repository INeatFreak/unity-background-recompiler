#if UNITY_EDITOR

// Made by INeatFreak
// Asset Store: https://u3d.as/2W4H

using System.IO;
using UnityEngine;
using UnityEditor;

namespace Plugins.BackgroundRecompiler
{
	public static class BackgroundRecompiler
	{
		// constants
        public const string Version = "1.0.0";
		public const string AssetURL = "https://u3d.as/2W4H";

		// variables
		public static bool Enabled = true;
		public static bool LogWhenBackgroundCompiled = true;
		private static bool shouldRecompile = false;


        // gets called when the editor is started and after the editor is recompile
        [InitializeOnLoadMethod]
		private static void Initialize()
		{
			LoadPrefs();
			
			// watch for any file changes with the .cs extension
			var watcher = new FileSystemWatcher(Application.dataPath, "*.cs")
			{
				NotifyFilter =
					NotifyFilters.LastAccess	|
					NotifyFilters.LastWrite		|
					NotifyFilters.FileName		|
					NotifyFilters.DirectoryName	,
				IncludeSubdirectories = true	,
				EnableRaisingEvents	= true		,
			};
			watcher.Changed += OnScriptFileChange;
			watcher.Created += OnScriptFileChange;
			watcher.Deleted += OnScriptFileChange;
			watcher.Renamed += OnScriptFileChange;


			// Log("Auto background compiler is active.");
			
			EditorApplication.update += OnUpdate;
		}

		private static void OnScriptFileChange(object sender, FileSystemEventArgs e) {
			shouldRecompile = true;
		}

		private static void OnUpdate()
		{
			if (Enabled == false) {
				shouldRecompile = false; // to prevent recompile when changes are made while and enabled again
				return;
			}

			if (shouldRecompile == false) return;
			if (EditorApplication.isCompiling) return;
			if (EditorApplication.isUpdating) return;

			if (LogWhenBackgroundCompiled) {
				Log("Changes detected in background! Auto Recompiling...");
			}

			// Calling Refresh will be enough for unity
			//   to detect script changes and recompile
			// Must be called inside editor update loop!
			AssetDatabase.Refresh();

			shouldRecompile = false;
		}



		#region Preferences
			private const string PREFS_KEY = "BACKGROUND_RECOMPILER_";
			private const string PREFS_KEY_ENABLED = PREFS_KEY + "ENABLED";
			private const string PREFS_KEY_LOG_COMPILES = PREFS_KEY + "LOG_COMPILES";

			public static void SavePrefs()
			{
				EditorPrefs.SetBool(PREFS_KEY_ENABLED, Enabled);
				EditorPrefs.SetBool(PREFS_KEY_LOG_COMPILES, LogWhenBackgroundCompiled);
			}
			public static void LoadPrefs()
			{
				Enabled = EditorPrefs.GetBool(PREFS_KEY_ENABLED, Enabled);
				LogWhenBackgroundCompiled = EditorPrefs.GetBool(PREFS_KEY_LOG_COMPILES, LogWhenBackgroundCompiled);
			}
		#endregion Preferences
		
		
		// custom logging
		private const string LOG_PREFIX = "[ Background Recompiler ] - ";
		private const string LOG_POSTFIX = "\n\n- You can disable this log in the preferences window @\"Preferences/Plugins/Background Recompiler\".\n";

		private static void Log(string message)
		{
			UnityEngine.Debug.Log(LOG_PREFIX + message + LOG_POSTFIX);
		}
		private static void LogError(string message)
		{
			UnityEngine.Debug.LogError(LOG_PREFIX + message + LOG_POSTFIX);
		}
		


		#if PACKAGE_SETTINGS_MANAGER	// this define is declared in assembly definition file (located in the same directory) when the package is installed in the project.

			// Preferences Window Entry
			internal class PluginPrefsProvider : SettingsProvider
			{
				private const string PreferencePath = "Plugins/Background Recompiler";
				

				private static PluginPrefsProvider provider;


				private PluginPrefsProvider(string path, SettingsScope scope)
					: base(path, scope) {}


				public override void OnGUI(string searchContext)
				{
					EditorGUI.BeginChangeCheck();

					GUILayout.Space(10);

					EditorGUILayout.BeginHorizontal();
					GUILayout.Space(10);
					EditorGUIUtility.labelWidth += 100;
					EditorGUILayout.BeginVertical();

					// draw pref fields
					BackgroundRecompiler.Enabled = EditorGUILayout.Toggle("Enabled", BackgroundRecompiler.Enabled);
					BackgroundRecompiler.LogWhenBackgroundCompiled = EditorGUILayout.Toggle("Log When Background Compiled", BackgroundRecompiler.LogWhenBackgroundCompiled);

					EditorGUILayout.EndVertical();
					EditorGUIUtility.labelWidth -= 100;
					GUILayout.Space(10);
					EditorGUILayout.EndHorizontal();

					GUILayout.FlexibleSpace();

					GUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
					string label = "Background Recompiler v" + BackgroundRecompiler.Version + " by INeatFreak";
					if (GUILayout.Button(new GUIContent(label, "Click to open the Asset Store page!"), EditorStyles.miniLabel)) {
						Application.OpenURL(AssetURL);
					}
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();

					GUILayout.Space(5);

					if(EditorGUI.EndChangeCheck()) {
						BackgroundRecompiler.SavePrefs();
					}
				}


				[SettingsProvider]
				private static SettingsProvider GetSettingsProvider()
				{
					if (provider == null) {
						provider = new PluginPrefsProvider(PreferencePath, SettingsScope.User);
					}
					
					return provider;
				}
			}

		#else // when Settings Manager package not included

			// Preferences Menu Item
			private const string MENU_ITEM_PATH = "Plugins/Background Recompiler/";
			private const string MENU_ITEM_ENABLED = MENU_ITEM_PATH + "Enabled";
			private const string MENU_ITEM_LOG_COMPILES = MENU_ITEM_PATH + "Log Compiles";
			
			[MenuItem(MENU_ITEM_ENABLED)]
			private static void ToggleOnOff() {
				Enabled = !Enabled;
				SavePrefs();
				
				Menu.SetChecked(MENU_ITEM_ENABLED, Enabled);
			}

			[MenuItem(MENU_ITEM_LOG_COMPILES)]
			private static void ToggleLogCompiles() {
				LogWhenBackgroundCompiled = !LogWhenBackgroundCompiled;
				SavePrefs();
				
				Menu.SetChecked(MENU_ITEM_LOG_COMPILES, LogWhenBackgroundCompiled);
			}

			// add dependend packages
			[MenuItem("Help/Plugins/Background Recompiler/Fix")]
			private static void HelpFixPlugin()
			{
				if (EditorUtility.DisplayDialog("Missing Dependencies", "Plugin needs SettingsManager package to create preferences window. Import now?", "Yes", "No")) {

					var request = UnityEditor.PackageManager.Client.Add("com.unity.settings-manager@1.0.0");
					while (!request.IsCompleted) {
						if (request.Status == UnityEditor.PackageManager.StatusCode.Failure) {
							LogError("Failed to add package: " + request.Error.message);
							break;
						}
					}
				}
			}
		#endif

	}
}

#endif