#if AtlaS_ON
using System;
using System.IO;
using UnityEngine;

namespace UnityEditor.UI.Atlas
{
    public class AtlasPackerWindow : EditorWindow
    {
        [MenuItem("AtlaS/AtlasPacker")]
        private static void CreateAtlasPacker()
        {
            GetWindow<AtlasPackerWindow>().Show();
        }

        [SerializeField]
        private AtlasPackData mPackData = new AtlasPackData(1024, 1, true, false, false);

        private void OnGUI()
        {
            mPackData.maxAtlasSize = EditorGUILayout.IntPopup("Max Size", mPackData.maxAtlasSize, Array.ConvertAll(AtlasPacker.AtlasSizeList, value => value.ToString()), AtlasPacker.AtlasSizeList);
            mPackData.padding = EditorGUILayout.IntField("Padding", mPackData.padding);
            mPackData.isPOT = EditorGUILayout.Toggle("Power Of 2", mPackData.isPOT);
            GUI.enabled = mPackData.isPOT;
            if (!mPackData.isPOT) mPackData.forceSquare = false;
            mPackData.forceSquare = EditorGUILayout.Toggle("Force Square", mPackData.forceSquare);
            GUI.enabled = true;
            mPackData.removeFragmentWhenPackingOver = EditorGUILayout.Toggle(new GUIContent("Clear Fragments", "Delete all packed textures from target folder after packing."), mPackData.removeFragmentWhenPackingOver);
            var activeObject = Selection.activeObject;
            if (activeObject == null)
            {
                EditorGUILayout.LabelField("Select a folder to pack atlas.");
                return;
            }
            var assetPath = AssetDatabase.GetAssetPath(activeObject);
            if (string.IsNullOrEmpty(assetPath) ||
                (File.GetAttributes(assetPath) & FileAttributes.Directory) == 0)
            {
                EditorGUILayout.LabelField("Please select a folder to pack atlas.");
                return;
            }
            EditorGUILayout.LabelField("Target Folder", assetPath);
            if (GUILayout.Button("Pack", GUILayout.MaxWidth(200)))
            {
                AtlasPackerUtil.PackAssetFolder(mPackData, assetPath);
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
    }
}
#endif