using System.Collections.Generic;

namespace VRMExpressionExporter
{
    /// <summary>
    /// VRMファイルから抽出した表情データを格納するクラス
    /// </summary>
    [System.Serializable]
    public class VRMExpressionData
    {
        /// <summary>
        /// VRMファイル名
        /// </summary>
        public string FileName;
        
        /// <summary>
        /// キャラクター名
        /// </summary>
        public string CharacterName;
        
        /// <summary>
        /// VRMファイルのパス
        /// </summary>
        public string FilePath;
        
        /// <summary>
        /// 表情情報のリスト
        /// </summary>
        public List<ExpressionInfo> Expressions;
    }

    /// <summary>
    /// 個別の表情情報
    /// </summary>
    [System.Serializable]
    public class ExpressionInfo
    {
        /// <summary>
        /// 表情名
        /// </summary>
        public string Name;
        
        /// <summary>
        /// 表情タイプ (Preset または Custom)
        /// </summary>
        public string Type;
        
        /// <summary>
        /// ブレンドシェイプ情報のリスト
        /// </summary>
        public List<BlendShapeInfo> BlendShapes;
        
        /// <summary>
        /// プレビュー画像のパス
        /// </summary>
        public string PreviewImagePath;
        
        /// <summary>
        /// マテリアルカラー変更情報
        /// </summary>
        public List<MaterialColorInfo> MaterialColors;
        
        /// <summary>
        /// マテリアルUV変更情報
        /// </summary>
        public List<MaterialUVInfo> MaterialUVs;
    }

    /// <summary>
    /// ブレンドシェイプ情報
    /// </summary>
    [System.Serializable]
    public class BlendShapeInfo
    {
        /// <summary>
        /// ブレンドシェイプ名
        /// </summary>
        public string Name;
        
        /// <summary>
        /// メッシュへの相対パス
        /// </summary>
        public string RelativePath;
        
        /// <summary>
        /// ブレンドシェイプのインデックス
        /// </summary>
        public int Index;
        
        /// <summary>
        /// ウェイト値 (0-100)
        /// </summary>
        public float Weight;
    }

    /// <summary>
    /// マテリアルカラー変更情報
    /// </summary>
    [System.Serializable]
    public class MaterialColorInfo
    {
        /// <summary>
        /// マテリアルへの相対パス
        /// </summary>
        public string RelativePath;
        
        /// <summary>
        /// マテリアルインデックス
        /// </summary>
        public int MaterialIndex;
        
        /// <summary>
        /// プロパティ名
        /// </summary>
        public string PropertyName;
        
        /// <summary>
        /// 色値
        /// </summary>
        public UnityEngine.Color Color;
    }

    /// <summary>
    /// マテリアルUV変更情報
    /// </summary>
    [System.Serializable]
    public class MaterialUVInfo
    {
        /// <summary>
        /// マテリアルへの相対パス
        /// </summary>
        public string RelativePath;
        
        /// <summary>
        /// マテリアルインデックス
        /// </summary>
        public int MaterialIndex;
        
        /// <summary>
        /// UV変換情報 (offset.x, offset.y, scale.x, scale.y)
        /// </summary>
        public UnityEngine.Vector4 UVTransform;
    }
}