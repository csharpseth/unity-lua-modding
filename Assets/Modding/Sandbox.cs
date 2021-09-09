using MoonSharp.Interpreter;
using Unity.Mathematics;
using UnityEngine;

[MoonSharpUserData]
public static class LUAMath
{
	//Float Math
	public static float sqrt(float value)
	{
		return math.sqrt(value);
	}

	public static float abs(float value)
	{
		return math.abs(value);
	}

	public static float pow(float value, float power)
	{
		return math.pow(value, power);
	}


	//Integer Math
	public static int abs(int value)
	{
		return math.abs(value);
	}

	public static int pow(int value, int power)
	{
		return (int)math.pow(value, power);
	}

	//Double Math
	public static double sqrt(double value)
	{
		return math.sqrt(value);
	}

	public static double abs(double value)
	{
		return math.abs(value);
	}

	public static double pow(double value, double power)
	{
		return math.pow(value, power);
	}
}
[MoonSharpUserData]
public static class LUALogging
{
	public static void log(object content)
	{
		ThreadManager.ExecOnMain(() => Debug.Log(content));
		//Debug.Log(content);
	}

	public static void error(object content)
	{
		Debug.LogError(content);
	}

	public static void warn(object content)
	{
		Debug.LogWarning(content);
	}
}

[MoonSharpUserData]
public static class CustomTween
{
	public static float from_to(float from, float to, float percent)
	{
		return from + ((to - from) * percent);
	}

	public static double from_to(double from, double to, double percent)
	{
		return from + ((to - from) * percent);
	}

	public static int from_to(int from, int to, float percent)
	{
		return from + (int)((to - from) * percent);
	}

	public static Vector3 from_to(Vector3 from, Vector3 to, float percent)
	{
		float x = from_to(from.x, to.x, percent);
		float y = from_to(from.y, to.y, percent);
		float z = from_to(from.z, to.z, percent);

		return new Vector3(x, y, z);
	}

	public static float ping_pong(float from, float to, float percent)
	{
		float scaledPercent = percent * 2f;
		bool doTo = (scaledPercent <= 1f);

		float val = from;

		if(doTo)
		{
			val = from_to(from, to, (scaledPercent));
		}else
		{
			val = from_to(to, from, (scaledPercent - 1f));
		}

		return val;
	}


	public static Vector3 ping_pong(Vector3 from, Vector3 to, float percent)
	{
		float x = ping_pong(from.x, to.x, percent);
		float y = ping_pong(from.y, to.y, percent);
		float z = ping_pong(from.z, to.z, percent);

		return new Vector3(x, y, z);
	}

	public static float ease_in(float percent)
	{
		float a = 1f;
		float b = 0;
		float c = 0;

		return (percent * math.pow(percent, 2)) + (b * percent) + c;
	}

	public static float ease_in_out(float percent)
	{
		float a = 0.63f;
		float b = 0.25f;
		float c = 2f;
		float d = 1.26f;

		return (math.pow(percent * d - a, 3) + b) * c;
	}
}

[MoonSharpUserData]
public static class LUAGUI
{	
	public static bool button(object content)
	{
		return GUILayout.Button(content.ToString());
	}

	public static void label(object content)
	{
		GUILayout.Label(content.ToString());
	}
}