using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.ModLoader;

namespace ReimaginingAchievements.Common;

public static class ForeignFlags
{
	private static readonly Dictionary<string, object> Instances = new Dictionary<string, object>();
	private static readonly Dictionary<string, Type> BossSystems = new Dictionary<string, Type>();
	private static readonly HashSet<string> BrokenCalls = new HashSet<string>();

	public static bool CallBool(string modName, string command, string argument)
	{
		string key = modName + ":" + command + ":" + argument;
		if (BrokenCalls.Contains(key) || !ModLoader.TryGetMod(modName, out Mod mod))
			return false;

		try
		{
			return mod.Call(command, argument) is bool value && value;
		}
		catch (Exception)
		{
			BrokenCalls.Add(key);
			return false;
		}
	}

	public static int ItemType(string modName, string itemName)
	{
		if (!ModLoader.TryGetMod(modName, out Mod mod) || !mod.TryFind(itemName, out ModItem item))
			return 0;

		return item.Type;
	}

	public static bool StaticBool(string modName, string systemName, string member)
	{
		return Read(modName, systemName, member, true) is bool value && value;
	}

	public static bool InstanceBool(string modName, string systemName, string member)
	{
		return Read(modName, systemName, member, false) is bool value && value;
	}

	public static int StaticInt(string modName, string systemName, string member)
	{
		return AsInt(Read(modName, systemName, member, true));
	}

	public static int InstanceInt(string modName, string systemName, string member)
	{
		return AsInt(Read(modName, systemName, member, false));
	}

	public static bool Downed(string modName, string fieldName)
	{
		Type type = BossSystem(modName);
		if (type == null)
			return false;

		FieldInfo field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		return field?.GetValue(null) is bool value && value;
	}

	private static object Read(string modName, string systemName, string member, bool staticMember)
	{
		object instance = Instance(modName, systemName);
		if (instance == null)
			return null;

		BindingFlags flags = BindingFlags.Public | (staticMember ? BindingFlags.Static : BindingFlags.Instance);
		PropertyInfo property = instance.GetType().GetProperty(member, flags);
		if (property == null)
			return null;

		return property.GetValue(staticMember ? null : instance);
	}

	private static object Instance(string modName, string systemName)
	{
		string key = modName + ":" + systemName;
		if (Instances.TryGetValue(key, out object cached))
			return cached;

		object found = null;
		if (ModLoader.TryGetMod(modName, out Mod mod))
		{
			foreach (ModSystem system in mod.GetContent<ModSystem>())
			{
				if (system.Name == systemName)
				{
					found = system;
					break;
				}
			}

			if (found == null)
				found = LoadInstance(mod.Code, systemName);
		}

		Instances[key] = found;
		return found;
	}

	private static object LoadInstance(Assembly assembly, string systemName)
	{
		Type type = FindType(assembly, systemName);
		if (type == null)
			return null;

		MethodInfo generic = null;
		foreach (MethodInfo method in typeof(ModContent).GetMethods(BindingFlags.Public | BindingFlags.Static))
		{
			if (method.Name == "GetInstance" && method.IsGenericMethodDefinition && method.GetParameters().Length == 0)
			{
				generic = method;
				break;
			}
		}

		if (generic == null)
			return null;

		try
		{
			return generic.MakeGenericMethod(type).Invoke(null, null);
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static Type BossSystem(string modName)
	{
		if (BossSystems.TryGetValue(modName, out Type cached))
			return cached;

		Type found = null;
		if (ModLoader.TryGetMod(modName, out Mod mod))
			found = FindType(mod.Code, "DownedBossSystem");

		BossSystems[modName] = found;
		return found;
	}

	private static Type FindType(Assembly assembly, string simpleName)
	{
		if (assembly == null)
			return null;

		Type[] types;
		try
		{
			types = assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException exception)
		{
			types = exception.Types;
		}

		if (types == null)
			return null;

		foreach (Type type in types)
		{
			if (type != null && type.Name == simpleName)
				return type;
		}

		return null;
	}

	private static int AsInt(object value)
	{
		if (value is int number)
			return number;

		if (value is Enum)
			return Convert.ToInt32(value);

		return 0;
	}
}
