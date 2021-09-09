using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using System.IO;
using System;
using System.Threading;
using Unity.Mathematics;

public enum ScriptScope
{
	Client,
	Server,
	Shared
}

public struct ScriptData
{
	public string pathToScript;
	public string scriptContent;
	public ScriptScope scriptScope;
}

public static class ModManager
{
	public static readonly string INIT_FUNCTION = "OnInit";
	public static readonly string UPDATE_FUNCTION = "OnUpdate";
	public static readonly string GUI_FUNCTION = "OnGUI";

	public static readonly string MODS_PATH = Path.Combine(Application.streamingAssetsPath, "Mods");
	public static bool ScriptsLoaded { get { return m_readyToExecute; } }
	public static int NumScriptsLoaded { get { return m_numLoadedScripts; } }
	public static bool ScriptsInitialized { get { return m_scriptsInitialized; } }

	// Stores important information about a Script
	private static ScriptData[] m_clientScriptDatas;  // These are intended to be executed on the Client ONLY
	private static ScriptData[] m_serverScriptDatas;  // These are intended to be executed on the Server ONLY
	private static ScriptData[] m_sharedScriptDatas;  // These are intended to be executed on Client & Server

	// 'MoonSharp.Interpreter.Script' used to interpret LUA text as functionality that can be acted on in C#
	private static Script[] m_clientScripts;
	private static Script[] m_serverScripts;
	private static Script[] m_sharedScripts;

	private static DynValue[] m_clientUpdateFunctions;
	private static DynValue[] m_serverUpdateFunctions;
	private static DynValue[] m_sharedUpdateFunctions;

	private static DynValue[] m_clientGUIFunctions;

	private static bool m_readyToExecute = false;
	private static bool m_scriptsInitialized = false;
	private static int m_numLoadedScripts = 0;

	//Used to determine what scripts to load and execute[  Server=Server + Shared Code,   Client=Client + Shared Code,   Shared(for testing)=Client + Server + Shared Code  ]
	private static ScriptScope m_scope = ScriptScope.Shared;


