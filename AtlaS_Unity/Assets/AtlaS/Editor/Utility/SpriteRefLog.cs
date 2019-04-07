using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Atlas;
using UnityEngine.SceneManagement;
using EditorSceneManager = UnityEditor.SceneManagement.EditorSceneManager;

namespace UnityEditor.UI.Atlas
{
    public sealed class SpriteRefLog
    {
        private static SpriteRefLog mInstance;

        public static SpriteRefLog Instance { get { if (mInstance == null) mInstance = new SpriteRefLog(); return mInstance; } }

        private const string LogFile = "Assets/AtlaS/Resource/__log_sprite.bytes";

        public void Traverse()
        {
            using (var ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
#if AtlaS_ON
                writer.Write(true);
#else
                writer.Write(false);
#endif
                var prefabs = AssetDatabase.FindAssets("t:prefab").Select(i => AssetDatabase.GUIDToAssetPath(i)).ToArray();
                writer.Write(prefabs.Length);
                var prefabLog = new PrefabLog();
                for (int i = 0; i < prefabs.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("Prefab", prefabs[i], (float)i / prefabs.Length);
                    prefabLog.path = prefabs[i];
                    prefabLog.Serialize(writer);
                }
                var scenes = AssetDatabase.FindAssets("t:scene").Select(i => AssetDatabase.GUIDToAssetPath(i)).ToArray();
                writer.Write(scenes.Length);
                var sceneLog = new SceneLog();
                for (int i = 0; i < scenes.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("Scene", scenes[i], (float)i / scenes.Length);
                    sceneLog.path = scenes[i];
                    sceneLog.Serialize(writer);
                }
                var clips = AssetDatabase.FindAssets("t:animationclip").Select(i => AssetDatabase.GUIDToAssetPath(i)).ToArray();
                writer.Write(clips.Length);
                var clipAction = new AnimationClipAction();
                for (int i = 0; i < clips.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("AnimationClip", clips[i], (float)i / scenes.Length);
                    clipAction.path = clips[i];
#if AtlaS_ON
                    clipAction.OnSwitchOff();
#else
                    clipAction.OnSwitchOn();
#endif
                }
                File.WriteAllBytes(LogFile, ms.ToArray());
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
        }

        public void Revert()
        {
            if (!File.Exists(LogFile))
                return;
            using (var fs = File.OpenRead(LogFile))
            {
                BinaryReader reader = new BinaryReader(fs);
                var isOn = reader.ReadBoolean();
                if (
#if AtlaS_ON
                    isOn == true
#else
                    isOn == false
#endif
                    )
                {
                    return;
                }
                var prefabLog = new PrefabLog();
                var prefabLen = reader.ReadInt32();
                for (int i = 0; i < prefabLen; i++)
                {
                    EditorUtility.DisplayProgressBar("Reverting", "", (float)i / prefabLen);
                    prefabLog.Deserialize(reader);
                }
                var sceneLog = new SceneLog();
                var sceneLen = reader.ReadInt32();
                for (int i = 0; i < sceneLen; i++)
                {
                    EditorUtility.DisplayProgressBar("Reverting", "", (float)i / sceneLen);
                    sceneLog.Deserialize(reader);
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.DeleteAsset(LogFile);
            EditorUtility.ClearProgressBar();
        }

        public interface IContextLog : ILog
        {
            
        }

        public class ContextLog : IContextLog
        {
            private static Dictionary<Type, ICompLog> CompLogs = new Dictionary<Type, ICompLog>();

            public static void InitLog()
            {
                var baseType = typeof(ICompLog);
                var customTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract && !t.IsGenericType);
                foreach (var customType in customTypes)
                {
                    var compLog = (ICompLog)Activator.CreateInstance(customType);
                    CompLogs.Add(compLog.compType, compLog);
                }
            }

            public static ICompLog FetchLog(Type compType)
            {
                if (CompLogs.Count == 0) InitLog();
                Debug.Assert(CompLogs.ContainsKey(compType), "Type not exist: " + compType.FullName);
                return CompLogs[compType];
            }

            public static IEnumerable<Type> ListCompLogTypes()
            {
                if (CompLogs.Count == 0) InitLog();
                return CompLogs.Keys;
            }

            private static Dictionary<Type, ICompAction> CompActions = new Dictionary<Type, ICompAction>();

            public static void InitAction()
            {
                var baseType = typeof(ICompAction);
                var customTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract && !t.IsGenericType);
                foreach (var customType in customTypes)
                {
                    var compAction = (ICompAction)Activator.CreateInstance(customType);
                    CompActions.Add(compAction.compType, compAction);
                }
            }

            public static ICompAction FetchAction(Type compType)
            {
                if (CompActions.Count == 0) InitAction();
                Debug.Assert(CompActions.ContainsKey(compType), "Type not exist: " + compType.FullName);
                return CompActions[compType];
            }

            public static IEnumerable<Type> ListCompActionTypes()
            {
                if (CompActions.Count == 0) InitAction();
                return CompActions.Keys;
            }

            public virtual void Serialize(BinaryWriter writer)
            {

            }

            public virtual void Deserialize(BinaryReader reader)
            {

            }
        }

        public class GameObjectLog : ContextLog
        {
            public GameObject go;

            public override void Serialize(BinaryWriter writer)
            {
                var list = new List<Component>();
                var actionTypes = ListCompActionTypes();
                foreach (var actionType in actionTypes)
                {
                    list.Clear();
                    Util.FindComponents(go, actionType, list);
                    var compAction = FetchAction(actionType);
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        compAction.component = list[i];
#if AtlaS_ON
                        compAction.OnSwitchOff();
#else
                        compAction.OnSwitchOn();
#endif
                    }
                }
                var logTypes = ListCompLogTypes();
                writer.Write(logTypes.Count());
                foreach (var logType in logTypes)
                {
                    list.Clear();
                    Util.FindComponents(go, logType, list);
                    var compLog = FetchLog(logType);
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        compLog.component = list[i];
                        if (!compLog.recordable)
                            list.RemoveAt(i);
                    }
                    writer.Write(logType.FullName);
                    writer.Write(list.Count);
                    list.ForEach(item =>
                    {
                        writer.Write(Util.TrackPath(item.transform));
                        compLog.component = item;
                        compLog.Serialize(writer);
                    });
                }
            }

