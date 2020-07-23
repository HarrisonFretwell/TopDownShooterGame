#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.SceneManagement;


namespace EModules
{


    [InitializeOnLoad]
    public static class AutoSaveHandler
    {

        static CachedFloat __m_saveInterval = new CachedFloat("EModules/AutoSave/auto2", 5);
        static float m_saveInterval {
            get { return (float)__m_saveInterval * 60; }
            set { __m_saveInterval.Set( value / 60 ); ; }
        }
        static CachedBool m_debug = new CachedBool("EModules/AutoSave/auto3", false);
        static float? launchTime;
        static CachedBool m_enable= new CachedBool("EModules/AutoSave/enablesave", true);
        static CachedInt m_filesCount= new CachedInt("EModules/AutoSave/auto1", 10);
        static CachedFloat __lastSave  = new CachedFloat("EModules/AutoSave/nextsave", 0);
        static float lastSave {
            get { return (float)__lastSave; }
            set { __lastSave.Set( value ); ; }
        }
        static float EDITOR_TIMER {
            get { return (float)(EditorApplication.timeSinceStartup % 1000000); }
        }


        static string AutoSaveFolder {
            get { return string.IsNullOrEmpty( GET_STRING( "Auto-Save Location" ) ) ? "AutoSave" : GET_STRING( "Auto-Save Location" ); }
            set { SET_STRING( "Auto-Save Location" , value ); }
        }
        static string autoSaveFileName {
            get {
                if ( !System.IO.Directory.Exists( Application.dataPath + "/" + AutoSaveFolder ) ) {
                    System.IO.Directory.CreateDirectory( Application.dataPath + "/" + AutoSaveFolder );
                    AssetDatabase.Refresh();
                }
                //if (!AssetDatabase.IsValidFolder("Assets/" + AutoSaveFolder)) AssetDatabase.CreateFolder("Assets", AutoSaveFolder);
                var files = System.IO.Directory.GetFiles(Application.dataPath + "/" + AutoSaveFolder).Select(f => f.Replace('\\', '/')).Where(f =>
                f.EndsWith(".unity") && f.Substring(f.LastIndexOf('/') + 1).StartsWith("AutoSave")).ToArray();
                if ( files.Length == 0 ) return "AutoSave_00";

                var times = files.Select(System.IO.File.GetCreationTime).ToList();
                var max = times.Max();
                var ind = times.IndexOf(max);
                var count = 0;
                files = files.Select( n => n.Remove( n.LastIndexOf( '.' ) ) ).ToArray();
                if ( int.TryParse( files[ind].Substring( files[ind].Length - 2 ) , out count ) ) {
                    count = (count + 1) % m_filesCount;
                    return "AutoSave_" + count.ToString( "D2" );
                }
                return "AutoSave_00";
            }
        }



        //* INITIALIZATION *//
        static AutoSaveHandler() {
            EditorApplication.update -= UpdateCS;
            EditorApplication.update += UpdateCS;

            resetSet();
        }

        static void resetSet() {
            //  if ( !HAS_KEY( "enablesave" ) ) SET_INT( "enablesave" , 1 );
            //  m_enable = GET_INT( "enablesave" ) == 1;

            //  if ( HAS_KEY( "auto1" ) ) {
            // m_filesCount = GET_INT( "auto1" );
            // m_saveInterval = GET_INT( "auto2" ) * 60;
            // m_debug = GET_BOOL( "auto3" );
        }
        //* INITIALIZATION *//



        //#if UNITY_2018_3_OR_NEWER

#if !UNITY_2018_3_OR_NEWER
        [PreferenceItem( "Auto-Save" )]
        public static void OnPreferencesGUI() {
            _OnPreferencesGUI( null );
        }
#else
        [SettingsProvider]
        static SettingsProvider MyNewPrefCode0() {
            var p = new MyPrefSettingsProvider("Preferences/Auto-Save" ,SettingsScope.User );
            p.keywords = new[] { "AutoSaveAuto SaveEmodulesei" };
            return p;
        }
        private class MyPrefSettingsProvider : SettingsProvider
        {
            public MyPrefSettingsProvider( string path , SettingsScope scopes = SettingsScope.User ) : base( path , scopes ) { }
            public override void OnGUI( string searchContext ) {
                _OnPreferencesGUI( searchContext );
            }
        }

#endif

        //* GUI *//

