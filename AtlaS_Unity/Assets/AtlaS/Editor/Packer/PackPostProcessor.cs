#if AtlaS_ON
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI.Atlas;

namespace UnityEditor.UI.Atlas
{
    public abstract class PackPostProcessor
    {
        public static PackPostProcessor Fetch(PackQuality quality)
        {
            var baseType = typeof(PackPostProcessor);
            var customTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract);
            foreach (var customType in customTypes)
            {
                var attributes = customType.GetCustomAttributes(typeof(PackQualityAttribute), false);
                if (attributes.Length > 0 && (attributes[0] as PackQualityAttribute).quality == quality)
                    return (PackPostProcessor)Activator.CreateInstance(customType);
            }
            throw new NotImplementedException();
        }

        protected BinRaw bin;

        protected PackSetting setting;

        protected Texture2D mainAsset;

        protected string mainAssetPath;

        protected TextureImporter mainImporter;

        protected Texture2D addAsset;

        protected string addAssetPath;

        protected TextureImporter addImporter;

        protected bool transparency;

        protected int maxTextureSize;

        public void Setup(PackSetting setting, BinRaw bin)
        {
            this.setting = setting;
            this.bin = bin;
            mainAsset = bin.main;
            mainAssetPath = AssetDatabase.GetAssetPath(mainAsset);
            mainImporter = AssetImporter.GetAtPath(mainAssetPath) as TextureImporter;
            if (bin.addition != null)
            {
                addAsset = bin.addition;
                addAssetPath = AssetDatabase.GetAssetPath(addAsset);
                addImporter = AssetImporter.GetAtPath(addAssetPath) as TextureImporter;
            }
            transparency = PackUtil.CheckAtlasBinTranparency(bin);
            maxTextureSize = PackUtil.Scale2POT(Mathf.Max((int)bin.size.x, (int)bin.size.y));
        }

        public void Execute()
        {
            ProcessCommon();
            ProcessStandalone();
            ProcessiPhone();
            ProcessAndroid();
            mainImporter.SaveAndReimport();
            if (addImporter != null)
                addImporter.SaveAndReimport();
        }

        protected virtual void ProcessCommon()
        {
            mainImporter.isReadable = false;
            mainImporter.maxTextureSize = maxTextureSize;
            mainImporter.mipmapEnabled = false;
            mainImporter.wrapMode = TextureWrapMode.Clamp;
            mainImporter.npotScale = TextureImporterNPOTScale.None;
            mainImporter.textureCompression = TextureImporterCompression.Uncompressed;
            mainImporter.alphaIsTransparency = true;
            if (addImporter != null)
            {
                addImporter.isReadable = false;
                addImporter.maxTextureSize = PackUtil.Scale2POT(Mathf.Max(mainAsset.width, mainAsset.height));
                addImporter.mipmapEnabled = false;
                addImporter.wrapMode = TextureWrapMode.Clamp;
                addImporter.npotScale = TextureImporterNPOTScale.None;
                addImporter.textureCompression = TextureImporterCompression.Uncompressed;
                addImporter.alphaIsTransparency = true;
            }
        }

        protected virtual void ProcessStandalone()
        {
            TextureImporterPlatformSettings main;
            TextureImporterPlatformSettings add;
            GetStandaloneSettings(out main, out add);
            main.name = "Standalone";
            mainImporter.SetPlatformTextureSettings(main);
            if (addImporter != null)
            {
                add.name = "Standalone";
                addImporter.SetPlatformTextureSettings(add);
            }
        }

        protected virtual void ProcessiPhone()
        {
            TextureImporterPlatformSettings main;
            TextureImporterPlatformSettings add;
            GetiPhoneSettings(out main, out add);
            main.name = "iPhone";
            mainImporter.SetPlatformTextureSettings(main);
            if (addImporter != null)
            {
                add.name = "iPhone";
                addImporter.SetPlatformTextureSettings(add);
            }
        }

        protected virtual void ProcessAndroid()
        {
            TextureImporterPlatformSettings main;
            TextureImporterPlatformSettings add;
            GetAndroidSettings(out main, out add);
            main.name = "Android";
            mainImporter.SetPlatformTextureSettings(main);
            if (addImporter != null)
            {
                add.name = "Android";
                addImporter.SetPlatformTextureSettings(add);
            }
        }

        protected virtual void GetStandaloneSettings(out TextureImporterPlatformSettings main, out TextureImporterPlatformSettings add)
        {
            throw new NotImplementedException();
        }

        protected virtual void GetiPhoneSettings(out TextureImporterPlatformSettings main, out TextureImporterPlatformSettings add)
        {
            throw new NotImplementedException();
        }

        protected virtual void GetAndroidSettings(out TextureImporterPlatformSettings main, out TextureImporterPlatformSettings add)
        {
            throw new NotImplementedException();
        }
    }
}
#endif