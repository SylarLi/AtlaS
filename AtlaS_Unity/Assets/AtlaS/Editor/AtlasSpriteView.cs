#if AtlaS_ON
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI.Atlas;

namespace UnityEditor.UI.Atlas
{
    [Serializable]
    public sealed class AtlasSpriteView : EditorWindow
    {
        public static AtlasSpriteView Display()
        {
            var window = GetWindow<AtlasSpriteView>();
            window.titleContent = new GUIContent("Select Sprite");
            var position = window.position;
            position.x = EditorPrefs.GetFloat("AtlasSpriteViewX", 100f);
            position.y = EditorPrefs.GetFloat("AtlasSpriteViewY", 100f);
            position.width = EditorPrefs.GetFloat("AtlasSpriteViewWidth", 200f);
            position.height = EditorPrefs.GetFloat("AtlasSpriteViewHeight", 390f);
            window.position = position;
            window.VSplit = EditorPrefs.GetFloat("AtlasSpriteViewVSplit", 100f);
            window.ShowAuxWindow();
            return window;
        }

        private static readonly Color ColorAtlasItem = new Color(0, 0, 0, 0.4f);
        private static readonly Color ColorSelected = new Color(62f / 255, 95f / 255, 150 / 255f);

        private static GUIStyle SpritItemLabelStyle;
        private static GUIStyle GetSpritItemLabelStyle()
        {
            if (SpritItemLabelStyle == null)
            {
                SpritItemLabelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                SpritItemLabelStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 1);
            }
            return SpritItemLabelStyle;
        }

        [SerializeField]
        private float mVSplit = 100;
        public float VSplit { set { mVSplit = value; } }

        [SerializeField]
        private string mWords = "";

        [NonSerialized]
        private bool mTextFocused = false;

        [NonSerialized]
        private AtlasCache mCache = new AtlasCache();

        [NonSerialized]
        private int[] mAtlases = new int[0];

        [NonSerialized]
        private SpriteIndex[] mSprites = new SpriteIndex[0];

        [NonSerialized]
        private SpriteIndex[] mFilter = new SpriteIndex[0];

        [NonSerialized]
        private Vector2 mAtlasScrPos = Vector2.zero;

        [NonSerialized]
        private int mSelectedAtlas = -1;

        [NonSerialized]
        private SpriteIndex mSelectedSprite;

        public SpriteIndex selectedSprite
        {
            get
            {
                return mSelectedSprite;
            }
            set
            {
                mSelectedSprite = value;
                if (mSelectedSprite != null)
                {
                    AtlasRaw atlas;
                    BinRaw bin;
                    SpriteRaw sprite;
                    if (mCache.Fetch(mSelectedSprite, out atlas, out bin, out sprite))
                    {
                        onSelectSprite(atlas, sprite.name);
                    }
                }
                else
                {
                    onSelectSprite(null, null);
                }
            }
        }

        public delegate void OnSelectSprite(AtlasRaw atlas, string spriteName);

        public OnSelectSprite onSelectSprite = (atlas, spriteName) => { };

        [NonSerialized]
        private bool mCloseMark;

        private void FilterAtlas(int atlasIndex)
        {
            if (mSelectedAtlas == atlasIndex)
            {
                mSelectedAtlas = -1;
                mFilter = mSprites;
            }
            else
            {
                mSelectedAtlas = atlasIndex;
                mFilter = (from spriteIndex in mSprites
                           where spriteIndex.atlas == mSelectedAtlas
                           select spriteIndex)
                           .ToArray();
            }
        }

        public void SetInitSprite(AtlasRaw atlasRaw, string spriteName)
        {
            mWords = "";
            mCache.Match(mWords, out mSprites, out mAtlases);
            mCache.Fetch(atlasRaw, spriteName, out mSelectedSprite);
            FilterAtlas(mSelectedSprite != null ? mSelectedSprite.atlas : -1);
        }

        private void OnGUI()
        {
            DrawSearchArea();
            DrawAtlasList();
            DrawSpriteList();
            DrawSplitter();
            HandleCommonEvent();
            CheckWindowCloseMark();
        }

