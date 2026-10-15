using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.ModLoader;

namespace ReimaginingAchievements.Common;

/// <summary>
/// Reads progress flags out of other mods without a hard dependency.
/// Every lookup is cached, and any member that throws is remembered and never asked again.
/// </summary>
public static class ForeignFlags
{
	private const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
	private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

	private static readonly Dictionary<string, object> Instances = new Dictionary<string, object>();
	private static readonly Dictionary<string, Type> Types = new Dictionary<string, Type>();
	private static readonly Dictionary<string, MemberInfo> Members = new Dictionary<string, MemberInfo>();
	private static readonly HashSet<string> BrokenCalls = new HashSet<string>();

	public static void Clear()
	{
		Instances.Clear();
		Types.Clear();
		Members.Clear();
		BrokenCalls.Clear();
	}

	public static bool CallBool(string modName, string command, string argument)
	{
		string key = modName + ":" + command + ":" + argument;
		if (BrokenCalls.Contains(key) || !ModLoader.TryGetMod(modName, out Mod mod))
			return false;

		try
		{
			object result = mod.Call(command, argument);
			if (result is bool value)
				return value;

			// Calamity answers an unknown name with an Exception object instead of throwing.
			if (result is Exception)
				BrokenCalls.Add(key);

			return false;
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

	/// <summary>Reads a static bool. Fields and properties are both accepted
	/// (Calamity keeps its downed flags as properties, Calamity Entropy as fields).</summary>
	public static bool StaticBool(string modName, string typeName, string member)
	{
		return Read(modName, typeName, member, true) is bool value && value;
	}

	public static bool InstanceBool(string modName, string typeName, string member)
	{
		return Read(modName, typeName, member, false) is bool value && value;
	}

	public static int StaticInt(string modName, string typeName, string member)
	{
		return AsInt(Read(modName, typeName, member, true));
	}

	public static int InstanceInt(string modName, string typeName, string member)
	{
		return AsInt(Read(modName, typeName, member, false));
	}

	private static object Read(string modName, string typeName, string member, bool staticMember)
	{
		string key = modName + ":" + typeName + ":" + member + ":" + staticMember;
		if (BrokenCalls.Contains(key))
			return null;

		object instance = null;
		Type type;
		if (staticMember)
		{
			type = FindModType(modName, typeName);
		}
		else
		{
			instance = Instance(modName, typeName);
			type = instance?.GetType();
		}

		if (type == null)
			return null;

		if (!Members.TryGetValue(key, out MemberInfo info))
		{
			BindingFlags flags = staticMember ? AnyStatic : AnyInstance;
			info = (MemberInfo)type.GetProperty(member, flags) ?? type.GetField(member, flags);
			Members[key] = info;
		}

		try
		{
			return info switch
			{
				PropertyInfo property => property.GetValue(staticMember ? null : instance),
				FieldInfo field => field.GetValue(staticMember ? null : instance),
				_ => null
			};
		}
		catch (Exception)
		{
			BrokenCalls.Add(key);
			return null;
		}
	}

	private static object Instance(string modName, string typeName)
	{
		string key = modName + ":" + typeName;
		if (Instances.TryGetValue(key, out object cached))
			return cached;

		object found = null;
		if (ModLoader.TryGetMod(modName, out Mod mod))
		{
			foreach (ModSystem system in mod.GetContent<ModSystem>())
			{
				if (system.Name == typeName)
				{
					found = system;
					break;
				}
			}

			if (found == null)
				found = LoadInstance(FindModType(modName, typeName));
		}

		Instances[key] = found;
		return found;
	}

	private static object LoadInstance(Type type)
	{
		if (type == null || !typeof(ILoadable).IsAssignableFrom(type))
			return null;

		foreach (MethodInfo method in typeof(ModContent).GetMethods(BindingFlags.Public | BindingFlags.Static))
		{
			if (method.Name != "GetInstance" || !method.IsGenericMethodDefinition || method.GetParameters().Length != 0)
				continue;

			try
			{
				return method.MakeGenericMethod(type).Invoke(null, null);
			}
			catch (Exception)
			{
				return null;
			}
		}

		return null;
	}

	/// <summary>Finds a top-level type by simple name. Loadable content wins over helper classes that share
	/// the name (WotG has an asset class called StargazingScene next to the real ModSystem).</summary>
	private static Type FindModType(string modName, string simpleName)
	{
		string key = modName + ":" + simpleName;
		if (Types.TryGetValue(key, out Type cached))
			return cached;

		Type found = null;
		if (ModLoader.TryGetMod(modName, out Mod mod) && mod.Code != null)
		{
			Type[] types;
			try
			{
				types = mod.Code.GetTypes();
			}
			catch (ReflectionTypeLoadException exception)
			{
				types = exception.Types;
			}

			foreach (Type type in types ?? Array.Empty<Type>())
			{
				if (type == null || type.Name != simpleName || type.IsNested)
					continue;

				found = type;
				if (typeof(ILoadable).IsAssignableFrom(type))
					break;
			}
		}

		Types[key] = found;
		return found;
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
