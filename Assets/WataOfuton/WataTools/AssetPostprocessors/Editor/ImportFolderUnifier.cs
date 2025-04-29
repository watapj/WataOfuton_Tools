using UnityEngine;
using System.IO;
using UnityEditor;
using System.Linq;

namespace WataOfuton.Tools
{
    /// <summary>
    /// ルート（Assets 直下）にインポートされたフォルダを、
    /// 指定フォルダ（TargetDirNames）に同名フォルダがある場合は自動的に統合するポストプロセッサ。
    /// 統合後は対象フォルダを Project ビューで選択＆Ping。
    /// </summary>
    public sealed class ImportFolderUnifier : AssetPostprocessor
    {
        private static readonly string[] TargetDirNames =
        {
            "Model_Acc",
            "Model_Dres",
            "Model_Hair",
        };

        private static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deleted, string[] moved, string[] movedFrom)
        {
            // ① 今回インポートで登場した「最上位フォルダ」を抽出
            var rootFolders = importedAssets
                .Select(GetTopMostImportedFolder)       // Assets/AAA/BBB.fbx → Assets/AAA
                .Where(p => AssetDatabase.IsValidFolder(p))
                .Distinct()
                .ToArray();

            foreach (var root in rootFolders)
            {
                TryUnify(root);
            }

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 対象フォルダと同名の統合先があれば結合処理を行う
        /// </summary>
        private static void TryUnify(string srcRoot)
        {
            var folderName = Path.GetFileName(srcRoot);

            foreach (var targetDir in TargetDirNames)
            {
                var dstRoot = $"Assets/{targetDir}/{folderName}";
                if (!AssetDatabase.IsValidFolder(dstRoot) || srcRoot == dstRoot) continue;

                // ★ インポートしたフォルダ直下の「最初のサブフォルダ名」を保持
                var firstSub = Directory.GetDirectories(srcRoot)
                                        .Select(Path.GetFileName)
                                        .FirstOrDefault();   // 無ければ null

                Debug.Log($"[ImportFolderUnifier] Merge '{srcRoot}' → '{dstRoot}'");
                MergeFoldersByFileOp(srcRoot, dstRoot);
                AssetDatabase.DeleteAsset(srcRoot);

                var pingPath = firstSub != null ? $"{dstRoot}/{firstSub}" : dstRoot;
                Ping(pingPath);
                break; // 1 つ統合したら終了
            }
        }

        /// <summary>
        /// ファイル単位で移動（MyMoveFile 使用）
        /// </summary>
        private static void MergeFoldersByFileOp(string src, string dst)
        {
            foreach (var file in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".meta")) continue;               // .meta は MyMoveFile が同時処理
                var relative = file.Substring(src.Length + 1);      // +1 は '/'
                var dstPath = Path.Combine(dst, relative)
                               .Replace("\\", "/");

                // 物理フォルダ生成（GUID は Refresh 後に生成される）
                Directory.CreateDirectory(Path.GetDirectoryName(dstPath)!);

                // フルパス → Unity パスへ変換して MyMoveFile
                var unitySrc = FullToUnityPath(file);
                var unityDst = FullToUnityPath(dstPath);
                MyMoveFile(unitySrc, unityDst);
            }
        }

        #region utils --------------------------------------------------------
        /// <summary>
        /// Assets/AAA/BBB を受け取ったら Assets/AAA を返す
        /// </summary>
        private static string GetTopMostImportedFolder(string assetPath)
        {
            var parts = assetPath.Split('/');
            return parts.Length >= 3 ? $"Assets/{parts[1]}" : string.Empty;
        }

        private static string FullToUnityPath(string full)
            => full.Replace(Application.dataPath, "Assets").Replace("\\", "/");

        private static void DeleteEmptyFoldersRecursively(string folder)
        {
            foreach (var sub in AssetDatabase.GetSubFolders(folder))
                DeleteEmptyFoldersRecursively(sub);

            bool hasAssets = AssetDatabase.FindAssets(string.Empty, new[] { folder }).Length > 0;
            bool hasSub = AssetDatabase.GetSubFolders(folder).Length > 0;
            if (!hasAssets && !hasSub && AssetDatabase.IsValidFolder(folder))
                AssetDatabase.DeleteAsset(folder);
        }

        /// <summary>Project ビューでフォルダを選択＆Ping</summary>
        private static void Ping(string folderPath)
        {
            var obj = AssetDatabase.LoadAssetAtPath<Object>(folderPath);
            if (obj == null) return;
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }
        #endregion

        // https://qiita.com/uni928/items/abf3a3502f760c8208cd
        private static void MyMoveFile(string rootFile, string targetFile)
        {
            if (rootFile.StartsWith("Assets"))
            {
                rootFile = UnityEngine.Application.dataPath + rootFile.Substring(6);
            }
            if (targetFile.StartsWith("Assets"))
            {
                targetFile = UnityEngine.Application.dataPath + targetFile.Substring(6);
            }
            if (!System.IO.File.Exists(rootFile))
            {
                return;
            }
            if (System.IO.File.Exists(targetFile))
            {
                System.IO.File.Delete(targetFile);
            }
            if (System.IO.File.Exists(rootFile + ".meta"))
            {
                if (System.IO.File.Exists(targetFile + ".meta"))
                {
                    System.IO.File.Delete(targetFile + ".meta");
                }
                System.IO.File.Copy(rootFile, targetFile);
                TryFileMove(rootFile + ".meta", targetFile + ".meta");
                System.IO.File.Delete(rootFile);
                if (System.IO.File.Exists(rootFile + ".meta")) //自動で生成された場合に警告を出さないための if 文
                {
                    try
                    {
                        System.IO.File.Delete(rootFile + ".meta"); //自動で削除される可能性があるため try-catch 必須
                    }
                    catch
                    {
                    }
                }
            }
            else
            {
                System.IO.File.Move(rootFile, targetFile);
            }
        }

        private static void TryFileMove(string rootFileMeta, string targetFileMeta)
        {
            try
            {
                System.IO.File.Move(rootFileMeta, targetFileMeta);
            }
            catch
            {
                System.IO.File.Delete(targetFileMeta);
                TryFileMove(rootFileMeta, targetFileMeta);
            }
        }

    }
}