        public static void _OnPreferencesGUI( string searchContext ) {
            EditorGUILayout.LabelField( "Assets/" + AutoSaveFolder + " - Auto-Save Location" );
            var R = EditorGUILayout.GetControlRect(GUILayout.Height(30));
            GUI.Box( R , "" );
            R.x += 7;
            R.y += 7;
            m_enable.Set( EditorGUI.ToggleLeft( R , "Enable" , m_enable ) );
            GUI.enabled = m_enable;



            m_filesCount.Set( Mathf.Clamp( EditorGUILayout.IntField( "Maximum Files Version" , m_filesCount ) , 1 , 99 ) );
            m_saveInterval = Mathf.Clamp( EditorGUILayout.IntField( "Save Every (Minutes)" , (int)(m_saveInterval / 60) ) , 1 , 60 ) * 60;

            var location = EditorGUILayout.TextField("Location", AutoSaveFolder).Replace('\\', '/');
            if ( location.IndexOfAny( System.IO.Path.GetInvalidPathChars() ) >= 0 ) location = AutoSaveFolder;

            m_debug.Set( EditorGUILayout.Toggle( "Log" , m_debug ) );

            if ( GUI.changed ) {
                AutoSaveFolder = location;
                /*SET_INT( "enablesave" , m_enable ? 1 : 0 );
                 SET_INT( "auto1" , m_filesCount );
                 SET_INT( "auto2" , (int)(m_saveInterval / 60) );
                 SET_BOOL( "auto3" , m_debug );*/
                lastSave = (float)EDITOR_TIMER;
                resetSet();
            }
            GUI.enabled = true;
        }
        //* GUI *//



        static float speeder = 0;

        //* UPDATER *//
        public static void UpdateCS() {
            if ( !m_enable ) return;
            if ( Application.isPlaying ) {
                if ( launchTime == null ) launchTime = EDITOR_TIMER;
                return;
            }

            if ( launchTime != null ) {
                lastSave += (float)(EDITOR_TIMER - launchTime.Value);
                launchTime = null;
            }

            if ( Mathf.Abs( speeder - EDITOR_TIMER ) < 4 ) return;
            speeder = EDITOR_TIMER;

            if ( Mathf.Abs( lastSave - (float)EDITOR_TIMER ) >= m_saveInterval * 2 ) {
                lastSave = (float)EDITOR_TIMER;
                resetSet();
            }

            if ( Mathf.Abs( lastSave - (float)EDITOR_TIMER ) >= m_saveInterval ) {
                SaveScene();
                EditorApplication.update -= UpdateCS;
                EditorApplication.update += UpdateCS;
            }
        }



        static void SaveScene() {
            if ( !System.IO.Directory.Exists( Application.dataPath + "/" + AutoSaveFolder ) ) {
                System.IO.Directory.CreateDirectory( Application.dataPath + "/" + AutoSaveFolder );
                AssetDatabase.Refresh();
            }

            var relativeSavePath = "Assets/" + AutoSaveFolder + "/";
            EditorSceneManager.SaveScene( EditorSceneManager.GetActiveScene() , relativeSavePath + autoSaveFileName + ".unity" , true );
            var dif = (float)EDITOR_TIMER - lastSave - m_saveInterval;
            if ( dif < m_saveInterval && dif > 0 ) lastSave = (float)EDITOR_TIMER - dif;
            else lastSave = (float)EDITOR_TIMER;

        }
        //* UPDATER *//












        public class CachedBool
        {

            public CachedBool( string key , bool defaultValue ) {
                this.key = key;
                this.defaultValue = defaultValue ? 1 : 0;
                this.lastDif = -1;
            }
            string key;
            int lastDif;
            int defaultValue;
            bool CurrentDif {
                get {
                    if ( lastDif == -1 ) lastDif = EditorPrefs.GetInt( key , defaultValue );
                    return lastDif == 1;
                }
                set {
                    if ( lastDif == -1 ) lastDif = EditorPrefs.GetInt( key , defaultValue );
                    if ( lastDif == (value ? 1 : 0) ) return;
                    lastDif = value ? 1 : 0;
                    EditorPrefs.SetInt( key , value ? 1 : 0 );
                }
            }

            public static implicit operator bool( CachedBool d ) {
                return d.CurrentDif;
            }

            internal void Set( bool i ) {
                CurrentDif = i;
            }
        }
        public class CachedFloat
        {

            public CachedFloat( string key , float defaultValue ) {
                this.key = key;
                this.defaultValue = defaultValue;
                this.lastDif = -1;
            }
            string key;
            float lastDif;
            float defaultValue;
            float CurrentDif {
                get {
                    if ( lastDif == -1 ) lastDif = EditorPrefs.GetFloat( key , defaultValue );
                    return lastDif;
                }
                set {
                    if ( lastDif == -1 ) lastDif = EditorPrefs.GetFloat( key , defaultValue );
                    if ( lastDif == value ) return;
                    lastDif = value;
                    EditorPrefs.SetFloat( key , value );
                }
            }

