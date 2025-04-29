using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace WataOfuton
{
    /// <summary>
    /// 1行目に “MY_CUSTOM_TEMPLATE_MARKER” を含むシェーダーを検出したら、
    /// ① 同名 .hlsl を生成
    /// ② .shader 先頭行のマーカーを削除
    /// ③ .hlsl 内のインクルードガード名を「シェーダーファイル名」と同一にする
    ///    （先頭が数字等の場合に備え、英数字とアンダースコアのみの識別子へ変換）
    /// ❹ シェーダーを参照する同名 .mat を自動生成  
    /// </summary>
    public class ShaderAssetsAutoGenerator : AssetPostprocessor
    {
        // シェーダーテンプレート内に含めておくマーカー文字列
        private const string TEMPLATE_MARKER = "// MY_CUSTOM_TEMPLATE_MARKER";

        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (var shaderPath in importedAssets)
            {
                // .shader 以外は対象外
                if (Path.GetExtension(shaderPath).ToLower() != ".shader") continue;

                // 1 行目だけ確認
                string[] shaderLines = File.ReadAllLines(shaderPath);
                if (shaderLines.Length == 0 || !shaderLines[0].Contains(TEMPLATE_MARKER)) continue;

                string dir = Path.GetDirectoryName(shaderPath);
                string fileName = Path.GetFileNameWithoutExtension(shaderPath); // 例) MyLitShader
                string guardName = SanitizeIdentifier(fileName);                // 例) MYLITSHADER
                string hlslPath = Path.Combine(dir, fileName + ".hlsl");
                string matPath = Path.Combine(dir, fileName + ".mat");

                // ❷ .hlsl を生成
                if (!File.Exists(hlslPath))
                {
                    File.WriteAllText(hlslPath, BuildHLSLTemplate(guardName));
                }

                // ❸ マーカー行削除
                var trimmedLines = new List<string>(shaderLines);
                trimmedLines.RemoveAt(0);
                File.WriteAllLines(shaderPath, trimmedLines.ToArray());

                // ❹ 同名 Material を生成
                Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
                if (shader != null && !File.Exists(matPath))
                {
                    var mat = new Material(shader)
                    {
                        name = fileName
                    };
                    AssetDatabase.CreateAsset(mat, matPath);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 識別子に使えない文字をアンダースコアへ、先頭が数字なら先頭に '_' を付与
        /// </summary>
        private static string SanitizeIdentifier(string src)
        {
            string id = Regex.Replace(src, @"[^A-Za-z0-9_]", "_");
            if (id.Length == 0 || char.IsDigit(id[0])) id = "_" + id;
            return id.ToUpper();          // プリプロセッサガードは大文字慣習
        }

        /// <summary>
        /// インクルードガード名を差し替えたテンプレート文字列を返す
        /// </summary>
        private static string BuildHLSLTemplate(string guard)
        {
            // ---------------------------------------------------------------
            // {guard}.hlsl
            // 自動生成テンプレート
            // ---------------------------------------------------------------

            return $@"
#ifndef {guard}
#define {guard}

#ifdef UNITY_PASS_FORWARDBASE
#endif
#ifdef UNITY_PASS_SHADOWCASTER
#endif
#ifdef _VMODE_MODE1
#endif

CBUFFER_START(MyShaderProperties)
    sampler2D _MainTex; float4 _MainTex_ST;
    float4 _Color;
    float _Param;
CBUFFER_END

inline void CalcVert(inout float4 pos, inout float4 vertex) {{
    pos = UnityObjectToClipPos(vertex);
}}

v2f vert(appdata v) 
{{
    v2f o = (v2f)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o); // 行列使わない場合不要.
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    // o.pos = UnityObjectToClipPos(v.vertex);
    // FullQuad(o.pos, v.uv, 1.0, 0.0);
    CalcVert(o.pos, v.vertex);
    o.uv = v.uv;

    #ifdef UNITY_PASS_SHADOWCASTER
    // to Local Positon for ShadowCaster
    // v.vertex = mul(unity_WorldToObject, p);
    TRANSFER_SHADOW_CASTER(o)
    // TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
    #endif
    return o;
}}



// float4 frag (v2f i) : SV_Target
// {{
//     // 行列使わない場合不要.
//     UNITY_SETUP_INSTANCE_ID(i);
//     UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

//     float4 col = tex2D(_MainTex, i.uv);
//     return col;
// }}

#endif // {guard}
";
        }
    }
}