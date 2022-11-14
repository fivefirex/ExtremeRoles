﻿using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;
using UnityEngine.Networking;

using AmongUs.Data;


namespace ExtremeRoles.Compat
{
    public sealed class BepInExUpdater : MonoBehaviour
    {
        public static bool UpdateRequired => typeof(IL2CPPChainloader).Assembly.GetName().Version < Version.Parse(minimumBepInExVersion);

        private const string minimumBepInExVersion = "6.0.0.662";
        private const string bepInExDownloadURL = "https://builds.bepinex.dev/projects/bepinex_be/662/BepInEx-Unity.IL2CPP-win-x86-6.0.0-be.662%2B3ad398a.zip";
        private const string exeFileName = "ExtremeBepInExInstaller.exe";

        public void Awake()
        {
            ExtremeRolesPlugin.Logger.LogInfo("BepInEx Update Required...");
            this.StartCoroutine(Excute());
        }

        [HideFromIl2Cpp]
        public IEnumerator Excute()
        {
            string showStr = Helper.Translation.GetString("ReqBepInExUpdate");

            Task.Run(() => Module.DllApi.MessageBox(
                IntPtr.Zero,
                showStr, "Extreme Roles", 0));

            UnityWebRequest www = UnityWebRequest.Get(bepInExDownloadURL);
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
            {
                ExtremeRolesPlugin.Logger.LogInfo(www.error);
                yield break;
            }

            string tmpFolder = Path.Combine(Paths.GameRootPath, "tmp");
            string zipPath = Path.Combine(tmpFolder, "BepInEx.zip");
            string extractPath = Path.Combine(tmpFolder, "BepInEx");

            if (Directory.Exists(tmpFolder))
            {
                Directory.Delete(tmpFolder, true);
            }
            Directory.CreateDirectory(tmpFolder);
            
            File.WriteAllBytes(zipPath, www.downloadHandler.data);
            
            ZipFile.ExtractToDirectory(zipPath, extractPath);

            Assembly asm = Assembly.GetExecutingAssembly();
            string exePath = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(exeFileName));

            using (var resource = asm.GetManifestResourceStream(exePath))
            {
                using (var file = new FileStream(
                    Path.Combine(tmpFolder, exeFileName),
                    FileMode.OpenOrCreate, FileAccess.Write))
                {
                    resource!.CopyTo(file);
                }
            }


            Process.Start(
                Path.Combine(Paths.GameRootPath, "tmp", exeFileName),
                $"{Paths.GameRootPath} {extractPath} {(uint)DataManager.Settings.Language.CurrentLanguage}");

            Application.Quit();
        }
    }
}