            public override void Deserialize(BinaryReader reader)
            {
                var length = reader.ReadInt32();
                for (int i = 0; i < length; i++)
                {
                    var compName = reader.ReadString();
                    var compType = Util.FindType(compName);
                    Debug.Assert(compType != null, "Type not found: " + compName);
                    var compLog = FetchLog(compType);
                    Debug.Assert(compLog != null, "Log of type not found: " + compName);
                    var count = reader.ReadInt32();
                    for (int index = 0; index < count; index++)
                    {
                        var hierarchy = reader.ReadString();
                        var target = go.transform.Find(hierarchy);
                        var comp = target.GetComponent(compType);
                        Debug.Assert(comp != null, "Component not found: " + compName);
                        compLog.component = comp;
                        compLog.Deserialize(reader);
                    }
                }
            }
        }

        public class PrefabLog : ContextLog
        {
            public string path;

            private GameObjectLog goLog = new GameObjectLog();

            public override void Serialize(BinaryWriter writer)
            {
                writer.Write(path);
                goLog.go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                goLog.Serialize(writer);
                EditorUtility.SetDirty(goLog.go);
            }

            public override void Deserialize(BinaryReader reader)
            {
                path = reader.ReadString();
                goLog.go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                goLog.Deserialize(reader);
                EditorUtility.SetDirty(goLog.go);
            }
        }

        public class SceneLog : ContextLog
        {
            public string path;

            private GameObjectLog goLog = new GameObjectLog();

            public override void Serialize(BinaryWriter writer)
            {
                writer.Write(path);
                Scene scene = default(Scene);
                bool loaded = false;
                for (int i = EditorSceneManager.sceneCount - 1; i >= 0; i--)
                {
                    var iscene = EditorSceneManager.GetSceneAt(i);
                    if (iscene.IsValid() && Equals(iscene.path, path))
                    {
                        scene = iscene;
                        loaded = true;
                        break;
                    }
                }
                if (!loaded)
                    scene = EditorSceneManager.OpenScene(path, SceneManagement.OpenSceneMode.Additive);
                var gos = scene.GetRootGameObjects();
                writer.Write(gos.Length);
                for (int i = 0; i < gos.Length; i++)
                {
                    goLog.go = gos[i];
                    goLog.Serialize(writer);
                }
                EditorSceneManager.SaveScene(scene);
                if (!loaded)
                    EditorSceneManager.CloseScene(scene, true);
            }

