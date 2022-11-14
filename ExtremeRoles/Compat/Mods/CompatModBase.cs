﻿using System.Collections.Generic;
using System.Reflection;

using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace ExtremeRoles.Compat.Mods
{
    public abstract class CompatModBase
    {
        public readonly SemanticVersioning.Version Version;
        protected BasePlugin Plugin;
        protected Assembly Dll;
        protected System.Type[] ClassType;

        internal CompatModBase(
            string guid, PluginInfo plugin)
        {
            this.Plugin = plugin!.Instance as BasePlugin;
            this.Version = plugin.Metadata.Version;
            this.Dll = Plugin!.GetType().Assembly;
            this.ClassType = AccessTools.GetTypesFromAssembly(this.Dll);

            this.PatchAll(new Harmony($"ExR.{guid}.Patch"));
        }

        protected abstract void PatchAll(Harmony harmony);
    }
}
