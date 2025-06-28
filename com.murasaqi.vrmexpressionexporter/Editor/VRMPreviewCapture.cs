using System.Collections;
using System.IO;
using UnityEngine;
using UnityEditor;
using UniVRM10;
using VRMExpressionExporter;

namespace VRMExpressionExporter.Editor
{
    /// <summary>
    /// VRM表情のプレビュー画像をキャプチャするユーティリティクラス
    /// </summary>
    public static class VRMPreviewCapture
    {
        private static Camera previewCamera;
        private static RenderTexture renderTexture;
        private const int PREVIEW_LAYER = 31; // プレビュー専用レイヤー（通常未使用の最後のレイヤー）
        
        /// <summary>
        /// 表情を適用してプレビュー画像をキャプチャする
        /// </summary>
        public static string CaptureExpression(GameObject vrmInstance, VRM10Expression expression, string characterName, string expressionName, string outputPath, int width = 512, int height = 512)
        {
            if (vrmInstance == null || expression == null) return "";
            
            // 出力ディレクトリを作成
            string characterDir = Path.Combine(outputPath, characterName);
            if (!Directory.Exists(characterDir))
            {
                Directory.CreateDirectory(characterDir);
            }
            
            // 既存のプレビューオブジェクトをクリーンアップ
            CleanupAllPreviewObjects();
            
            // VRMインスタンスを専用レイヤーに設定
            int originalLayer = vrmInstance.layer;
            SetLayerRecursively(vrmInstance, PREVIEW_LAYER);
            
            // セットアップ
            SetupPreviewEnvironment(vrmInstance, width, height);
            
            try
            {
                // 表情を適用
                ApplyExpression(vrmInstance, expression);
                
                // キャプチャ
                string fileName = $"{expressionName}.png";
                string filePath = Path.Combine(characterDir, fileName);
                CaptureToFile(filePath);
                
                // 表情をリセット
                ResetExpression(vrmInstance);
                
                return filePath;
            }
            finally
            {
                // レイヤーを元に戻す
                SetLayerRecursively(vrmInstance, originalLayer);
                
                // クリーンアップ
                CleanupPreviewEnvironment();
            }
        }
        
        private static void SetupPreviewEnvironment(GameObject vrmInstance, int width, int height)
        {
            // カメラ用のGameObjectを作成
            GameObject cameraObj = new GameObject("VRM Preview Camera");
            cameraObj.hideFlags = HideFlags.HideAndDontSave; // シーンに保存しない
            previewCamera = cameraObj.AddComponent<Camera>();
            
            // カメラの設定
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0, 0, 0, 0); // 透過背景
            previewCamera.orthographic = false;
            previewCamera.fieldOfView = 35f; // 適度な視野角で自然な見た目
            previewCamera.cullingMask = 1 << PREVIEW_LAYER; // プレビューレイヤーのみをレンダリング
            previewCamera.enabled = false; // 手動でレンダリングするため無効化
            
            // RenderTextureの作成（毎回新規作成）
            if (renderTexture != null)
            {
                Object.DestroyImmediate(renderTexture);
            }
            renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            renderTexture.antiAliasing = 4;
            previewCamera.targetTexture = renderTexture;
            
            // VRMモデルを適切な位置に配置
            PositionModel(vrmInstance);
            
            // カメラを適切な位置に配置
            PositionCamera(vrmInstance);
            
            // ライティングの設定
            SetupLighting();
        }
        
        private static void PositionModel(GameObject vrmInstance)
        {
            // モデルを原点に配置（位置調整はしない）
            vrmInstance.transform.position = Vector3.zero;
            vrmInstance.transform.rotation = Quaternion.identity;
        }
        