        private void DrawSearchArea()
        {
            EditorGUI.BeginChangeCheck();
            GUI.SetNextControlName("SearchTextField");
            mWords = EditorGUILayout.TextField(mWords);
            if (!mTextFocused)
            {
                mTextFocused = true;
                GUI.FocusControl("SearchTextField");
                EditorGUIUtility.editingTextField = true;
            }
            if (EditorGUI.EndChangeCheck())
            {
                mCache.Match(mWords, out mSprites, out mAtlases);
                FilterAtlas(-1);
            }
        }

        private void DrawAtlasList()
        {
            var rect = new Rect(0, 20, mVSplit - 5, position.height - 20);
            var titleRect = new Rect(rect.x, rect.y, rect.width + 5, EditorStyles.toolbar.fixedHeight);
            AtlasEditorUtil.TitleBar(titleRect, new GUIContent("Atlas"));
            var itemHeight = 20;
            var itemSpace = 2;
            var viewRect = new Rect(0, 0, titleRect.width, position.height - titleRect.y - titleRect.height);
            var contentRect = new Rect(rect.x, titleRect.y + titleRect.height, viewRect.width, Mathf.Max((itemHeight + itemSpace) * mAtlases.Length, viewRect.height));
            mAtlasScrPos = GUI.BeginScrollView(contentRect, mAtlasScrPos, viewRect);
            var controlId = GUIUtility.GetControlID(FocusType.Passive);
            var eventType = Event.current.GetTypeForControl(controlId);
            var index = 0;
            foreach (var atlasIndex in mAtlases)
            {
                AtlasRaw atlas;
                if (mCache.Fetch(atlasIndex, out atlas))
                {
                    var itemRect = new Rect(0, index * (itemHeight + itemSpace), contentRect.width, itemHeight);
                    EditorGUI.DrawRect(itemRect, mSelectedAtlas == atlasIndex ? ColorSelected : ColorAtlasItem);
                    EditorGUI.LabelField(new Rect(itemRect.x + 5, itemRect.y + 2, itemRect.width, itemRect.height), new GUIContent(atlas.name));
                    var clickRect = new Rect(itemRect.x, itemRect.y, rect.width, itemHeight);
                    if (eventType == EventType.MouseDown &&
                        clickRect.Contains(Event.current.mousePosition))
                    {
                        FilterAtlas(atlasIndex);
                        Event.current.Use();
                    }
                    index += 1;
                }
            }
            GUI.EndScrollView();
        }

