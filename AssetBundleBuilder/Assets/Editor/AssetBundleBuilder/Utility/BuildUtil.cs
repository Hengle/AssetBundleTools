using UnityEngine;
using System.Collections;
using System;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AssetBundleBuilder;

public class BuildUtil : Editor
{

    public static string AssetBundleOutputPath = Application.streamingAssetsPath;

    public static Dictionary<string, AssetBundleElement> dataDict = new Dictionary<string, AssetBundleElement>();     // 深度依赖字典


    public static bool debug = true;

    public static string m_Extension = ".unity3d";

    /// <summary>  
    /// 清除之前设置过的AssetBundleName，避免产生不必要的资源也打包  
    /// 设置了AssetBundleName的，都会进行打包，不论在什么目录下 避免乱打包
    /// </summary> 
    public static void ClearAssetBundleName()
    {
        //Debug.Log("get the bundle length " + length);
        string[] oldAssetBundleName = AssetDatabase.GetAllAssetBundleNames();

        for (int i = 0; i < oldAssetBundleName.Length; ++i)
        {
            AssetDatabase.RemoveAssetBundleName(oldAssetBundleName[i], true);
        }
        int length = AssetDatabase.GetAllAssetBundleNames().Length;
        if (length == 0)
        {
            Debug.Log("Clear Asset Bundle Name Succeed ");
        }
        //Debug.Log(length);
        AssetDatabase.SaveAssets();
    }

    public static void SelectionFile(string source, bool dependencies)
    {
        DirectoryInfo folder = new DirectoryInfo(source);
        FileSystemInfo[] files = folder.GetFileSystemInfos();
        int length = files.Length;

        for (int i = 0; i < length; ++i)
        {
            //隐藏文件
            if ((files[i].Attributes & FileAttributes.Hidden) == FileAttributes.Hidden )//&& (files[i].Attributes & FileAttributes.System) != FileAttributes.System)
            {
                continue;
            }
            if (files[i] is DirectoryInfo)
            {
                SelectionFile(files[i].FullName, dependencies);
            }
            else
            {

                if (!files[i].Name.EndsWith(".meta"))
                {
   
                    SetAssetBundleName(files[i].FullName);
                }
            }

        }
    }
    public static void SetAssetBundleName(string source)
    {
        string _source = Replace(source);
        string _assetPath = "Assets" + _source.Substring(Application.dataPath.Length);
        string _assetPath2 = _source.Substring(Application.dataPath.Length + 1);

        //在代码中给资源设置AssetBundleName  
        AssetImporter assetImporter = AssetImporter.GetAtPath(_assetPath);
        string onlyID = AssetDatabase.AssetPathToGUID(_assetPath);
        string extension = Path.GetExtension(_assetPath2);
        string assetName = "";

        // 如果是调试状态，用路径名，容易看出来,方便调试
        if (debug)
        {
            assetName = _assetPath2.Substring(_assetPath2.IndexOf("/") + 1);
            assetName = assetName.Replace(Path.GetExtension(assetName), m_Extension);

        }
        else
        {
            assetName = onlyID;
            assetName = string.Format("{0}{1}{2}", assetName, extension, m_Extension);

        }

        Debug.Log (assetName +"=="+ _assetPath2);  
        assetImporter.assetBundleName = assetName;
    }


    public static void SetAssetbundleName(string path, string assetbundleName)
    {
        AssetImporter importer = AssetImporter.GetAtPath(path);
        importer.assetBundleName = assetbundleName;
        importer.assetBundleVariant = ABData.VARIANT_V1;
    }


    public static void SetAssetbundleName(AssetImporter importer, string assetbundleName)
    {
        importer.assetBundleName = assetbundleName;
        importer.assetBundleVariant = ABData.VARIANT_V1;
    }

