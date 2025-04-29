/*  Reference
    https://karanokan.info/2020/10/22/post-5625/
    https://qiita.com/luckin/items/96f0ce9e1ac86f9b51fc
    https://tips.hecomi.com/entry/2016/10/15/004144
    https://qiita.com/Fuji0k0/items/4f4bfa552e5c7967ac60

    https://qiita.com/skkn/items/76767ee7e1897e5ba8ec
    https://light11.hatenadiary.com/entry/2018/10/17/222641

    Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license
    StandardShaderGUI.cs
*/

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// namespace WataOfuton{

/// <summary>
/// 自作アトリビュートを Properties に記述するといい感じにまとめてくれる
/// アトリビュートがなくてもいい.
/// ↓ 使用できる自作アトリビュート 
/// [wHeader(TITLE)]  ...[Header(TITLE)] じゃなければなんでもいい(標準の Header が動作してしまうため).
/// [HeaderEnd] ...記述しなくてもいい.
/// [wFoldout(TITLE)] ...Foldout を作成する.
/// [FoldoutEnd] ...記述しなくてもいいが記述したほうがバグらないはず.
/// </summary>
public class WataShaderInspector : ShaderGUI
{
    private bool isNoHeader;
    private bool isHeaderOpen;
    private bool isReadyRenderSettings;
    private string giMode = "_GIMode";
    private string blendMode = "_blendMode";
    private List<bool> isFoldoutOpen = new List<bool>();
    private int foldoutIndex = 0;


    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        // マテリアルを取得
        Material material = materialEditor.target as Material;
        Shader shader = material.shader;

        // プロパティ数に応じてisFoldoutOpenのサイズを調整
        if (isFoldoutOpen.Count != properties.Length)
        {
            isFoldoutOpen = new List<bool>();
            for (int i = 0; i < properties.Length; i++)
                isFoldoutOpen.Add(false);
        }

        isNoHeader = true;

