#if AtlaS_ON
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI.Atlas;

namespace UnityEditor.UI.Atlas
{
    public class BinPreviewer
    {
        private bool mMultiSelectEnabled = false;

        private Vector2 mOffset = Vector2.zero;

        private float mScale = 1;

        public BinPreviewer(bool multiSelectEnabled)
        {
            mMultiSelectEnabled = multiSelectEnabled;
        }

        public void OnGUI(Rect rect, AtlasRaw atlas, BinRaw bin, List<SpriteIndex> selected)
        {
            GUI.BeginClip(rect);
            var localRect = new Rect(0, 0, rect.width, rect.height);
            var texDragOffset = mOffset;
            var texScale = mScale;
            var texScaleOffset = new Vector2(
                localRect.width * (texScale - 1) * 0.5f,
                localRect.height * (texScale - 1) * 0.5f);
            var texOffset = texDragOffset - texScaleOffset;
            var texSize = localRect.size * texScale;
            AtlasEditorUtil.DrawGrid(localRect);
            AtlasEditorUtil.DrawSprite(localRect, bin.main, bin.addition,
                new Rect(-texOffset.x / texSize.x, (texDragOffset.y + texScaleOffset.y) / texSize.y, localRect.width / texSize.x, localRect.height / texSize.y));
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            var eventType = Event.current.GetTypeForControl(controlID);
            switch (eventType)
            {
                case EventType.ScrollWheel:
                    {
                        if (localRect.Contains(Event.current.mousePosition))
                        {
                            var scaleDelta = 15;
                            texScale -= Event.current.delta.y / scaleDelta;
                            texScale = Mathf.Max(texScale, 1);
                            texScaleOffset = new Vector2(localRect.width * (texScale - 1) * 0.5f,
                                localRect.height * (texScale - 1) * 0.5f);
                            texDragOffset.x = Mathf.Clamp(texDragOffset.x, -texScaleOffset.x, texScaleOffset.x);
                            texDragOffset.y = Mathf.Clamp(texDragOffset.y, -texScaleOffset.y, texScaleOffset.y);
                            Event.current.Use();
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
                case EventType.MouseDrag:
                    {
                        if (GUIUtility.hotControl == controlID)
                        {
                            var pos = GUIUtility.QueryStateObject(typeof(List<int>), controlID) as List<int>;
                            if (pos[0] <= localRect.width && pos[1] <= localRect.height)
                            {
                                texDragOffset.x += Event.current.delta.x;
                                texDragOffset.y += Event.current.delta.y;
                                texDragOffset.x = Mathf.Clamp(texDragOffset.x, -texScaleOffset.x, texScaleOffset.x);
                                texDragOffset.y = Mathf.Clamp(texDragOffset.y, -texScaleOffset.y, texScaleOffset.y);
                                Event.current.Use();
                            }
                        }
                        break;
                    }
                case EventType.MouseUp:
                    {
                        if (GUIUtility.hotControl == controlID)
                        {
                            var pos = GUIUtility.QueryStateObject(typeof(List<int>), controlID) as List<int>;
                            if (pos != null &&
                                Mathf.Abs(pos[0] - (int)Event.current.mousePosition.x) <= 1 &&
                                Mathf.Abs(pos[1] - (int)Event.current.mousePosition.y) <= 1)
                            {
                                var rPos = new Vector2(
                                    (pos[0] - texOffset.x) / texSize.x,
                                    (pos[1] - texOffset.y) / texSize.y
                                );
                                for (var i = 0; i < bin.sprites.Length; i++)
                                {
                                    var sprite = bin.sprites[i];
                                    var spriteRect = sprite.rect;
                                    var rRect = new Rect(
                                        spriteRect.x / bin.main.width,
                                        (bin.main.height - spriteRect.y - spriteRect.height) / bin.main.height,
                                        spriteRect.width / bin.main.width,
                                        spriteRect.height / bin.main.height
                                    );
                                    if (rRect.Contains(rPos))
                                    {
                                        var index = new SpriteIndex(0, sprite.bin, i);
                                        if (Event.current.button == 0)
                                        {
                                            if (!mMultiSelectEnabled || 
                                                !Event.current.control)
                                            {
                                                selected.Clear();
                                                selected.Add(index);
                                            }
                                            else
                                            {
                                                if (selected.Any(s => s.bin == index.bin && s.sprite == index.sprite))
                                                    selected.RemoveAll(s => s.bin == index.bin && s.sprite == index.sprite);
                                                else
                                                    selected.Add(index);
                                            }
                                            GUI.FocusControl("");
                                            Event.current.Use();
                                        }
                                        break;
                                    }
                                }
                            }
                            GUIUtility.hotControl = 0;
                        }
                        break;
                    }
            }
            foreach (var index in selected)
            {
                if (atlas.bins[index.bin] == bin)
                {
                    var sprite = bin.sprites[index.sprite];
                    var spriteRect = sprite.rect;
                    var rRect = new Rect(
                        spriteRect.x / bin.main.width,
                        (bin.main.height - spriteRect.y - spriteRect.height) / bin.main.height,
                        spriteRect.width / bin.main.width,
                        spriteRect.height / bin.main.height
                    );
                    var tRect = new Rect(
                        rRect.x * texSize.x + texOffset.x,
                        rRect.y * texSize.y + texOffset.y,
                        rRect.width * texSize.x,
                        rRect.height * texSize.y
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
            mOffset = texDragOffset;
            mScale = texScale;
        }
    }
}
#endif