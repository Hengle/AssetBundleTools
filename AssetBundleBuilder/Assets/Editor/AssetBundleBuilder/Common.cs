using System.ComponentModel;

namespace AssetBundleBuilder
{
    public enum ELoadType
    {
         None , PreLoad
    }

    public enum AssetTreeHeader
    {
        Icon , AssetName , NameAB, Order , File , Build //, PackAsset
    }

    [Description("资源文件类型")]
    public enum FileType
    {
        [Description("文件夹")]
        Folder = 0,
        [Description("图片")]
        Textrue,
        [Description("材质")]
        Material,
        [Description("音效")]
        Sound,
        [Description("动画文件")]
        Animation,
        [Description("动画控制")]
        Controller,
        [Description("模型")]
        Model,
        [Description("Shader（着色器）")]
        Shader,
        [Description("预设")]
        Prefab,
        [Description("二进制资源")]
        Asset,
        [Description("场景")]
        Scene,
        [Description("字体")]
        Font,
        [Description("Lua脚本")]
        Lua,
        [Description("其他资源")]
        Other = 100,
    }

    [System.Flags]
    public enum BuildType
    {
        TogetherFiles = 1 << 0,    // 目录下所有文件一起打包
        TogetherFolders = 1 << 1,    // 目录下所有目录一起打包
    }

    /// <summary>
    /// 资源类型
    /// </summary>
    public enum PackageAssetType
    {
        InPackage = 0,      // 整包资源(包含在包体中)
        OutPackage,         // 整包资源(不包含在包体中)
        PacthResources,     // 补丁资源（额外添加资源）
        NoNeedToDownload,   // 不需要下载的资源()
    }

    /// <summary>
    /// 一键打包
    /// </summary>
    public enum AutoBuildType
    {
        ALLBuild ,      //一键打包
        AllAssetBuild ,  //一键打包更新资源
        AllPackageBuild  //一键打包强更包
    }

}