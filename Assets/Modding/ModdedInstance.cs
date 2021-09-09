using System.Collections;
using UnityEngine;

public class ModdedInstance : MonoBehaviour
{
	private void Awake()
	{
		ModManager.LoadAllScriptDatas();
		ModManager.GenerateAllScripts();

		int iterations = 0;
		while (ModManager.VerifyScripts() == false || iterations < 10000) { iterations++; }
		ModManager.InitScripts();
	}

	private void Start()
	{
		//
	}

	private void Update()
	{
		ModManager.UpdateScripts(Time.deltaTime);
	}

	private void OnGUI()
	{
		ModManager.UpdateClientGUI();
	}
}