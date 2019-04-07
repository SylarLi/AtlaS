#if AtlaS_ON
using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Atlas;
using UnityEngine.SceneManagement;
using EditorSceneManager = UnityEditor.SceneManagement.EditorSceneManager;

namespace UnityEditor.UI.Atlas
{
    public class SpriteRefCollector
    {
        private static SpriteRefCollector mInstance;

        public static SpriteRefCollector Instance { get { if (mInstance == null) mInstance = new SpriteRefCollector(); return mInstance; } }

        private const string EmptyTagName = "_Homeless_";

        /// <summary>
        /// Translate by sprite's type
        /// </summary>
        /// <param name="targetFolder">Target folder for generating atlas or sprite.</param>
        /// <param name="forward">If true, translate sprite to atlas, else translate atlas to sprite</param>
        public void Translate(string targetFolder, bool forward, string resourceFolder = null, PackSetting setting = null)
        {
            var collectors = new List<ICollector>();
            var transfer = new List<UnityEngine.UI.Sprite>();
            var exclude = new List<UnityEngine.UI.Sprite>();
            var prefabs = AssetDatabase.FindAssets("t:prefab").Select(i => AssetDatabase.GUIDToAssetPath(i)).ToArray();
            for (int i = 0; i < prefabs.Length; i++)
            {
                EditorUtility.DisplayProgressBar("Prefab", prefabs[i], (float)i / prefabs.Length);
                var prefabCollector = new PrefabCollector();
                prefabCollector.path = prefabs[i];
                prefabCollector.Collect();
                collectors.Add(prefabCollector);
                transfer.AddRange(prefabCollector.sprites);
            }
            var scenes = AssetDatabase.FindAssets("t:scene").Select(i => AssetDatabase.GUIDToAssetPath(i)).ToArray();
            for (int i = 0; i < scenes.Length; i++)
            {
                EditorUtility.DisplayProgressBar("Scene", scenes[i], (float)i / scenes.Length);
                var sceneCollector = new SceneCollector();
                sceneCollector.path = scenes[i];
                sceneCollector.Collect();
                collectors.Add(sceneCollector);
                transfer.AddRange(sceneCollector.sprites);
            }
            var clips = AssetDatabase.FindAssets("t:animationclip").Select(i => AssetDatabase.GUIDToAssetPath(i)).ToArray();
            for (int i = 0; i < clips.Length; i++)
            {
                EditorUtility.DisplayProgressBar("AnimationClip", clips[i], (float)i / scenes.Length);
                var clipCollector = new AnimationClipCollector();
                clipCollector.path = clips[i];
                clipCollector.Collect();
                exclude.AddRange(clipCollector.sprites);
            }
            // 过滤类型不符的sprite，过滤在AnimationClip引用的sprite，过滤不在resourceFolder中的资源
            for (int i = transfer.Count - 1; i >= 0; i--)
            {
                if (transfer[i] != null &&
                    ((transfer[i].type == UnityEngine.UI.Sprite.Type.Sprite) != forward ||
                    exclude.Any(e => e == transfer[i])))
                    transfer[i] = null;
            }
            if (!string.IsNullOrEmpty(resourceFolder))
            {
                for (int i = transfer.Count - 1; i >= 0; i--)
                {
                    if (transfer[i] != null)
                    {
                        var transferPath = "";
                        if (transfer[i].type == UnityEngine.UI.Sprite.Type.Sprite)
                            transferPath = AssetDatabase.GetAssetPath(transfer[i].sprite);
                        else if (transfer[i].type == UnityEngine.UI.Sprite.Type.Atlas)
                            transferPath = AssetDatabase.GetAssetPath(transfer[i].atlasRaw);
                        if (!transferPath.Contains(resourceFolder))
                            transfer[i] = null;
                    }
                }
            }
            if (forward) MapSprite2Atlas(targetFolder, setting, transfer);
            else MapAtlas2Sprite(targetFolder, transfer);
            Util.RebindThenTranslate(collectors, transfer);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
        }