            public override void Deserialize(BinaryReader reader)
            {
                path = reader.ReadString();
                Scene scene = default(Scene);
                bool loaded = false;
                for (int i = EditorSceneManager.sceneCount - 1; i >= 0; i--)
                {
                    var iscene = EditorSceneManager.GetSceneAt(i);
                    if (iscene.IsValid() && Equals(iscene.path, path))
                    {
                        scene = iscene;
                        loaded = true;
                        break;
                    }
                }
                if (!loaded)
                    scene = EditorSceneManager.OpenScene(path, SceneManagement.OpenSceneMode.Additive);
                var gos = scene.GetRootGameObjects();
                var length = reader.ReadInt32();
                for (int i = 0; i < length; i++)
                {
                    goLog.go = gos[i];
                    goLog.Deserialize(reader);
                }
                EditorSceneManager.SaveScene(scene);
                if (!loaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        public interface IContextAction : IAction
        {
        
        }

        public class ContextAction : IContextAction
        {
            public virtual void OnSwitchOn() { }

            public virtual void OnSwitchOff() { }
        }

        public class AnimationClipAction : ContextAction
        {
            public string path;

            public override void OnSwitchOn()
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                bool setDirty = false;
                foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                {
                    if (binding.type == typeof(Image) &&
                        binding.propertyName == "m_Sprite")
                    {
                        var replacement = EditorCurveBinding.PPtrCurve(binding.path, typeof(ImageSpriteAnimationHook), "sprite");
                        var keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                        AnimationUtility.SetObjectReferenceCurve(clip, replacement, keyframes);
                        AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
                        setDirty = true;
                    }
                }
                if (setDirty)
                    EditorUtility.SetDirty(clip);
            }

            public override void OnSwitchOff()
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                bool setDirty = false;
                foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                {
                    if (binding.type == typeof(ImageSpriteAnimationHook) &&
                        binding.propertyName == "sprite")
                    {
                        var replacement = EditorCurveBinding.PPtrCurve(binding.path, typeof(Image), "m_Sprite");
                        var keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                        AnimationUtility.SetObjectReferenceCurve(clip, replacement, keyframes);
                        AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
                        setDirty = true;
                    }
                }
                if (setDirty)
                    EditorUtility.SetDirty(clip);
            }
        }

        public interface ILog
        {
            void Serialize(BinaryWriter writer);

            void Deserialize(BinaryReader reader);
        }

        public interface ICompLog : ILog
        {
            Component component { get; set; }

            Type compType { get; }

            bool recordable { get; }
        }

        public class CompLog<T> : ICompLog where T : Component
        {
            protected T comp;

            public Component component { get { return comp; } set { comp = value as T; } }

            public Type compType { get { return typeof(T); } }

            public virtual bool recordable { get { return false; } }

            public virtual void Serialize(BinaryWriter writer) { }

            public virtual void Deserialize(BinaryReader reader) { }
        }

        public class ImageLog : CompLog<Image>
        {
            public override bool recordable { get { return !Util.CheckSpriteIsNull(comp.sprite); } }

            public override void Serialize(BinaryWriter writer)
            {
                var sprite = Util.Sprite2AssetPath(Util.GetUnitySprite(comp.sprite));
                writer.Write(sprite);
            }

            public override void Deserialize(BinaryReader reader)
            {
                var sprite = reader.ReadString();
                comp.sprite = Util.GetUISprite(Util.AssetPath2Sprite(sprite));
            }
        }

        public class SelectableLog : CompLog<Selectable>
        {
            public override bool recordable
            {
                get
                {
                    var state = comp.spriteState;
                    return !Util.CheckSpriteIsNull(state.highlightedSprite) ||
                        !Util.CheckSpriteIsNull(state.pressedSprite) ||
                        !Util.CheckSpriteIsNull(state.disabledSprite);
                }
            }

            public override void Serialize(BinaryWriter writer)
            {
                var state = comp.spriteState;
                var highlightedSprite = Util.Sprite2AssetPath(Util.GetUnitySprite(state.highlightedSprite));
                var pressedSprite = Util.Sprite2AssetPath(Util.GetUnitySprite(state.pressedSprite));
                var disabledSprite = Util.Sprite2AssetPath(Util.GetUnitySprite(state.disabledSprite));
                writer.Write(highlightedSprite);
                writer.Write(pressedSprite);
                writer.Write(disabledSprite);
            }