    // 遍历检测深度依赖资源
    public static void CheckDependencies(string source)
    {
        DirectoryInfo folder = new DirectoryInfo(source);
        FileSystemInfo[] files = folder.GetFileSystemInfos();
        int length = files.Length;
       
        for (int i = 0; i < length; ++i)
        {
            //隐藏文件
            if ((files[i].Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)//&& (files[i].Attributes & FileAttributes.System) != FileAttributes.System)
            {
                continue;
            }
            //Debug.Log("======== " + files[i].FullName +"===="+ (files[i].Attributes == FileAttributes.Directory) +"==="+ (files[i].Attributes == FileAttributes.Normal));

            //文件夹
            if (files[i].Attributes == FileAttributes.Directory)
            {
                string key = RelativePaths(files[i].FullName);

                if (dataDict.ContainsKey(key))
                {
                    Debug.LogError("have same key "+ files[i].FullName);
                    continue;
                }
                AssetBundleElement abInfo = AssetBundleElement.Init(files[i]);
                abInfo = AssetBundleElement.GetParent(abInfo);

                dataDict.Add(key, abInfo);
                CheckDependencies(files[i].FullName);
            }
            else
            {

                if (!files[i].Name.EndsWith(".meta"))
                {
                    //获取相对路径
                    string key = RelativePaths(files[i].FullName);
                    if (dataDict.ContainsKey(key))
                    {
                        //Debug.LogError("have same key " + files[i].FullName);
                        Dependencies(files[i]);

                        continue;
                    }
                    AssetBundleElement abInfo = AssetBundleElement.Init(files[i]);
                    abInfo = AssetBundleElement.GetParent(abInfo);

                    dataDict.Add(key, abInfo);
                    Dependencies(files[i]);


                }
            }

        }
    }
    public static void Dependencies(FileSystemInfo file)
    {
        string assetPath = RelativePaths(file.FullName);


        string onlyID = AssetDatabase.AssetPathToGUID(assetPath);
        string[] dependenciesList = AssetDatabase.GetDependencies(assetPath, true);
        int j = 0;

        AssetBundleElement curABInfo = dataDict[assetPath];

        // 将资源的深度引用加入到字典里面
        for (int i = 0; i < dependenciesList.Length; ++i)
        {
            //忽略自身
            if(dependenciesList[i] == assetPath)
            {
                continue;
            }
            //忽略脚本
            if(dependenciesList[i].Contains(".cs"))
            {
                continue;
            }
            //忽略shader
            if (dependenciesList[i].Contains(".shader"))
            {
                continue;
            }
            //获取相对路径
            string key = dependenciesList[i];
            FileInfo info = new FileInfo(dependenciesList[i]);
            //Debug.LogError(info.FullName);
            AssetBundleElement abInfo = AssetBundleElement.Init(info);
            abInfo = AssetBundleElement.GetParent(abInfo);

            if (!curABInfo.referencesDict.ContainsKey(key))
            {
                //curABInfo.AddReferences(key, abInfo);
                curABInfo.referencesDict.Add(key, abInfo);
                curABInfo.AddReferences(key, abInfo);

            }
            j++;

            //Debug.LogError(dependenciesList[i] +"===="+ dependenciesList.Length +"==="+ _assetPath+"==="+ j +"===="+ dataDict.ContainsKey(dependenciesList[i]));
            if (dataDict.ContainsKey(key))
            {
                if (!dataDict[key].referencesParentDict.ContainsKey(assetPath))
                {
                    dataDict[key].referencesParentDict.Add(assetPath, abInfo);//AddReferencesParent(assetPath, curABInfo);
                    //dataDict[key].AddReferencesParent(assetPath,abInfo);
                }
             
            }
            else
            {

                if (dataDict.ContainsKey(key))
                {
                    Debug.LogError("have same key " + info.FullName);
                    continue;
                }
                if(!abInfo.referencesParentDict.ContainsKey(assetPath))
                {
                    abInfo.referencesParentDict.Add(assetPath, curABInfo);
                    //abInfo.AddReferencesParent(assetPath, curABInfo);
                    //abInfo.AddReferencesParent(assetPath, curABInfo);

                }
                dataDict.Add(key, abInfo);
              
            }
        }
    }

    public static void DependenciesLog()
    {
        foreach (var item in dataDict)
        {
            if(item.Value.fileType == FileType.Folder)
            {
                continue;
            }
            Debug.LogWarning(item.Key +"-------------------------------------------------------------------------");

            Debug.Log("=path=" + item.Key + "=referencesCount=" + item.Value.referencesCount + "==referencedCount==" + item.Value.referencedCount+"=id=" + AssetDatabase.AssetPathToGUID(item.Key)+"");
            foreach (var parent in item.Value.referencesDict)
            {
                Debug.Log("referencesDict=path1=" + item.Key + "=path=" + parent.Key + "=Extension=" + parent.Value + "=id=");

            }

            foreach (var parent in item.Value.referencesParentDict)
            {
                Debug.Log("referencesParentDict=path1=" + item.Key + "=path=" + parent.Key + "=Extension=" + parent.Value + "=id=");

            }
            Debug.LogWarning("---------------------------------------------------------------------------");

        }
    }

    public static string GetSelectFilePath()
    {
        string path = "";
        foreach (UnityEngine.Object o in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.DeepAssets))
        {
            path = AssetDatabase.GetAssetPath(o);
            break;
        }

        if (string.IsNullOrEmpty(path))
        {
            EditorUtility.DisplayDialog("提示", "选中的路径为空", "ok");
            return path;
        }
        return path;

    }