        // プロパティごとにUIを自動生成
        foreach (var prop in properties)
        {
            if (prop.flags == MaterialProperty.PropFlags.HideInInspector) continue;

            int index = shader.FindPropertyIndex(prop.name);
            string[] shader_attribs = shader.GetPropertyAttributes(index);   //プロパティのAttributeを受け取る(GetPropertyFlagsで取得可能なAttribute以外)

            // Attributeは最後に記述されているものが優先される
            // Attributeを種類(attribs)と()内の記述(param)に分ける { PowerSlider(0.5)→attribs = PowerSlider, param = 0.5 }
            List<string> attribs = new List<string>();
            List<string> param = new List<string>();
            foreach (string attribute in shader_attribs)
            {
                // Space
                if (attribute.IndexOf("Space") == 0)
                {
                    if (attribute == "Space")
                    {
                        EditorGUILayout.Space();
                    }
                    else
                    {
                        MatchCollection matches = Regex.Matches(attribute, @"(?<=\().*(?=\))"); //括弧内を抽出
                        if (matches.Count != 0)
                        {
                            int space_height = 0;
                            try
                            {
                                space_height = int.Parse(matches[0].Value);
                            }
                            catch
                            {
                                // break;
                            }
                            EditorGUILayout.Space(space_height);
                        }
                    }
                }
                // Header
                else if (attribute.Contains("Header"))
                {
                    if (isHeaderOpen || attribute == "HeaderEnd")
                    {
                        if (isReadyRenderSettings)
                            RenderSettingsArea(material, materialEditor, null);
                        EditorGUILayout.EndVertical();
                        isHeaderOpen = false;
                        isNoHeader = true;
                    }

                    MatchCollection matches = Regex.Matches(attribute, @"(?<=\().*(?=\))"); //括弧内を抽出
                    if (matches.Count != 0)
                    {
                        string headerName = "";
                        try
                        {
                            headerName = matches[0].Value;
                            if (headerName == "RenderSettings")
                            {
                                isReadyRenderSettings = true;
                            }

                            CustomUI.ShurikenHeader(headerName);
                            EditorGUILayout.BeginVertical("HelpBox");
                            isHeaderOpen = true;
                            isNoHeader = false;
                        }
                        catch
                        {
                            // break;
                        }
                    }
                }
                // Foldout
                else if (attribute.Contains("Foldout"))
                {
                    if (attribute == "FoldoutEnd")
                    {
                        bool isFO = false;
                        for (int i = index; i >= foldoutIndex; i--)
                        {
                            if (isFoldoutOpen[i])
                            {
                                isFO = true;
                                break;
                            }
                        }
                        if (isFO)
                        {
                            EditorGUILayout.EndVertical();
                            isHeaderOpen = false;
                        }
                        isNoHeader = true;
                    }
                    else
                    {
                        if (isHeaderOpen)
                        {
                            EditorGUILayout.EndVertical();
                            isHeaderOpen = false;
                            isNoHeader = true;
                        }

                        MatchCollection matches = Regex.Matches(attribute, @"(?<=\().*(?=\))"); //括弧内を抽出
                        if (matches.Count != 0)
                        {
                            string headerName = "";
                            try
                            {
                                headerName = matches[0].Value;
                                isFoldoutOpen[index] = CustomUI.ShurikenFoldout(headerName, isFoldoutOpen[index]);
                                if (isFoldoutOpen[index])
                                {
                                    EditorGUILayout.BeginVertical("HelpBox");
                                    isHeaderOpen = true;
                                }
                                else
                                {
                                    isNoHeader = false;
                                }
                                foldoutIndex = index;
                            }
                            catch
                            {
                                // break;
                            }
                        }
                    }
                }
                // Free Text
                // フリーなテキストを入れられたらいいのにな～～～
                else if (attribute.IndexOf("Text") == 0)
                {
                    MatchCollection matches = Regex.Matches(attribute, @"(?<=\().*(?=\))"); //括弧内を抽出
                    if (matches.Count != 0)
                    {
                        string text = "";
                        try
                        {
                            text = matches[0].Value;
                            EditorGUILayout.HelpBox(text, MessageType.Info);
                        }
                        catch
                        {
                            // break;
                        }
                    }
                }
                // SpaceとHeader以外
                // 今は使ってない.
                else
                {
                    MatchCollection matches = Regex.Matches(attribute, @".*(?=\()"); //括弧の前を抽出
                    string atr;
                    if (matches.Count != 0)
                    {
                        atr = matches[0].Value;
                        attribs.Add(atr);
                        MatchCollection param_matches = Regex.Matches(attribute, @"(?<=\().*(?=\))"); //括弧内を抽出
                        if (param_matches.Count != 0)
                        {
                            param.Add(param_matches[0].Value);
                        }
                    }
                    else
                    {
                        //括弧がない場合
                        attribs.Add(attribute);
                        param.Add(null);
                    }
                }
            }
            if (attribs.Count != 0)
            {
                attribs.Reverse(); //Attributeは最後に記述されているものが優先されるので反転
                param.Reverse();
            }

            if (!isHeaderOpen && !isFoldoutOpen[index])
            {
                if (!isNoHeader) continue;
            }

            // プロパティのタイプに応じた表示
            switch (prop.type)
            {
                case MaterialProperty.PropType.Float:
                    if (prop.name == blendMode)
                        AlphaBlend(material, materialEditor, prop);
                    else if (prop.name == giMode)
                        RenderSettingsArea(material, materialEditor, prop);
                    else
                        // materialEditor.FloatProperty(prop, prop.displayName);
                        materialEditor.ShaderProperty(prop, prop.displayName);
                    break;
                case MaterialProperty.PropType.Range:
                    if (attribs == null || attribs.Count == 0)
                    {
                        materialEditor.RangeProperty(prop, prop.displayName);
                    }
                    else if (attribs[0] == "IntRange")
                    {
                        float slider_val = material.GetFloat(prop.name);
                        int min = (int)ShaderUtil.GetRangeLimits(shader, index, 1);
                        int max = (int)ShaderUtil.GetRangeLimits(shader, index, 2);
                        slider_val = EditorGUILayout.IntSlider(prop.displayName, (int)slider_val, min, max);
                        material.SetFloat(prop.name, slider_val);
                    }
                    else
                    {
                        materialEditor.RangeProperty(prop, prop.displayName);
                    }
                    break;
                case MaterialProperty.PropType.Int:
                    materialEditor.IntegerProperty(prop, prop.displayName);
                    break;

                case MaterialProperty.PropType.Texture:
                    if (prop.flags == MaterialProperty.PropFlags.NoScaleOffset)
                    {
                        materialEditor.TexturePropertySingleLine(new GUIContent(prop.displayName), prop);
                    }
                    else if (prop.flags == MaterialProperty.PropFlags.Normal)
                    {
                        materialEditor.TexturePropertySingleLine(new GUIContent(prop.displayName), prop);
                        using (new EditorGUI.IndentLevelScope())
                        {
                            materialEditor.TextureScaleOffsetProperty(prop);
                        }
                    }
                    else
                    {
                        materialEditor.TexturePropertySingleLine(new GUIContent(prop.displayName), prop);
                        using (new EditorGUI.IndentLevelScope())
                        {
                            materialEditor.TextureScaleOffsetProperty(prop);
                        }
                    }
                    break;

                case MaterialProperty.PropType.Color:
                    materialEditor.ColorProperty(prop, prop.displayName);
                    break;

                case MaterialProperty.PropType.Vector:
                    if (attribs == null || attribs.Count == 0)
                    {
                        materialEditor.VectorProperty(prop, prop.displayName);
                    }
                    else if (attribs[0] == "Vector2")
                    {
                        var slider_val = material.GetVector(prop.name);
                        slider_val = EditorGUILayout.Vector2Field(prop.displayName, new Vector2(slider_val.x, slider_val.y));
                        material.SetVector(prop.name, slider_val);
                    }
                    else if (attribs[0] == "Vector3")
                    {
                        var slider_val = material.GetVector(prop.name);
                        slider_val = EditorGUILayout.Vector3Field(prop.displayName, new Vector3(slider_val.x, slider_val.y, slider_val.z));
                        material.SetVector(prop.name, slider_val);
                    }
                    else
                    {
                        materialEditor.VectorProperty(prop, prop.displayName);
                    }
                    break;

                default:
                    // 未対応のプロパティ
                    EditorGUILayout.LabelField(prop.displayName + " (Unsupported property type)");
                    break;
            }

            if (prop == properties[properties.Length - 1])
            {
                if (isHeaderOpen)
                {
                    if (isReadyRenderSettings)
                        RenderSettingsArea(material, materialEditor, null);
                    EditorGUILayout.EndVertical();
                    isHeaderOpen = false;
                }
            }

            // Debug
            // EditorGUILayout.BeginVertical(GUI.skin.box);
            // // for (int j = 0; j < shader_attribs.Length; j++)
            // // {
            // //     EditorGUILayout.LabelField($"index : {index}, attrib[{j}] : " + shader_attribs[j]);
            // // }
            // // EditorGUILayout.LabelField($"properties[{index}] : " + prop.type + ", " + prop.displayName);
            // for (int j = 0; j < attribs.Count; j++)
            //     EditorGUILayout.LabelField($"attribs[{j}] : " + attribs[j] + ", Param : " + param[j]);
            // EditorGUILayout.EndVertical();
        }

