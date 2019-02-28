using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.UI.Atlas
{
    public static class AtlasSwitch
    {
        private const string AtlasMacro = "AtlaS_ON";
        private const string AtlasDllSourcePath = "AtlaS/Dll";
        private const string AtlasDllTargetPath = "UnityExtensions/Unity/GUISystem";
        private static string[] AtlasUIDlls = new string[] { "UnityEngine.UI.dll", "Editor/UnityEditor.UI.dll", "Standalone/UnityEngine.UI.dll" };
        private static string UnityAssembliesPath = "../Library/UnityAssemblies";

        [MenuItem("AtlaS/Switch/On")]
        public static void SwitchOn()
        {
            BuildTargetGroup targetGroup = BuildTargetGroup.Standalone;
            string macros = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            List<string> symbols = new List<string>(macros.Split(new char[] { ';' }));
            if (!symbols.Contains(AtlasMacro))
            {
                var fromDllPath = Path.Combine(Application.dataPath, AtlasDllSourcePath + "/New");
                var toDllPath = Path.Combine(EditorApplication.applicationContentsPath, AtlasDllTargetPath);
                foreach (var dll in AtlasUIDlls)
                {
                    FileUtil.ReplaceFile(Path.Combine(fromDllPath, dll), Path.Combine(toDllPath, dll));
                }
                RemoveAssemblies();
                symbols.Add(AtlasMacro);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, string.Join(";", symbols.ToArray()));
                EditorApplication.Exit(0);
            }
        }

        [MenuItem("AtlaS/Switch/Off")]
        public static void SwitchOff()
        {
            BuildTargetGroup targetGroup = BuildTargetGroup.Standalone;
            string macros = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            List<string> symbols = new List<string>(macros.Split(new char[] { ';' }));
            if (symbols.Contains(AtlasMacro))
            {
                var fromDllPath = Path.Combine(Application.dataPath, AtlasDllSourcePath + "/Raw");
                var toDllPath = Path.Combine(EditorApplication.applicationContentsPath, AtlasDllTargetPath);
                foreach (var dll in AtlasUIDlls)
                {
                    FileUtil.ReplaceFile(Path.Combine(fromDllPath, dll), Path.Combine(toDllPath, dll));
                }
                RemoveAssemblies();
                symbols.Remove(AtlasMacro);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, string.Join(";", symbols.ToArray()));
                EditorApplication.Exit(0);
            }
        }

        private static void RemoveAssemblies()
        {
            var unityAssembliesPath = Path.Combine(Application.dataPath, UnityAssembliesPath);
            if (Directory.Exists(unityAssembliesPath))
            {
                FileUtil.DeleteFileOrDirectory(unityAssembliesPath);
            }
        }
    }
}