    public static FileType GetFileType(FileSystemInfo file)
    {
        if (file is DirectoryInfo)
        {
            return FileType.Folder;
        }
        else
        {
            string extension = Path.GetExtension(file.FullName).ToLower();

            switch (extension)
            {
                case ".prefab":
                    return FileType.Prefab;
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".tga":
                case ".bmp":
                case ".gif":
                case ".psd":
                case ".tiff":
                case ".dds":
                case ".iff":
                    return FileType.Textrue;
                case ".fbx":
                case ".3ds":
                case ".dxf":
                case ".obj":
                case ".dae":
                case ".max":
                case ".mb":
                    return FileType.Model;

                case ".anim":
                    return FileType.Animation;

                case ".mat":
                    return FileType.Material;

                case ".controller":
                    return FileType.Controller;

                case ".unity":
                    return FileType.Scene;
                case ".shader":
                    return FileType.Shader;

                case ".ogg":
                case ".aiff":
                case ".wav":
                case ".mp3":
                case ".mp4":
                case ".mov":
                case ".mpg":
                case ".avi":
                case ".asf":
                case ".mpeg":

                    return FileType.Sound;
                case ".asset":
                    return FileType.Asset;
                case ".fontsettings":
                case ".ttf":
                    return FileType.Font;
                case ".lua":
                    return FileType.Lua;
                case "":
                    return FileType.Folder;
                default:
                    return FileType.Other;


            }

        }
    }


    /// <summary>
    /// 获得指定文件类型的扩展名后缀
    /// </summary>
    /// <param name="fileType"></param>
    /// <returns></returns>
    public static string[] GetFileExtension(FileType fileType)
    {
        switch (fileType)
        {
            case FileType.Prefab:
                return new []{ ".prefab" };
            case FileType.Textrue:
                return new []{ ".png" , ".jpg" , ".jpeg" , ".tga" , ".bmp", ".gif" , ".psd" , ".tiff" , ".dds" , ".iff"};
            case FileType.Model:
                return new []{ ".fbx" , ".3ds" , ".dxf" , ".obj" , ".dae" , ".max" , ".mb"};

            case FileType.Animation:
                return new []{ ".anim" };

            case FileType.Material:
                return new []{ ".mat" };
            case FileType.Controller:
                return new []{ ".controller" };
            case FileType.Scene:
                return new []{ ".unity" };
            case FileType.Shader:
                return new []{ ".shader" };
            case FileType.Sound:
                return new []{ ".ogg" , ".aiff",".wav",".mp3",".mp4",".mov",".mpg",".avi",".asf",".mpeg"};
            case FileType.Asset:
                return new []{ ".asset" };
            case FileType.Font:
                return new []{ ".fontsettings" , ".ttf"};
            case FileType.Lua:
                return new []{ ".lua" };
            }
        return null;
    }