	public static void LoadAllScriptDatas()
	{
		m_readyToExecute = false;
		m_scriptsInitialized = false;

		string[] scriptPaths = Directory.GetFiles(MODS_PATH, "*.lua", SearchOption.AllDirectories);
		string[] scriptNames = new string[scriptPaths.Length];


		//Get Script Names
		for (int i = 0; i < scriptPaths.Length; i++)
		{
			scriptNames[i] = StripPath(scriptPaths[i]);
		}

		//Sort & Load Scripts by Scope
		List<ScriptData> clientScripts = new List<ScriptData>();
		List<ScriptData> serverScripts = new List<ScriptData>();
		List<ScriptData> sharedScripts = new List<ScriptData>();

		for (int i = 0; i < scriptPaths.Length; i++)
		{
			string path = scriptPaths[i];
			string content = File.ReadAllText(path);

			if(scriptNames[i].StartsWith("cl_"))
			{
				if (m_scope == ScriptScope.Server) return;

				ScriptData data = new ScriptData()
				{
					pathToScript = path,
					scriptContent = content,
					scriptScope = ScriptScope.Client
				};

				clientScripts.Add(data);
			}else if(scriptNames[i].StartsWith("sv_"))
			{
				if (m_scope == ScriptScope.Client) return;

				ScriptData data = new ScriptData()
				{
					pathToScript = path,
					scriptContent = content,
					scriptScope = ScriptScope.Server
				};

				serverScripts.Add(data);
			}
			else if(scriptNames[i].StartsWith("sh_"))
			{
				ScriptData data = new ScriptData()
				{
					pathToScript = path,
					scriptContent = content,
					scriptScope = ScriptScope.Server
				};

				sharedScripts.Add(data);
			}
		}

		m_clientScriptDatas = clientScripts.ToArray();
		m_serverScriptDatas = serverScripts.ToArray();
		m_sharedScriptDatas = sharedScripts.ToArray();

		int clScripts = m_clientScriptDatas.Length;
		int svScripts = m_serverScriptDatas.Length;
		int shScripts = m_sharedScriptDatas.Length;
		m_numLoadedScripts = clScripts + svScripts + shScripts;

		Debug.LogFormat("Loaded {0} Scripts: {1}x Client Scripts, {2}x Server Scripts, {3}x Shared Scripts", m_numLoadedScripts, clScripts, svScripts, shScripts);
	}
	public static void GenerateAllScripts()
	{
		m_readyToExecute = false;
		m_scriptsInitialized = false;

		m_clientScripts = new Script[m_clientScriptDatas.Length];
		m_serverScripts = new Script[m_serverScriptDatas.Length];
		m_sharedScripts = new Script[m_sharedScriptDatas.Length];


		UserData.RegisterAssembly();


		if (m_scope == ScriptScope.Client || m_scope == ScriptScope.Shared)
			GenerateClientScripts();

		if (m_scope == ScriptScope.Server || m_scope == ScriptScope.Shared)
			GenerateServerScripts();


		GenerateSharedScripts();
	}
	public static bool VerifyScripts()
	{
		if (m_readyToExecute) return true;

		if (m_clientScripts == null || m_serverScripts == null || m_sharedScripts == null) return false;

		int validClientScripts = 0;

		for (int i = 0; i < m_clientScripts.Length; i++)
		{
			if(m_clientScripts[i] != null)
			{
				validClientScripts++;
			}
		}

		if (m_scope == ScriptScope.Server) validClientScripts = m_clientScripts.Length;

		if (validClientScripts < m_clientScripts.Length) return false;

		int validServerScripts = 0;

		for (int i = 0; i < m_serverScripts.Length; i++)
		{
			if (m_serverScripts[i] != null)
			{
				validServerScripts++;
			}
		}

		if (m_scope == ScriptScope.Client) validServerScripts = m_clientScripts.Length;

		if (validServerScripts < m_serverScripts.Length) return false;

		int validSharedScripts = 0;

		for (int i = 0; i < m_sharedScripts.Length; i++)
		{
			if (m_sharedScripts[i] != null)
			{
				validSharedScripts++;
			}
		}

		if (validSharedScripts < m_sharedScripts.Length) return false;

		m_readyToExecute = true;
		return true;
	}
	public static void DefineScriptGlobals(Script script)
	{
		script.Globals["console"] = typeof(LUALogging);
		script.Globals["math"] = typeof(LUAMath);
		script.Globals["tween"] = typeof(CustomTween);
		script.Globals["gui"] = typeof(LUAGUI);
	}


	public static void InitScripts()
	{
		if (m_scriptsInitialized == true || m_readyToExecute == false) return;

		if(m_scope == ScriptScope.Client || m_scope == ScriptScope.Shared)
		{
			for (int i = 0; i < m_clientScripts.Length; i++)
			{
				InitClientScript(i);
			}
		}
		if (m_scope == ScriptScope.Server || m_scope == ScriptScope.Shared)
		{
			for (int i = 0; i < m_serverScripts.Length; i++)
			{
				InitServerScript(i);
			}
		}
			
		for (int i = 0; i < m_sharedScripts.Length; i++)
		{
			InitSharedScript(i);
		}

		m_scriptsInitialized = true;
	}
	public static void UpdateScripts(float deltaTime)
	{
		if (m_readyToExecute == false) return;

		if(m_scope == ScriptScope.Client || m_scope == ScriptScope.Shared)
        {
            for (int i = 0; i < m_clientScripts.Length; i++)
			{
				UpdateClientScript(i, deltaTime);
            }
        }
		if (m_scope == ScriptScope.Server || m_scope == ScriptScope.Shared)
		{
			for (int i = 0; i < m_serverScripts.Length; i++)
			{
				UpdateServerScript(i, deltaTime);
			}
		}

		for (int i = 0; i < m_sharedScripts.Length; i++)
		{
			UpdateSharedScript(i, deltaTime);
		}
	}
	public static void UpdateClientGUI()
	{
		for (int i = 0; i < m_clientGUIFunctions.Length; i++)
		{
			if (m_clientGUIFunctions[i] == null || m_clientGUIFunctions[i].IsNilOrNan() == true) continue;

			m_clientScripts[i].Call(m_clientGUIFunctions[i]);
		}
	}


