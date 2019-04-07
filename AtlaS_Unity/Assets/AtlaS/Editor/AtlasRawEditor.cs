#if AtlaS_ON
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.UI.Atlas;

namespace UnityEditor.UI.Atlas
{
    [CustomEditor(typeof(AtlasRaw))]
    public class AtlasRawEditor : Editor
    {
        [MenuItem("Assets/Create/Atlas Raw")]
        private static void CreateAtlasRaw()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            path = string.IsNullOrEmpty(path) ? "Assets" : path;
            if (File.Exists(path))
                path = Path.GetDirectoryName(path);
            var atlas = CreateInstance<AtlasRaw>();
            atlas.bins = new BinRaw[0];
            var setting = new PackSetting();
            atlas.maxSize = setting.maxAtlasSize;
            atlas.padding = setting.padding;
            atlas.isPOT = setting.isPOT;
            atlas.forceSquare = setting.forceSquare;
            var assetPath = Path.Combine(path, PackConst.DefaultAtlasAssetName);
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            atlas.id = PackUtil.GenerateAtlasId();
            AssetDatabase.CreateAsset(atlas, assetPath);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<AtlasRaw>(assetPath);
        }

        private static Color SelectedColor = new Color(62f / 255, 95f / 255, 150 / 255f);

        [SerializeField]
        private int mSelectedBin = 0;

        [SerializeField]
        private List<SpriteIndex> mSelectedSprites = new List<SpriteIndex>();

        [SerializeField]
        private bool mShowBorder = false;

        [SerializeField]
        private string mSearchPattern = "";

        [NonSerialized]
        private bool mRepackFold = false;

        [NonSerialized]
        private PackSetting mSetting;

        [NonSerialized]
        private bool mPackDataInit = false;

        [NonSerialized]
        private bool mAtlasDirty = true;

        [NonSerialized]
        private List<SpriteIndex> mSearchResults = new List<SpriteIndex>();

        [NonSerialized]
        private BinPreviewer mBinPreviwer;

