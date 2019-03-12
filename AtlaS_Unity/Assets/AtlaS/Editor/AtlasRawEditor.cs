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
        private Vector2 mScrollPos = Vector2.zero;

        [SerializeField]
        private int mSelectedBin = 0;

        [SerializeField]
        private List<SelectedSprite> mSelectedSprites = new List<SelectedSprite>();

        [SerializeField]
        private Vector3 mTexViewParams = new Vector3(0, 0, 1);

        [SerializeField]
        private bool mSpriteBorder = false;

        [SerializeField]
        private string mFindSprite = "";

        [NonSerialized]
        private SpriteRaw[] mFindResult;

        [NonSerialized]
        private bool mAtlasDirty = true;

        [NonSerialized]
        private bool mRepackFold = false;

        [NonSerialized]
        private PackSetting mSetting;

        [NonSerialized]
        private bool mPackDataInit = false;

        [NonSerialized]
        private Material mPreviewMat;

        [NonSerialized]
        private Mesh mPreviewMesh;

        [NonSerialized]
        private Material mGridMat;

        [NonSerialized]
        private Mesh mGridMesh;

        public override void OnInspectorGUI()
        {
            var atlas = target as AtlasRaw;
            mSelectedBin = Mathf.Clamp(mSelectedBin, 0, atlas.bins.Length - 1);
            for (int i = mSelectedSprites.Count - 1; i >= 0; i--)
            {
                if (mSelectedSprites[i].bin >= atlas.bins.Length ||
                    mSelectedSprites[i].sprite >= atlas.bins[mSelectedSprites[i].bin].sprites.Length)
                {
                    mSelectedSprites.RemoveAt(i);
                }
            }
            mScrollPos = EditorGUILayout.BeginScrollView(mScrollPos);
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+File", GUILayout.Width(80))) DisplayImportMenu(atlas, false);
            if (GUILayout.Button("+Folder", GUILayout.Width(80))) DisplayImportMenu(atlas, true);
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel += 1;
            mRepackFold = EditorGUILayout.Foldout(mRepackFold, "Repack", true);
            if (mRepackFold)
            {
                if (!mPackDataInit)
                {
                    mPackDataInit = true;
                    mSetting = new PackSetting(atlas.maxSize, atlas.padding, atlas.isPOT, atlas.forceSquare);
                }
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
                EditorGUILayout.Space();
            }
            EditorGUI.indentLevel -= 1;
            EditorGUI.BeginChangeCheck();
            mFindSprite = EditorGUILayout.TextField("Search Sprite", mFindSprite);
            if (EditorGUI.EndChangeCheck() || mAtlasDirty)
            {
                mFindResult = string.IsNullOrEmpty(mFindSprite) ? null :
                    PackUtil.SearchSprites(atlas, mFindSprite);
            }
            if (mAtlasDirty)
            {
                mAtlasDirty = false;
                mSelectedBin = Mathf.Clamp(mSelectedBin, 0, atlas.bins.Length - 1);
                mSelectedSprites.Clear();
            }
            EditorGUI.indentLevel += 1;
            if (!string.IsNullOrEmpty(mFindSprite))
            {
                EditorGUILayout.LabelField(string.Format("{0} results", mFindResult.Length));
            }
            if (mFindResult != null && mFindResult.Length > 0)
            {
                OnFindResultGUI(atlas);
            }
            EditorGUI.indentLevel -= 1;
            if (atlas.bins.Length > 0)
            {
                var binNames = Array.ConvertAll(atlas.bins, i => PackUtil.GetDisplayName(i));
                var binIndexes = new int[binNames.Length];
                for (int i = 0; i < binNames.Length; i++) binIndexes[i] = i;
                mSelectedBin = EditorGUILayout.IntPopup("Preview Atlas", mSelectedBin, binNames, binIndexes);
                EditorGUILayout.Space();

                var bin = atlas.bins[mSelectedBin];
                var rect = EditorGUILayout.GetControlRect(false, 512);
                rect.width = Mathf.Min(rect.width, (float)bin.main.width / bin.main.height * rect.height);
                rect.height = (float)bin.main.height / bin.main.width * rect.width;
                OnPreviewBin(rect, atlas, bin);
            }
            EditorGUILayout.EndScrollView();
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

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            base.OnPreviewGUI(r, background);
            var atlas = target as AtlasRaw;
            if (mSelectedSprites.Count == 1)
            {
                var bin = atlas.bins[mSelectedSprites[0].bin];
                var sprite = bin.sprites[mSelectedSprites[0].sprite];
                var rect = sprite.rect;
                var border = sprite.border;
                var previewRect = DrawSpriteInRect(bin.main, bin.addition, sprite, r, new Vector2(100, 100));
                var controlID = GUIUtility.GetControlID(FocusType.Passive);
                var eventType = Event.current.GetTypeForControl(controlID);
                if (mSpriteBorder)
                {
                    var borderScale = new Vector2(
                        previewRect.width / rect.width,
                        previewRect.height / rect.height);
                    var scaledBorder = new Vector4(
                        border.x * borderScale.x,
                        border.y * borderScale.y,
                        border.z * borderScale.x,
                        border.w * borderScale.y);
                    var lines = new Rect[]
                    {
                        new Rect(r.x, previewRect.y + scaledBorder.w, r.width, 1),
                        new Rect(r.x, previewRect.yMax - scaledBorder.y - 1, r.width, 1),
                        new Rect(previewRect.x + scaledBorder.x, r.y, 1, r.height),
                        new Rect(previewRect.xMax - scaledBorder.z - 1, r.y, 1, r.height),
                    };
                    foreach (var line in lines)
                    {
                        EditorGUI.DrawRect(line, Color.green);
                    }
                    var area = Mathf.Min(3, previewRect.width * 0.5f, previewRect.height * 0.5f);
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
                                    r.Contains(Event.current.mousePosition))
                                {
                                    var delta = Event.current.delta;
                                    delta.x *= 1 / borderScale.x;
                                    delta.y *= 1 / borderScale.y;
                                    var index = GUIUtility.GetStateObject(typeof(List<int>), controlID) as List<int>;
                                    var i = index[0];
                                    if (i == 0) border.w = Mathf.Clamp(border.w + delta.y, 0, rect.height);
                                    else if (i == 1) border.y = Mathf.Clamp(border.y - delta.y, 0, rect.height);
                                    else if (i == 2) border.x = Mathf.Clamp(border.x + delta.x, 0, rect.width);
                                    else if (i == 3) border.z = Mathf.Clamp(border.z - delta.x, 0, rect.width);
                                    sprite.border = border;
                                    EditorUtility.SetDirty(atlas);
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
                var toolbarh = 20;
                var barRect = new Rect(r.x, r.y, r.width, toolbarh);
                controlID = GUIUtility.GetControlID(FocusType.Passive);
                eventType = Event.current.GetTypeForControl(controlID);
                if (eventType == EventType.Repaint)
                {
                    EditorStyles.toolbar.Draw(barRect, GUIContent.none, controlID);
                }
                if (GUI.Button(new Rect(r.x, r.y, 60, toolbarh), "Save", EditorStyles.toolbarButton))
                {
                    AssetDatabase.SaveAssets();
                }
                EditorGUI.LabelField(new Rect(r.x + r.width - 320, r.y, 40, toolbarh), "Name", EditorStyles.miniLabel);
                EditorGUI.BeginChangeCheck();
                sprite.name = EditorGUI.TextField(new Rect(r.x + r.width - 280, r.y + 1, 200, toolbarh), sprite.name, EditorStyles.toolbarTextField);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(atlas);
                }
                var bottomInfo = new GUIContent(sprite.rect.width + "x" + sprite.rect.height);
                var bottomInfoSize = EditorStyles.boldLabel.CalcSize(bottomInfo);
                EditorGUI.LabelField(new Rect(r.x + r.width * 0.5f - bottomInfoSize.x * 0.5f, r.yMax, bottomInfoSize.x, bottomInfoSize.y), bottomInfo, EditorStyles.boldLabel);
                mSpriteBorder = GUI.Toggle(new Rect(r.x + r.width - 80, r.y, 80, toolbarh), mSpriteBorder, "Edit Border", EditorStyles.toolbarButton);
                if (mSpriteBorder)
                {
                    EditorGUI.LabelField(new Rect(r.x, r.y + r.height - toolbarh, 200, toolbarh),
                        string.Format("L:{0}, B:{1}, R:{2}, T:{3}", (int)border.x, (int)border.y, (int)border.z, (int)border.w));
                }
                controlID = GUIUtility.GetControlID(FocusType.Passive);
                eventType = Event.current.GetTypeForControl(controlID);
                if (eventType == EventType.MouseDown &&
                    r.Contains(Event.current.mousePosition) &&
                    !barRect.Contains(Event.current.mousePosition))
                {
                    GUI.FocusControl("");
                    Event.current.Use();
                }
            }
            else if (mSelectedSprites.Count >= 2)
            {
                var margin = new Vector2(60, 60);
                var size = 100;
                var space = 20;
                var columns = Mathf.FloorToInt((r.width + space - margin.x) / (size + space));
                columns = Mathf.Clamp(columns, 1, mSelectedSprites.Count);
                var rows = Mathf.FloorToInt((r.height + space - margin.y) / (size + space));
                rows = Mathf.Clamp(rows, 1, Mathf.CeilToInt((float)mSelectedSprites.Count / columns));
                var width = (r.width - margin.x - (columns - 1) * space) / columns;
                var height = (r.height - margin.y - (rows - 1) * space) / rows;
                for (int row = 0; row < rows; row++)
                {
                    for (int column = 0; column < columns; column++)
                    {
                        int index = row * columns + column;
                        if (index < mSelectedSprites.Count)
                        {
                            var bin = atlas.bins[mSelectedSprites[index].bin];
                            var sprite = bin.sprites[mSelectedSprites[index].sprite];
                            DrawSpriteInRect(bin.main, bin.addition, sprite,
                                new Rect(r.x + margin.x * 0.5f + (width + space) * column,
                                    r.y + margin.y * 0.5f + (height + space) * row,
                                    width,
                                    height),
                                new Vector2(0, 0));
                        }
                    }
                }
                var bottomInfo = new GUIContent(string.Format("Previewing {0} of {1} objects", Mathf.Min(rows * columns, mSelectedSprites.Count), mSelectedSprites.Count));
                var bottomInfoSize = EditorStyles.boldLabel.CalcSize(bottomInfo);
                EditorGUI.LabelField(new Rect(r.x + r.width * 0.5f - bottomInfoSize.x * 0.5f, r.yMax, bottomInfoSize.x, bottomInfoSize.y), bottomInfo, EditorStyles.boldLabel);
            }
        }

        private void OnPreviewBin(Rect r, AtlasRaw atlas, BinRaw bin)
        {
            GUI.BeginClip(r);
            var offscale = mTexViewParams;
            var offset = new Vector2(r.width * (offscale.z - 1) * 0.5f,
                r.height * (offscale.z - 1) * 0.5f);
            var texRect = new Rect(
                offscale.x - offset.x,
                offscale.y - offset.y,
                r.width * offscale.z,
                r.height * offscale.z);
            DrawGrid(new Rect(0, 0, r.width, r.height));
            DrawTexture(texRect, bin.main, bin.addition, new Rect(0, 0, r.width, r.height), Rect.zero);
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            var eventType = Event.current.GetTypeForControl(controlID);
            switch (eventType)
            {
                case EventType.ScrollWheel:
                    {
                        if (new Rect(0, 0, r.width, r.height).Contains(Event.current.mousePosition))
                        {
                            offscale.z = offscale.z - Event.current.delta.y / 15;
                            offscale.z = Mathf.Max(offscale.z, 1);
                            offset = new Vector2(r.width * (offscale.z - 1) * 0.5f,
                                r.height * (offscale.z - 1) * 0.5f);
                            offscale.x = Mathf.Clamp(offscale.x, -offset.x, offset.x);
                            offscale.y = Mathf.Clamp(offscale.y, -offset.y, offset.y);
                            Event.current.Use();
                        }
                        break;
                    }
                case EventType.MouseDrag:
                    {
                        if (GUIUtility.hotControl == controlID)
                        {
                            var pos = GUIUtility.QueryStateObject(typeof(List<int>), controlID) as List<int>;
                            if (pos[0] <= r.width && pos[1] <= r.height)
                            {
                                offscale.x += Event.current.delta.x;
                                offscale.y += Event.current.delta.y;
                                offscale.x = Mathf.Clamp(offscale.x, -offset.x, offset.x);
                                offscale.y = Mathf.Clamp(offscale.y, -offset.y, offset.y);
                                Event.current.Use();
                            }
                        }
                        break;
                    }
                case EventType.MouseDown:
                    {
                        var pos = GUIUtility.GetStateObject(typeof(List<int>), controlID) as List<int>;
                        pos.Clear();
                        pos.Add((int)Event.current.mousePosition.x);
                        pos.Add((int)Event.current.mousePosition.y);
                        GUIUtility.hotControl = controlID;
                        break;
                    }
                case EventType.MouseUp:
                    {
                        if (GUIUtility.hotControl == controlID)
                        {
                            var pos = GUIUtility.QueryStateObject(typeof(List<int>), controlID) as List<int>;
                            if (pos != null &&
                                Math.Abs(pos[0] - (int)Event.current.mousePosition.x) <= 1 &&
                                Math.Abs(pos[1] - (int)Event.current.mousePosition.y) <= 1)
                            {
                                var rPos = new Vector2(
                                    (pos[0] - texRect.x) / texRect.width,
                                    (pos[1] - texRect.y) / texRect.height
                                );
                                for (int i = 0; i < bin.sprites.Length; i++)
                                {
                                    var sprite = bin.sprites[i];
                                    var rect = sprite.rect;
                                    var rRect = new Rect(
                                        rect.x / bin.main.width,
                                        (bin.main.height - rect.y - rect.height) / bin.main.height,
                                        rect.width / bin.main.width,
                                        rect.height / bin.main.height
                                    );
                                    if (rRect.Contains(rPos))
                                    {
                                        if (Event.current.button == 0)
                                        {
                                            if (!Event.current.control)
                                            {
                                                mSelectedSprites.Clear();
                                                mSelectedSprites.Add(new SelectedSprite(sprite.bin, i));
                                            }
                                            else
                                            {
                                                if (mSelectedSprites.Any(s => s.bin == sprite.bin && s.sprite == i))
                                                    mSelectedSprites.RemoveAll(s => s.bin == sprite.bin && s.sprite == i);
                                                else
                                                    mSelectedSprites.Add(new SelectedSprite(sprite.bin, i));
                                            }
                                        }
                                        else if (Event.current.button == 1)
                                        {
                                            DisplayOperateMenu(atlas);
                                        }
                                        GUI.FocusControl("");
                                        Event.current.Use();
                                        break;
                                    }
                                }
                            }
                            GUIUtility.hotControl = 0;
                        }
                        break;
                    }
            }
            foreach (var selectedSprite in mSelectedSprites)
            {
                if (atlas.bins[selectedSprite.bin] == bin)
                {
                    var sprite = bin.sprites[selectedSprite.sprite];
                    var rect = sprite.rect;
                    var rRect = new Rect(
                        rect.x / bin.main.width,
                        (bin.main.height - rect.y - rect.height) / bin.main.height,
                        rect.width / bin.main.width,
                        rect.height / bin.main.height
                    );
                        var tRect = new Rect(
                            rRect.x * texRect.width + texRect.x,
                            rRect.y * texRect.height + texRect.y,
                            rRect.width * texRect.width,
                            rRect.height * texRect.height
                        );
                        Handles.color = Color.green;
                        Handles.DrawPolyLine(new Vector3[] {
                        new Vector3(tRect.x, tRect.y, 0),
                        new Vector3(tRect.xMax, tRect.y, 0),
                        new Vector3(tRect.xMax, tRect.yMax, 0),
                        new Vector3(tRect.x, tRect.yMax, 0),
                        new Vector3(tRect.x, tRect.y, 0)
                    });
                    Handles.color = Color.white;
                }
            }
            GUI.EndClip();
            mTexViewParams = offscale;
        }

        private void OnFindResultGUI(AtlasRaw atlas)
        {
            int itemHeight = 20;
            var gridRect = EditorGUILayout.GetControlRect(false, mFindResult.Length * itemHeight);
            for (int i = 0; i < mFindResult.Length; i++)
            {
                var sprite = mFindResult[i];
                var itemRect = new Rect(gridRect.x, gridRect.y, gridRect.width, itemHeight);
                var iBin = -1;
                var iSprite = -1;
                PackUtil.IndexSprite(atlas, sprite, out iBin, out iSprite);
                if (iBin != -1 && iSprite != -1 &&
                    mSelectedSprites.Any(s => s.bin == iBin && s.sprite == iSprite))
                {
                    EditorGUI.DrawRect(itemRect, SelectedColor);
                }
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
                        if (!Event.current.control)
                        {
                            if (Event.current.shift && mSelectedSprites.Count > 0)
                            {
                                for (int s = mSelectedSprites.Count - 1; s >= 0; s--)
                                {
                                    var index = Array.FindIndex(mFindResult, r => r == atlas.bins[mSelectedSprites[s].bin].sprites[mSelectedSprites[s].sprite]);
                                    if (index >= 0)
                                    {
                                        for (int m = Mathf.Min(index, i); m <= Mathf.Max(index, i); m++)
                                        {
                                            var sp = mFindResult[m];
                                            var sBin = -1;
                                            var sSprite = -1;
                                            PackUtil.IndexSprite(atlas, sp, out sBin, out sSprite);
                                            if (sBin != -1 && sSprite != -1 &&
                                                !mSelectedSprites.Any(ss => ss.bin == sBin && ss.sprite == sSprite))
                                            {
                                                mSelectedSprites.Add(new SelectedSprite(sBin, sSprite));
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                mSelectedSprites.Clear();
                                mSelectedSprites.Add(new SelectedSprite(iBin, iSprite));
                            }
                        }
                        else
                        {
                            if (mSelectedSprites.Any(s => s.bin == iBin && s.sprite == iSprite))
                                mSelectedSprites.RemoveAll(s => s.bin == iBin && s.sprite == iSprite);
                            else
                                mSelectedSprites.Add(new SelectedSprite(iBin, iSprite));
                        }
                    }
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
            int keyControlId = GUIUtility.GetControlID(FocusType.Keyboard);
            var keyEventType = Event.current.GetTypeForControl(keyControlId);
            if (keyEventType == EventType.KeyUp &&
                Event.current.control &&
                Event.current.keyCode == KeyCode.A)
            {
                mSelectedSprites.Clear();
                for (int i = 0; i < mFindResult.Length; i++)
                {
                    var sprite = mFindResult[i];
                    var iBin = -1;
                    var iSprite = -1;
                    PackUtil.IndexSprite(atlas, sprite, out iBin, out iSprite);
                    if (iBin != -1 && iSprite != -1)
                    {
                        mSelectedSprites.Add(new SelectedSprite(iBin, iSprite));
                    }
                }
                Event.current.Use();
            }
        }

        private Rect DrawSpriteInRect(Texture2D texture, Texture2D alpha, SpriteRaw sprite, Rect r, Vector2 margin)
        {
            var spw = sprite.rect.width;
            var sph = sprite.rect.height;
            if (spw / sph >= r.width / r.height)
            {
                if (spw > r.width - margin.x)
                {
                    spw = Math.Max(r.width - margin.x, 1);
                    sph = sprite.rect.height / sprite.rect.width * spw;
                }
            }
            else
            {
                if (sph > r.height - margin.y)
                {
                    sph = Math.Max(r.height - margin.y, 1);
                    spw = sprite.rect.width / sprite.rect.height * sph;
                }
            }
            var previewRect = new Rect(
                r.x + (r.width - spw) * 0.5f,
                r.y + (r.height - sph) * 0.5f,
                spw,
                sph
            );
            var uvRect = new Rect(
                sprite.rect.x / texture.width,
                sprite.rect.y / texture.height,
                sprite.rect.width / texture.width,
                sprite.rect.height / texture.height);
            DrawGrid(previewRect);
            DrawTexture(previewRect, texture, alpha, Rect.zero, uvRect);
            return previewRect;
        }

        private void DrawGrid(Rect rect)
        {
            if (mGridMat == null)
            {
                mGridMat = new Material(Shader.Find("Unlit/Texture"));
            }
            var mainTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/AtlaS/Resource/grid.jpg");
            mGridMat.SetTexture("_MainTex", mainTex);
            mGridMat.SetPass(0);

            var scale = new Vector2(rect.width / 40, rect.height / 40);
            mGridMesh = mGridMesh ?? new Mesh();
            mGridMesh.vertices = new Vector3[]
            {
                new Vector3(rect.x, rect.y, 0),
                new Vector3(rect.xMax, rect.y, 0),
                new Vector3(rect.xMax, rect.yMax, 0),
                new Vector3(rect.x, rect.yMax, 0),
            };
            mGridMesh.uv = new Vector2[]
            {
                new Vector2(0, scale.y),
                new Vector2(scale.x, scale.y),
                new Vector2(scale.x, 0),
                new Vector2(0, 0),
            };
            mGridMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };

            Graphics.DrawMeshNow(mGridMesh, Vector3.zero, Quaternion.identity);
        }

        private void DrawTexture(Rect rect, Texture2D mainTex, Texture2D alphaTex, Rect clipRect, Rect uvRect)
        {
            mPreviewMat = mPreviewMat ?? new Material(Shader.Find("UI/Default"));
            mPreviewMat.shader = Shader.Find(alphaTex != null ? "UI/DefaultETC1" : "UI/Default");
            mPreviewMat.SetTexture("_MainTex", mainTex);
            if (alphaTex != null) mPreviewMat.SetTexture("_AlphaTex", alphaTex);
            mPreviewMat.SetVector("_ClipRect", clipRect.width > 0 ?
                new Vector4(clipRect.x, clipRect.y, clipRect.xMax, clipRect.yMax) :
                new Vector4(0, 0, float.MaxValue, float.MaxValue));
            mPreviewMat.SetPass(0);

            var offset = Vector2.zero;
            var scale = Vector2.one;
            if (uvRect.width > 0)
            {
                offset = new Vector2(uvRect.x, uvRect.y);
                scale = new Vector2(uvRect.width, uvRect.height);
            }
            mPreviewMesh = mPreviewMesh ?? new Mesh();
            mPreviewMesh.vertices = new Vector3[]
            {
                new Vector3(rect.x, rect.y, 0),
                new Vector3(rect.xMax, rect.y, 0),
                new Vector3(rect.xMax, rect.yMax, 0),
                new Vector3(rect.x, rect.yMax, 0),
            };
            mPreviewMesh.uv = new Vector2[]
            {
                new Vector2(offset.x, offset.y + scale.y),
                new Vector2(offset.x + scale.x, offset.y + scale.y),
                new Vector2(offset.x + scale.x, offset.y),
                new Vector2(offset.x, offset.y),
            };
            mPreviewMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };

            Graphics.DrawMeshNow(mPreviewMesh, Vector3.zero, Quaternion.identity);
        }

        private void DisplayOperateMenu(AtlasRaw atlas)
        {
            var menu = new GenericMenu();
            if (mSelectedSprites.Count > 0)
            {
                var qualityNames = Enum.GetNames(typeof(PackQuality));
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
                menu.AddDisabledItem(new GUIContent("Delete"));
            }
            menu.ShowAsContext();
        }

        private void ExportSelected(AtlasRaw atlas, List<SelectedSprite> selected)
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

        private void DeleteSelected(AtlasRaw atlas, List<SelectedSprite> selected)
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

        private void CompressSelected(AtlasRaw atlas, PackQuality quality, List<SelectedSprite> selected)
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
                        var texture = new PackAssetSprite(image);
                        texture.name = assetLabel;
                        texture.quality = quality;
                        addTexture(texture);
                    }
                }
            }
            AtlasPacker.Repack(atlas, textures.ToArray());
        }

        [Serializable]
        private class SelectedSprite
        {
            public int bin;

            public int sprite;

            public SelectedSprite(int bin, int sprite)
            {
                this.bin = bin;
                this.sprite = sprite;
            }
        }
    }
}
#endif