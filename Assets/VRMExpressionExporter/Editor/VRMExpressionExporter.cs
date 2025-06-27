using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UniVRM10;

namespace VRMExpressionExporter.Editor
{
    /// <summary>
    /// VRM表情データをエクスポートするクラス
    /// </summary>
    public class VRMExpressionExporter : EditorWindow
    {
        private List<GameObject> vrmObjects = new List<GameObject>();
        private string outputPath = "Assets/VRMExpressionList.csv";
        private string animationOutputPath = "Assets/VRMExpressionAnimations";
        private bool generateAnimationClips = false;
        private bool includeZeroValues = true;
        private bool excludeMouthBlendShapes = true; // デフォルトで口を除外
        private bool generateWithMouthVersion = true; // 口を含むバージョンも生成
        private List<string> logMessages = new List<string>();
        private Vector2 mainScrollPosition;
        private Vector2 vrmListScrollPosition;
        private Vector2 logScrollPosition;
        
        // 画像キャプチャー設定
        private bool capturePreviewImages = false;
        private string imageOutputPath = "Assets/VRMExpressionImages";
        private int imageWidth = 512;
        private int imageHeight = 512;
        
        [MenuItem("VRM Expression Exporter/Expression Exporter")]
        private static void ShowWindow()
        {
            var window = GetWindow<VRMExpressionExporter>();
            window.titleContent = new GUIContent("VRM Expression Exporter");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnGUI()
        {
            // メインスクロールビュー開始
            mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition);
            
            EditorGUILayout.LabelField("VRM Expression Exporter", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox("VRMオブジェクトを追加して、表情リストをCSVファイルに書き出します。\nヒエラルキーやプロジェクトからVRMオブジェクトをドラッグ&ドロップしてください。", MessageType.Info);
            EditorGUILayout.Space();
            
            // VRMオブジェクトリスト
            EditorGUILayout.LabelField("VRMオブジェクト:", EditorStyles.boldLabel);
            
            // ドラッグ&ドロップエリア
            var dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "VRMオブジェクトをここにドラッグ&ドロップ", EditorStyles.helpBox);
            
            HandleDragAndDrop(dropArea);
            
            // オブジェクトリスト表示
            vrmListScrollPosition = EditorGUILayout.BeginScrollView(vrmListScrollPosition, GUILayout.Height(200));
            
            for (int i = 0; i < vrmObjects.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                vrmObjects[i] = EditorGUILayout.ObjectField($"VRM {i + 1}:", vrmObjects[i], typeof(GameObject), true) as GameObject;
                
                if (GUILayout.Button("削除", GUILayout.Width(50)))
                {
                    vrmObjects.RemoveAt(i);
                    i--;
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            
            // 追加ボタン
            if (GUILayout.Button("VRMオブジェクトを追加"))
            {
                vrmObjects.Add(null);
            }
            
            EditorGUILayout.Space();
            
            // 出力パス設定
            EditorGUILayout.LabelField("出力設定:", EditorStyles.boldLabel);
            outputPath = EditorGUILayout.TextField("CSVファイルパス:", outputPath);
            
            EditorGUILayout.Space();
            
            // AnimationClip生成設定
            EditorGUILayout.LabelField("AnimationClip設定:", EditorStyles.boldLabel);
            generateAnimationClips = EditorGUILayout.Toggle("AnimationClipを生成", generateAnimationClips);
            
            if (generateAnimationClips)
            {
                EditorGUI.indentLevel++;
                animationOutputPath = EditorGUILayout.TextField("保存先フォルダ:", animationOutputPath);
                includeZeroValues = EditorGUILayout.Toggle("値0のブレンドシェイプも含める", includeZeroValues);
                EditorGUILayout.HelpBox("値0を含めることで、表情切り替え時に確実にリセットされます。", MessageType.Info);
                excludeMouthBlendShapes = EditorGUILayout.Toggle("口のブレンドシェイプを除外", excludeMouthBlendShapes);
                if (excludeMouthBlendShapes)
                {
                    EditorGUILayout.HelpBox("口の動き（リップシンク）に関するブレンドシェイプを除外します。", MessageType.Info);
                    EditorGUI.indentLevel++;
                    generateWithMouthVersion = EditorGUILayout.Toggle("口を含むバージョンも生成", generateWithMouthVersion);
                    if (generateWithMouthVersion)
                    {
                        EditorGUILayout.HelpBox("口を含むAnimationClipを「WithMouth」フォルダに追加生成します。", MessageType.Info);
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // 画像キャプチャー設定
            EditorGUILayout.LabelField("画像キャプチャー設定:", EditorStyles.boldLabel);
            capturePreviewImages = EditorGUILayout.Toggle("表情画像をキャプチャー", capturePreviewImages);
            
            if (capturePreviewImages)
            {
                EditorGUI.indentLevel++;
                imageOutputPath = EditorGUILayout.TextField("保存先フォルダ:", imageOutputPath);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("画像サイズ:", GUILayout.Width(70));
                imageWidth = EditorGUILayout.IntField(imageWidth, GUILayout.Width(50));
                EditorGUILayout.LabelField("x", GUILayout.Width(15));
                imageHeight = EditorGUILayout.IntField(imageHeight, GUILayout.Width(50));
                EditorGUILayout.LabelField("ピクセル", GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.HelpBox("各表情の画像を透過背景でキャプチャーします。", MessageType.Info);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // エクスポートボタン
            GUI.enabled = vrmObjects.Count > 0 && vrmObjects.Any(obj => obj != null);
            if (GUILayout.Button("表情リストを書き出す", GUILayout.Height(30)))
            {
                ExportExpressionList();
            }
            GUI.enabled = true;
            
            // ログ表示
            if (logMessages.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("実行ログ:", EditorStyles.boldLabel);
                logScrollPosition = EditorGUILayout.BeginScrollView(logScrollPosition, GUILayout.Height(150));
                
                foreach (var message in logMessages)
                {
                    EditorGUILayout.LabelField(message, EditorStyles.wordWrappedLabel);
                }
                
                EditorGUILayout.EndScrollView();
                
                if (GUILayout.Button("ログをクリア"))
                {
                    logMessages.Clear();
                }
            }
            
            // メインスクロールビュー終了
            EditorGUILayout.EndScrollView();
        }

        private void HandleDragAndDrop(Rect dropArea)
        {
            Event evt = Event.current;
            
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition))
                        return;
                    
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        
                        foreach (Object draggedObject in DragAndDrop.objectReferences)
                        {
                            if (draggedObject is GameObject go)
                            {
                                // VRM10Instanceコンポーネントを持っているか確認
                                if (go.GetComponent<Vrm10Instance>() != null)
                                {
                                    if (!vrmObjects.Contains(go))
                                    {
                                        vrmObjects.Add(go);
                                        logMessages.Add($"追加: {go.name}");
                                    }
                                }
                                else
                                {
                                    logMessages.Add($"警告: {go.name} はVRMオブジェクトではありません。");
                                }
                            }
                        }
                    }
                    
                    Event.current.Use();
                    break;
            }
        }

        private void ExportExpressionList()
        {
            logMessages.Clear();
            logMessages.Add($"[{System.DateTime.Now:HH:mm:ss}] 表情リスト書き出しを開始します...");
            
            try
            {
                // 有効なVRMオブジェクトのみをフィルタリング
                var validVrmObjects = vrmObjects.Where(obj => obj != null).ToList();
                
                if (validVrmObjects.Count == 0)
                {
                    logMessages.Add("エラー: 有効なVRMオブジェクトが選択されていません。");
                    EditorUtility.DisplayDialog("エラー", "有効なVRMオブジェクトを選択してください。", "OK");
                    return;
                }
                
                logMessages.Add($"{validVrmObjects.Count}個のVRMオブジェクトを処理します。");
                
                // CSVデータを作成
                var csvData = new StringBuilder();
                if (capturePreviewImages)
                {
                    csvData.AppendLine("オブジェクト名,表情名,ブレンドシェイプパス,ブレンドシェイプ名,値(%),画像パス");
                }
                else
                {
                    csvData.AppendLine("オブジェクト名,表情名,ブレンドシェイプパス,ブレンドシェイプ名,値(%)");
                }
                
                int totalExpressions = 0;
                int totalBlendShapes = 0;
                int successCount = 0;
                
                foreach (var vrmObject in validVrmObjects)
                {
                    var expressions = ExtractExpressionsFromVRM(vrmObject);
                    if (expressions != null && expressions.Count > 0)
                    {
                        totalExpressions += expressions.Count;
                        successCount++;
                        
                        foreach (var expr in expressions)
                        {
                            // 画像パスを構築
                            string imagePath = "";
                            if (capturePreviewImages)
                            {
                                // 相対パスで記録（Assetsフォルダからの相対パス）
                                string characterName = expr.CharacterName;
                                imagePath = $"{imageOutputPath}/{characterName}/{expr.ExpressionName}.png";
                            }
                            
                            if (expr.BlendShapeSettings.Count == 0)
                            {
                                // ブレンドシェイプ設定がない表情も記録
                                if (capturePreviewImages)
                                {
                                    csvData.AppendLine($"\"{expr.ObjectName}\",\"{expr.ExpressionName}\",\"\",\"（ブレンドシェイプなし）\",\"0\",\"{imagePath}\"");
                                }
                                else
                                {
                                    csvData.AppendLine($"\"{expr.ObjectName}\",\"{expr.ExpressionName}\",\"\",\"（ブレンドシェイプなし）\",\"0\"");
                                }
                            }
                            else
                            {
                                foreach (var bs in expr.BlendShapeSettings)
                                {
                                    totalBlendShapes++;
                                    if (capturePreviewImages)
                                    {
                                        csvData.AppendLine($"\"{expr.ObjectName}\",\"{expr.ExpressionName}\",\"{bs.Path}\",\"{bs.Name}\",\"{bs.Value:F1}\",\"{imagePath}\"");
                                    }
                                    else
                                    {
                                        csvData.AppendLine($"\"{expr.ObjectName}\",\"{expr.ExpressionName}\",\"{bs.Path}\",\"{bs.Name}\",\"{bs.Value:F1}\"");
                                    }
                                }
                            }
                        }
                    }
                }
                
                if (totalExpressions == 0)
                {
                    logMessages.Add("エラー: 表情データが見つかりませんでした。");
                    EditorUtility.DisplayDialog("エラー", "選択されたオブジェクトから表情データを取得できませんでした。", "OK");
                    return;
                }
                
                // ファイルに書き出し
                var directory = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(outputPath, csvData.ToString(), Encoding.UTF8);
                
                logMessages.Add($"成功: {successCount}/{validVrmObjects.Count} オブジェクト");
                logMessages.Add($"合計 {totalExpressions} 個の表情、{totalBlendShapes} 個のブレンドシェイプ設定を書き出しました。");
                logMessages.Add($"ファイル保存先: {outputPath}");
                
                // AnimationClip生成
                if (generateAnimationClips)
                {
                    logMessages.Add("");
                    logMessages.Add("AnimationClip生成を開始します...");
                    GenerateAnimationClips(validVrmObjects);
                }
                
                // 画像キャプチャー
                if (capturePreviewImages)
                {
                    logMessages.Add("");
                    logMessages.Add("表情画像のキャプチャーを開始します...");
                    CaptureExpressionImages(validVrmObjects);
                }
                
                AssetDatabase.Refresh();
                
                var message = $"表情リストの書き出しが完了しました。\n\n" +
                    $"処理成功: {successCount}/{validVrmObjects.Count} オブジェクト\n" +
                    $"表情数: {totalExpressions}\n" +
                    $"ブレンドシェイプ設定数: {totalBlendShapes}\n" +
                    $"保存先: {outputPath}";
                    
                if (generateAnimationClips)
                {
                    message += $"\n\nAnimationClip保存先: {animationOutputPath}";
                }
                
                if (capturePreviewImages)
                {
                    message += $"\n\n画像保存先: {imageOutputPath}";
                }
                
                EditorUtility.DisplayDialog("完了", message, "OK");
                
                // ファイルを開く
                EditorUtility.RevealInFinder(outputPath);
            }
            catch (System.Exception e)
            {
                logMessages.Add($"エラーが発生しました: {e.Message}");
                Debug.LogError($"表情リスト書き出しエラー: {e}");
                EditorUtility.DisplayDialog("エラー", $"書き出し中にエラーが発生しました:\n{e.Message}", "OK");
            }
        }

        private List<ExpressionData> ExtractExpressionsFromVRM(GameObject vrmObject)
        {
            var expressions = new List<ExpressionData>();
            
            try
            {
                logMessages.Add($"処理中: {vrmObject.name}");
                
                // Vrm10Instanceコンポーネントを取得
                var vrm10Instance = vrmObject.GetComponent<Vrm10Instance>();
                if (vrm10Instance == null || vrm10Instance.Vrm == null || vrm10Instance.Vrm.Expression == null)
                {
                    logMessages.Add($"警告: {vrmObject.name} に有効なVRM10Instanceが見つかりませんでした。");
                    return null;
                }
                
                var expressionData = vrm10Instance.Vrm.Expression;
                string characterName = vrmObject.name;
                
                // メタデータからキャラクター名を取得
                if (vrm10Instance.Vrm.Meta != null && !string.IsNullOrEmpty(vrm10Instance.Vrm.Meta.Name))
                {
                    characterName = vrm10Instance.Vrm.Meta.Name;
                }
                
                string objectName = vrmObject.name;
                
                // プリセット表情を抽出
                var presetExpressions = new[]
                {
                    ("happy", expressionData.Happy),
                    ("angry", expressionData.Angry),
                    ("sad", expressionData.Sad),
                    ("relaxed", expressionData.Relaxed),
                    ("surprised", expressionData.Surprised),
                    ("aa", expressionData.Aa),
                    ("ih", expressionData.Ih),
                    ("ou", expressionData.Ou),
                    ("ee", expressionData.Ee),
                    ("oh", expressionData.Oh),
                    ("blink", expressionData.Blink),
                    ("blinkLeft", expressionData.BlinkLeft),
                    ("blinkRight", expressionData.BlinkRight),
                    ("lookUp", expressionData.LookUp),
                    ("lookDown", expressionData.LookDown),
                    ("lookLeft", expressionData.LookLeft),
                    ("lookRight", expressionData.LookRight),
                    ("neutral", expressionData.Neutral)
                };
                
                foreach (var (name, expression) in presetExpressions)
                {
                    if (expression != null)
                    {
                        var exprData = new ExpressionData
                        {
                            CharacterName = characterName,
                            ObjectName = objectName,
                            ExpressionName = name,
                            ExpressionType = "Preset"
                        };
                        
                        // ブレンドシェイプ設定を抽出
                        ExtractBlendShapeSettings(expression, exprData, vrmObject);
                        
                        expressions.Add(exprData);
                    }
                }
                
                // カスタム表情を抽出
                if (expressionData.CustomClips != null)
                {
                    foreach (var customClip in expressionData.CustomClips)
                    {
                        if (customClip != null)
                        {
                            var exprData = new ExpressionData
                            {
                                CharacterName = characterName,
                                ObjectName = objectName,
                                ExpressionName = customClip.name,
                                ExpressionType = "Custom"
                            };
                            
                            // ブレンドシェイプ設定を抽出
                            ExtractBlendShapeSettings(customClip, exprData, vrmObject);
                            
                            expressions.Add(exprData);
                        }
                    }
                }
                
                logMessages.Add($"{characterName}: {expressions.Count} 個の表情を抽出しました。");
            }
            catch (System.Exception e)
            {
                logMessages.Add($"エラー ({vrmObject.name}): {e.Message}");
                Debug.LogError($"VRM表情抽出エラー ({vrmObject.name}): {e}");
            }
            
            return expressions;
        }
        
        private void ExtractBlendShapeSettings(VRM10Expression expression, ExpressionData exprData, GameObject vrmObject)
        {
            if (expression.MorphTargetBindings == null) return;
            
            foreach (var binding in expression.MorphTargetBindings)
            {
                var blendShapeSetting = new BlendShapeSetting
                {
                    Path = binding.RelativePath,
                    Value = binding.Weight * 100f // 0-1 を 0-100% に変換
                };
                
                // ブレンドシェイプ名を取得
                var targetTransform = vrmObject.transform.Find(binding.RelativePath);
                if (targetTransform != null)
                {
                    var renderer = targetTransform.GetComponent<SkinnedMeshRenderer>();
                    if (renderer != null && renderer.sharedMesh != null)
                    {
                        if (binding.Index >= 0 && binding.Index < renderer.sharedMesh.blendShapeCount)
                        {
                            blendShapeSetting.Name = renderer.sharedMesh.GetBlendShapeName(binding.Index);
                        }
                        else
                        {
                            blendShapeSetting.Name = $"Index_{binding.Index}";
                        }
                    }
                    else
                    {
                        blendShapeSetting.Name = $"Index_{binding.Index}";
                    }
                }
                else
                {
                    blendShapeSetting.Name = $"Index_{binding.Index}";
                }
                
                exprData.BlendShapeSettings.Add(blendShapeSetting);
            }
        }

        private class ExpressionData
        {
            public string CharacterName;
            public string ObjectName;
            public string ExpressionName;
            public string ExpressionType;
            public List<BlendShapeSetting> BlendShapeSettings = new List<BlendShapeSetting>();
        }
        
        private class BlendShapeSetting
        {
            public string Path;
            public string Name;
            public float Value; // 0-100
        }
        
        private void GenerateAnimationClips(List<GameObject> vrmObjects)
        {
            if (!Directory.Exists(animationOutputPath))
            {
                Directory.CreateDirectory(animationOutputPath);
            }
            
            int totalClips = 0;
            
            foreach (var vrmObject in vrmObjects)
            {
                var vrm10Instance = vrmObject.GetComponent<Vrm10Instance>();
                if (vrm10Instance == null || vrm10Instance.Vrm == null || vrm10Instance.Vrm.Expression == null)
                    continue;
                
                // 全ブレンドシェイプを列挙
                var allBlendShapes = GetAllBlendShapes(vrmObject);
                logMessages.Add($"{vrmObject.name}: {allBlendShapes.Count} 個のメッシュでブレンドシェイプを検出");
                
                // 表情データを取得
                var expressions = ExtractExpressionsFromVRM(vrmObject);
                if (expressions == null || expressions.Count == 0)
                    continue;
                
                // 各メッシュごとにAnimationClipを生成
                foreach (var meshPath in allBlendShapes.Keys)
                {
                    var meshName = string.IsNullOrEmpty(meshPath) ? "Root" : meshPath.Replace("/", "_");
                    
                    foreach (var expression in expressions)
                    {
                        // 口を除外したバージョンを生成（メインフォルダ）
                        if (excludeMouthBlendShapes)
                        {
                            var clip = CreateAnimationClipForMesh(expression, allBlendShapes[meshPath], meshPath, vrmObject, true);
                            if (clip != null)
                            {
                                var clipPath = Path.Combine(animationOutputPath, $"{vrmObject.name}_{meshName}_{expression.ExpressionName}.anim");
                                AssetDatabase.CreateAsset(clip, clipPath);
                                totalClips++;
                                logMessages.Add($"AnimationClip作成: {clipPath}");
                            }
                            
                            // 口を含むバージョンも生成（WithMouthフォルダ）
                            if (generateWithMouthVersion)
                            {
                                var withMouthClip = CreateAnimationClipForMesh(expression, allBlendShapes[meshPath], meshPath, vrmObject, false);
                                if (withMouthClip != null)
                                {
                                    var withMouthPath = Path.Combine(animationOutputPath, "WithMouth");
                                    if (!Directory.Exists(withMouthPath))
                                    {
                                        Directory.CreateDirectory(withMouthPath);
                                    }
                                    var withMouthClipPath = Path.Combine(withMouthPath, $"{vrmObject.name}_{meshName}_{expression.ExpressionName}_WithMouth.anim");
                                    AssetDatabase.CreateAsset(withMouthClip, withMouthClipPath);
                                    totalClips++;
                                    logMessages.Add($"AnimationClip作成（口含む）: {withMouthClipPath}");
                                }
                            }
                        }
                        else
                        {
                            // 口を除外しない場合は通常通り生成
                            var clip = CreateAnimationClipForMesh(expression, allBlendShapes[meshPath], meshPath, vrmObject, false);
                            if (clip != null)
                            {
                                var clipPath = Path.Combine(animationOutputPath, $"{vrmObject.name}_{meshName}_{expression.ExpressionName}.anim");
                                AssetDatabase.CreateAsset(clip, clipPath);
                                totalClips++;
                                logMessages.Add($"AnimationClip作成: {clipPath}");
                            }
                        }
                    }
                }
            }
            
            logMessages.Add($"合計 {totalClips} 個のAnimationClipを生成しました。");
        }
        
        private Dictionary<string, List<string>> GetAllBlendShapes(GameObject vrmObject)
        {
            var blendShapeDict = new Dictionary<string, List<string>>();
            
            var renderers = vrmObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMesh == null) continue;
                
                var relativePath = GetRelativePath(vrmObject.transform, renderer.transform);
                var blendShapeNames = new List<string>();
                
                for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
                {
                    blendShapeNames.Add(renderer.sharedMesh.GetBlendShapeName(i));
                }
                
                if (blendShapeNames.Count > 0)
                {
                    blendShapeDict[relativePath] = blendShapeNames;
                }
            }
            
            return blendShapeDict;
        }
        
        private AnimationClip CreateAnimationClipForMesh(ExpressionData expression, List<string> meshBlendShapes, string meshPath, GameObject vrmObject, bool excludeMouth)
        {
            var clip = new AnimationClip();
            var meshName = string.IsNullOrEmpty(meshPath) ? "Root" : meshPath.Replace("/", "_");
            var suffix = excludeMouth ? "" : "_WithMouth";
            clip.name = $"{expression.ObjectName}_{meshName}_{expression.ExpressionName}{suffix}";
            
            // 表情で設定されているブレンドシェイプを記録
            var setBlendShapes = new Dictionary<string, float>();
            foreach (var bs in expression.BlendShapeSettings)
            {
                // このメッシュに対する設定のみを抽出
                if (bs.Path == meshPath)
                {
                    setBlendShapes[bs.Name] = bs.Value;
                }
            }
            
            // 全ブレンドシェイプに対してキーフレームを設定
            foreach (var blendShapeName in meshBlendShapes)
            {
                // 口のブレンドシェイプを除外する場合
                if (excludeMouth && IsMouthBlendShape(blendShapeName))
                {
                    continue;
                }
                
                float value = 0f;
                
                // 表情で設定されている値があれば使用
                if (setBlendShapes.ContainsKey(blendShapeName))
                {
                    value = setBlendShapes[blendShapeName];
                }
                
                // 値が0でも含める設定の場合、または値が0でない場合にキーフレームを追加
                if (includeZeroValues || value != 0f)
                {
                    var curve = AnimationCurve.Constant(0f, 0f, value);
                    var propertyName = $"blendShape.{blendShapeName}";
                    clip.SetCurve("", typeof(SkinnedMeshRenderer), propertyName, curve);
                }
            }
            
            return clip;
        }
        
        private string GetRelativePath(Transform root, Transform target)
        {
            if (target == root) return "";
            
            var path = target.name;
            var parent = target.parent;
            
            while (parent != null && parent != root)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }
        
        private bool IsMouthBlendShape(string blendShapeName)
        {
            // 口に関連するブレンドシェイプ名のパターン
            string[] mouthPatterns = new string[]
            {
                "mouth", "Mouth", "MOUTH",
                "lip", "Lip", "LIP",
                "jaw", "Jaw", "JAW",
                "tongue", "Tongue", "TONGUE",
                "teeth", "Teeth", "TEETH",
                // VRM標準の口関連
                "aa", "Aa", "AA",
                "ih", "Ih", "IH",
                "ou", "Ou", "OU",
                "ee", "Ee", "EE",
                "oh", "Oh", "OH",
                // パラメータ名
                "Param.A", "Param.I", "Param.U", "Param.E", "Param.O",
                "Param.MouthOpenY", "Param.MouthOpenU",
                "Param.MouthOpenExP", "Param.MouthOpenExN",
                "Param.MouthU", "Param.MouthExP", "Param.MouthExN"
            };
            
            foreach (var pattern in mouthPatterns)
            {
                if (blendShapeName.Contains(pattern))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        private void CaptureExpressionImages(List<GameObject> vrmObjects)
        {
            if (!Directory.Exists(imageOutputPath))
            {
                Directory.CreateDirectory(imageOutputPath);
            }
            
            int totalImages = 0;
            int currentImage = 0;
            
            // 総画像数を計算
            foreach (var vrmObject in vrmObjects)
            {
                var expressions = ExtractExpressionsFromVRM(vrmObject);
                if (expressions != null)
                {
                    totalImages += expressions.Count;
                }
            }
            
            foreach (var vrmObject in vrmObjects)
            {
                GameObject vrmClone = null;
                try
                {
                    // 前のキャラクターのクローンが残っていないか確認して削除
                    GameObject[] existingPreviews = GameObject.FindObjectsOfType<GameObject>();
                    foreach (var obj in existingPreviews)
                    {
                        if (obj != null && (obj.name.EndsWith("_Preview") || obj.name.Contains("VRM Preview")))
                        {
                            GameObject.DestroyImmediate(obj);
                        }
                    }
                    
                    // DestroyImmediateを確実に実行
                    EditorUtility.UnloadUnusedAssetsImmediate();
                    
                    // VRMインスタンスのクローンを作成
                    vrmClone = GameObject.Instantiate(vrmObject);
                    vrmClone.name = vrmObject.name + "_Preview";
                    vrmClone.hideFlags = HideFlags.HideAndDontSave; // シーンに保存しない
                    
                    // キャラクター名を取得
                    var vrm10Instance = vrmClone.GetComponent<Vrm10Instance>();
                    string characterName = vrmObject.name;
                    if (vrm10Instance != null && vrm10Instance.Vrm != null && vrm10Instance.Vrm.Meta != null && !string.IsNullOrEmpty(vrm10Instance.Vrm.Meta.Name))
                    {
                        characterName = vrm10Instance.Vrm.Meta.Name;
                    }
                    
                    // 表情データを取得
                    var expressions = ExtractExpressionsFromVRM(vrmObject);
                    if (expressions == null || expressions.Count == 0)
                    {
                        GameObject.DestroyImmediate(vrmClone);
                        continue;
                    }
                    
                    logMessages.Add($"{characterName}: {expressions.Count} 個の表情画像をキャプチャー中...");
                    
                    // 各表情をキャプチャー
                    foreach (var expression in expressions)
                    {
                        currentImage++;
                        
                        // プログレスバーを表示
                        float progress = (float)currentImage / totalImages;
                        if (EditorUtility.DisplayCancelableProgressBar("表情画像キャプチャー", 
                            $"{characterName} - {expression.ExpressionName} ({currentImage}/{totalImages})", 
                            progress))
                        {
                            // キャンセルされた場合
                            EditorUtility.ClearProgressBar();
                            
                            // vrmCloneを確実に削除
                            if (vrmClone != null)
                            {
                                GameObject.DestroyImmediate(vrmClone);
                            }
                            
                            // 残っているプレビューオブジェクトも削除
                            GameObject[] cancelPreviews = GameObject.FindObjectsOfType<GameObject>();
                            foreach (var obj in cancelPreviews)
                            {
                                if (obj != null && (obj.name.EndsWith("_Preview") || obj.name.Contains("VRM Preview")))
                                {
                                    GameObject.DestroyImmediate(obj);
                                }
                            }
                            
                            logMessages.Add("キャプチャーがキャンセルされました。");
                            return;
                        }
                        
                        // 表情に対応するVRM10Expressionを取得
                        VRM10Expression vrmExpression = GetVRM10Expression(vrm10Instance, expression.ExpressionName);
                        if (vrmExpression != null)
                        {
                            string imagePath = VRMPreviewCapture.CaptureExpression(
                                vrmClone, 
                                vrmExpression, 
                                characterName, 
                                expression.ExpressionName, 
                                imageOutputPath, 
                                imageWidth, 
                                imageHeight
                            );
                            
                            if (!string.IsNullOrEmpty(imagePath))
                            {
                                logMessages.Add($"  {expression.ExpressionName} - キャプチャー成功");
                            }
                            else
                            {
                                logMessages.Add($"  {expression.ExpressionName} - キャプチャー失敗");
                            }
                        }
                    }
                    
                    // クローンを削除
                    if (vrmClone != null)
                    {
                        GameObject.DestroyImmediate(vrmClone);
                        vrmClone = null;
                    }
                    
                    // 追加のクリーンアップ
                    GameObject[] remainingPreviews = GameObject.FindObjectsOfType<GameObject>();
                    foreach (var obj in remainingPreviews)
                    {
                        if (obj != null && (obj.name.EndsWith("_Preview") || obj.name.Contains("VRM Preview")))
                        {
                            GameObject.DestroyImmediate(obj);
                        }
                    }
                    
                    // ガベージコレクション
                    EditorUtility.UnloadUnusedAssetsImmediate();
                    System.GC.Collect();
                    
                    logMessages.Add($"{characterName}: 画像キャプチャー完了");
                }
                catch (System.Exception e)
                {
                    logMessages.Add($"エラー ({vrmObject.name}): {e.Message}");
                    Debug.LogError($"画像キャプチャーエラー ({vrmObject.name}): {e}");
                }
                finally
                {
                    // 例外が発生してもクローンを確実に削除
                    if (vrmClone != null)
                    {
                        GameObject.DestroyImmediate(vrmClone);
                    }
                    
                    // 最終的なクリーンアップ
                    GameObject[] finalPreviews = GameObject.FindObjectsOfType<GameObject>();
                    foreach (var obj in finalPreviews)
                    {
                        if (obj != null && (obj.name.EndsWith("_Preview") || obj.name.Contains("VRM Preview")))
                        {
                            GameObject.DestroyImmediate(obj);
                        }
                    }
                }
            }
            
            EditorUtility.ClearProgressBar();
            logMessages.Add($"合計 {currentImage} 個の表情画像をキャプチャーしました。");
        }
        
        private VRM10Expression GetVRM10Expression(Vrm10Instance vrm10Instance, string expressionName)
        {
            if (vrm10Instance == null || vrm10Instance.Vrm == null || vrm10Instance.Vrm.Expression == null)
                return null;
            
            var expressionData = vrm10Instance.Vrm.Expression;
            
            // プリセット表情をチェック
            switch (expressionName.ToLower())
            {
                case "happy": return expressionData.Happy;
                case "angry": return expressionData.Angry;
                case "sad": return expressionData.Sad;
                case "relaxed": return expressionData.Relaxed;
                case "surprised": return expressionData.Surprised;
                case "aa": return expressionData.Aa;
                case "ih": return expressionData.Ih;
                case "ou": return expressionData.Ou;
                case "ee": return expressionData.Ee;
                case "oh": return expressionData.Oh;
                case "blink": return expressionData.Blink;
                case "blinkleft": return expressionData.BlinkLeft;
                case "blinkright": return expressionData.BlinkRight;
                case "lookup": return expressionData.LookUp;
                case "lookdown": return expressionData.LookDown;
                case "lookleft": return expressionData.LookLeft;
                case "lookright": return expressionData.LookRight;
                case "neutral": return expressionData.Neutral;
            }
            
            // カスタム表情をチェック
            if (expressionData.CustomClips != null)
            {
                foreach (var customClip in expressionData.CustomClips)
                {
                    if (customClip != null && customClip.name == expressionName)
                    {
                        return customClip;
                    }
                }
            }
            
            return null;
        }
    }
}