        public override void OnInspectorGUI()
        {
            OnValidateSelected();
            var atlas = target as AtlasRaw;
            EditorGUILayout.GetControlRect(false, 2);
            int clickIndex = AtlasEditorUtil.TitleBar(new GUIContent[]
            {
                new GUIContent("+File"),
                new GUIContent("+Folder"),
            });
            if (clickIndex == 0) DisplayImportMenu(atlas, false);
            else if (clickIndex == 1) DisplayImportMenu(atlas, true);
            mRepackFold = AtlasEditorUtil.ToggleBar(new GUIContent("Settings"), mRepackFold);
            if (mRepackFold)
            {
                if (!mPackDataInit)
                {
                    mPackDataInit = true;
                    mSetting = new PackSetting(atlas.maxSize, atlas.padding, atlas.isPOT, atlas.forceSquare);
                }
                EditorGUI.indentLevel += 1;
                mSetting.maxAtlasSize = EditorGUILayout.IntPopup("Max Size", mSetting.maxAtlasSize, Array.ConvertAll(PackConst.AtlasSizeList, value => value.ToString()), PackConst.AtlasSizeList);
                mSetting.padding = EditorGUILayout.IntField("Padding", mSetting.padding);
                mSetting.isPOT = EditorGUILayout.Toggle("Power Of 2", mSetting.isPOT);
                GUI.enabled = mSetting.isPOT;
                if (!mSetting.isPOT) mSetting.forceSquare = false;
                mSetting.forceSquare = EditorGUILayout.Toggle("Force Square", mSetting.forceSquare);
                GUI.enabled = true;
                var rect = EditorGUILayout.GetControlRect(false, 20);
                if (GUI.Button(new Rect(rect.center.x - 75, rect.y, 150, rect.height), "Repack"))
                {
                    AtlasPacker.Repack(atlas, null, mSetting);
                }
                EditorGUI.indentLevel -= 1;
                EditorGUILayout.Space();
            }
            EditorGUI.BeginChangeCheck();
            mSearchPattern = AtlasEditorUtil.SearchBar(new GUIContent("Search Sprite"), mSearchPattern);
            if (EditorGUI.EndChangeCheck() || mAtlasDirty)
            {
                mSearchResults.Clear();
                if (!string.IsNullOrEmpty(mSearchPattern))
                    mSearchResults.AddRange(AtlasEditorUtil.SearchSprites(atlas, 0, mSearchPattern));
            }
            if (mAtlasDirty)
            {
                mAtlasDirty = false;
                mSelectedBin = Mathf.Clamp(mSelectedBin, 0, atlas.bins.Length - 1);
                mSelectedSprites.Clear();
            }
            EditorGUI.indentLevel += 1;
            if (!string.IsNullOrEmpty(mSearchPattern))
            {
                EditorGUILayout.LabelField(string.Format("{0} results", mSearchResults.Count));
            }
            if (mSearchResults != null && mSearchResults.Count > 0)
            {
                OnValidateResult();
                OnSearchResultGUI();
            }
            EditorGUI.indentLevel -= 1; 
            if (atlas.bins.Length > 0)
            {
                var titleRect = EditorGUILayout.GetControlRect(false, EditorStyles.toolbar.fixedHeight);
                var controlId = GUIUtility.GetControlID(FocusType.Passive);
                var eventType = Event.current.GetTypeForControl(controlId);
                if (eventType == EventType.Repaint)
                {
                    EditorStyles.toolbar.Draw(titleRect, GUIContent.none, controlId);
                }
                var binNames = Array.ConvertAll(atlas.bins, i => PackUtil.GetDisplayName(i));
                var binIndexes = new int[binNames.Length];
                for (int i = 0; i < binNames.Length; i++) binIndexes[i] = i;
                mSelectedBin = EditorGUI.IntPopup(new Rect(10, titleRect.y, titleRect.width - 10, titleRect.height), "Preview Bin", mSelectedBin, binNames, binIndexes, EditorStyles.toolbarPopup);
                mSelectedBin = Mathf.Clamp(mSelectedBin, 0, atlas.bins.Length - 1);
                EditorGUILayout.Space();
                var bin = atlas.bins[mSelectedBin];
                var previewRect = EditorGUILayout.GetControlRect(false, 512);
                previewRect.width = Mathf.Min(previewRect.width, (float)bin.main.width / bin.main.height * previewRect.height);
                previewRect.height = (float)bin.main.height / bin.main.width * previewRect.width;
                OnPreviewBin(previewRect);
            }
        }

        public override bool UseDefaultMargins()
        {
            return false;
        }

        public override bool HasPreviewGUI()
        {
            return true;
        }

        public override GUIContent GetPreviewTitle()
        {
            return new GUIContent("Sprite Preview");
        }