        // // Debug
        // EditorGUILayout.BeginVertical(GUI.skin.box);
        // for (int j = 0; j < isFoldoutOpen.Count; j++)
        //     EditorGUILayout.LabelField($"isFoldoutOpen[{j}] : " + isFoldoutOpen[j]);
        // EditorGUILayout.EndVertical();

        // マテリアルの変更を保存
        if (GUI.changed)
        {
            foreach (Material mat in materialEditor.targets)
            {
                EditorUtility.SetDirty(mat);
            }
        }
    }

    void RenderSettingsArea(Material material, MaterialEditor materialEditor, MaterialProperty prop)
    {
        isReadyRenderSettings = false;

        // Render Queue
        EditorGUILayout.Space();
        materialEditor.RenderQueueField(); // Render Queue設定を表示

        // GPU Instancing
        EditorGUI.BeginChangeCheck();
        bool gpuInstancing = EditorGUILayout.Toggle("Enable GPU Instancing", material.enableInstancing);
        if (EditorGUI.EndChangeCheck())
        {
            material.enableInstancing = gpuInstancing;
        }

        if (prop == null) return;
        // Global Illumination
        materialEditor.ShaderProperty(prop, prop.displayName);
        if (material.GetFloat("_GIMode") == 0)
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        else if (material.GetFloat("_GIMode") == 1)
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        else if (material.GetFloat("_GIMode") == 2)
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        else if (material.GetFloat("_GIMode") == 3)
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
    }

    void AlphaBlend(Material material, MaterialEditor materialEditor, MaterialProperty prop)
    {
        EditorGUILayout.Space(10);
        bool blendModeChanged = false;

        blendModeChanged = BlendModePopup(materialEditor, prop);

        if (blendModeChanged)
        {
            foreach (var obj in prop.targets)
                SetupMaterialWithBlendMode((Material)obj, (BlendMode)((Material)obj).GetFloat("_blendMode"), true);
        }

        foreach (var obj in prop.targets)
            if ((BlendMode)((Material)obj).GetFloat("_blendMode") == BlendMode.Manual)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    int sb = (int)material.GetFloat("_SrcBlend");
                    sb = EditorGUILayout.Popup("_SrcBlend", sb, Enum.GetNames(typeof(UnityEngine.Rendering.BlendMode)));
                    material.SetFloat("_SrcBlend", (float)sb);
                    int db = (int)material.GetFloat("_DstBlend");
                    db = EditorGUILayout.Popup("_DstBlend", db, Enum.GetNames(typeof(UnityEngine.Rendering.BlendMode)));
                    material.SetFloat("_DstBlend", (float)db);
                }
            }
    }

    public enum BlendMode
    {
        Opaque,
        Cutout,
        Fade,   // Old school alpha-blending mode, fresnel does not affect amount of transparency
        Transparent, // Physically plausible transparency mode, implemented as alpha pre-multiply
        Manual
    }
    bool BlendModePopup(MaterialEditor materialEditor, MaterialProperty prop)
    {
        var blendMode = prop;
        MaterialEditor.BeginProperty(blendMode);
        var mode = (BlendMode)blendMode.floatValue;

        EditorGUI.BeginChangeCheck();
        mode = (BlendMode)EditorGUILayout.Popup("Blend", (int)mode, Enum.GetNames(typeof(BlendMode)));
        bool result = EditorGUI.EndChangeCheck();
        if (result)
        {
            materialEditor.RegisterPropertyChangeUndo("Rendering Mode");
            blendMode.floatValue = (float)mode;
        }

        MaterialEditor.EndProperty();

        return result;
    }
    public static void SetupMaterialWithBlendMode(Material material, BlendMode blendMode, bool overrideRenderQueue)
    {
        EditorGUILayout.LabelField($"Blend");
        int minRenderQueue = -1;
        int maxRenderQueue = 5000;
        int defaultRenderQueue = -1;
        switch (blendMode)
        {
            case BlendMode.Opaque:
                material.SetOverrideTag("RenderType", "");
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                material.SetFloat("_ZWrite", 1.0f);
                material.SetFloat("_AlphaToMask", 0.0f);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                minRenderQueue = -1;
                maxRenderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest - 1;
                defaultRenderQueue = -1;
                break;
            case BlendMode.Cutout:
                material.SetOverrideTag("RenderType", "TransparentCutout");
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                material.SetFloat("_ZWrite", 1.0f);
                material.SetFloat("_AlphaToMask", 1.0f);
                material.EnableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                minRenderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                maxRenderQueue = (int)UnityEngine.Rendering.RenderQueue.GeometryLast;
                defaultRenderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                break;
            case BlendMode.Fade:
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0.0f);
                material.SetFloat("_AlphaToMask", 0.0f);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                minRenderQueue = (int)UnityEngine.Rendering.RenderQueue.GeometryLast + 1;
                maxRenderQueue = (int)UnityEngine.Rendering.RenderQueue.Overlay - 1;
                defaultRenderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                break;
            case BlendMode.Transparent:
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0.0f);
                material.SetFloat("_AlphaToMask", 0.0f);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                minRenderQueue = (int)UnityEngine.Rendering.RenderQueue.GeometryLast + 1;
                maxRenderQueue = (int)UnityEngine.Rendering.RenderQueue.Overlay - 1;
                defaultRenderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                break;
            case BlendMode.Manual:
                // minRenderQueue = -1;
                // maxRenderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest - 1;
                // defaultRenderQueue = -1;
                break;
        }

        if (overrideRenderQueue || material.renderQueue < minRenderQueue || material.renderQueue > maxRenderQueue)
        {
            if (!overrideRenderQueue)
                Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "Render queue value outside of the allowed range ({0} - {1}) for selected Blend mode, resetting render queue to default", minRenderQueue, maxRenderQueue);
            material.renderQueue = defaultRenderQueue;
        }
    }
}


