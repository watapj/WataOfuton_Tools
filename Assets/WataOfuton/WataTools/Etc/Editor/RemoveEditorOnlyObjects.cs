using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;


namespace WataOfuton.Tools
{
    public class DeactivateEditorOnlyObjectsWindow : EditorWindow
    {
        private const string MENU_ITEM_PATH = "Window/WataOfuton/DeactivateEditorOnlyObjects";
        public const string PREF_KEY_ENABLE_AUTO_CHANGE = "DeactivateEditorOnlyObjectsWindow.EnableDeactivate";

        // 現在の設定をMenuItemに反映
        [MenuItem(MENU_ITEM_PATH, true)]
        public static bool ToggleActionValidation()
        {
            // メニューがチェックされているかどうかを示す
            Menu.SetChecked(MENU_ITEM_PATH, EditorPrefs.GetBool(PREF_KEY_ENABLE_AUTO_CHANGE, true));
            return true;
        }

        // メニューアイテムが選択されたときの挙動を設定
        [MenuItem(MENU_ITEM_PATH, false, 1)]
        public static void ToggleAction()
        {
            // 現在のチェック状態を取得し反転させる
            bool current = EditorPrefs.GetBool(PREF_KEY_ENABLE_AUTO_CHANGE, true);
            EditorPrefs.SetBool(PREF_KEY_ENABLE_AUTO_CHANGE, !current);
        }
    }

    [InitializeOnLoad]
    public class DeactivateEditorOnlyObjects
    {
        private static List<GameObject> editorOnlyObjects;

        static DeactivateEditorOnlyObjects()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!EditorPrefs.GetBool(DeactivateEditorOnlyObjectsWindow.PREF_KEY_ENABLE_AUTO_CHANGE, false))
                return;

            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                case PlayModeStateChange.ExitingPlayMode:
                    // ReactivateObjects();
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    // DeactivateObjects();
                    // DeleteObjects();
                    break;
            }
        }


        private static void DeactivateObjects()
        {
            editorOnlyObjects = new List<GameObject>(GameObject.FindGameObjectsWithTag("EditorOnly"));
            foreach (GameObject obj in editorOnlyObjects)
            {
                if (obj.activeSelf)
                    obj.SetActive(false);
                else
                    editorOnlyObjects.Remove(obj); // If already inactive, remove from list so it doesn't get reactivated
            }
        }

        private static void ReactivateObjects()
        {
            if (editorOnlyObjects != null)
            {
                foreach (GameObject obj in editorOnlyObjects)
                {
                    if (obj != null) // Check if the object wasn't destroyed while in play mode
                        obj.SetActive(true);
                }
            }
        }

        private static void DeleteObjects()
        {
            // GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            GameObject[] allObjects = GetALLObjects();
            foreach (GameObject obj in allObjects)
            {
                if (obj.CompareTag("EditorOnly"))
                {
                    GameObject.DestroyImmediate(obj, true);
                }
            }
        }

        private static GameObject[] GetALLObjects()
        {
            var editorOnlyObjects = Resources.FindObjectsOfTypeAll<GameObject>()
                                    .Where(go => go.CompareTag("EditorOnly") &&
                                                 go.hideFlags != HideFlags.NotEditable &&
                                                 go.hideFlags != HideFlags.HideAndDontSave)
                                    .ToArray();

            return editorOnlyObjects;
        }
    }
}