#if AtlaS_ON
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Atlas;
using Sprite = UnityEngine.UI.Sprite;

namespace UnityEditor.UI.Atlas
{
    [CustomPropertyDrawer(typeof(Sprite), true)]
    public class SpriteDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty prop, GUIContent label)
        {
            Rect tempRect = rect;
            tempRect.height = EditorGUIUtility.singleLineHeight;
            float startX = tempRect.x;
            float startWidth = tempRect.width;
            SerializedProperty typeProp = prop.FindPropertyRelative("m_Type");
            GUIContent typeLabel = new GUIContent(prop.displayName);
            Vector2 typeSize = EditorStyles.label.CalcSize(typeLabel);
            tempRect.width = typeSize.x + 20f;
            EditorGUI.LabelField(tempRect, typeLabel);
            tempRect.x += tempRect.width;
            tempRect.width = 60;
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(tempRect, typeProp, GUIContent.none);
            Sprite.Type spriteType = (Sprite.Type)typeProp.enumValueIndex;
            if (EditorGUI.EndChangeCheck())
            {
                if (spriteType == Sprite.Type.Atlas)
                {
                    prop.FindPropertyRelative("m_Sprite").objectReferenceValue = null;
                }
                else
                {
                    prop.FindPropertyRelative("m_AtlasRaw").objectReferenceValue = null;
                    prop.FindPropertyRelative("m_SpriteName").stringValue = null;
                }
            }
            tempRect.x += tempRect.width;
            if (spriteType == Sprite.Type.Atlas)
            {
                SerializedProperty atlasRawProp = prop.FindPropertyRelative("m_AtlasRaw");
                SerializedProperty spriteNameProp = prop.FindPropertyRelative("m_SpriteName");
                tempRect.width = startWidth - (tempRect.x - startX);
                var controlId = GUIUtility.GetControlID(FocusType.Passive);
                var eventType = Event.current.GetTypeForControl(controlId);
                switch (eventType)
                {
                    case EventType.Repaint:
                        {
                            var atlasRaw = atlasRawProp.objectReferenceValue as AtlasRaw;
                            var spriteName = spriteNameProp.stringValue;
                            var nameLabel = new GUIContent(atlasRaw != null && spriteName != null ? atlasRaw.name + "/" + spriteName : "None (Sprite)");
                            EditorStyles.objectField.Draw(tempRect, nameLabel, controlId);
                            break;
                        }
                    case EventType.MouseDown:
                        {
                            EditorGUIUtility.editingTextField = false;
                            if (tempRect.Contains(Event.current.mousePosition))
                            {
                                if (GUI.enabled)
                                {
                                    var targetObjects = atlasRawProp.serializedObject.targetObjects;
                                    var atlasRawPropPath = atlasRawProp.propertyPath;
                                    var spriteNamePropPath = spriteNameProp.propertyPath;
                                    var spriteDirtyPropPath = prop.FindPropertyRelative("m_SpriteDirty").propertyPath;
                                    var selector = AtlasSpriteView.Display();
                                    selector.SetInitSprite(atlasRawProp.objectReferenceValue as AtlasRaw, spriteNameProp.stringValue);
                                    selector.onSelectSprite = (a, s) =>
                                    {
                                        var sobj = new SerializedObject(targetObjects);
                                        sobj.FindProperty(atlasRawPropPath).objectReferenceValue = a;
                                        sobj.FindProperty(spriteNamePropPath).stringValue = s;
                                        sobj.FindProperty(spriteDirtyPropPath).boolValue = true;
                                        sobj.ApplyModifiedProperties();
                                    };
                                    Event.current.Use();
                                    GUIUtility.ExitGUI();
                                }
                            }
                            break;
                        }
                }
            }
            else
            {
                tempRect.width = startWidth - (tempRect.x - startX);
                SerializedProperty spriteProp = prop.FindPropertyRelative("m_Sprite");
                EditorGUI.PropertyField(tempRect, spriteProp, GUIContent.none);
            }
        }

        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
#endif