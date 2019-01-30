using System.Linq;
using UnityEngine;
using UnityEditor.AnimatedValues;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.UI;

namespace AtlaS.UI
{
    /// <summary>
    /// Editor class used to edit UI Sprites.
    /// </summary>

    [CustomEditor(typeof(AtlasImage), true)]
    [CanEditMultipleObjects]
    public class AtlasImageEditor : GraphicEditor
    {
        SerializedProperty m_FillMethod;
        SerializedProperty m_FillOrigin;
        SerializedProperty m_FillAmount;
        SerializedProperty m_FillClockwise;
        SerializedProperty m_Type;
        SerializedProperty m_FillCenter;
        SerializedProperty m_PreserveAspect;
        GUIContent m_SpriteContent;
        GUIContent m_SpriteTypeContent;
        GUIContent m_ClockwiseContent;
        AnimBool m_ShowSlicedOrTiled;
        AnimBool m_ShowSliced;
        AnimBool m_ShowTiled;
        AnimBool m_ShowFilled;
        AnimBool m_ShowType;

        SerializedProperty mAtlasRaw;
        SerializedProperty mSpriteRaw;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_SpriteContent = new GUIContent("Source Image");
            m_SpriteTypeContent = new GUIContent("Image Type");
            m_ClockwiseContent = new GUIContent("Clockwise");

            m_Type = serializedObject.FindProperty("m_Type");
            m_FillCenter = serializedObject.FindProperty("m_FillCenter");
            m_FillMethod = serializedObject.FindProperty("m_FillMethod");
            m_FillOrigin = serializedObject.FindProperty("m_FillOrigin");
            m_FillClockwise = serializedObject.FindProperty("m_FillClockwise");
            m_FillAmount = serializedObject.FindProperty("m_FillAmount");
            m_PreserveAspect = serializedObject.FindProperty("m_PreserveAspect");

            mAtlasRaw = serializedObject.FindProperty("mAtlasRaw");
            mSpriteRaw = serializedObject.FindProperty("mSpriteRaw");

            m_ShowType = new AnimBool(!CheckSpriteIsNull());
            m_ShowType.valueChanged.AddListener(Repaint);

            var typeEnum = (AtlasImage.Type)m_Type.enumValueIndex;

            m_ShowSlicedOrTiled = new AnimBool(!m_Type.hasMultipleDifferentValues && typeEnum == AtlasImage.Type.Sliced);
            m_ShowSliced = new AnimBool(!m_Type.hasMultipleDifferentValues && typeEnum == AtlasImage.Type.Sliced);
            m_ShowTiled = new AnimBool(!m_Type.hasMultipleDifferentValues && typeEnum == AtlasImage.Type.Tiled);
            m_ShowFilled = new AnimBool(!m_Type.hasMultipleDifferentValues && typeEnum == AtlasImage.Type.Filled);
            m_ShowSlicedOrTiled.valueChanged.AddListener(Repaint);
            m_ShowSliced.valueChanged.AddListener(Repaint);
            m_ShowTiled.valueChanged.AddListener(Repaint);
            m_ShowFilled.valueChanged.AddListener(Repaint);

            SetShowNativeSize(true);
        }

        protected override void OnDisable()
        {
            m_ShowType.valueChanged.RemoveListener(Repaint);
            m_ShowSlicedOrTiled.valueChanged.RemoveListener(Repaint);
            m_ShowSliced.valueChanged.RemoveListener(Repaint);
            m_ShowTiled.valueChanged.RemoveListener(Repaint);
            m_ShowFilled.valueChanged.RemoveListener(Repaint);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SpriteGUI();
            AppearanceControlsGUI();
            RaycastControlsGUI();

            m_ShowType.target = !CheckSpriteIsNull();
            if (EditorGUILayout.BeginFadeGroup(m_ShowType.faded))
                TypeGUI();
            EditorGUILayout.EndFadeGroup();

            SetShowNativeSize(false);
            if (EditorGUILayout.BeginFadeGroup(m_ShowNativeSize.faded))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_PreserveAspect);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();
            NativeSizeButtonGUI();