    /// <summary>
    /// 获得文件类型的默认打包顺序值
    /// </summary>
    /// <param name="fileType"></param>
    /// <returns></returns>
    public static int GetFileOrder(FileType fileType)
    {
        switch (fileType)
        {
            case FileType.Shader:
            case FileType.Lua:
                return 1000;
            case FileType.Textrue:
            case FileType.Sound:
                return 2000;
            case FileType.Material:
                return 3000;
            case FileType.Font:
            case FileType.Animation:
                return 4000;
            case FileType.Controller:
                return 5000;
            case FileType.Asset:
            case FileType.Model:
                return 6000;
            case FileType.Prefab:
                return 7000;
            case FileType.Scene:
                return 8000;
        }
        return 0;
    }

    /// <summary>
    /// 搜索指定目录下对应类型的文件
    /// </summary>
    /// <returns></returns>
    public static string[] SearchFiles(string folder, FileType fileType, SearchOption searchOption)
    {
        string[] extensions = GetFileExtension(fileType);
        HashSet<string> extensionSet = new HashSet<string>(extensions);
        string[] allFiles = Directory.GetFiles(folder, "*.*", searchOption)
            .Where(f => extensionSet.Contains(Path.GetExtension(f).ToLower()) ).ToArray();
        return allFiles;
    }

    public static string[] SearchFiles(AssetBuildRule rule)
    {
        List<AssetBuildRule> rules = rule.TreeToList();
        HashSet<string> extensionSet = new HashSet<string>();
        for (int i = 0; i < rules.Count; i++)
        {
            string[] extends = GetFileExtension(rules[i].FileFilterType);
            for (int j = 0; j < extends.Length; j++)
            {
                if(extensionSet.Contains(extends[j]))   continue;
                extensionSet.Add(extends[j]);
            }
        }
        string[] allFiles = Directory.GetFiles(rule.Path, "*.*", SearchOption.AllDirectories)
            .Where(f => extensionSet.Contains(Path.GetExtension(f).ToLower())).ToArray();

        return allFiles;
    }

    /// <summary>
    /// 搜索指定目录下的文件
    /// </summary>
    /// <returns></returns>
    public static List<string> SearchFiles(string folder, SearchOption searchOption)
    {
        string[] folderFiles = Directory.GetFiles(folder, "*.*", searchOption);
        List<string> files = new List<string>();
        for (int j = 0; j < folderFiles.Length; j++)
        {
            if (folderFiles[j].EndsWith(".meta")) continue;

            string relativePath = BuildUtil.RelativePaths(folderFiles[j]);
            files.Add(relativePath);
        }
        return files;
    }

    public static string RelativePaths(string FullName)
    {
        string _source = Replace(FullName);
        //相对路径
        string relativePath = "Assets" + _source.Substring(Application.dataPath.Length);
        return relativePath;
    }
    // 做一下字符串小写转化，斜杆转化
    public static string Replace(string s)
    {
        //s.ToLower();
        return s.Replace("\\", "/");
    }

    public static string GetPlatformFolder(UnityEditor.BuildTarget target)
    {
        switch (target)
        {
            case UnityEditor.BuildTarget.Android:
            case UnityEditor.BuildTarget.StandaloneWindows:
            case UnityEditor.BuildTarget.StandaloneWindows64:
                return "Android";
            case UnityEditor.BuildTarget.iOS:
            case UnityEditor.BuildTarget.StandaloneOSXIntel:
            case UnityEditor.BuildTarget.StandaloneOSXIntel64:
                return "IOS";
            default:
                return "Other";
        }
    }
    public static PlatformType GetPlatformType(UnityEditor.BuildTarget target)
    {
        switch (target)
        {
            case UnityEditor.BuildTarget.StandaloneWindows:
            case UnityEditor.BuildTarget.StandaloneWindows64:
                return PlatformType.Window;
            case UnityEditor.BuildTarget.StandaloneOSXIntel:
            case UnityEditor.BuildTarget.StandaloneOSXIntel64:
                return PlatformType.Mac;
            case UnityEditor.BuildTarget.Android:
   
                return PlatformType.Android;
            case UnityEditor.BuildTarget.iOS:

                return PlatformType.IOS;
            default:
                return PlatformType.None;
        }
    }