	//Calls the "OnInit" function in each scope of each script
	private static void InitClientScript(int i)
	{
		m_clientUpdateFunctions = new DynValue[m_clientScripts.Length];
		m_clientGUIFunctions = new DynValue[m_clientScripts.Length];

		DynValue initFunc = m_clientScripts[i].Globals.Get(INIT_FUNCTION);

		if (initFunc.IsNilOrNan() == false)
		{
			m_clientScripts[i].Call(initFunc);
		}

		DynValue updateFunc = m_clientScripts[i].Globals.Get(UPDATE_FUNCTION);
		m_clientUpdateFunctions[i] = updateFunc;

		m_clientGUIFunctions[i] = m_clientScripts[i].Globals.Get(GUI_FUNCTION);
	}
	private static void InitServerScript(int i)
	{
		m_serverUpdateFunctions = new DynValue[m_serverScripts.Length];

		DynValue initFunc = m_serverScripts[i].Globals.Get(INIT_FUNCTION);

		if (initFunc.IsNilOrNan() == false)
		{
			m_serverScripts[i].Call(initFunc);
		}

		DynValue updateFunc = m_serverScripts[i].Globals.Get(UPDATE_FUNCTION);
		m_serverUpdateFunctions[i] = updateFunc;
	}
	private static void InitSharedScript(int i)
	{
		m_sharedUpdateFunctions = new DynValue[m_sharedScripts.Length];

		DynValue initFunc = m_sharedScripts[i].Globals.Get(INIT_FUNCTION);

		if (initFunc.IsNilOrNan() == false)
		{
			m_sharedScripts[i].Call(initFunc);
		}

		DynValue updateFunc = m_sharedScripts[i].Globals.Get(UPDATE_FUNCTION);
		m_sharedUpdateFunctions[i] = updateFunc;
	}


	//Calls the "OnUpdate" function in each scope of each script and passes the current "deltaTime"
	private static void UpdateClientScript(int i, float deltaTime)
	{
		if (m_clientUpdateFunctions[i] == null || m_clientUpdateFunctions[i].IsNilOrNan() == true) return;

		m_clientScripts[i].Call(m_clientUpdateFunctions[i], deltaTime);
	}
	private static void UpdateServerScript(int i, float deltaTime)
	{
		if (m_serverUpdateFunctions[i] == null || m_serverUpdateFunctions[i].IsNilOrNan() == true) return;

		m_serverScripts[i].Call(m_serverUpdateFunctions[i], deltaTime);
	}
	private static void UpdateSharedScript(int i, float deltaTime)
	{
		if (m_sharedUpdateFunctions[i] == null || m_sharedUpdateFunctions[i].IsNilOrNan() == true) return;

		m_sharedScripts[i].Call(m_sharedUpdateFunctions[i], deltaTime);
	}


	//Creates Moonsharp.Inerpreter.Scripts from the loaded lua files
	private static void GenerateClientScripts()
	{
		for (int i = 0; i < m_clientScripts.Length; i++)
		{
			m_clientScripts[i] = new Script();
			m_clientScripts[i].DoString(m_clientScriptDatas[i].scriptContent);
			DefineScriptGlobals(m_clientScripts[i]);
		}
	}
	private static void GenerateServerScripts()
	{
		for (int i = 0; i < m_serverScripts.Length; i++)
		{
			m_serverScripts[i] = new Script();
			m_serverScripts[i].DoString(m_serverScriptDatas[i].scriptContent);
			DefineScriptGlobals(m_serverScripts[i]);
		}
	}
	private static void GenerateSharedScripts()
	{
		for (int i = 0; i < m_sharedScripts.Length; i++)
		{
			m_sharedScripts[i] = new Script();
			m_sharedScripts[i].DoString(m_sharedScriptDatas[i].scriptContent);
			DefineScriptGlobals(m_sharedScripts[i]);
		}
	}


	private static string StripPath(string filePath)
	{
		string fileBack = "";
		for (int i = (filePath.Length - 1); i >= 0; i--)
		{
			if (filePath[i] == '/' || filePath[i] == '\\') break;
			fileBack += filePath[i];
		}
		char[] chars = fileBack.ToCharArray();
		Array.Reverse(chars);

		return new string(chars);
	}

}


