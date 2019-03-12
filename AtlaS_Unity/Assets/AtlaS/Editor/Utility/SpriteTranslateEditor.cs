#if AtlaS_ON
using System;
using UnityEngine;

namespace UnityEditor.UI.Atlas
{
    /// <summary>
    /// Translate sprites which are referenced by resource folder to atlas,
    /// or translate atlas to sprites.
    /// </summary>
    public class SpriteTranslateEditor : EditorWindow
    {
        [MenuItem("AtlaS/SpriteTranslater")]
        private static void CreateSpriteTranslater()
        {
            GetWindow<SpriteTranslateEditor>().Show();
        }

        [Serializable]
        private enum TranslateType
        {
            Sprite2Atlas,
            Atlas2Sprite,
        }

        [SerializeField]
        private TranslateType mTranslateType = TranslateType.Sprite2Atlas;

        [SerializeField]
        private string targetFolder = "";

        [SerializeField]
        private string resourceFolder = "";

        [SerializeField]
        private PackSetting mSetting = new PackSetting(1024, 1, false, false);

        private void OnGUI()
        {
            mTranslateType = (TranslateType)EditorGUILayout.EnumPopup("Type", mTranslateType);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField("Target Folder", targetFolder);
            if (GUILayout.Button("Browser", GUILayout.Width(100)))
            {
                var newPath = EditorUtility.OpenFolderPanel("Select target folder", targetFolder, null);
                if (newPath.Contains(Application.dataPath))
                    targetFolder = newPath.Replace(Application.dataPath, "Assets");
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField("Resource Folder", resourceFolder);
            if (GUILayout.Button("Browser", GUILayout.Width(100)))
            {
                var newPath = EditorUtility.OpenFolderPanel("Select resource folder", resourceFolder, null);
                if (newPath.Contains(Application.dataPath))
                    resourceFolder = newPath.Replace(Application.dataPath, "Assets");
            }
            EditorGUILayout.EndHorizontal();
            if (mTranslateType == TranslateType.Sprite2Atlas)
            {
                EditorGUILayout.LabelField("Atlas Settings");
                mSetting.maxAtlasSize = EditorGUILayout.IntPopup("Max Size", mSetting.maxAtlasSize, Array.ConvertAll(PackConst.AtlasSizeList, value => value.ToString()), PackConst.AtlasSizeList);
                mSetting.padding = EditorGUILayout.IntField("Padding", mSetting.padding);
                mSetting.isPOT = EditorGUILayout.Toggle("Power Of 2", mSetting.isPOT);
                GUI.enabled = mSetting.isPOT;
                if (!mSetting.isPOT) mSetting.forceSquare = false;
                mSetting.forceSquare = EditorGUILayout.Toggle("Force Square", mSetting.forceSquare);
                GUI.enabled = true;
            }
            if (GUILayout.Button("Pack", GUILayout.MaxWidth(200)))
            {
                SpriteRefCollector.Instance.Translate(targetFolder, mTranslateType == TranslateType.Sprite2Atlas, resourceFolder, mSetting);
            }
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("SpTrans");
        }

        private void OnDisable()
        {

        }      
    }
}
#endif