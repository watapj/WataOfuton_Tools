// // ------------------------------------------------------------
// // PrefabBoundsAutoFixer
// //  ・インポートされた Prefab 内の SkinnedMeshRenderer.localBounds を自動修正
// //  ・元 FBX が見つかれば FBX の Bounds をコピーし、ルートボーン差分も補正
// //  ・元 FBX が無い場合は sharedMesh.bounds を基準に再計算
// // ------------------------------------------------------------
// using System.Linq;
// using UnityEditor;
// using UnityEngine;

// namespace WataOfuton.Tools
// {
//     public class PrefabBoundsAutoFixerOnImporter : AssetPostprocessor
//     {
//         /// <summary>
//         /// Bounds 上書きを行わないフォルダ名 (部分一致・大小無視)
//         /// </summary>
//         private static readonly string[] IgnoreDirKeywords =
//         {
//             "Avatar_Masscat 2",
//             "WataOfuton",
//             // "ThirdParty",
//             // "IgnoreBounds",
//             // "DoNotTouch"
//         };

//         // すべてのアセットインポート後に呼ばれるコールバック
//         private static void OnPostprocessAllAssets(
//             string[] importedAssets,
//             string[] _,
//             string[] __,
//             string[] ___)
//         {
//             foreach (string path in importedAssets)
//             {
//                 // Prefab 以外は無視
//                 if (!path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase)) continue;

//                 // 除外フォルダに含まれていれば無視
//                 if (IgnoreDirKeywords.Any(k =>
//                         path.IndexOf(k, System.StringComparison.OrdinalIgnoreCase) >= 0)) continue;

//                 // Prefab の中身を一時的に開く
//                 GameObject root = PrefabUtility.LoadPrefabContents(path);
//                 bool modified = false;

//                 foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
//                 {
//                     Bounds fixedBounds = CalculateFixedBounds(smr);
//                     if (!BoundsApproximatelyEqual(smr.localBounds, fixedBounds))
//                     {
//                         smr.localBounds = fixedBounds;
//                         modified = true;
//                     }
//                 }

//                 // 変更があった場合のみ Prefab に書き戻す
//                 if (modified)
//                 {
//                     PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
//                     Debug.Log(success
//                         ? $"[PrefabBoundsAutoFixer] Bounds fixed: {System.IO.Path.GetFileName(path)}"
//                         : $"[PrefabBoundsAutoFixer] Bounds 保存失敗: {path}");
//                 }

//                 // メモリ解放
//                 PrefabUtility.UnloadPrefabContents(root);
//             }
//         }

//         /// <summary>
//         /// smr の正しい Bounds を計算
//         /// </summary>
//         private static Bounds CalculateFixedBounds(SkinnedMeshRenderer smr)
//         {
//             // ① Prefab ↔ FBX 対応 SMR を取得
//             // https://light11.hatenadiary.com/entry/2019/04/18/202742
//             /*
//             var fbxSmr = PrefabUtility.GetCorrespondingObjectFromOriginalSource(smr);

//             // ─ FBX が取得できた場合 ───────────────────────────
//             if (fbxSmr != null && fbxSmr.sharedMesh != null)
//             {
//                 // ベースは FBX の localBounds
//                 Bounds bounds = fbxSmr.localBounds;

//                 // ルートボーンが異なれば差分行列で丸ごと変換
//                 if (smr.rootBone && fbxSmr.rootBone && smr.rootBone.name != fbxSmr.rootBone.name)
//                 {
//                     Matrix4x4 toPrefabRenderer =
//                           smr.transform.worldToLocalMatrix
//                         * smr.rootBone.worldToLocalMatrix
//                         * fbxSmr.rootBone.localToWorldMatrix
//                         * fbxSmr.transform.localToWorldMatrix;

//                     bounds = TransformBounds(bounds, toPrefabRenderer);
//                 }
//                 return bounds;
//             }

//             // ─ FBX が無い場合 ────────────────────────────────
//             if (smr.sharedMesh == null)            // sharedMesh すら無ければ元値を返す
//                 return smr.localBounds;