            public override void Deserialize(BinaryReader reader)
            {
                var highlightedSprite = reader.ReadString();
                var pressedSprite = reader.ReadString();
                var disabledSprite = reader.ReadString();
                var state = new SpriteState();
                if (!string.IsNullOrEmpty(highlightedSprite))
                    state.highlightedSprite = Util.GetUISprite(Util.AssetPath2Sprite(highlightedSprite));
                if (!string.IsNullOrEmpty(pressedSprite))
                    state.pressedSprite = Util.GetUISprite(Util.AssetPath2Sprite(pressedSprite));
                if (!string.IsNullOrEmpty(disabledSprite))
                    state.disabledSprite = Util.GetUISprite(Util.AssetPath2Sprite(disabledSprite));
                comp.spriteState = state;
            }
        }

        public class DropdownLog : CompLog<Dropdown>
        {
            public override bool recordable
            {
                get
                {
                    return comp.options.Any(op => !Util.CheckSpriteIsNull(op.image));
                }
            }

            public override void Serialize(BinaryWriter writer)
            {
                var options = comp.options.Select(opt => Util.Sprite2AssetPath(Util.GetUnitySprite(opt.image))).ToArray();
                writer.Write(options.Length);
                foreach (var option in options)
                {
                    writer.Write(option);
                }
            }

            public override void Deserialize(BinaryReader reader)
            {
                int length = reader.ReadInt32();
                var options = new string[length];
                for (int i = 0; i < length; i++)
                {
                    options[i] = reader.ReadString();
                }
                var compOptions = comp.options;
                for (int index = Math.Min(compOptions.Count, options.Length) - 1;
                    index >= 0; index--)
                {
                    if (!string.IsNullOrEmpty(options[index]))
                        compOptions[index].image = Util.GetUISprite(Util.AssetPath2Sprite(options[index]));
                }
            }
        }

        public interface IAction
        {
            void OnSwitchOn();

            void OnSwitchOff();
        }

        public interface ICompAction : IAction
        {
            Component component { get; set; }

            Type compType { get; }
        }

        public class CompAction<T> : ICompAction where T : Component
        {
            protected T comp;

            public Component component { get { return comp; } set { comp = value as T; } }

            public Type compType { get { return typeof(T); } }

            public virtual void OnSwitchOn() { }

            public virtual void OnSwitchOff() { }
        }

        public class AnimationAction : CompAction<Animation>
        {
            private List<GameObject> list = new List<GameObject>();

            public override void OnSwitchOn()
            {
                list.Clear();
                Util.FindRelativeAnimationObjects(comp.gameObject, list);
                list.ForEach(item => Util.GetOrAddComponent(item, typeof(ImageSpriteAnimationHook)));
            }

            public override void OnSwitchOff()
            {
                list.Clear();
                Util.FindRelativeAnimationObjects(comp.gameObject, list);
                list.ForEach(item => Util.DestroyComponent(item, typeof(ImageSpriteAnimationHook)));
            }
        }

        public class AnimatorAction : CompAction<Animator>
        {
            private List<GameObject> list = new List<GameObject>();

            public override void OnSwitchOn()
            {
                list.Clear();
                Util.FindRelativeAnimationObjects(comp.gameObject, list);
                list.ForEach(item => Util.GetOrAddComponent(item, typeof(ImageSpriteAnimationHook)));
            }

            public override void OnSwitchOff()
            {
                list.Clear();
                Util.FindRelativeAnimationObjects(comp.gameObject, list);
                list.ForEach(item => Util.DestroyComponent(item, typeof(ImageSpriteAnimationHook)));
            }
        }

        public class Util
        {
            private static readonly string UnityEditorResPath = "Library/unity editor resources";
            private static readonly string UnityDefaultResPath = "Library/unity default resources";
            private static readonly string UnityBuiltinExtraResPath = "Resources/unity_builtin_extra";

            private static StringBuilder mBuilder = new StringBuilder();

            public static string TrackPath(Transform transform)
            {
                mBuilder.Length = 0;
                while (transform != null)
                {
                    if (transform.parent != null)
                    {
                        if (mBuilder.Length > 0)
                            mBuilder.Insert(0, "/");
                        mBuilder.Insert(0, transform.name);
                    }
                    transform = transform.parent;
                }
                return mBuilder.ToString();
            }

#if AtlaS_ON
            public static bool CheckSpriteIsNull(UnityEngine.UI.Sprite sprite)
            {
                return sprite == null || sprite.type != UnityEngine.UI.Sprite.Type.Sprite;
            }

