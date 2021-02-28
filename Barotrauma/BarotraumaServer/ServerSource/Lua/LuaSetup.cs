﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Barotrauma.Networking;
using MoonSharp.Interpreter;
using Microsoft.Xna.Framework;
using System.Threading.Tasks;
using MoonSharp.VsCodeDebugger;

namespace Barotrauma
{
	partial class LuaSetup
	{

		public Script lua;
		public LuaHook hook;
		public LuaGame game;

		public void DoString(string code)
		{
			try
			{
				lua.DoString(code);
			}
			catch (Exception e)
			{
				if (e is InterpreterException)
				{

					Console.WriteLine(((InterpreterException)e).DecoratedMessage);
				}
				else
				{
					Console.WriteLine(e.ToString());
				}
			}
		}


		public void RunFunction(DynValue func)
		{
			try
			{
				lua.Call(func);
			}
			catch (Exception e)
			{
				if (e is InterpreterException)
				{

					Console.WriteLine(((InterpreterException)e).DecoratedMessage);
				}
				else
				{
					Console.WriteLine(e.ToString());
				}
			}
		}

		public void DoFile(string file)
		{
			try
			{
				lua.DoFile(file);
			}
			catch (Exception e)
			{
				if (e is InterpreterException)
				{

					Console.WriteLine(((InterpreterException)e).DecoratedMessage);
				}
				else
				{
					Console.WriteLine(e.ToString());
				}
			}
		}


		public LuaSetup()
		{
			Console.WriteLine("Lua!");


			LuaScriptLoader luaScriptLoader = new LuaScriptLoader(this);

			LuaCustomConverters.RegisterAll();

			UserData.RegisterType<Character>();
			UserData.RegisterType<Submarine>();
			UserData.RegisterType<Client>();
			UserData.RegisterType<LuaPlayer>();
			UserData.RegisterType<LuaHook>();
			UserData.RegisterType<LuaGame>();
			UserData.RegisterType<LuaRandom>();
			UserData.RegisterType<LuaTimer>();
			UserData.RegisterType<Vector2>();
			UserData.RegisterType<Vector3>();
			UserData.RegisterType<Vector4>();

			lua = new Script(CoreModules.Preset_SoftSandbox | CoreModules.LoadMethods);

			lua.Options.ScriptLoader = luaScriptLoader;

			hook = new LuaHook(lua);
			game = new LuaGame(this);

			lua.Globals["Player"] = new LuaPlayer();
			lua.Globals["Game"] = game;
			lua.Globals["Hook"] = hook;
			lua.Globals["Random"] = new LuaRandom();
			lua.Globals["Timer"] = new LuaTimer(this);

			luaScriptLoader.RunFolder("Lua/autorun");


		}

	}



}