        private static void PositionCamera(GameObject vrmInstance)
        {
            // Faceメッシュのバウンディングボックスを取得
            Bounds faceBounds = GetFaceMeshBounds(vrmInstance);
            
            // デバッグ情報
            Debug.Log($"Face Bounds Center: {faceBounds.center}, Size: {faceBounds.size}");
            
            // VRMモデルの標準的な頭部位置を推定
            // 通常、VRMモデルの頭部は地面から1.4〜1.6m程度の高さにある
            float estimatedHeadHeight = 1.5f;
            
            // 顔の中心位置を計算
            Vector3 faceCenter = faceBounds.center;
            
            // フォールバック: もしfaceの位置が異常に低い場合は推定値を使用
            if (faceCenter.y < 0.5f)
            {
                faceCenter.y = estimatedHeadHeight;
                Debug.LogWarning($"Face center too low ({faceBounds.center.y}), using estimated height: {estimatedHeadHeight}");
            }
            
            // カメラ距離の計算
            // 顔のサイズに基づいて適切な距離を設定
            float faceHeight = faceBounds.size.y;
            if (faceHeight < 0.1f) // 異常に小さい場合
            {
                faceHeight = 0.25f; // 標準的な顔の高さ
            }
            
            // カメラを顔から適切な距離に配置（顔の高さの約2.5倍）
            float distance = faceHeight * 2.5f;
            
            // カメラを顔の正面に設定
            Vector3 cameraPos = faceCenter + new Vector3(0, 0, distance);
            previewCamera.transform.position = cameraPos;
            
            // カメラを顔の中心に向ける
            previewCamera.transform.LookAt(faceCenter);
            
            Debug.Log($"Camera Position: {cameraPos}, Looking at: {faceCenter}, Distance: {distance}");
        }
        
        private static Bounds GetModelBounds(GameObject model)
        {
            Bounds bounds = new Bounds(model.transform.position, Vector3.zero);
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }
            