        private void DrawSpriteList()
        {
            var rect = new Rect(mVSplit + 5, 20, position.width - mVSplit - 5, position.height - 20);
            var titleRect = new Rect(rect.x - 5, rect.y, rect.width + 5, EditorStyles.toolbar.fixedHeight);
            AtlasEditorUtil.TitleBar(titleRect, new GUIContent("Sprite"));
            var padding = new Vector2(20, 20);
            var itemSize = new Vector2(100, 120);
            var itemSpace = new Vector2(20, 20);
            var columns = Mathf.Max(Mathf.FloorToInt((titleRect.width - padding.x) / (itemSize.x + itemSpace.x)), 1);
            var rows = Mathf.CeilToInt((float)(mFilter.Length + 1) / columns);
            var viewRect = new Rect(0, 0, titleRect.width, position.height - titleRect.y - titleRect.height);
            var contentRect = new Rect(rect.x, titleRect.y + titleRect.height, viewRect.width, padding.y + Mathf.Max((itemSize.y + itemSpace.y) * rows, viewRect.height));
            GUI.BeginGroup(contentRect);
            var controlId = GUIUtility.GetControlID(FocusType.Passive);
            var eventType = Event.current.GetTypeForControl(controlId);
            var index = 0;
            var count = Mathf.CeilToInt(viewRect.height / (itemSize.y + itemSpace.y)) * columns;
            count = Mathf.Min(mFilter.Length + 1, count);
            for (int i = 0; i < count; i++)
            {
                var clicked = false;
                if (i == 0)
                {
                    var previewRect = new Rect(padding.x, padding.y, itemSize.x, 100);
                    AtlasEditorUtil.DrawGrid(previewRect);
                    var selectedRect = new Rect(previewRect.x, previewRect.y + previewRect.height + 2, previewRect.width, 18);
                    if (selectedSprite == null)
                    {
                        EditorGUI.DrawRect(selectedRect, ColorSelected);
                    }
                    var labelRect = new Rect(selectedRect.x, selectedRect.y, selectedRect.width, selectedRect.height);
                    EditorGUI.LabelField(labelRect, "None", GetSpritItemLabelStyle());
                    var clickRect = new Rect(previewRect.x, previewRect.y, previewRect.width, itemSize.y);
                    if (eventType == EventType.MouseDown &&
                        clickRect.Contains(Event.current.mousePosition))
                    {
                        selectedSprite = null;
                        clicked = true;
                        Event.current.Use();
                    }
                    index += 1;
                }
                else
                {
                    var spriteIndex = mFilter[i - 1];
                    AtlasRaw atlas;
                    BinRaw bin;
                    SpriteRaw sprite;
                    if (mCache.Fetch(spriteIndex, out atlas, out bin, out sprite))
                    {
                        var row = index / columns;
                        var column = index % columns;
                        var previewRect = new Rect(
                            padding.x + column * (itemSize.x + itemSpace.x),
                            padding.y + row * (itemSize.y + itemSpace.y),
                            itemSize.x, 100);
                        AtlasEditorUtil.DrawSpriteInRect(bin.main, bin.addition, sprite, previewRect, Vector2.zero);
                        var selectedRect = new Rect(previewRect.x, previewRect.y + previewRect.height + 2, previewRect.width, 18);
                        if (spriteIndex.Equals(selectedSprite))
                        {
                            EditorGUI.DrawRect(selectedRect, ColorSelected);
                        }
                        var labelRect = new Rect(selectedRect.x, selectedRect.y, selectedRect.width, selectedRect.height);
                        EditorGUI.LabelField(labelRect, sprite.name, GetSpritItemLabelStyle());
                        var clickRect = new Rect(previewRect.x, previewRect.y, previewRect.width, itemSize.y);
                        if (eventType == EventType.MouseDown &&
                            clickRect.Contains(Event.current.mousePosition))
                        {
                            selectedSprite = new SpriteIndex(spriteIndex.atlas, spriteIndex.bin, spriteIndex.sprite);
                            clicked = true;
                            Event.current.Use();
                        }
                        index += 1;
                    }
                }
                if (clicked && Event.current.clickCount == 2)
                {
                    MarkCloseWindow();
                }
            }
            var countLabel = new GUIContent((count - 1) + "/" + mFilter.Length);
            var countSize = EditorStyles.miniLabel.CalcSize(countLabel);
            var countRect = new Rect(viewRect.xMax - countSize.x - 10, viewRect.yMax - countSize.y, countSize.x, countSize.y);
            GUI.Label(countRect, countLabel, EditorStyles.miniLabel);
            GUI.EndGroup();
        }

        private void DrawSplitter()
        {
            EditorGUI.DrawRect(new Rect(mVSplit, 20, 1, position.height), Color.gray);
            var rect = new Rect(mVSplit - 5, 20, 10, position.height);
            var controlId = GUIUtility.GetControlID(FocusType.Passive);
            var eventType = Event.current.GetTypeForControl(controlId);
            switch (eventType)
            {
                case EventType.MouseDown:
                    {
                        if (rect.Contains(Event.current.mousePosition))
                        {
                            GUIUtility.hotControl = controlId;
                            Event.current.Use();
                        }
                        break;
                    }
                case EventType.MouseDrag:
                    {
                        if (GUIUtility.hotControl == controlId)
                        {
                            mVSplit = Mathf.Clamp(Event.current.mousePosition.x, 10, position.width - 10);
                            Event.current.Use();
                        }
                        break;
                    }
                case EventType.MouseUp:
                    {
                        if (GUIUtility.hotControl == controlId)
                        {
                            GUIUtility.hotControl = 0;
                        }
                        break;
                    }
            }
        }

        private void HandleCommonEvent()
        {
            if (Event.current.type == EventType.KeyDown &&
                Event.current.keyCode == KeyCode.Escape)
            {
                EditorApplication.update -= CheckWindowFocused;
                MarkCloseWindow();
            }
            if (Event.current.type == EventType.MouseDown)
            {
                GUI.FocusControl("");
                Event.current.Use();
            }
        }

