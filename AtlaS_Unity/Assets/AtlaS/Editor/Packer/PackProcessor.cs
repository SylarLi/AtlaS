#if AtlaS_ON
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Atlas;

namespace UnityEditor.UI.Atlas
{
    public abstract class PackProcessor
    {
        public static PackProcessor Fetch(PackQuality quality)
        {
            var baseType = typeof(PackProcessor);
            var customTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract);
            foreach (var customType in customTypes)
            {
                var attributes = customType.GetCustomAttributes(typeof(PackQualityAttribute), false);
                if (attributes.Length > 0 && (attributes[0] as PackQualityAttribute).quality == quality)
                    return (PackProcessor)Activator.CreateInstance(customType);
            }
            throw new NotImplementedException();
        }

        private PackSetting mSetting;
        public PackSetting setting { get { return mSetting; } }

        private IPackSprite[] mSprites;
        public IPackSprite[] sprites { get { return mSprites; } }

        private bool mTransparency;
        public bool transparency { get { return mTransparency; } }

        public void Setup(PackSetting setting, IPackSprite[] sprites, bool transparency)
        {
            mSetting = setting;
            mSprites = sprites;
            mTransparency = transparency;
        }

        public BinRaw[] Execute()
        {
            SortSprites(sprites);
            return PackSprites(setting, sprites);
        }

        protected virtual void SortSprites(IPackSprite[] sprites)
        {
            Array.Sort(sprites, (aps1, aps2) =>
            {
                if (aps1.rect.width != aps2.rect.width)
                {
                    return (int)aps2.rect.width - (int)aps1.rect.width;
                }
                else if (aps1.rect.height != aps2.rect.height)
                {
                    return (int)aps2.rect.height - (int)aps1.rect.height;
                }
                else
                {
                    return string.Compare(aps1.name, aps2.name);
                }
            });
        }

        protected virtual BinRaw[] PackSprites(PackSetting setting, IPackSprite[] sprites)
        {
            var textures = sprites.Select(sprite => new Packer.Area((int)sprite.rect.width, (int)sprite.rect.height)).ToArray();
            var spriteRaws = new List<SpriteRaw>();
            var packers = new List<Packer>();
            var sizeList = PackConst.AtlasSizeList;
            var maxSize = setting.maxAtlasSize;
            var padding = setting.padding;
            var isPOT = forcePowerOf2 || setting.isPOT;
            var forceSquare = this.forceSquare || setting.forceSquare;
            var index = 0;
            while (index < textures.Length)
            {
                var texArea = textures[index];
                Packer.Rect texRect = null;
                for (int i = 0; i < packers.Count; i++)
                {
                    var packer = packers[i];
                    while (true)
                    {
                        if (packer.Push(texArea, out texRect))
                        {
                            spriteRaws.Add(new SpriteRaw()
                            {
                                bin = i,
                                rect = new Rect(texRect.x, texRect.y, texRect.width, texRect.height),
                            });
                            break;
                        }
                        var width = packer.bin.width;
                        var height = packer.bin.height;
                        if (forceSquare)
                        {
                            width *= 2;
                            height *= 2;
                        }
                        else
                        {
                            if (width > height) height *= 2;
                            else width *= 2;
                        }
                        if (width > maxSize || height > maxSize) break;
                        packer.Extend(new Packer.Bin(width, height));
                    }
                    if (texRect != null)
                    {
                        break;
                    }
                }
                if (texRect == null)
                {
                    packers.Add(new Packer(Packer.Algorithm.AdvancedHorizontalSkyline,
                        new Packer.Bin(sizeList[0], sizeList[0]), padding));
                }
                else
                {
                    index += 1;
                }
            }

            var bins = new BinRaw[packers.Count];
            for (int i = 0; i < packers.Count; i++)
            {
                bins[i] = new BinRaw();
                bins[i].size = !isPOT && !forceSquare ?
                    Vector2.zero : new Vector2(packers[i].bin.width, packers[i].bin.height);
            }
            if (!isPOT && !forceSquare)
            {
                for (int i = 0; i < spriteRaws.Count; i++)
                {
                    var sprite = spriteRaws[i];
                    var bin = bins[sprite.bin];
                    bin.size = new Vector2(
                        Mathf.Max(bin.size.x, sprite.rect.xMax),
                        Mathf.Max(bin.size.y, sprite.rect.yMax));
                }
                if (forceMultiplyOf4)
                {
                    foreach (var bin in bins)
                    {
                        bin.size = new Vector2(
                            Mathf.CeilToInt(bin.size.x / 4) * 4,
                            Mathf.CeilToInt(bin.size.y / 4) * 4);
                    }
                }
            }

            for (int i = 0; i < sprites.Length; i++)
            {
                var sprite = sprites[i];
                var spriteRaw = spriteRaws[i];
                spriteRaw.name = sprite.name.Replace("\\", "/");
                spriteRaw.border = sprite.border;
                spriteRaw.pivot = sprite.pivot;
                var rect = spriteRaw.rect;
                var binRaw = bins[spriteRaw.bin];
                binRaw.sprites = binRaw.sprites ?? new SpriteRaw[0];
                Array.Resize(ref binRaw.sprites, binRaw.sprites.Length + 1);
                binRaw.sprites[binRaw.sprites.Length - 1] = spriteRaw;
                var main = binRaw.main;
                var add = binRaw.addition;
                var colors = sprite.Read();
                if (main == null)
                {
                    main = PackUtil.CreateBlankTexture((int)binRaw.size.x, (int)binRaw.size.y, 
                        !alphaSplit && transparency ? TextureFormat.RGBA32 : TextureFormat.RGB24);
                    main.hideFlags = HideFlags.DontUnloadUnusedAsset;
                    binRaw.main = main;
                }
                main.SetPixels(
                    (int)rect.x, (int)rect.y,
                    (int)rect.width, (int)rect.height,
                    colors);
                if (alphaSplit && transparency)
                {
                    if (add == null)
                    {
                        add = PackUtil.CreateBlankTexture((int)binRaw.size.x, (int)binRaw.size.y, TextureFormat.RGB24);
                        add.hideFlags = HideFlags.DontUnloadUnusedAsset;
                        binRaw.addition = add;
                    }
                    for (int c = 0; c < colors.Length; c++)
                    {
                        colors[c] = new Color(colors[c].a, 0, 0);
                    }
                    add.SetPixels(
                        (int)rect.x, (int)rect.y,
                        (int)rect.width, (int)rect.height,
                        colors);
                }
            }

            return bins;
        }

        public virtual bool alphaSplit
        {
            get
            {
                return false;
            }
        }

        public virtual bool forcePowerOf2
        {
            get
            {
                return false;
            }
        }

        public virtual bool forceSquare
        {
            get
            {
                return false;
            }
        }

        public virtual bool forceMultiplyOf4
        {
            get
            {
                return false;
            }
        }
    }
}
#endif