        private static void MapSprite2Atlas(string targetFolder, PackSetting setting, List<UnityEngine.UI.Sprite> sprites)
        {
            var spritePaths = new List<string>();
            foreach (var sprite in sprites)
            {
                if (sprite != null && sprite.type == UnityEngine.UI.Sprite.Type.Sprite)
                {
                    var path = AssetDatabase.GetAssetPath(sprite.sprite);
                    if (!string.IsNullOrEmpty(path) && !spritePaths.Contains(path))
                        spritePaths.Add(path);
                }
            }
            var pathSpriteMap = new Dictionary<string, UnityEngine.UI.Sprite>();
            var spriteGroups = spritePaths.GroupBy(path =>
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                return importer.spritePackingTag;
            });
            foreach (var group in spriteGroups)
            {
                var groupPaths = group.ToArray();
                var groupNames = Util.MapSpritePath2SpriteNameInAtlas(groupPaths);
                var groupSprites = new PackAssetSprite[groupPaths.Length];
                for (int i = 0; i < groupPaths.Length; i++)
                {
                    groupSprites[i] = new PackAssetSprite(groupPaths[i]) { name = groupNames[i] };
                    groupSprites[i].quality = PackUtil.CheckTextureCompressed(groupPaths[i]) ? PackQuality.AlphaSplit : PackQuality.Full;
                }
                var groupTag = string.IsNullOrEmpty(group.Key) ? EmptyTagName : group.Key;
                var groupFolder = Path.Combine(targetFolder, groupTag);
                if (!Directory.Exists(groupFolder))
                    Directory.CreateDirectory(groupFolder);
                var atlasRaw = AtlasPacker.Pack(groupFolder, groupSprites, setting);
                for (int i = 0; i < groupPaths.Length; i++)
                    pathSpriteMap[groupPaths[i]] = new UnityEngine.UI.Sprite(atlasRaw, groupNames[i]);
            };
            for (int i = 0; i < sprites.Count; i++)
            {
                var sprite = sprites[i];
                if (sprite != null && sprite.type == UnityEngine.UI.Sprite.Type.Sprite)
                {
                    var path = AssetDatabase.GetAssetPath(sprite.sprite);
                    if (!string.IsNullOrEmpty(path))
                        sprites[i] = pathSpriteMap[path];
                }
            }
        }