        private void OnEnable()
        {
            EditorApplication.update -= CheckWindowFocused;
            EditorApplication.update += CheckWindowFocused;
        }

        private void OnDisable()
        {
            EditorPrefs.SetFloat("AtlasSpriteViewX", position.x);
            EditorPrefs.SetFloat("AtlasSpriteViewY", position.y);
            EditorPrefs.SetFloat("AtlasSpriteViewWidth", position.width);
            EditorPrefs.SetFloat("AtlasSpriteViewHeight", position.height);
            EditorPrefs.SetFloat("AtlasSpriteViewVSplit", mVSplit);
        }

        private void CheckWindowFocused()
        {
            if (focusedWindow != this)
            {
                EditorApplication.update -= CheckWindowFocused;
                Close();
            }
        }

        private void MarkCloseWindow()
        {
            mCloseMark = true;
        }

        private void CheckWindowCloseMark()
        {
            if (mCloseMark)
            {
                mCloseMark = false;
                EditorApplication.update -= CheckWindowFocused;
                Close();
            }
        }

        private class AtlasCache
        {
            private bool mDirty = true;

            private List<AtlasRaw> mAtlases = new List<AtlasRaw>();

            public AtlasCache() { }

            public void Refresh()
            {
                mAtlases = (from atlas in AssetDatabase.FindAssets("t:AtlasRaw")
                    select AssetDatabase.LoadAssetAtPath<AtlasRaw>(AssetDatabase.GUIDToAssetPath(atlas))).ToList();
            }

            public void Match(string words, out SpriteIndex[] sprites, out int[] atlases)
            {
                if (mDirty)
                {
                    mDirty = false;
                    Refresh();
                }
                var rets = new List<SpriteIndex>();
                for (int i = 0; i < mAtlases.Count; i++)
                {
                    var atlas = mAtlases[i];
                    rets.AddRange(AtlasEditorUtil.SearchSprites(atlas, i, words));
                }
                sprites = rets.ToArray();
                atlases = (from sprite in sprites
                           group sprite by sprite.atlas into spriteGroup
                           select spriteGroup.Key).ToArray();
            }

            public bool Fetch(SpriteIndex index, out AtlasRaw atlas, out BinRaw bin, out SpriteRaw sprite)
            {
                atlas = null;
                bin = null;
                sprite = null;
                if (index.atlas < mAtlases.Count)
                {
                    atlas = mAtlases[index.atlas];
                    if (index.bin < atlas.bins.Length)
                    {
                        bin = atlas.bins[index.bin];
                        if (index.sprite < bin.sprites.Length)
                        {
                            sprite = bin.sprites[index.sprite];
                            return true;
                        }
                    }
                }
                return false;
            }

            public bool Fetch(AtlasRaw atlas, string spriteName, out SpriteIndex index)
            {
                index = null;
                var atlasIndex = -1;
                if (Fetch(atlas, out atlasIndex))
                {
                    for (int i = 0; i < atlas.bins.Length; i++)
                    {
                        var bin = atlas.bins[i];
                        for (int j = 0; j < bin.sprites.Length; j++)
                        {
                            var sprite = bin.sprites[j];
                            if (sprite.name == spriteName)
                            {
                                index = new SpriteIndex(atlasIndex, i, j);
                                return true;
                            }
                        }
                    }
                }
                return false;
            }

            public bool Fetch(int atlasIndex, out AtlasRaw atlas)
            {
                atlas = null;
                if (atlasIndex < mAtlases.Count)
                {
                    atlas = mAtlases[atlasIndex];
                    return true;
                }
                return false;
            }

            public bool Fetch(AtlasRaw atlas, out int atlasIndex)
            {
                atlasIndex = -1;
                var path = AssetDatabase.GetAssetPath(atlas);
                if (!string.IsNullOrEmpty(path))
                {
                    for (int i = 0; i < mAtlases.Count; i++)
                    {
                        if (AssetDatabase.GetAssetPath(mAtlases[i]) == path)
                        {
                            atlasIndex = i;
                            return true;
                        }
                    }
                }
                return false;
            }
        }
    }
}
#endif