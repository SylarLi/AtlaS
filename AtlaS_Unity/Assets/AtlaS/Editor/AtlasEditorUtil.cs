#if AtlaS_ON
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Atlas;

namespace UnityEditor.UI.Atlas
{
    public sealed class AtlasEditorUtil
    {
        private static string GridTex = "Assets/AtlaS/Resource/grid.jpg";
        private static int GridSize = 40;
        private static Material GridMat;
        private static Mesh GridMesh;

        public static void DrawGrid(Rect rect)
        {
            if (GridMat == null)
            {
                GridMat = new Material(Shader.Find("Unlit/Texture"));
                var mainTex = AssetDatabase.LoadAssetAtPath<Texture2D>(GridTex);
                GridMat.SetTexture("_MainTex", mainTex);
            }
            GridMat.SetPass(0);
            var scale = new Vector2(rect.width / GridSize, rect.height / GridSize);
            GridMesh = GridMesh ?? new Mesh();
            GridMesh.vertices = new Vector3[]
            {
                new Vector3(rect.x, rect.y, 0),
                new Vector3(rect.xMax, rect.y, 0),
                new Vector3(rect.xMax, rect.yMax, 0),
                new Vector3(rect.x, rect.yMax, 0),
            };
            GridMesh.uv = new Vector2[]
            {
                new Vector2(0, scale.y),
                new Vector2(scale.x, scale.y),
                new Vector2(scale.x, 0),
                new Vector2(0, 0),
            };
            GridMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            Graphics.DrawMeshNow(GridMesh, Vector3.zero, Quaternion.identity);
        }

        private static Material SpriteMat;
        private static Mesh SpriteMesh;

        public static void DrawSprite(Rect rect, Texture2D mainTex, Texture2D alphaTex, Rect uvRect)
        {
            SpriteMat = SpriteMat ?? new Material(Shader.Find("UI/Default"));
            SpriteMat.shader = Shader.Find(alphaTex != null ? "UI/DefaultETC1" : "UI/Default");
            SpriteMat.SetTexture("_MainTex", mainTex);
            if (alphaTex != null) SpriteMat.SetTexture("_AlphaTex", alphaTex);
            SpriteMat.SetVector("_ClipRect", new Vector4(0, 0, float.MaxValue, float.MaxValue));
            SpriteMat.SetPass(0);
            var offset = new Vector2(uvRect.x, uvRect.y);
            var scale = new Vector2(uvRect.width, uvRect.height);
            SpriteMesh = SpriteMesh ?? new Mesh();
            SpriteMesh.vertices = new Vector3[]
            {
                new Vector3(rect.x, rect.y, 0),
                new Vector3(rect.xMax, rect.y, 0),
                new Vector3(rect.xMax, rect.yMax, 0),
                new Vector3(rect.x, rect.yMax, 0),
            };
            SpriteMesh.uv = new Vector2[]
            {
                new Vector2(offset.x, offset.y + scale.y),
                new Vector2(offset.x + scale.x, offset.y + scale.y),
                new Vector2(offset.x + scale.x, offset.y),
                new Vector2(offset.x, offset.y),
            };
            SpriteMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            Graphics.DrawMeshNow(SpriteMesh, Vector3.zero, Quaternion.identity);
        }

        public static Rect DrawSpriteInRect(Texture2D main, Texture2D alpha, SpriteRaw sprite, Rect rect, Vector2 margin)
        {
            var spw = sprite.rect.width;
            var sph = sprite.rect.height;
            if (spw / sph >= rect.width / rect.height)
            {
                if (spw > rect.width - margin.x)
                {
                    spw = Mathf.Max(rect.width - margin.x, 1);
                    sph = sprite.rect.height / sprite.rect.width * spw;
                }
            }
            else
            {
                if (sph > rect.height - margin.y)
                {
                    sph = Mathf.Max(rect.height - margin.y, 1);
                    spw = sprite.rect.width / sprite.rect.height * sph;
                }
            }
            var previewRect = new Rect(
                rect.x + (rect.width - spw) * 0.5f,
                rect.y + (rect.height - sph) * 0.5f,
                spw,
                sph
            );
            var uvRect = new Rect(
                sprite.rect.x / main.width,
                sprite.rect.y / main.height,
                sprite.rect.width / main.width,
                sprite.rect.height / main.height);
            DrawGrid(previewRect);
            DrawSprite(previewRect, main, alpha, uvRect);
            return previewRect;
        }