        private static void MapAtlas2Sprite(string targetFolder, List<UnityEngine.UI.Sprite> sprites)
        {
            var atlasRaw2AtlasFolder = new Dictionary<string, string>();
            var atlasRaw2AtlasSprites = new Dictionary<string, Dictionary<string, string>>();
            foreach (var sprite in sprites)
            {
                if (sprite != null && sprite.type == UnityEngine.UI.Sprite.Type.Atlas)
                {
                    var path = AssetDatabase.GetAssetPath(sprite.atlasRaw);
                    if (!string.IsNullOrEmpty(path))
                    {
                        atlasRaw2AtlasFolder[path] = Path.Combine(targetFolder, Path.GetFileName(Path.GetDirectoryName(path)));
                        atlasRaw2AtlasSprites[path] = new Dictionary<string, string>();
                    }    
                }
            }
            foreach (var pair in atlasRaw2AtlasFolder)
            {
                var atlasPath = pair.Key;
                var targetPath = pair.Value;
                var atlasFolder = Path.GetDirectoryName(atlasPath);
                var atlasTag = Path.GetFileName(atlasFolder);
                var atlasRaw = AssetDatabase.LoadAssetAtPath<AtlasRaw>(atlasPath);
                var spriteRaws = atlasRaw.bins.SelectMany(i => i.sprites).ToArray();
                var exportSprites = AtlasPacker.Export(atlasRaw, spriteRaws, targetPath);
                for (int i = 0; i < exportSprites.Length; i++)
                {
                    atlasRaw2AtlasSprites[atlasPath][spriteRaws[i].name] = exportSprites[i];
                    var maxTextureSize = PackUtil.Scale2POT((int)Mathf.Max(spriteRaws[i].rect.width, spriteRaws[i].rect.height));
                    var binRaw = atlasRaw.bins[spriteRaws[i].bin];
                    var compressed = (PackQuality)binRaw.quality != PackQuality.Full;
                    var tranparency = PackUtil.CheckAtlasBinTranparency(binRaw);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(exportSprites[i]);
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spritePivot = spriteRaws[i].pivot;
                    importer.spriteBorder = spriteRaws[i].border;
                    importer.spritePackingTag = atlasTag;
                    importer.isReadable = false;
                    importer.maxTextureSize = maxTextureSize;
                    importer.mipmapEnabled = false;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.npotScale = TextureImporterNPOTScale.None;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.alphaIsTransparency = true;
                    if (compressed)
                    {
                        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
                        {
                            name = "Standalone",
                            overridden = true,
                            maxTextureSize = maxTextureSize,
                            compressionQuality = (int)TextureCompressionQuality.Normal,
                            format = TextureImporterFormat.DXT5,
                        });
                        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
                        {
                            name = "iPhone",
                            overridden = true,
                            maxTextureSize = maxTextureSize,
                            compressionQuality = (int)TextureCompressionQuality.Normal,
                            format = tranparency ? TextureImporterFormat.RGBA16 : TextureImporterFormat.RGB16,
                        });
                        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
                        {
                            name = "Android",
                            overridden = true,
                            maxTextureSize = maxTextureSize,
                            compressionQuality = (int)TextureCompressionQuality.Normal,
                            format = tranparency ? TextureImporterFormat.ETC2_RGBA8 : TextureImporterFormat.ETC2_RGB4,
                        });
                    }
                    else
                    {
                        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
                        {
                            name = "Standalone",
                            overridden = false,
                        });
                        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
                        {
                            name = "iPhone",
                            overridden = false,
                        });
                        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
                        {
                            name = "Android",
                            overridden = false,
                        });
                    }
                    importer.SaveAndReimport();
                }
            }
            for (int i = 0; i < sprites.Count; i++)
            {
                var sprite = sprites[i];
                if (sprite != null && sprite.type == UnityEngine.UI.Sprite.Type.Atlas)
                {
                    var atlasPath = AssetDatabase.GetAssetPath(sprite.atlasRaw);
                    if (atlasRaw2AtlasSprites.ContainsKey(atlasPath))
                    {
                        var atlasSprites = atlasRaw2AtlasSprites[atlasPath];
                        if (atlasSprites.ContainsKey(sprites[i].spriteName))
                        {
                            var spritePath = atlasSprites[sprites[i].spriteName];
                            var unitySprite = AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(spritePath);
                            sprites[i] = new UnityEngine.UI.Sprite(unitySprite);
                        }
                    }
                }
            }
        }

        public interface IContextCollector : ICollector
        {

        }

        public class ContextCollector : IContextCollector
        {
            private static Dictionary<Type, Type> CompCollectors = new Dictionary<Type, Type>();

            public static void InitCollector()
            {
                var baseType = typeof(ICompCollector);
                var customTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract && !t.IsGenericType);
                foreach (var customType in customTypes)
                {
                    var compCollector = (ICompCollector)Activator.CreateInstance(customType);
                    CompCollectors.Add(compCollector.compType, customType);
                }
            }

            public static ICompCollector FetchCollector(Type compType)
            {
                if (CompCollectors.Count == 0) InitCollector();
                Debug.Assert(CompCollectors.ContainsKey(compType), "Type not exist: " + compType.FullName);
                return (ICompCollector)Activator.CreateInstance(CompCollectors[compType]);
            }

            public static IEnumerable<Type> ListCompCollectorTypes()
            {
                if (CompCollectors.Count == 0) InitCollector();
                return CompCollectors.Keys;
            }

            public UnityEngine.UI.Sprite[] sprites { get; set; }

            public virtual void Collect() { }

            public virtual void Translate() { }
        }

        public class GameObjectCollector : ContextCollector
        {
            public GameObject go;

            public List<ICollector> collectors = new List<ICollector>();

            public override void Collect()
            {
                var list = new List<UnityEngine.UI.Sprite>();
                var comps = new List<Component>();
                var collectorTypes = ListCompCollectorTypes();
                foreach (var collectorType in collectorTypes)
                {
                    comps.Clear();
                    Util.FindComponents(go, collectorType, comps);
                    foreach (var comp in comps)
                    {
                        var compCollector = FetchCollector(collectorType);
                        compCollector.component = comp;
                        compCollector.Collect();
                        collectors.Add(compCollector);
                        list.AddRange(compCollector.sprites);
                    }
                }
                sprites = list.ToArray();
            }

            public override void Translate()
            {
                Util.RebindThenTranslate(collectors, sprites);
            }
        }

        public class PrefabCollector : ContextCollector
        {
            public string path;

            private GameObjectCollector goCollector = new GameObjectCollector();

            public override void Collect()
            {
                goCollector.go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                goCollector.Collect();
                sprites = new UnityEngine.UI.Sprite[goCollector.sprites.Length];
                Array.Copy(goCollector.sprites, sprites, sprites.Length);
            }

            public override void Translate()
            {
                goCollector.sprites = sprites;
                goCollector.Translate();
                EditorUtility.SetDirty(goCollector.go);
            }
        }

        public class SceneCollector : ContextCollector
        {
            public string path;

            public override void Collect()
            {
                var list = new List<UnityEngine.UI.Sprite>();
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
                for (int i = 0; i < gos.Length; i++)
                {
                    var goCollector = new GameObjectCollector();
                    goCollector.go = gos[i];
                    goCollector.Collect();
                    list.AddRange(goCollector.sprites);
                }
                EditorSceneManager.SaveScene(scene);
                if (!loaded)
                    EditorSceneManager.CloseScene(scene, true);
                sprites = list.ToArray();
            }

            public override void Translate()
            {
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
                var collectors = new List<ICollector>();
                for (int i = 0; i < gos.Length; i++)
                {
                    var collector = new GameObjectCollector();
                    collector.go = gos[i];
                    collector.Collect();
                    collectors.Add(collector);
                }
                Util.RebindThenTranslate(collectors, sprites);
                EditorSceneManager.SaveScene(scene);
                if (!loaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        public class AnimationClipCollector : ContextCollector
        {
            public string path;

            public override void Collect()
            {
                var list = new List<UnityEngine.UI.Sprite>();
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                {
                    if (binding.type == typeof(ImageSpriteAnimationHook) &&
                        binding.propertyName == "sprite")
                    {
                        var keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                        foreach (var keyframe in keyframes)
                        {
                            var sprite = keyframe.value as UnityEngine.Sprite;
                            list.Add(sprite != null ? new UnityEngine.UI.Sprite(sprite) : null);
                        }
                    }
                }
                sprites = list.ToArray();
            }

            public override void Translate()
            {
                throw new InvalidOperationException();
            }
        }

        public interface ICollector
        {
            UnityEngine.UI.Sprite[] sprites { get; set; }

            void Collect();

            void Translate();
        }

        public interface ICompCollector : ICollector
        {
            Component component { get; set; }

            Type compType { get; }
        }

        public class CompCollector<T> : ICompCollector where T : Component
        {
            protected T comp;

            public Component component { get { return comp; } set { comp = value as T; } }

            public Type compType { get { return typeof(T); } }

            public UnityEngine.UI.Sprite[] sprites { get; set; }

            public virtual void Collect() { }

            public virtual void Translate() { }
        }

        public class ImageCollector : CompCollector<Image>
        {
            public override void Collect()
            {
                sprites = new UnityEngine.UI.Sprite[] { comp.sprite };
            }

            public override void Translate()
            {
                if (sprites[0] != null)
                    comp.sprite = sprites[0];
            }
        }

        public class SelectableCollector : CompCollector<Selectable>
        {
            public override void Collect()
            {
                var state = comp.spriteState;
                sprites = new UnityEngine.UI.Sprite[] { state.highlightedSprite, state.pressedSprite, state.disabledSprite };
            }

            public override void Translate()
            {
                var state = comp.spriteState;
                if (sprites[0] != null)
                    state.highlightedSprite = sprites[0];
                if (sprites[1] != null)
                    state.pressedSprite = sprites[1];
                if (sprites[2] != null)
                    state.disabledSprite = sprites[2];
            }
        }

        public class DropdownCollector : CompCollector<Dropdown>
        {
            public override void Collect()
            {
                sprites = comp.options.Select(opt => opt.image).ToArray();
            }

            public override void Translate()
            {
                for (int i = 0; i < sprites.Length; i++)
                {
                    if (sprites[i] != null)
                        comp.options[i].image = sprites[i];
                }
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

            public static string Sprite2AssetPath(UnityEngine.Sprite sprite)
            {
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
                if (assetPath.StartsWith(UnityEditorResPath))
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
                        if (binding.type == typeof(ImageSpriteAnimationHook) &&
                            binding.propertyName == "sprite")
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

            public static void RebindThenTranslate(IList<ICollector> collectors, IList<UnityEngine.UI.Sprite> sprites)
            {
                var sindex = 0;
                var cindex = 0;
                while (cindex < collectors.Count)
                {
                    var collector = collectors[cindex];
                    var current = collector.sprites;
                    for (int i = 0; i < current.Length; i++)
                    {
                        current[i] = sprites[sindex];
                        sindex += 1;
                    }
                    collector.Translate();
                    cindex += 1;
                }
                Debug.Assert(sindex == sprites.Count);
            }

            public static string[] MapSpritePath2SpriteNameInAtlas(string[] files)
            {
                if (files.Length == 1)
                    return new string[] { Path.GetFileNameWithoutExtension(files[0]) };
                var folder = files[0];
                while (!string.IsNullOrEmpty(folder) &&
                    files.Any(i => !i.StartsWith(folder)))
                    folder = Path.GetDirectoryName(folder);
                if (!string.IsNullOrEmpty(folder) &&
                    !folder.EndsWith("/"))
                    folder = folder + "/";
                return files.Select(i =>
                {
                    var path = i.Replace(folder, "");
                    var dir = Path.GetDirectoryName(path);
                    var name = Path.GetFileNameWithoutExtension(path);
                    if (!string.IsNullOrEmpty(dir)) name = dir + "/" + name;
                    return name;
                }).ToArray();
            }
        }
    }
}
#endif