        protected override void OnHeaderGUI()
        {

        }

        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            base.OnPreviewGUI(rect, background);
            if (mSelectedSprites.Count == 1)
            {
                var atlas = target as AtlasRaw;
                var bin = atlas.bins[mSelectedSprites[0].bin];
                var sprite = bin.sprites[mSelectedSprites[0].sprite];
                var borderRect = AtlasEditorUtil.DrawSpriteInRect(bin.main, bin.addition, sprite, rect, new Vector2(100, 100));
                if (mShowBorder) OnSpriteBorderGUI(rect, borderRect, sprite);
                var titleRect = new Rect(rect.x, rect.y, rect.width, 20);
                OnPreivewTitleGUI(titleRect, sprite);
                OnPreviewInfoGUI(rect, sprite);
                var controlID = GUIUtility.GetControlID(FocusType.Passive);
                var eventType = Event.current.GetTypeForControl(controlID);
                if (eventType == EventType.MouseDown &&
                    rect.Contains(Event.current.mousePosition) &&
                    !titleRect.Contains(Event.current.mousePosition))
                {
                    GUI.FocusControl("");
                    Event.current.Use();
                }
            }
            else if (mSelectedSprites.Count >= 2)
            {
                OnPreviewSpritesGUI(rect, mSelectedSprites);
            }
        }

        private void OnValidateSelected()
        {
            var atlas = target as AtlasRaw;
            for (int i = mSelectedSprites.Count - 1; i >= 0; i--)
            {
                if (mSelectedSprites[i].bin >= atlas.bins.Length ||
                    mSelectedSprites[i].sprite >= atlas.bins[mSelectedSprites[i].bin].sprites.Length)
                {
                    mSelectedSprites.RemoveAt(i);
                }
            }
            mSelectedBin = Mathf.Clamp(mSelectedBin, 0, atlas.bins.Length - 1);
        }

        private void OnPreviewBin(Rect rect)
        {
            var atlas = target as AtlasRaw;
            var bin = atlas.bins[mSelectedBin];
            mBinPreviwer = mBinPreviwer ?? new BinPreviewer(true);
            mBinPreviwer.OnGUI(rect, atlas, bin, mSelectedSprites);
            if (Event.current.type == EventType.MouseUp &&
                Event.current.button == 1 &&
                rect.Contains(Event.current.mousePosition))
            {
                DisplayOperateMenu(atlas);
            }
        }

        private void OnSpriteBorderGUI(Rect panelRect, Rect borderRect, SpriteRaw sprite)
        {
            var controlID = GUIUtility.GetControlID(FocusType.Passive);
            var eventType = Event.current.GetTypeForControl(controlID);
            var border = sprite.border;
            var borderScale = new Vector2(
                borderRect.width / sprite.rect.width,
                borderRect.height / sprite.rect.height);
            var scaledBorder = new Vector4(
                border.x * borderScale.x,
                border.y * borderScale.y,
                border.z * borderScale.x,
                border.w * borderScale.y);
            var lines = new Rect[]
            {
                new Rect(panelRect.x, borderRect.y + scaledBorder.w, panelRect.width, 1),
                new Rect(panelRect.x, borderRect.yMax - scaledBorder.y - 1, panelRect.width, 1),
                new Rect(borderRect.x + scaledBorder.x, panelRect.y, 1, panelRect.height),
                new Rect(borderRect.xMax - scaledBorder.z - 1, panelRect.y, 1, panelRect.height),
            };
            foreach (var line in lines)
            {
                EditorGUI.DrawRect(line, Color.green);
            }
            var area = Mathf.Min(3, borderRect.width * 0.5f, borderRect.height * 0.5f);
            var areas = new Rect[]
            {
                new Rect(lines[0].x, lines[0].y - area, lines[0].width, lines[0].height + area * 2),
                new Rect(lines[1].x, lines[1].y - area, lines[1].width, lines[1].height + area * 2),
                new Rect(lines[2].x - area, lines[2].y, lines[2].width + area * 2, lines[2].height),
                new Rect(lines[3].x - area, lines[3].y, lines[3].width + area * 2, lines[3].height),
            };
            switch (eventType)
            {
                case EventType.MouseDown:
                    {
                        for (int i = 0; i < areas.Length; i++)
                        {
                            if (areas[i].Contains(Event.current.mousePosition))
                            {
                                GUIUtility.hotControl = controlID;
                                var index = GUIUtility.GetStateObject(typeof(List<int>), controlID) as List<int>;
                                index.Clear();
                                index.Add(i);
                                break;
                            }
                        }
                        break;
                    }
                case EventType.MouseDrag:
                    {
                        if (GUIUtility.hotControl == controlID &&
                            panelRect.Contains(Event.current.mousePosition))
                        {
                            var delta = Event.current.delta;
                            delta.x *= 1 / borderScale.x;
                            delta.y *= 1 / borderScale.y;
                            var index = GUIUtility.GetStateObject(typeof(List<int>), controlID) as List<int>;
                            var i = index[0];
                            if (i == 0) border.w = Mathf.Clamp(border.w + delta.y, 0, sprite.rect.height);
                            else if (i == 1) border.y = Mathf.Clamp(border.y - delta.y, 0, sprite.rect.height);
                            else if (i == 2) border.x = Mathf.Clamp(border.x + delta.x, 0, sprite.rect.width);
                            else if (i == 3) border.z = Mathf.Clamp(border.z - delta.x, 0, sprite.rect.width);
                            sprite.border = border;
                            EditorUtility.SetDirty(target);
                            Event.current.Use();
                        }
                        break;
                    }
                case EventType.MouseUp:
                    {
                        if (GUIUtility.hotControl == controlID)
                        {
                            GUIUtility.hotControl = 0;
                        }
                        break;
                    }
            }
        }

        private void OnPreivewTitleGUI(Rect rect, SpriteRaw sprite)
        {
            var controlID = GUIUtility.GetControlID(FocusType.Passive);
            var eventType = Event.current.GetTypeForControl(controlID);
            if (eventType == EventType.Repaint)
            {
                EditorStyles.toolbar.Draw(rect, GUIContent.none, controlID);
            }
            if (GUI.Button(new Rect(rect.x, rect.y, 60, rect.height), "Save", EditorStyles.toolbarButton))
            {
                AssetDatabase.SaveAssets();
            }
            EditorGUI.LabelField(new Rect(rect.x + rect.width - 320, rect.y, 40, rect.height), "Name", EditorStyles.miniLabel);
            EditorGUI.BeginChangeCheck();
            sprite.name = EditorGUI.TextField(new Rect(rect.x + rect.width - 280, rect.y + 1, 200, rect.height), sprite.name, EditorStyles.toolbarTextField);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
            }
            mShowBorder = GUI.Toggle(new Rect(rect.x + rect.width - 80, rect.y, 80, rect.height), mShowBorder, "Edit Border", EditorStyles.toolbarButton);
        }

        private void OnPreviewInfoGUI(Rect rect, SpriteRaw sprite)
        {
            var bottomInfo = new GUIContent(sprite.rect.width + "x" + sprite.rect.height);
            var bottomInfoSize = EditorStyles.boldLabel.CalcSize(bottomInfo);
            EditorGUI.LabelField(new Rect(rect.x + rect.width * 0.5f - bottomInfoSize.x * 0.5f, rect.yMax, bottomInfoSize.x, bottomInfoSize.y), bottomInfo, EditorStyles.boldLabel);
            if (mShowBorder)
            {
                var border = sprite.border;
                EditorGUI.LabelField(new Rect(rect.x, rect.y + rect.height - 40, rect.width, 20),
                    string.Format("L:{0}, B:{1}, R:{2}, T:{3}", (int)border.x, (int)border.y, (int)border.z, (int)border.w));
                var pivot = sprite.pivot;
                EditorGUI.LabelField(new Rect(rect.x, rect.y + rect.height - 20, rect.width, 20),
                    string.Format("PX:{0}, PY:{1}", pivot.x, pivot.y));
            }
        }

        private void OnPreviewSpritesGUI(Rect rect, List<SpriteIndex> sprites)
        {
            var atlas = target as AtlasRaw;
            var margin = new Vector2(60, 60);
            var size = 100;
            var space = 20;
            var columns = Mathf.FloorToInt((rect.width + space - margin.x) / (size + space));
            columns = Mathf.Clamp(columns, 1, sprites.Count);
            var rows = Mathf.FloorToInt((rect.height + space - margin.y) / (size + space));
            rows = Mathf.Clamp(rows, 1, Mathf.CeilToInt((float)sprites.Count / columns));
            var width = (rect.width - margin.x - (columns - 1) * space) / columns;
            var height = (rect.height - margin.y - (rows - 1) * space) / rows;
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    int index = row * columns + column;
                    if (index < sprites.Count)
                    {
                        var bin = atlas.bins[sprites[index].bin];
                        var sprite = bin.sprites[sprites[index].sprite];
                        AtlasEditorUtil.DrawSpriteInRect(bin.main, bin.addition, sprite,
                            new Rect(rect.x + margin.x * 0.5f + (width + space) * column,
                                rect.y + margin.y * 0.5f + (height + space) * row,
                                width,
                                height),
                            new Vector2(0, 0));
                    }
                }
            }
            var bottomInfo = new GUIContent(string.Format("Previewing {0} of {1} objects", Mathf.Min(rows * columns, sprites.Count), sprites.Count));
            var bottomInfoSize = EditorStyles.boldLabel.CalcSize(bottomInfo);
            EditorGUI.LabelField(new Rect(rect.x + rect.width * 0.5f - bottomInfoSize.x * 0.5f, rect.yMax, bottomInfoSize.x, bottomInfoSize.y), bottomInfo, EditorStyles.boldLabel);
        }

        private void OnValidateResult()
        {
            var atlas = target as AtlasRaw;
            for (int i = mSearchResults.Count - 1; i >= 0; i--)
            {
                var result = mSearchResults[i];
                if (result.bin >= atlas.bins.Length ||
                    result.sprite >= atlas.bins[result.bin].sprites.Length)
                {
                    mSearchResults.RemoveAt(i);
                }
            }
        }

        private void OnSearchResultGUI()
        {
            var atlas = target as AtlasRaw;
            int itemHeight = 20;
            var gridRect = EditorGUILayout.GetControlRect(false, mSearchResults.Count * itemHeight);
            for (int i = 0; i < mSearchResults.Count; i++)
            {
                var itemRect = new Rect(gridRect.x, gridRect.y, gridRect.width, itemHeight);
                var result = mSearchResults[i];
                var iBin = result.bin;
                var iSprite = result.sprite;
                if (mSelectedSprites.Any(s => s.bin == iBin && s.sprite == iSprite))
                {
                    EditorGUI.DrawRect(itemRect, SelectedColor);
                }
                var sprite = atlas.bins[iBin].sprites[iSprite];
                EditorGUI.LabelField(itemRect, "◆ " + sprite.name);
                int controlId = GUIUtility.GetControlID(FocusType.Passive);
                var eventType = Event.current.GetTypeForControl(controlId);
                if (eventType == EventType.MouseDown &&
                    itemRect.Contains(Event.current.mousePosition) &&
                    Event.current.button == 0)
                {
                    if (iBin != -1 && iSprite != -1)
                    {
                        mSelectedBin = iBin;
                        if (Event.current.control)
                        {
                            if (mSelectedSprites.Any(s => s.bin == iBin && s.sprite == iSprite))
                                mSelectedSprites.RemoveAll(s => s.bin == iBin && s.sprite == iSprite);
                            else
                                mSelectedSprites.Add(new SpriteIndex(0, iBin, iSprite));
                        }
                        else
                        {
                            if (Event.current.shift && mSelectedSprites.Count > 0)
                            {
                                for (int s = mSelectedSprites.Count - 1; s >= 0; s--)
                                {
                                    var ii = mSearchResults.FindIndex(r => r.Equals(mSelectedSprites[s]));
                                    if (ii >= 0)
                                    {
                                        for (int m = Mathf.Min(ii, i); m <= Mathf.Max(ii, i); m++)
                                        {
                                            var sp = mSearchResults[m];
                                            var sBin = sp.bin;
                                            var sSprite = sp.sprite;
                                            if (!mSelectedSprites.Any(ss => ss.bin == sBin && ss.sprite == sSprite))
                                            {
                                                mSelectedSprites.Add(new SpriteIndex(0, sBin, sSprite));
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                mSelectedSprites.Clear();
                                mSelectedSprites.Add(new SpriteIndex(0, iBin, iSprite));
                            }
                        }
                    }
                    GUI.FocusControl("");
                    Event.current.Use();
                }
                if (eventType == EventType.MouseUp &&
                    itemRect.Contains(Event.current.mousePosition) &&
                    Event.current.button == 1)
                {
                    DisplayOperateMenu(atlas);
                    Event.current.Use();
                }
                gridRect.y += itemRect.height;
            }
            if (Event.current.type == EventType.ValidateCommand &&
                Event.current.commandName == "SelectAll")
            {
                mSelectedSprites.Clear();
                for (int i = 0; i < mSearchResults.Count; i++)
                {
                    var sprite = mSearchResults[i];
                    mSelectedSprites.Add(new SpriteIndex(0, sprite.bin, sprite.sprite));
                }
                Event.current.Use();
            }
        }

        private void DisplayOperateMenu(AtlasRaw atlas)
        {
            var menu = new GenericMenu();
            var qualityNames = Enum.GetNames(typeof(PackQuality));
            foreach (var qualityName in qualityNames)
            {
                var quality = (PackQuality)Enum.Parse(typeof(PackQuality), qualityName);
                menu.AddItem(new GUIContent("Import/File/" + qualityName), false, () =>
                {
                    var files = SFB.StandaloneFileBrowser.OpenFilePanel("Select File", "", new[] { new SFB.ExtensionFilter("Image Files", "png", "jpg") }, true);
                    if (files != null && files.Length > 0)
                    {
                        AddFiles(atlas, quality, files);
                    }
                });
                menu.AddItem(new GUIContent("Import/Folder/" + qualityName), false, () =>
                {
                    var files = SFB.StandaloneFileBrowser.OpenFolderPanel("Select Folder", "", true);
                    if (files != null && files.Length > 0)
                    {
                        AddFiles(atlas, quality, files);
                    }
                });
            }
            if (mSelectedSprites.Count > 0)
            {
                foreach (var qualityName in qualityNames)
                {
                    var quality = (PackQuality)Enum.Parse(typeof(PackQuality), qualityName);
                    menu.AddItem(new GUIContent("Quality/" + qualityName), false, () =>
                    {
                        CompressSelected(atlas, quality, mSelectedSprites);
                    });
                }
                menu.AddItem(new GUIContent("Export/Selected"), false, () =>
                {
                    ExportSelected(atlas, mSelectedSprites);
                });
                menu.AddItem(new GUIContent("Export/All"), false, () =>
                {
                    ExportAll(atlas);
                });
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Delete"), false, () =>
                {
                    DeleteSelected(atlas, mSelectedSprites);
                });
            }
            else
            {
                menu.AddItem(new GUIContent("Export/All"), false, () =>
                {
                    ExportAll(atlas);
                });
            }
            menu.ShowAsContext();
        }

        private void ExportSelected(AtlasRaw atlas, List<SpriteIndex> selected)
        {
            var folder = EditorUtility.OpenFolderPanel("Select target folder", "", "");
            if (!string.IsNullOrEmpty(folder))
            {
                var exports = selected.Select(sp => atlas.bins[sp.bin].sprites[sp.sprite]).ToArray();
                AtlasPacker.Export(atlas, exports, folder);
            }
        }

        private void ExportAll(AtlasRaw atlas)
        {
            var folder = EditorUtility.OpenFolderPanel("Select target folder", "", "");
            if (!string.IsNullOrEmpty(folder))
            {
                var exports = new List<SpriteRaw>();
                foreach (var bin in atlas.bins)
                {
                    foreach (var sprite in bin.sprites)
                    {
                        exports.Add(sprite);
                    }
                }
                AtlasPacker.Export(atlas, exports.ToArray(), folder);
            }
        }

        private void DeleteSelected(AtlasRaw atlas, List<SpriteIndex> selected)
        {
            var list = new List<IPackSprite>(PackAtlasSprite.ListSprites(atlas));
            foreach (var selectedSprite in selected)
            {
                var bin = atlas.bins[selectedSprite.bin];
                var sprite = bin.sprites[selectedSprite.sprite];
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (string.Equals(list[i].name, sprite.name))
                    {
                        list.RemoveAt(i);
                    }
                }
            }
            AtlasPacker.Repack(atlas, list.ToArray());
        }

        private void CompressSelected(AtlasRaw atlas, PackQuality quality, List<SpriteIndex> selected)
        {
            var count = 0;
            var list = new List<IPackSprite>(PackAtlasSprite.ListSprites(atlas));
            foreach (var selectedSprite in selected)
            {
                var bin = atlas.bins[selectedSprite.bin];
                var sprite = bin.sprites[selectedSprite.sprite];
                foreach (var packSprite in list)
                {
                    if (string.Equals(packSprite.name, sprite.name) &&
                        packSprite.quality != quality)
                    {
                        packSprite.quality = quality;
                        count += 1;
                    }
                }
            }
            if (count > 0)
            {
                AtlasPacker.Repack(atlas, list.ToArray());
            }
        }

        private void DisplayImportMenu(AtlasRaw atlas, bool isFolder)
        {
            var menu = new GenericMenu();
            var qualityNames = Enum.GetNames(typeof(PackQuality));
            foreach (var qualityName in qualityNames)
            {
                var quality = (PackQuality)Enum.Parse(typeof(PackQuality), qualityName);
                menu.AddItem(new GUIContent(qualityName), false, () =>
                {
                    string[] files = null;
                    if (isFolder)
                        files = SFB.StandaloneFileBrowser.OpenFolderPanel("Select Folder", "", true);
                    else
                        files = SFB.StandaloneFileBrowser.OpenFilePanel("Select File", "", new[] { new SFB.ExtensionFilter("Image Files", "png", "jpg") }, true);
                    if (files != null && files.Length > 0)
                    {
                        AddFiles(atlas, quality, files);
                    }
                });
            }
            menu.ShowAsContext();
        }

        private void AddFiles(AtlasRaw atlas, PackQuality quality, string[] files, bool replaceSpriteByName = true)
        {
            var textures = new List<IPackSprite>(PackAtlasSprite.ListSprites(atlas));
            Action<IPackSprite> addTexture = (texture) =>
            {
                if (replaceSpriteByName)
                {
                    for (int i = textures.Count - 1; i >= 0; i--)
                    {
                        if (textures[i].name.Equals(texture.name))
                        {
                            textures.RemoveAt(i);
                        }
                    }
                }
                textures.Add(texture);
            };
            foreach (var file in files)
            {
                if (File.Exists(file))
                {
                    var texture = new PackAssetSprite(file);
                    texture.name = Path.GetFileNameWithoutExtension(file);
                    texture.quality = quality;
                    addTexture(texture);
                }
                else if (Directory.Exists(file))
                {
                    var images = new List<string>();
                    images.AddRange(Directory.GetFiles(file, "*.png", SearchOption.AllDirectories));
                    images.AddRange(Directory.GetFiles(file, "*.jpg", SearchOption.AllDirectories));
                    foreach (var image in images)
                    {
                        var assetPath = image.Replace(file + "\\", "").Replace("\\", "/");
                        var assetDir = Path.GetDirectoryName(assetPath);
                        var assetName = Path.GetFileNameWithoutExtension(assetPath);
                        var assetLabel = string.IsNullOrEmpty(assetDir) ? assetName : assetDir + "/" + assetName;
                        var texture = new PackAssetSprite(image)
                        {
                            name = assetLabel,
                            quality = quality
                        };
                        addTexture(texture);
                    }
                }
            }
            AtlasPacker.Repack(atlas, textures.ToArray());
        }
    }
}
#endif