    public static BuildTarget GetBuildTarget(PlatformType type)
    {
        BuildTarget buildTarget = BuildTarget.StandaloneWindows;

        switch (type)
        {
            case PlatformType.Window:
                if (EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.StandaloneWindows64
                    || EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.StandaloneWindows
                    || EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android)
                {
                    buildTarget = BuildTarget.StandaloneWindows;
                }
                break;
            case PlatformType.Mac:
                if (EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.StandaloneOSXIntel64
                    || EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.StandaloneOSXIntel
                    || EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.iOS)
                {
                    buildTarget = BuildTarget.StandaloneOSXIntel64;

                }
                break;
            case PlatformType.Android:
                buildTarget = BuildTarget.Android;
                break;
            case PlatformType.IOS:
                buildTarget = BuildTarget.iOS;
                break;
        }
        return buildTarget;
    }

    public static string GetFileHash(string filePath)
    {
        try
        {
            FileStream fs = new FileStream(filePath, FileMode.Open);
            int len = (int)fs.Length;
            byte[] data = new byte[len];
            fs.Read(data, 0, len);
            fs.Close();
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] result = md5.ComputeHash(data);
            string fileMD5 = "";
            foreach (byte b in result)
            {
                fileMD5 += Convert.ToString(b, 16);
            }
            return fileMD5;
        }
        catch (FileNotFoundException e)
        {
            //Console.WriteLine(e.Message);
            Debug.LogError(e.Message);
            return "";
        }
    }
    public static Dictionary<string, string> md5Dict = new Dictionary<string, string>();
    public static Dictionary<string, long> SizeDict = new Dictionary<string, long>();

    public static void GeneratorMD5(string source)
    {
        DirectoryInfo folder = new DirectoryInfo(source);
        FileSystemInfo[] files = folder.GetFileSystemInfos();
        int length = files.Length;


        for (int i = 0; i < length; ++i)
        {
            if (files[i] is DirectoryInfo)
            {
                GeneratorMD5(files[i].FullName);
            }
            else
            {
				if (files[i].Name.EndsWith(".meta"))
				{
					continue;
				}
                string md5Str = BuildUtil.GetFileHash(files[i].FullName);
                if(md5Dict.ContainsKey(md5Str))
                {
                    Debug.LogError("have same MD5 key :" + md5Str);
                    continue;
                }
                else
				{
                    if(!files[i].FullName.Contains("updata.txt"))//不把update.txt的md5写入，因为update.txt只是用来自己看的
                    {
                        string platform = GetPlatformFolder(EditorUserBuildSettings.activeBuildTarget);
                        int index = files[i].FullName.IndexOf(platform);
                        if (index >= 0)
                        {
                            index += platform.Length + 1;
                            string name = files[i].FullName.Substring(index);
                            name = Replace(name);
                            long size =GetFileSize(files[i].FullName);
                            //Debug.LogError(files[i].FullName+"====size=" + size + "===="+ CountSize(size));
                            SizeDict.Add(name, size);
                           md5Dict.Add(name, md5Str);
                        }
                    }
                   // md5Dict.Add(md5Str, files[i].FullName);
                }

            }

        }
    }

    public static long GetFileSize(string sFullName)
    {
        long lSize = 0;
        if (File.Exists(sFullName))
            lSize = new FileInfo(sFullName).Length;
        return lSize;
    }

    public static string CountSize(long Size)
    {
        string m_strSize = "";
        long FactSize = 0;
        FactSize = Size;
        if (FactSize < 1024.00)
            m_strSize = FactSize.ToString("F2") + " Byte";
        else if (FactSize >= 1024.00 && FactSize < 1048576)
            m_strSize = (FactSize / 1024.00).ToString("F2") + " K";
        else if (FactSize >= 1048576 && FactSize < 1073741824)
            m_strSize = (FactSize / 1024.00 / 1024.00).ToString("F2") + " M";
        else if (FactSize >= 1073741824)
            m_strSize = (FactSize / 1024.00 / 1024.00 / 1024.00).ToString("F2") + " G";
        return m_strSize;
    }

    public static void WriteFile()
    {
        string path = Path.Combine(AssetBundleOutputPath ,BuildUtil.GetPlatformFolder(EditorUserBuildSettings.activeBuildTarget));
        path  = path + "/assetMD5.txt";

        string UpdaPath = Path.Combine(AssetBundleOutputPath, BuildUtil.GetPlatformFolder(EditorUserBuildSettings.activeBuildTarget));
        UpdaPath = UpdaPath + "/updata.txt";

        Dictionary<string, string> md5OldDict = new Dictionary<string, string>();
        Dictionary<string, string> md5ChangDict = new Dictionary<string, string>();

        if (File.Exists(path))//如果存在就删除
        {
            StreamReader sr = new StreamReader(path, Encoding.Default);
            String line;
            while ((line = sr.ReadLine()) != null)
            {
                String[] md5 = line.Split(':');
                if(!md5OldDict.ContainsKey(md5[1]))
                {
                    if(md5.Length>=3)
                    {
                        md5OldDict.Add(md5[1], md5[3]);
                    }
                }
                else
                {
                    Debug.LogError("have same key :" + md5[1]);
                }
            }
            sr.Dispose();
            sr.Close();
            //资源更新完才能删
            File.Delete(path);
        }
        FileStream fs = new FileStream(path, FileMode.Create);

        StreamWriter sw = new StreamWriter(fs);
		sw.Write("Version:" + PlayerSettings.bundleVersion+"\r\n");
        //sw.Write(System.Environment.NewLine);
		sw.Write("VersionCode:" + PlayerSettings.Android.bundleVersionCode+"\r\n");
        //sw.Write(System.Environment.NewLine);
        long size = 0;
        //开始写入
        foreach (KeyValuePair<string ,string> kvp in md5Dict)
        {
            if(!md5OldDict.ContainsKey(kvp.Key))
            {
                if (md5ChangDict.ContainsKey(kvp.Key))
                {
                    Debug.LogError("have same key :" + kvp.Key);

                }
                else
                {
                    md5ChangDict.Add(kvp.Key, kvp.Value);

                }
            }
            if(SizeDict.ContainsKey(kvp.Key))
            {
                size = SizeDict[kvp.Key];
            }
			sw.Write("Name:" + kvp.Key + ":" + "MD5:" + kvp.Value+":Size:"+size+"\r\n");
          //  sw.Write("MD5:" + kvp.Key + ":  Name:" + kvp.Value);
            //sw.Write(System.Environment.NewLine);
        }


        //清空缓冲区
        sw.Flush();
        //关闭流
        sw.Close();
        fs.Close();

        if (File.Exists(UpdaPath))//如果存在就删除
        {
            File.Delete(UpdaPath);
        }

        fs = new FileStream(UpdaPath, FileMode.Create);

        sw = new StreamWriter(fs);

        foreach (KeyValuePair<string, string> kvp in md5ChangDict)
        {
			sw.Write("Name:" + kvp.Key + ":" + "MD5:" + kvp.Value+"\r\n");
           // sw.Write("MD5:" + kvp.Key + ":  Name:" + kvp.Value);
            //sw.Write(System.Environment.NewLine);
        }
        //清空缓冲区
        sw.Flush();
        //关闭流
        sw.Close();
        fs.Close();
    }

    public static void Reset()
    {
        dataDict.Clear();
    }

    public enum PlatformType
    {
        None,
        Android,
        IOS,
        Window,
        Mac,

    }
    public enum BuildAssetType
    {
        All,                //所以资源
        All_Res,            //所以res下面的资源
        Scene,              //所以场景
        Lua,                //所有lua代码
    }
}
