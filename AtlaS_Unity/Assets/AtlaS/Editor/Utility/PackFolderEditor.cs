#if AtlaS_ON
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityEditor.UI.Atlas
{
    /// <summary>
    /// Pack sprites in selected folder to one atlas.
    /// </summary>
    public class PackFolderEditor : EditorWindow
    {
        [MenuItem("AtlaS/PackFolder")]
        private static void CreateAtlasPacker()
        {
            GetWindow<PackFolderEditor>().Show();
        }

        [SerializeField]
        private PackSetting mSetting = new PackSetting(1024, 1, false, false);

        private void OnGUI()
        {
            mSetting.maxAtlasSize = EditorGUILayout.IntPopup("Max Size", mSetting.maxAtlasSize, Array.ConvertAll(PackConst.AtlasSizeList, value => value.ToString()), PackConst.AtlasSizeList);
            mSetting.padding = EditorGUILayout.IntField("Padding", mSetting.padding);
            mSetting.isPOT = EditorGUILayout.Toggle("Power Of 2", mSetting.isPOT);
            GUI.enabled = mSetting.isPOT;
            if (!mSetting.isPOT) mSetting.forceSquare = false;
            mSetting.forceSquare = EditorGUILayout.Toggle("Force Square", mSetting.forceSquare);
            GUI.enabled = true;
            var activeObject = Selection.activeObject;
            if (activeObject == null)
            {
                EditorGUILayout.LabelField("Select a folder to pack atlas.");
                return;
            }
            var folder = AssetDatabase.GetAssetPath(activeObject);
            if (string.IsNullOrEmpty(folder) ||
                (File.GetAttributes(folder) & FileAttributes.Directory) == 0)
            {
                EditorGUILayout.LabelField("Please select a folder to pack atlas.");
                return;
            }
            EditorGUILayout.LabelField("Target Folder", folder);
            if (GUILayout.Button("Pack", GUILayout.MaxWidth(200)))
            {
                PackFolder(folder);
            }
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("AtlasPacker");
            Selection.selectionChanged -= Repaint;
            Selection.selectionChanged += Repaint;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= Repaint;
        }

        private void PackFolder(string folder)
        {
            var textures = AssetDatabase.FindAssets("t:Texture2D", new string[] { folder })
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => new PackAssetSprite(path)).ToArray();
            if (textures.Length == 0) return;
            for (int i = 0; i < textures.Length; i++)
            {
                var texPath = textures[i].assetPath;
                var assetPath = texPath.Replace(folder + "/", "");
                var assetDir = Path.GetDirectoryName(assetPath);
                var assetName = Path.GetFileNameWithoutExtension(assetPath);
                var assetLabel = string.IsNullOrEmpty(assetDir) ? assetName : assetDir + "/" + assetName;
                textures[i].name = assetLabel;
                textures[i].quality = PackUtil.CheckTextureCompressed(texPath) ? PackQuality.Normal : PackQuality.Full;
            }
            var atlasRaw = AtlasPacker.Pack(folder, textures, mSetting);
            if (atlasRaw != null)
                Selection.activeObject = atlasRaw;
        }
    }
}
#endif