            serializedObject.ApplyModifiedProperties();
        }

        void SetShowNativeSize(bool instant)
        {
            AtlasImage.Type type = (AtlasImage.Type)m_Type.enumValueIndex;
            bool showNativeSize = (type == AtlasImage.Type.Simple || type == AtlasImage.Type.Filled) && !CheckSpriteIsNull();
            base.SetShowNativeSize(showNativeSize, instant);
        }

        /// <summary>
        /// Draw the atlas and Image selection fields.
        /// </summary>

        protected void SpriteGUI()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(mAtlasRaw, new GUIContent("Atlas"));
            EditorGUILayout.PropertyField(mSpriteRaw, new GUIContent("Sprite"));
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var target in targets)
                {
                    var atlasImage = target as AtlasImage;
                    atlasImage.OnRebuildRequested();
                    var newSprite = atlasImage.sprite;
                    if (newSprite)
                    {
                        AtlasImage.Type oldType = atlasImage.type;
                        if (newSprite.border.SqrMagnitude() > 0)
                        {
                            atlasImage.type = AtlasImage.Type.Sliced;
                        }
                        else if (oldType == AtlasImage.Type.Sliced)
                        {
                            atlasImage.type = AtlasImage.Type.Simple;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sprites's custom properties based on the type.
        /// </summary>

        protected void TypeGUI()
        {
            EditorGUILayout.PropertyField(m_Type, m_SpriteTypeContent);

            ++EditorGUI.indentLevel;
            {
                AtlasImage.Type typeEnum = (AtlasImage.Type)m_Type.enumValueIndex;

                bool showSlicedOrTiled = (!m_Type.hasMultipleDifferentValues && (typeEnum == AtlasImage.Type.Sliced || typeEnum == AtlasImage.Type.Tiled));
                if (showSlicedOrTiled && targets.Length > 1)
                    showSlicedOrTiled = targets.Select(obj => obj as AtlasImage).All(img => img.hasBorder);

                m_ShowSlicedOrTiled.target = showSlicedOrTiled;
                m_ShowSliced.target = (showSlicedOrTiled && !m_Type.hasMultipleDifferentValues && typeEnum == AtlasImage.Type.Sliced);
                m_ShowTiled.target = (showSlicedOrTiled && !m_Type.hasMultipleDifferentValues && typeEnum == AtlasImage.Type.Tiled);
                m_ShowFilled.target = (!m_Type.hasMultipleDifferentValues && typeEnum == AtlasImage.Type.Filled);

                AtlasImage image = target as AtlasImage;
                if (EditorGUILayout.BeginFadeGroup(m_ShowSlicedOrTiled.faded))
                {
                    if (image.hasBorder)
                        EditorGUILayout.PropertyField(m_FillCenter);
                }
                EditorGUILayout.EndFadeGroup();

                if (EditorGUILayout.BeginFadeGroup(m_ShowSliced.faded))
                {
                    if (image.sprite != null && !image.hasBorder)
                        EditorGUILayout.HelpBox("This Image doesn't have a border.", MessageType.Warning);
                }
                EditorGUILayout.EndFadeGroup();

                if (EditorGUILayout.BeginFadeGroup(m_ShowTiled.faded))
                {
                    if (image.sprite != null && !image.hasBorder && (image.sprite.texture.wrapMode != TextureWrapMode.Repeat || image.sprite.packed))
                        EditorGUILayout.HelpBox("It looks like you want to tile a sprite with no border. It would be more efficient to convert the Sprite to an Advanced texture, clear the Packing tag and set the Wrap mode to Repeat.", MessageType.Warning);
                }
                EditorGUILayout.EndFadeGroup();

                if (EditorGUILayout.BeginFadeGroup(m_ShowFilled.faded))
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(m_FillMethod);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_FillOrigin.intValue = 0;
                    }
                    switch ((AtlasImage.FillMethod)m_FillMethod.enumValueIndex)
                    {
                        case AtlasImage.FillMethod.Horizontal:
                            m_FillOrigin.intValue = (int)(AtlasImage.OriginHorizontal)EditorGUILayout.EnumPopup("Fill Origin", (AtlasImage.OriginHorizontal)m_FillOrigin.intValue);
                            break;
                        case AtlasImage.FillMethod.Vertical:
                            m_FillOrigin.intValue = (int)(AtlasImage.OriginVertical)EditorGUILayout.EnumPopup("Fill Origin", (AtlasImage.OriginVertical)m_FillOrigin.intValue);
                            break;
                        case AtlasImage.FillMethod.Radial90:
                            m_FillOrigin.intValue = (int)(AtlasImage.Origin90)EditorGUILayout.EnumPopup("Fill Origin", (AtlasImage.Origin90)m_FillOrigin.intValue);
                            break;
                        case AtlasImage.FillMethod.Radial180:
                            m_FillOrigin.intValue = (int)(AtlasImage.Origin180)EditorGUILayout.EnumPopup("Fill Origin", (AtlasImage.Origin180)m_FillOrigin.intValue);
                            break;
                        case AtlasImage.FillMethod.Radial360:
                            m_FillOrigin.intValue = (int)(AtlasImage.Origin360)EditorGUILayout.EnumPopup("Fill Origin", (AtlasImage.Origin360)m_FillOrigin.intValue);
                            break;
                    }
                    EditorGUILayout.PropertyField(m_FillAmount);
                    if ((AtlasImage.FillMethod)m_FillMethod.enumValueIndex > AtlasImage.FillMethod.Vertical)
                    {
                        EditorGUILayout.PropertyField(m_FillClockwise, m_ClockwiseContent);
                    }
                }
                EditorGUILayout.EndFadeGroup();
            }
            --EditorGUI.indentLevel;
        }

        /// <summary>
        /// All graphics have a preview.
        /// </summary>

        public override bool HasPreviewGUI() { return true; }

        /// <summary>
        /// Draw the Image preview.
        /// </summary>

        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            AtlasImage image = target as AtlasImage;
            if (image == null) return;

            Sprite sf = image.sprite;
            if (sf == null) return;

            //SpriteDrawUtility.DrawSprite(sf, rect, image.canvasRenderer.GetColor());
        }

        /// <summary>
        /// Info String drawn at the bottom of the Preview
        /// </summary>

        public override string GetInfoString()
        {
            AtlasImage image = target as AtlasImage;
            Sprite sprite = image.sprite;

            int x = (sprite != null) ? Mathf.RoundToInt(sprite.rect.width) : 0;
            int y = (sprite != null) ? Mathf.RoundToInt(sprite.rect.height) : 0;

            return string.Format("Image Size: {0}x{1}", x, y);
        }

        bool CheckSpriteIsNull()
        {
            return targets.Any(target => (target as AtlasImage).sprite == null);
        }
    }
}
