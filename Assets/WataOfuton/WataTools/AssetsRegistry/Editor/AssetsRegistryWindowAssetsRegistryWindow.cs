#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace WataOfuton.Tools
{

    public class AssetsRegistryWindow : EditorWindow
    {
        /* ───────── フィールド ───────── */
        private AssetsRegistry _current;
        private ReorderableList _groupList;
        private readonly Dictionary<AssetsRegistry.PrefabGroup, ReorderableList> _lists = new();

        private AssetsRegistry.PrefabGroup _selGroup;  // 選択中グループ
        private int _selIndex = -1;                    // 選択中インデックス

        private Vector2 _scroll;
        private Editor _previewEditor;                // インタラクティブプレビュー Editor
        private const float SplitRatio = 0.45f;        // 左ペイン幅 (%)
        private const float IndentPx = 15f;          // 1 段インデント幅


        [MenuItem("Window/WataOfuton/Assets Registry")]
        private static void Open() => GetWindow<AssetsRegistryWindow>("Assets Registry");

        /* ───────── Unity Callbacks ───────── */
        private void OnEnable()
        {
            if (_current == null) _current = FindFirstRegistry();
            InitAllLists();

            EditorApplication.update += TryUpdatePreview;
        }

        private void OnDisable()
        {
            EditorApplication.update -= TryUpdatePreview;
            if (_previewEditor) DestroyImmediate(_previewEditor);
        }

        /* ───────── GUI ───────── */
        private void OnGUI()
        {
            DrawToolbar();
            if (_current == null)
            {
                EditorGUILayout.HelpBox("AssetsRegistry が見つかりません。『New』で作成してください。", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();

            /* 左ペイン : グループ＆リスト */
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * SplitRatio));
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            _groupList?.DoLayoutList();
            if (GUILayout.Button("＋ 新しいリストを追加"))
            {
                Undo.RecordObject(_current, "Add List");
                var g = new AssetsRegistry.PrefabGroup();
                _current.groups.Add(g);
                g.isOpen = true;
                EditorUtility.SetDirty(_current);
                AssetDatabase.SaveAssets();
                InitAllLists();
            }
            EditorGUILayout.EndScrollView();

            DrawSceneSearchSection();
            EditorGUILayout.EndVertical();                 // 左ペイン
            // EditorGUILayout.Space(2);

            /* 右ペイン : プレビュー */
            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            DrawPreview();
            GUILayout.FlexibleSpace();
            DrawToggleFixBounds();
            EditorGUILayout.EndVertical();                 // 右ペイン
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUI.BeginChangeCheck();
            _current = (AssetsRegistry)EditorGUILayout.ObjectField(_current, typeof(AssetsRegistry),
                false, GUILayout.Width(position.width - 130));
            if (EditorGUI.EndChangeCheck())
                InitAllLists();

            if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(40)))
                CreateNewRegistry();

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                InitAllLists();
            GUILayout.EndHorizontal();
        }

        /* ───────── Drag & Drop ───────── */
        private void HandleDrop(Rect rect, AssetsRegistry.PrefabGroup g)
        {
            Event e = Event.current;
            if (!rect.Contains(e.mousePosition)) return;

            if (e.type is EventType.DragUpdated or EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (e.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (var obj in DragAndDrop.objectReferences.OfType<GameObject>())
                    {
                        string path = AssetDatabase.GetAssetPath(obj);
                        if (string.IsNullOrEmpty(path)) continue;
                        if (g.items.Any(i => i.prefab == obj)) continue;

                        if (_current.isFixBounds)
                            PrefabBoundsAutoFixer.FixPrefabBounds(path);

                        var item = new AssetsRegistry.RegistryItem
                        {
                            prefab = obj,
                            sceneNames = FindScenesUsingPrefab(path)
                        };
                        Undo.RecordObject(_current, "Add Prefab");
                        g.items.Add(item);
                    }
                    EditorUtility.SetDirty(_current);
                    AssetDatabase.SaveAssets();
                }
                e.Use();
            }
        }

        /* ───────── リスト生成 ───────── */
        private void InitAllLists()
        {
            // プレビューをいったん無効化
            _selGroup = null;
            _selIndex = -1;
            if (_previewEditor) DestroyImmediate(_previewEditor);
            _previewEditor = null;

            _lists.Clear();
            if (_current == null) return;

            foreach (var g in _current.groups)
                _lists[g] = CreateListForGroup(g);

            InitGroupList();   // グループ用 RL も更新
        }

        private ReorderableList CreateListForGroup(AssetsRegistry.PrefabGroup group)
        {
            var rl = new ReorderableList(group.items, typeof(AssetsRegistry.RegistryItem),
                                         true, true, false, true)
            {
                elementHeight = EditorGUIUtility.singleLineHeight + 8
            };

            rl.drawHeaderCallback = r => EditorGUI.LabelField(r, "Prefab / Scenes");

            rl.drawElementCallback = (r, i, _, _) =>
            {
                r.y += 4;
                var item = group.items[i];

                float wPrefab = r.width * 0.45f;
                var rectPf = new Rect(r.x, r.y, wPrefab, EditorGUIUtility.singleLineHeight);
                item.prefab = (GameObject)EditorGUI.ObjectField(rectPf, item.prefab, typeof(GameObject), false);

                var rectScn = new Rect(rectPf.xMax + 4, r.y, r.width - wPrefab - 4, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(rectScn, string.Join(", ", item.sceneNames), EditorStyles.wordWrappedLabel);
            };

            rl.onSelectCallback = l =>
            {
                _selGroup = group;
                _selIndex = l.index;
                CreatePreviewEditor();
                Repaint();
            };

            rl.onRemoveCallback = l =>
            {
                Undo.RecordObject(_current, "Remove Prefab");
                group.items.RemoveAt(l.index);
                EditorUtility.SetDirty(_current);
                AssetDatabase.SaveAssets();
                if (_selGroup == group) _selIndex = -1;
            };

            return rl;
        }

        private void InitGroupList()
        {
            if (_current == null) return;

            _groupList = new ReorderableList(_current.groups, typeof(AssetsRegistry.PrefabGroup),
                                             true, false, true, false);

            _groupList.elementHeightCallback = idx =>
            {
                var g = _current.groups[idx];
                float h = EditorGUIUtility.singleLineHeight + 6;
                if (g.isOpen) h += 50 + _lists[g].GetHeight() + 8;
                return h;
            };

            _groupList.drawElementCallback = (rect, idx, act, foc) =>
            {
                var g = _current.groups[idx];
                var list = _lists[g];

                /* ── ヘッダー ── */
                Rect head = new Rect(rect.x, rect.y + 2, rect.width, EditorGUIUtility.singleLineHeight);
                g.isOpen = EditorGUI.Foldout(new Rect(head.x, head.y, 14, head.height),
                                             g.isOpen, GUIContent.none, true);

                EditorGUI.BeginChangeCheck();
                string newName = EditorGUI.TextField(new Rect(head.x + 14, head.y, head.width - 40, head.height), g.groupName);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_current, "Rename List");
                    g.groupName = newName;
                    EditorUtility.SetDirty(_current);
                }

                if (GUI.Button(new Rect(head.xMax - 18, head.y, 18, head.height), "×"))
                {
                    Undo.RecordObject(_current, "Delete List");
                    _current.groups.RemoveAt(idx);
                    EditorUtility.SetDirty(_current);
                    AssetDatabase.SaveAssets();
                    InitAllLists();
                    GUIUtility.ExitGUI();
                    return;
                }

                /* ── 内容 ── */
                if (!g.isOpen) return;

                float y = head.yMax + 2;

                Rect drop = new Rect(rect.x + IndentPx, y, rect.width - IndentPx, 50);
                GUI.Box(drop, "ここに Prefab をドロップ");
                HandleDrop(drop, g);
                y += 50;

                list.DoList(new Rect(rect.x + IndentPx, y, rect.width - IndentPx, list.GetHeight()));
            };

            _groupList.onReorderCallback = _ => InitAllLists();   // 並べ替え後に辞書を再同期
        }

        /* ───────── Scene リスト & プレビュー ───────── */
        private void DrawSceneSearchSection()
        {
            SerializedObject so = new SerializedObject(_current);
            SerializedProperty prop = so.FindProperty("scenesToSearch");
            EditorGUILayout.PropertyField(prop, includeChildren: true);
            if (so.ApplyModifiedProperties()) _current.ClearDependencyCache();
        }

        private void DrawPreview()
        {
            if (_previewEditor == null) return;

            float w = position.width * (1f - SplitRatio) * 0.95f;
            float h = position.height * 0.9f;
            float size = Mathf.Min(w, h);

            Rect r = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false));
            _previewEditor.OnInteractivePreviewGUI(r, EditorStyles.whiteLabel);
        }

        private void TryUpdatePreview()
        {
            if (_previewEditor != null) return;
            if (GetSelectedPrefab() == null) return;

            CreatePreviewEditor();
            Repaint();
        }

        private void CreatePreviewEditor()
        {
            if (_previewEditor) DestroyImmediate(_previewEditor);
            var prefab = GetSelectedPrefab();
            _previewEditor = prefab ? Editor.CreateEditor(prefab) : null;
        }

        private void DrawToggleFixBounds()
        {
            SerializedObject so = new SerializedObject(_current);
            SerializedProperty prop = so.FindProperty("isFixBounds");
            EditorGUILayout.PropertyField(prop);
            if (so.ApplyModifiedProperties()) _current.ClearDependencyCache();
        }

        /* ───────── ヘルパー ───────── */
        private static AssetsRegistry FindFirstRegistry()
        {
            string[] guids = AssetDatabase.FindAssets("t:AssetsRegistry");
            return guids.Length == 0 ? null
                                     : AssetDatabase.LoadAssetAtPath<AssetsRegistry>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private void CreateNewRegistry()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create Assets Registry", "AssetsRegistry.asset", "asset",
                                                               "保存先フォルダを選択してください");
            if (string.IsNullOrEmpty(path)) return;

            var asset = ScriptableObject.CreateInstance<AssetsRegistry>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            _current = asset;
            InitAllLists();
        }

        private GameObject GetSelectedPrefab()
        {
            return _selGroup != null && _selIndex >= 0 && _selIndex < _selGroup.items.Count
                ? _selGroup.items[_selIndex].prefab
                : null;
        }

        private List<string> FindScenesUsingPrefab(string prefabPath)
        {
            var sceneNames = new List<string>();
            var sceneAssets = _current.scenesToSearch.Where(a => a != null).ToList();
            int total = sceneAssets.Count;

            try
            {
                for (int i = 0; i < total; i++)
                {
                    var sceneAsset = sceneAssets[i];
                    string scenePath = AssetDatabase.GetAssetPath(sceneAsset);

                    // ── 進行状況を表示（キャンセル可能） ──
                    bool cancel = EditorUtility.DisplayCancelableProgressBar(
                        "Scene 参照検索中…",
                        $"{sceneAsset.name}  ({i + 1}/{total})",
                        (float)i / total);

                    if (cancel) break; // Esc キー or Cancel ボタンで中断

                    // 依存関係キャッシュから判定
                    if (_current.GetDependenciesCached(scenePath).Contains(prefabPath))
                        sceneNames.Add(sceneAsset.name);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar(); // 必ず消す
            }

            return sceneNames;
        }
    }
}
#endif