            public static UnityEngine.Sprite GetUnitySprite(UnityEngine.UI.Sprite sprite)
            {
                return sprite.sprite;
            }

            public static UnityEngine.UI.Sprite GetUISprite(UnityEngine.Sprite sprite)
            {
                return new UnityEngine.UI.Sprite(sprite);
            }
#else
            public static bool CheckSpriteIsNull(UnityEngine.Sprite sprite)
            {
                return sprite == null;
            }

            public static UnityEngine.Sprite GetUnitySprite(UnityEngine.Sprite sprite)
            {
                return sprite;
            }

            public static UnityEngine.Sprite GetUISprite(UnityEngine.Sprite sprite)
            {
                return sprite;
            }
#endif

            public static string Sprite2AssetPath(UnityEngine.Sprite sprite)
            {
                if (sprite == null) return "";
                var assetPath = AssetDatabase.GetAssetPath(sprite);
                Debug.Assert(!string.IsNullOrEmpty(assetPath), "Can not find sprite: " + sprite.name);
                if (assetPath.Equals(UnityEditorResPath) ||
                    assetPath.Equals(UnityDefaultResPath) ||
                    assetPath.Equals(UnityBuiltinExtraResPath))
                {
                    return assetPath + "|" + sprite.name;
                }
                else
                {
                    return assetPath;
                }
            }

            public static UnityEngine.Sprite AssetPath2Sprite(string assetPath)
            {
                if (string.IsNullOrEmpty(assetPath))
                {
                    return null;
                }
                else if (assetPath.StartsWith(UnityEditorResPath))
                {
                    var assetName = assetPath.Substring(UnityEditorResPath.Length + 1);
                    return (UnityEngine.Sprite)EditorGUIUtility.Load(assetName);
                }
                else if (assetPath.StartsWith(UnityDefaultResPath))
                {
                    var assetName = assetPath.Substring(UnityDefaultResPath.Length + 1);
                    return (UnityEngine.Sprite)Resources.GetBuiltinResource(typeof(UnityEngine.Sprite), assetName);
                }
                else if (assetPath.StartsWith(UnityBuiltinExtraResPath))
                {
                    var assetName = assetPath.Substring(UnityBuiltinExtraResPath.Length + 1);
                    return AssetDatabase.GetBuiltinExtraResource<UnityEngine.Sprite>("UI/Skin/" + assetName + ".psd");
                }
                else
                {
                    return AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(assetPath);
                }
            }

            public static Type FindType(string name, bool throwOnError = false)
            {
                Type type = null;
                if (!string.IsNullOrEmpty(name))
                {
                    type = Type.GetType(name);
                    if (type == null)
                    {
                        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                        foreach (var assembly in assemblies)
                        {
                            type = assembly.GetType(name);
                            if (type != null)
                                break;
                        }
                    }
                }
                if (throwOnError && type == null)
                    throw new NullReferenceException("Can not find type: " + name);
                return type;
            }

            public static void GetOrAddComponent(GameObject go, Type type)
            {
                if (go.GetComponent(type) == null)
                    go.AddComponent(type);
            }

            public static void DestroyComponent(GameObject go, Type type)
            {
                var comp = go.GetComponent(type);
                if (comp != null)
                    UnityEngine.Object.DestroyImmediate(comp, true);
            }

            public static void FindComponents(GameObject go, Type type, List<Component> list)
            {
                var comp = go.GetComponent(type);
                if (comp != null) list.Add(comp);
                list.AddRange(go.GetComponentsInChildren(type, true));
            }

            public static void FindRelativeAnimationObjects(GameObject root, List<GameObject> list)
            {
                var clips = AnimationUtility.GetAnimationClips(root);
                foreach (var clip in clips)
                {
                    foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                    {
#if AtlaS_ON
                        if (binding.type == typeof(ImageSpriteAnimationHook) &&
                            binding.propertyName == "sprite")
#else
                        if (binding.type == typeof(Image) &&
                            binding.propertyName == "m_Sprite")
#endif
                        {
                            var relative = root.transform.Find(binding.path);
                            if (relative)
                            {
                                list.Add(relative.gameObject);
                            }
                        }
                    }
                }
            }
        }
    }
}