using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace WataOfuton.Tools
{
    [CreateAssetMenu(fileName = "AssetsRegistry", menuName = "WataOfuton/Assets Registry")]
    public class AssetsRegistry : ScriptableObject
    {
        // public List<Object> assets = new();   // Prefab 以外にも登録できるよう Object 型
        [System.Serializable]
        public class RegistryItem
        {
            public GameObject prefab;           // 登録 Prefab
            public List<string> sceneNames = new(); // 参照している Scene 名
        }

        // public List<RegistryItem> items = new();

        [System.Serializable]
        public class PrefabGroup                // ★ 複数リストを束ねる
        {
            public string groupName = "New List";
            public bool isOpen = true;
            public List<RegistryItem> items = new();
        }

        public List<PrefabGroup> groups = new(); // ★ 1-SO に複数グループ

        // ★ 追加: 探索対象 Scene 一覧（ドラッグ&ドロップで設定）
        public List<SceneAsset> scenesToSearch = new();
        public bool isFixBounds = new();

        // ★ 追加: <scenePath, dependencies[]> キャッシュ
        [System.NonSerialized]                       // ScriptableObject には保存しない
        private Dictionary<string, string[]> _depCache = new();

        /// <summary>scenePath の依存ファイル GUID 配列を取得（キャッシュ付き）</summary>
        public string[] GetDependenciesCached(string scenePath)
        {
            if (_depCache.TryGetValue(scenePath, out var deps)) return deps;
            deps = AssetDatabase.GetDependencies(scenePath, true);
            _depCache[scenePath] = deps;
            return deps;
        }

        /// <summary>Scene を追加・削除したらキャッシュをクリア</summary>
        public void ClearDependencyCache() => _depCache.Clear();
    }
}