//             Bounds sharedBounds = smr.sharedMesh.bounds;

//             if (smr.rootBone)                      // rootBone 差分を補正
//             {
//                 Matrix4x4 toRenderer =
//                       smr.rootBone.worldToLocalMatrix
//                     * smr.transform.localToWorldMatrix;

//                 sharedBounds = TransformBounds(sharedBounds, toRenderer);
//             }
//             return sharedBounds;
//             // */
//             // Prefab ↔ オリジナル対応オブジェクト取得
//             Object original = PrefabUtility.GetCorrespondingObjectFromOriginalSource(smr);
//             SkinnedMeshRenderer fbxSmr = null;
//             bool hasValidFbx = false;

//             if (original != null)
//             {
//                 string originalPath = AssetDatabase.GetAssetPath(original);
//                 if (originalPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
//                 {
//                     // FBX の中の同名 SMR を取得（直リンクで取れない場合がある）
//                     fbxSmr = original as SkinnedMeshRenderer ??
//                              AssetDatabase.LoadAllAssetsAtPath(originalPath)
//                                  .OfType<SkinnedMeshRenderer>()
//                                  .FirstOrDefault(r => r.name == smr.name);

//                     hasValidFbx = fbxSmr != null && fbxSmr.sharedMesh != null;
//                 }
//             }

//             // ─ FBX が有効に取得できた場合 ─────────────────────
//             if (hasValidFbx)
//             {
//                 Bounds bounds = fbxSmr.localBounds;

//                 // ルートボーン名が異なれば差分を適用
//                 if (smr.rootBone && fbxSmr.rootBone && smr.rootBone.name != fbxSmr.rootBone.name)
//                 {
//                     Matrix4x4 toPrefabRenderer =
//                           smr.transform.worldToLocalMatrix
//                         * smr.rootBone.worldToLocalMatrix
//                         * fbxSmr.rootBone.localToWorldMatrix
//                         * fbxSmr.transform.localToWorldMatrix;

//                     bounds = TransformBounds(bounds, toPrefabRenderer);
//                 }
//                 return bounds;
//             }

//             // ─ FBX が無い / FBX でない場合は sharedMesh から再計算 ─────
//             if (smr.sharedMesh == null)
//                 return smr.localBounds; // どうにも出来ない場合は元値返却

//             Bounds meshBounds = smr.sharedMesh.bounds;

//             // RootBone があれば差分を適用
//             if (smr.rootBone)
//             {
//                 Matrix4x4 toRenderer =
//                       smr.rootBone.worldToLocalMatrix
//                     * smr.transform.localToWorldMatrix;

//                 meshBounds = TransformBounds(meshBounds, toRenderer);
//             }
//             return meshBounds;
//         }

//         // ──────────────────────────────────────────────────────
//         // Utilities
//         // ──────────────────────────────────────────────────────
//         /// <summary>
//         /// Bounds を行列で変換（回転・スケール・位置を含む）
//         /// </summary>
//         private static Bounds TransformBounds(in Bounds b, in Matrix4x4 m)
//         {
//             // 変換後中心
//             Vector3 center = m.MultiplyPoint3x4(b.center);

//             // 変換後エクステントを各軸毎に算出
//             Vector3 ext = b.extents;
//             Vector3 axisX = m.MultiplyVector(new Vector3(ext.x, 0, 0));
//             Vector3 axisY = m.MultiplyVector(new Vector3(0, ext.y, 0));
//             Vector3 axisZ = m.MultiplyVector(new Vector3(0, 0, ext.z));

//             ext = new Vector3(
//                 Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
//                 Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
//                 Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z));

//             return new Bounds(center, ext * 2f);
//         }

//         /// <summary>
//         /// Bounds 同士の近似比較（中心 & サイズ共に 1e-4 以内なら同一とみなす
//         /// </summary>
//         private static bool BoundsApproximatelyEqual(Bounds a, Bounds b, float eps = 1e-4f)
//         {
//             return Vector3.SqrMagnitude(a.center - b.center) < eps * eps
//                 && Vector3.SqrMagnitude(a.size - b.size) < eps * eps;
//         }
//     }
// }