        public static SpriteIndex[] SearchSprites(AtlasRaw atlas, int atlasIndex, string matchWord)
        {
            matchWord = matchWord.ToLower();
            var list = new List<SpriteIndex>();
            foreach (var bin in atlas.bins)
            {
                var sprites = bin.sprites;
                for (int i = 0; i < sprites.Length; i++)
                {
                    if (sprites[i].name.ToLower().Contains(matchWord))
                    {
                        list.Add(new SpriteIndex(atlasIndex, sprites[i].bin, i));
                    }
                }
            }
            return list.ToArray();
        }

        public static string SearchBar(GUIContent label, string words)
        {
            var rect = EditorGUILayout.GetControlRect(false, EditorStyles.toolbar.fixedHeight);
            return SearchBar(rect, label, words);
        }

        public static string SearchBar(Rect rect, GUIContent label, string words)
        {
            var controlId = GUIUtility.GetControlID(FocusType.Passive);
            var eventType = Event.current.GetTypeForControl(controlId);
            if (eventType == EventType.Repaint)
            {
                EditorStyles.toolbar.Draw(rect, GUIContent.none, controlId);
            }
            return EditorGUI.TextField(new Rect(10, rect.y, rect.width - 10, rect.height), label, words, EditorStyles.toolbarTextField);
        }

        public static void TitleBar(GUIContent label)
        {
            var rect = EditorGUILayout.GetControlRect(false, EditorStyles.toolbar.fixedHeight);
            TitleBar(rect, label);
        }

        public static void TitleBar(Rect rect, GUIContent label)
        {
            var controlId = GUIUtility.GetControlID(FocusType.Passive);
            var eventType = Event.current.GetTypeForControl(controlId);
            if (eventType == EventType.Repaint)
            {
                EditorStyles.toolbar.Draw(rect, GUIContent.none, controlId);
                EditorGUI.LabelField(rect, label, EditorStyles.centeredGreyMiniLabel);
            }
        }

        public static int TitleBar(GUIContent[] buttons)
        {
            var rect = EditorGUILayout.GetControlRect(false, EditorStyles.toolbar.fixedHeight);
            return TitleBar(rect, buttons);
        }

        public static int TitleBar(Rect rect, GUIContent[] buttons)
        {
            var index = -1;
            var controlId = GUIUtility.GetControlID(FocusType.Passive);
            var eventType = Event.current.GetTypeForControl(controlId);
            if (eventType == EventType.Repaint)
            {
                EditorStyles.toolbar.Draw(rect, GUIContent.none, controlId);
            }
            float offset = 0f;
            for (int i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                var width = EditorStyles.toolbarButton.CalcSize(button).x + 10;
                if (GUI.Button(new Rect(rect.x + offset, rect.y, width, rect.height), button, EditorStyles.toolbarButton))
                {
                    index = i;
                }
                offset += width;
            }
            return index;
        }

        public static bool ToggleBar(GUIContent label, bool isOn)
        {
            var rect = EditorGUILayout.GetControlRect(false, EditorStyles.toolbarButton.fixedHeight);
            return ToggleBar(rect, label, isOn);
        }

        public static bool ToggleBar(Rect rect, GUIContent label, bool isOn)
        {
            var controlId = GUIUtility.GetControlID(FocusType.Passive);
            var eventType = Event.current.GetTypeForControl(controlId);
            switch (eventType)
            {
                case EventType.Repaint:
                    {
                        EditorStyles.toolbarButton.Draw(rect, GUIContent.none, controlId, isOn);
                        var height = EditorStyles.foldout.CalcHeight(label, rect.width);
                        EditorStyles.foldout.Draw(new Rect(rect.x + 5, rect.y + (rect.height - height) * 0.5f, rect.width, height), label, controlId, isOn);
                        break;
                    }
                case EventType.MouseDown:
                    {
                        if (rect.Contains(Event.current.mousePosition))
                        {
                            isOn = !isOn;
                            Event.current.Use();
                        }
                        break;
                    }
            }
            return isOn;
        }
    }
}
#endif