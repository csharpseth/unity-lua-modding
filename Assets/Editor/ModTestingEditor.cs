using UnityEditor;
using UnityEngine;

public class ModTestingEditor : EditorWindow
{
    [MenuItem("Modding/Editor Debug")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        ModTestingEditor window = (ModTestingEditor)GetWindow(typeof(ModTestingEditor));
        window.Show();
    }

    private void OnGUI()
    {
        string loadedInfo = "Loaded {0} Scripts  ::  Initialized:{1}";
        loadedInfo = string.Format(loadedInfo, ModManager.NumScriptsLoaded, ModManager.ScriptsInitialized);
        string notLoaded = "No Scripts Loaded";


        GUILayout.Label((ModManager.ScriptsLoaded == true) ? loadedInfo : notLoaded);
        if (GUILayout.Button((ModManager.ScriptsLoaded) ? "Reload Mods" : "Load Mods"))
            LoadScripts();

        if(ModManager.ScriptsLoaded && ModManager.ScriptsInitialized == false)
            if (GUILayout.Button("Init Scripts"))
                InitScripts();

    }

    private void LoadScripts()
    {
        ModManager.LoadAllScriptDatas();
        ModManager.GenerateAllScripts();

        int iterations = 0;
        while (ModManager.VerifyScripts() == false || iterations < 10000) { iterations++; }
    }

    private void InitScripts()
    {
        ModManager.InitScripts();
    }
}