static class CustomUI
{
    private static Rect ShurikenStyle(string title)
    {
        var style = new GUIStyle("ShurikenModuleTitle");
        style.margin = new RectOffset(0, 0, 4, 0);
        style.font = new GUIStyle(EditorStyles.boldLabel).font;
        style.fontStyle = new GUIStyle(EditorStyles.boldLabel).fontStyle;
        style.fontSize = 12;
        style.border = new RectOffset(15, 7, 4, 4);
        style.fixedHeight = 22;
        style.contentOffset = new Vector2(20f, -2f);
        var rect = GUILayoutUtility.GetRect(16f, 22, style);
        GUI.Box(rect, title, style);
        return rect;
    }

    public static void ShurikenHeader(string title)
    {
        ShurikenStyle(title);
    }

    public static bool ShurikenFoldout(string title, bool display)
    {
        var rect = ShurikenStyle(title);
        var e = Event.current;
        var toggleRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
        if (e.type == EventType.Repaint)
        {
            EditorStyles.foldout.Draw(toggleRect, false, false, display, false);
        }
        if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
        {
            display = !display;
            e.Use();
        }
        return display;
    }

    // public static void VerticalGroup(Action action, float space)
    // {
    //     EditorGUILayout.BeginHorizontal();
    //     GUILayout.Space(space);
    //     EditorGUILayout.BeginVertical("HelpBox");
    //     action();
    //     EditorGUILayout.EndVertical();
    //     EditorGUILayout.EndHorizontal();
    // }

    // public static void Vector2Property(MaterialProperty property, string name)
    // {
    //     EditorGUI.BeginChangeCheck();
    //     Vector2 vector2 = EditorGUILayout.Vector2Field(name, new Vector2(property.vectorValue.x, property.vectorValue.y), null);
    //     if (EditorGUI.EndChangeCheck())
    //     {
    //         property.vectorValue = new Vector4(vector2.x, vector2.y);
    //     }
    // }
}
#endif
// }