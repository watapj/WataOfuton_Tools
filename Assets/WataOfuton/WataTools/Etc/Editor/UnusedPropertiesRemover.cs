using UnityEngine;
using UnityEditor;

namespace WataOfuton.Tool
{
    public class UnusedPropertiesRemover : MonoBehaviour
    {
        private const string MENU_ITEM_PATH = "Assets/WataOfuton/Remove Unused Properties in Material";

        [MenuItem(MENU_ITEM_PATH, false, 1200)]
        public static void RemoveUnusedProperties()
        {
            Object[] mats = Selection.GetFiltered(typeof(Material), SelectionMode.Assets);

            foreach (Material mat in mats)
            {
                string path = AssetDatabase.GetAssetPath(mat);

                var newMat = new Material(mat.shader);

                // パラメータをコピー
                newMat.name = mat.name;
                newMat.renderQueue = (mat.shader.renderQueue == mat.renderQueue) ? -1 : mat.renderQueue;
                newMat.enableInstancing = mat.enableInstancing;
                newMat.doubleSidedGI = mat.doubleSidedGI;
                newMat.globalIlluminationFlags = mat.globalIlluminationFlags;
                newMat.hideFlags = mat.hideFlags;
                newMat.shaderKeywords = mat.shaderKeywords;

                // プロパティをコピー
                var properties = MaterialEditor.GetMaterialProperties(new Material[] { mat });
                foreach (var prop in properties)
                {
                    if (prop.type == MaterialProperty.PropType.Color)
                    {
                        newMat.SetColor(prop.name, mat.GetColor(prop.name));
                    }
                    else if (prop.type == MaterialProperty.PropType.Float || prop.type == MaterialProperty.PropType.Range)
                    {
                        newMat.SetFloat(prop.name, mat.GetFloat(prop.name));
                    }
                    else if (prop.type == MaterialProperty.PropType.Texture)
                    {
                        newMat.SetTexture(prop.name, mat.GetTexture(prop.name));
                    }
                    else if (prop.type == MaterialProperty.PropType.Vector)
                    {
                        newMat.SetVector(prop.name, mat.GetVector(prop.name));
                    }
                    else if (prop.type == MaterialProperty.PropType.Int)
                    {
                        newMat.SetInt(prop.name, mat.GetInt(prop.name));
                    }
                }

                // 新しいマテリアルを古いマテリアルに置き換える（GUIDを保持）
                string tempPath = path + "_temp";
                AssetDatabase.CreateAsset(newMat, tempPath);
                FileUtil.ReplaceFile(tempPath, path);
                AssetDatabase.DeleteAsset(tempPath);

                Debug.Log("未使用のプロパティがマテリアルから削除されました: " + mat.name);
            }
        }

        // 上記の関数によって定義されたメニューアイテムの検証を行う.
        // この関数がfalseを返すと、メニューアイテムは無効化される.
        [MenuItem(MENU_ITEM_PATH, true, 1200)]
        public static bool ValidateRemoveUnusedProperties()
        {
            // マテリアルが選択されていなければfalseを返す.
            return Selection.GetFiltered(typeof(Material), SelectionMode.Assets).Length > 0;
        }
    }
}