            return bounds;
        }
        
        private static Bounds GetFaceMeshBounds(GameObject model)
        {
            Bounds? faceBounds = null;
            SkinnedMeshRenderer[] renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>();
            int faceCount = 0;
            
            // まず、BlendShapeを持つ"face"メッシュを探す
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                if (renderer.sharedMesh != null && renderer.sharedMesh.blendShapeCount > 0)
                {
                    string meshPath = GetRelativePath(model.transform, renderer.transform);
                    
                    // "face"を含む名前のメッシュを優先
                    if (renderer.name.ToLower().Contains("face") || 
                        renderer.transform.name.ToLower().Contains("face") ||
                        meshPath.ToLower().Contains("face"))
                    {
                        Debug.Log($"Found face mesh: {meshPath}, BlendShapes: {renderer.sharedMesh.blendShapeCount}");
                        faceCount++;
                        
                        if (!faceBounds.HasValue)
                        {
                            faceBounds = renderer.bounds;
                        }
                        else
                        {
                            faceBounds = EncapsulateBounds(faceBounds.Value, renderer.bounds);
                        }
                    }
                }
            }
            
            Debug.Log($"Total face meshes found: {faceCount}");
            
            // "face"メッシュが見つからない場合、BlendShapeを持つ任意のメッシュを使用
            if (!faceBounds.HasValue)
            {
                Debug.LogWarning("No 'face' mesh found, using any mesh with BlendShapes");
                foreach (SkinnedMeshRenderer renderer in renderers)
                {
                    if (renderer.sharedMesh != null && renderer.sharedMesh.blendShapeCount > 0)
                    {
                        string meshPath = GetRelativePath(model.transform, renderer.transform);
                        Debug.Log($"Using mesh: {meshPath}");
                        
                        if (!faceBounds.HasValue)
                        {
                            faceBounds = renderer.bounds;
                        }
                        else
                        {
                            faceBounds = EncapsulateBounds(faceBounds.Value, renderer.bounds);
                        }
                    }
                }
            }
            
            // それでも見つからない場合は全体のバウンディングボックスを使用
            if (!faceBounds.HasValue)
            {
                Debug.LogWarning("No mesh with BlendShapes found, using full model bounds");
                return GetModelBounds(model);
            }
            
            return faceBounds.Value;
        }
        
        private static string GetRelativePath(Transform root, Transform target)
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
        
        private static Bounds EncapsulateBounds(Bounds a, Bounds b)
        {
            a.Encapsulate(b);
            return a;
        }
        
        private static void SetupLighting()
        {
            // メインライト
            GameObject mainLight = new GameObject("VRM Preview Main Light");
            mainLight.hideFlags = HideFlags.HideAndDontSave;
            Light light = mainLight.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.0f;
            light.color = Color.white;
            mainLight.transform.rotation = Quaternion.Euler(30f, -30f, 0);
            
            // フィルライト
            GameObject fillLight = new GameObject("VRM Preview Fill Light");
            fillLight.hideFlags = HideFlags.HideAndDontSave;
            Light fill = fillLight.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.5f;
            fill.color = new Color(0.8f, 0.85f, 1.0f);
            fillLight.transform.rotation = Quaternion.Euler(30f, 150f, 0);
        }
        
        private static void ApplyExpression(GameObject vrmInstance, VRM10Expression expression)
        {
            // モーフターゲットの適用
            if (expression.MorphTargetBindings != null)
            {
                foreach (var binding in expression.MorphTargetBindings)
                {
                    Transform target = vrmInstance.transform.Find(binding.RelativePath);
                    if (target != null)
                    {
                        SkinnedMeshRenderer renderer = target.GetComponent<SkinnedMeshRenderer>();
                        if (renderer != null)
                        {
                            renderer.SetBlendShapeWeight(binding.Index, binding.Weight * 100f);
                        }
                    }
                }
            }
            
            // マテリアルカラーの適用
            if (expression.MaterialColorBindings != null)
            {
                foreach (var binding in expression.MaterialColorBindings)
                {
                    Transform target = vrmInstance.transform.Find(binding.MaterialName);
                    if (target != null)
                    {
                        Renderer renderer = target.GetComponent<Renderer>();
                        if (renderer != null && renderer.materials.Length > 0)
                        {
                            Material mat = renderer.materials[0];
                            string propertyName = GetMaterialPropertyName(binding.BindType);
                            if (mat.HasProperty(propertyName))
                            {
                                mat.SetColor(propertyName, binding.TargetValue);
                            }
                        }
                    }
                }
            }
        }
        
        private static string GetMaterialPropertyName(UniGLTF.Extensions.VRMC_vrm.MaterialColorType bindType)
        {
            switch (bindType)
            {
                case UniGLTF.Extensions.VRMC_vrm.MaterialColorType.color:
                    return "_Color";
                case UniGLTF.Extensions.VRMC_vrm.MaterialColorType.emissionColor:
                    return "_EmissionColor";
                case UniGLTF.Extensions.VRMC_vrm.MaterialColorType.shadeColor:
                    return "_ShadeColor";
                case UniGLTF.Extensions.VRMC_vrm.MaterialColorType.rimColor:
                    return "_RimColor";
                case UniGLTF.Extensions.VRMC_vrm.MaterialColorType.outlineColor:
                    return "_OutlineColor";
                default:
                    return "_Color";
            }
        }
        
        private static void ResetExpression(GameObject vrmInstance)
        {
            // 全てのブレンドシェイプをリセット
            SkinnedMeshRenderer[] renderers = vrmInstance.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMesh != null)
                {
                    for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
                    {
                        renderer.SetBlendShapeWeight(i, 0);
                    }
                }
            }
        }
        
        private static void CaptureToFile(string filePath)
        {
            // レンダーテクスチャをアクティブに
            RenderTexture currentRT = RenderTexture.active;
            RenderTexture.active = renderTexture;
            
            // レンダーテクスチャをクリア
            GL.Clear(true, true, Color.clear);
            
            // カメラでレンダリング
            previewCamera.Render();
            
            // Texture2Dに読み込み
            Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();
            
            // PNGとして保存
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(filePath, bytes);
            
            // クリーンアップ
            RenderTexture.active = currentRT;
            Object.DestroyImmediate(texture);
            
            // Assetデータベースを更新
            AssetDatabase.Refresh();
        }
        
        private static void CleanupPreviewEnvironment()
        {
            // カメラを削除
            if (previewCamera != null)
            {
                Object.DestroyImmediate(previewCamera.gameObject);
                previewCamera = null;
            }
            
            // プレビュー用オブジェクトを名前で検索して削除
            string[] previewObjectNames = new string[] 
            {
                "VRM Preview Camera",
                "VRM Preview Main Light", 
                "VRM Preview Fill Light"
            };
            
            foreach (string objName in previewObjectNames)
            {
                GameObject obj = GameObject.Find(objName);
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }
            
            // RenderTextureを削除
            if (renderTexture != null)
            {
                Object.DestroyImmediate(renderTexture);
                renderTexture = null;
            }
        }
        
        private static void CleanupAllPreviewObjects()
        {
            // すべてのプレビュー関連オブジェクトを確実に削除
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj.name.Contains("VRM Preview") || obj.name.Contains("_Preview"))
                {
                    Object.DestroyImmediate(obj);
                }
            }
            
            // 静的変数をクリア
            previewCamera = null;
            if (renderTexture != null)
            {
                Object.DestroyImmediate(renderTexture);
                renderTexture = null;
            }
        }
        
        private static void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null) return;
            
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }
}