            public static implicit operator float( CachedFloat d ) {
                return d.CurrentDif;
            }

            internal void Set( float i ) {
                CurrentDif = i;
            }
        }

        public class CachedInt
        {

            public CachedInt( string key , int defaultValue ) {
                this.key = key;
                this.defaultValue = defaultValue;
                this.lastDif = -1;
            }
            string key;
            int lastDif;
            int defaultValue;
            int CurrentDif {
                get {
                    if ( lastDif == -1 ) lastDif = EditorPrefs.GetInt( key , defaultValue );
                    return lastDif;
                }
                set {
                    if ( lastDif == -1 ) lastDif = EditorPrefs.GetInt( key , defaultValue );
                    if ( lastDif == value ) return;
                    lastDif = value;
                    EditorPrefs.SetInt( key , value );
                }
            }

            public static implicit operator int( CachedInt d ) {
                return d.CurrentDif;
            }

            internal void Set( int i ) {
                CurrentDif = i;
            }
        }



        static string GET_STRING( string key ) {
            return EditorPrefs.GetString( "EModules/AutoSave/" + key );
        }
        static void SET_STRING( string key , string value ) {
            EditorPrefs.SetString( "EModules/AutoSave/" + key , value );
        }

    }
}
#endif










//* private *//

//* private *//



//* editorprefs *//
/*static float GET_FLOAT( string key ) {
    return EditorPrefs.GetFloat( "EModules/AutoSave/" + key );
}
static void SET_FLOAT( string key , float value ) {
    EditorPrefs.SetFloat( "EModules/AutoSave/" + key , value );
}
static string GET_STRING( string key ) {
    return EditorPrefs.GetString( "EModules/AutoSave/" + key );
}
static void SET_STRING( string key , string value ) {
    EditorPrefs.SetString( "EModules/AutoSave/" + key , value );
}
static int GET_INT( string key ) {
    return EditorPrefs.GetInt( "EModules/AutoSave/" + key );
}
static void SET_INT( string key , int value ) {
    EditorPrefs.SetInt( "EModules/AutoSave/" + key , value );
}
static bool GET_BOOL( string key ) {
    return EditorPrefs.GetBool( "EModules/AutoSave/" + key );
}
static void SET_BOOL( string key , bool value ) {
    EditorPrefs.SetBool( "EModules/AutoSave/" + key , value );
}
static bool HAS_KEY( string key ) {
    if ( EditorPrefs.HasKey( "EModules/AutoSave/" + key ) ) return true;
    return false;
    / * if (EditorPrefs.HasKey( "AutoSave/" + key )) return true;
     return EditorPrefs.HasKey( key );* /
}*/
//* editorprefs *//


//* props *//
/*  static float lastSave {
      get { return GET_FLOAT( "nextsave" ); }
      set { EditorPrefs.SetFloat( "nextsave" , value ); }

  }*/












/*  class MyCustomSettingsProvider : SettingsProvider
{
  private SerializedObject m_CustomSettings;


  public MyCustomSettingsProvider( string path , SettingsScope scope = SettingsScope.Project )
      : base( path , scope ) { }

  public static bool IsSettingsAvailable() {
      return true;
  }

  public override void OnActivate( string searchContext , UnityEngine.UIElements.VisualElement rootElement ) {
  }

  public override void OnGUI( string searchContext ) {
      _OnPreferencesGUI( searchContext );
  }

  // Register the SettingsProvider
  [SettingsProvider]
  public static SettingsProvider CreateMyCustomSettingsProvider() {
      if ( IsSettingsAvailable() ) {
          var provider = new MyCustomSettingsProvider("Project/Auto-Save", SettingsScope.Project);

          // Automatically extract all keywords from the Styles.
          provider.keywords = new HashSet<string>( new[] { "AutoSave" , "Auto" , "Save" , "Auto-Save" } );
          return provider;
      }

      // Settings Asset doesn't exist yet; no need to display anything in the Settings window.
      return null;
  }
}*/
/*    static class MyCustomSettingsIMGUIRegister
    {

    }
    [SettingsProvider]
    public static SettingsProvider CreateMyCustomSettingsProvider() {
        // First parameter is the path in the Settings window.
        // Second parameter is the scope of this setting: it only appears in the Project Settings window.
        var provider = new SettingsProvider("Preferences/Auto-Save", SettingsScope.Project)
      {
            // By default the last token of the path is used as display name if no label is provided.
            label = "Auto-Save",
            // Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
            guiHandler = _OnPreferencesGUI,
            // Populate the search keywords to enable smart search filtering and label highlighting:
            keywords = new HashSet<string>(new[] { "AutoSave", "Auto", "Save" , "Auto-Save" })
        };
        ( "ASD" );
        return provider;
    }*/
