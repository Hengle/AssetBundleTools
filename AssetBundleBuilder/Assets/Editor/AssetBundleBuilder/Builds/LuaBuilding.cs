using System.Collections;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Riverlake.Crypto;
using Debug = UnityEngine.Debug;

namespace AssetBundleBuilder
{
    public class LuaBuilding : ABuilding{
    

        public static string[] Filters = new[] { "print(", "Debug.Log(", "UnityEngine.Debug.Log(" ,
                                                 "Debugger.Log(", "log(" };

        public static string[] luaPaths = new []{
                "Assets/LuaFramework/lua/",
                "Assets/LuaFramework/Tolua/Lua/"
        };

        public LuaBuilding() : base(30)
        {
        }

        public override IEnumerator OnBuilding()
        {
            bool rebuildAll = false;

            Dictionary<string, Dictionary<string, string>> luaMd5Dict = new Dictionary<string, Dictionary<string, string>>();

#if UNITY_IOS
            BuildLuaBundles();
            CopyHandleBundle();
            BuildFileIndex();
#else
            if (!rebuildAll)
            {
                ReadLuaMd5FromFile(luaMd5Dict);
            }
            BuildLuaBundles();

            yield return null;

            CopyHandleBundle();

            if (!rebuildAll)
            {
                CompareLuaMd5(luaMd5Dict);
                RecordLuaMd5();
            }
            AssetDatabase.Refresh();

            BuildFileIndex();
#endif
            AssetDatabase.Refresh();

            yield return null;
        }

        /// <summary>
        /// 拷贝协议文件
        /// </summary>
        private void CopyHandleBundle() {
            string bundleLuaPath = string.Concat(BuilderPreference.BUILD_PATH , "/lua/");
        
            for (int i = 0; i < luaPaths.Length; i++) {

                if(!Directory.Exists(luaPaths[i]))    continue;

                string luaDataPath = luaPaths[i].ToLower();
                List<string> includeFiles = BuildUtil.SearchFiles(luaDataPath , SearchOption.AllDirectories);

                //拷贝protocol
                foreach (string f in includeFiles) {

                    if (f.EndsWith(".lua")) continue;

                    var cmpStr = f.ToLower();

                    if (cmpStr.Contains("protocol/"))
                    {
                        string newPath = f.Replace(luaDataPath, bundleLuaPath);
                        BuildUtil.SwapPathDirectory(newPath);
                        
                        File.Copy(f, newPath, true);
                    }
                }
            }
        }

        private void ClearAllLuaFiles() {
            string luaRootPath = BuilderPreference.BUILD_PATH + "/lua";
        
            if (Directory.Exists(luaRootPath)) {
                Directory.Delete(luaRootPath, true);
            }
        }


        private void CopyLuaBytesFiles(string sourceDir, string destDir) {

            if (!Directory.Exists(sourceDir)) return;
        
            string[] files = Directory.GetFiles(sourceDir, "*.lua", SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++) {

                string dest = files[i].Replace(sourceDir , destDir) + ".bytes";

                BuildUtil.SwapPathDirectory(dest);

                if (AppConst.LuaByteMode) {
                    EncodeLuaFile(files[i], dest);
                } else {
                    var srcFile = files[i];
                    srcFile = RemoveLogInLua(srcFile, dest);
                    File.Copy(srcFile, dest, true);
                }
            }
        }

        private void BuildLuaBundles() {
            ClearAllLuaFiles();

            string output = BuilderPreference.BUILD_PATH + "/lua";
            BuildUtil.SwapDirectory(output);

            BuildAssetBundleOptions options = BuildAssetBundleOptions.DisableWriteTypeTree |
                                              BuildAssetBundleOptions.DeterministicAssetBundle |
                                              BuildAssetBundleOptions.UncompressedAssetBundle;

            string streamDir = "Assets/lua/";  //临时打包目录
            foreach (string luaPath in luaPaths)
                CopyLuaBytesFiles(luaPath, streamDir);

            AssetDatabase.Refresh();
        
            string[] dirs = Directory.GetDirectories(streamDir, "*", SearchOption.AllDirectories);

            List<AssetBundleBuild> abbs = new List<AssetBundleBuild>();
            for (int i = 0; i < dirs.Length; i++)
            {
                string relativePath = dirs[i].Replace("\\", "/");
                AssetBundleBuild abb = GenLuaBundleBuild(relativePath);
                abbs.Add(abb);
            }

            AssetBundleBuild rootBundle = GenLuaBundleBuild(streamDir);
            abbs.Add(rootBundle);

            AssetBundleBuild[] buildArr = abbs.Where(b => !string.IsNullOrEmpty(b.assetBundleName)).ToArray();

            if (BuildPipeline.BuildAssetBundles(output, buildArr, options, EditorUserBuildSettings.activeBuildTarget))
            {
                string[] bundles = Directory.GetDirectories(output, "*.unity3d", SearchOption.AllDirectories);

                for (int i = 0; i < bundles.Length; i++)
                {
                    string bundlePath = bundles[i];
                    byte[] bytes = File.ReadAllBytes(bundlePath);
                    //for (int i = 0; i < bytes.Length; ++i)
                    //{
                    //    bytes[i] = (byte)(bytes[i] ^ 0xffff);
                    //}
                    //var buffer = Crypto.Encode(bytes);
                    File.WriteAllBytes(bundlePath, Crypto.SimpleCrypto(bytes));                    
                }
            }

            Directory.Delete(streamDir, true);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 按目录格式化Lua文件夹的Bundle名称
        /// </summary>
        /// <param name="relativeDir"></param>
        /// <returns></returns>
        private string formatLuaBundleName(string relativeDir)
        {
            string newName = relativeDir.TrimEnd('/').Replace('/', '_').ToLower();
            return string.Concat("lua_", newName ,".", BuilderPreference.VARIANT_UNTIY3D);
        }
        /// <summary>
        /// 生成Lua打包配置
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private AssetBundleBuild GenLuaBundleBuild(string path) {
            string[] files = Directory.GetFiles(path, "*.lua.bytes");

            if (files.Length <= 0) return default(AssetBundleBuild);

            string relativePath = path.Replace("Assets/lua/", "");
            string bundleName = string.IsNullOrEmpty(relativePath) ? "lua.unity3d" : formatLuaBundleName(relativePath);

            List<string> list = new List<string>();
            for (int i = 0; i < files.Length; i++) {
                list.Add(BuildUtil.RelativePaths(files[i]));
            }
            
            AssetBundleBuild luaBuild = new AssetBundleBuild();
            luaBuild.assetBundleName = bundleName;
            luaBuild.assetNames = list.ToArray();

            return luaBuild;

    //        AssetDatabase.Refresh();
        }
    
        private void BuildFileIndex() {
            string resPath = BuilderPreference.BUILD_PATH +  "/";
            //----------------------创建文件列表-----------------------
            string newFilePath = resPath + "bundles/files.txt";
            if (File.Exists(newFilePath)) File.Delete(newFilePath);
        
            string tempSizeFile = resPath + "tempsizefile.txt";
            Dictionary<string, string> assetTypeDict = new Dictionary<string, string>();
            if (File.Exists(tempSizeFile))
            {
                var sizeFileContent = File.ReadAllText(tempSizeFile);
                var temps = sizeFileContent.Split('\n');
                for (int i = 0; i < temps.Length; ++i)
                {
                    if (!string.IsNullOrEmpty(temps[i]))
                    {
                        var temp = temps[i].Split('|');
                        if (temp.Length != 2 && temp.Length != 3) throw new System.IndexOutOfRangeException();

                        var assetType = temp[1];
                        if (temp.Length == 3) assetType += "|" + temp[2];

                        assetTypeDict.Add(temp[0], assetType);
                        //UpdateProgress(i, temps.Length, temps[i]);
                    }
                }
    //            EditorUtility.ClearProgressBar();
            }

            List<string> includeFiles = BuildUtil.SearchFiles(resPath, SearchOption.AllDirectories);
            HashSet<string> excludeSuffxs= new HashSet<string>(){".DS_Store" , ".manifest"};  //排除文件

            BuildUtil.SwapPathDirectory(newFilePath);

            using (FileStream fs = new FileStream(newFilePath, FileMode.CreateNew))
            {
                StreamWriter sw = new StreamWriter(fs);
                for (int i = 0; i < includeFiles.Count; i++) {
                    string file = includeFiles[i];
                    string ext = Path.GetExtension(file);

                    if (excludeSuffxs.Contains(ext) || file.EndsWith("apk_version.txt") || file.Contains("tempsizefile.txt") || file.Contains("luamd5.txt")) continue;

                    string md5 = MD5.ComputeHashString(file);
                    int size = (int)new FileInfo(file).Length;
                    string value = file.Replace(resPath, string.Empty).ToLower();
                    if (assetTypeDict.ContainsKey(value))
                        sw.WriteLine("{0}|{1}|{2}|{3}" ,value , md5 , size , assetTypeDict[value]);
                    else
                        sw.WriteLine("{0}|{1}|{2}", value, md5, size);
        //            UpdateProgress(i, includeFiles.Count, file);

                }
                sw.Close();
            }
    //        EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// 数据目录
        /// </summary>
        private string AppDataPath {
            get { return Application.dataPath.ToLower(); }
        }


        public void EncodeLuaFile(string srcFile, string outFile) {

            bool isWin = true; 
            string luaexe = string.Empty;
            string args = string.Empty;
            string exedir = string.Empty;
            string currDir = Directory.GetCurrentDirectory();
            srcFile = RemoveLogInLua(srcFile, outFile);

            if (Application.platform == RuntimePlatform.WindowsEditor) {
                isWin = true;
                luaexe = "luajit.exe";
                args = "-b -g " + srcFile + " " + outFile;
                exedir = AppDataPath.Replace("assets", "") + "LuaEncoder/luajit/";
            } else if (Application.platform == RuntimePlatform.OSXEditor) {
                isWin = false;
                luaexe = "./luajit";
                args = "-b -g " + srcFile + " " + outFile;
                exedir = AppDataPath.Replace("assets", "") + "LuaEncoder/luajit_mac/";
            }
            Directory.SetCurrentDirectory(exedir);

            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = luaexe;
            info.Arguments = args;
            info.WindowStyle = ProcessWindowStyle.Hidden;
            info.UseShellExecute = isWin;
            info.ErrorDialog = true;
            Debug.Log(info.FileName + " " + info.Arguments);

            Process pro = Process.Start(info);
            pro.WaitForExit();
            Directory.SetCurrentDirectory(currDir);
        }

        private string RemoveLogInLua(string srcFile, string outFile)
        {
            var newFile = outFile + ".tmp.lua";
            var lines = File.ReadAllLines(srcFile);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < lines.Length; ++i)
            {
                if (isFilter(lines[i])) continue;
                sb.AppendLine(lines[i]);
            }
            File.WriteAllText(newFile, sb.ToString());
            return newFile;
        }

        protected bool isFilter(string file)
        {
            string format = file.Trim();
            for (int i = 0; i < Filters.Length; i++)
            {
                if (format.StartsWith(Filters[i]))
                    return true;
            }
            return false;
        }

    #region Lua增量更新
        /// <summary>
        /// 读取lua md5配置文件
        /// </summary>
        private void ReadLuaMd5FromFile(Dictionary<string, Dictionary<string, string>> luaMd5Dict)
        {
            string luaMd5File = BuilderPreference.BUILD_PATH + "/luamd5.txt";
            if (!File.Exists(luaMd5File)) return;
            
            string[] lines = File.ReadAllLines(luaMd5File);
            for (int i = 0; i < lines.Length; ++i)
            {
                if (!string.IsNullOrEmpty(lines[i]))
                {
                    var temps = lines[i].ToLower().Split('|');
                    if (!luaMd5Dict.ContainsKey(temps[0]))
                    {
                        luaMd5Dict.Add(temps[0], new Dictionary<string, string>());
                    }
                    luaMd5Dict[temps[0]].Add(temps[1], temps[2]);
                }
//                UpdateProgress(i, lines.Length, "Getting lua md5...");
            }

            Builder.AddBuildLog("Getting lua md5...");
//            EditorUtility.ClearProgressBar();
//            AssetDatabase.Refresh();
            
        }

        /// <summary>
        /// 比较文件夹下lua更新情况，用来做lua的增量更新
        /// </summary>
        private void CompareLuaMd5(Dictionary<string, Dictionary<string, string>> luaMd5Dict)
        {
            int index = 0;
            HashSet<string> copyLuaFiles = new HashSet<string>();

            foreach (string luaBundleName in luaMd5Dict.Keys)
            {
                var relativePath = luaBundleName.Replace('-', '/');
                string dir;
                if (relativePath.EndsWith("/lua"))
                    dir = "Assets/" + relativePath.Substring(0, relativePath.LastIndexOf("/"));
                else
                    dir = "Assets/" + relativePath;

                if (!Directory.Exists(dir)) continue;

                string[] files = Directory.GetFiles(dir, "*.lua", SearchOption.TopDirectoryOnly);
                bool needCopy = true;
                // 如果文件夹下数量有变 说明有更改
                Dictionary<string, string> luaMaps = luaMd5Dict[luaBundleName];
                if (luaMaps.Count == files.Length)
                {
                    for (int i = 0; i < files.Length; ++i)
                    {
                        var cmpStr = files[i].ToLower().Replace("\\", "/");
                        string newfile = cmpStr.Replace(AppDataPath + "/", "");
                        var curMd5 = MD5.ComputeHashString(files[i]);
                        // 如果文件夹下有新增lua文件 说明有更改
                        if (!luaMaps.ContainsKey(newfile))
                            needCopy = false;
                        // 如果lua文件md5值有变动 说明有更改
                        else if (luaMaps[newfile] != curMd5)
                            needCopy = false;

                        if (!needCopy) break;
                    }
                }
                else
                {
                    needCopy = false;
                }
                // 文件夹下没有更改的资源，将新的lua包替换回上一个版本
                if (needCopy)
                {
                    for (int i = 0; i < luaPaths.Length; ++i)
                    {
                        if(!Directory.Exists(luaPaths[i]))  continue;

                        var temp = luaPaths[i].ToLower();
                        if (dir.Contains(temp))
                        {
                            var relativeDir = dir.Replace(temp, "").TrimEnd('/');
                            var bundleName = formatLuaBundleName(relativeDir);

                            if (!copyLuaFiles.Contains(bundleName))
                                copyLuaFiles.Add(bundleName);

                            break;
                        }
                    }
                }
    //            UpdateProgress(index++, luaMd5Dict.Count, "Comparing lua md5...");
            }
            CopyNoChangeLuaFiles(copyLuaFiles);
        }

        /// <summary>
        /// 记录lua的md5码
        /// </summary>
        private void RecordLuaMd5()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < luaPaths.Length; i++)
            {
                if(!Directory.Exists(luaPaths[i]))  continue;

                string luaDataPath = luaPaths[i].ToLower();
                List<string> files = BuildUtil.SearchFiles(luaDataPath , SearchOption.AllDirectories);
                foreach (string f in files)
                {
                    var path = f.ToLower();
                    if (!path.EndsWith(".lua"))   continue;
                
                    string newfile = f.Replace("assets/", "");

                    string dir = Path.GetDirectoryName(f);
                    string relativeDir = dir.Replace(luaDataPath, "");
                    string bundleName = formatLuaBundleName(relativeDir);
                    string md5 = MD5.ComputeHashString(path);

                    sb.AppendLine(string.Format("{0}|{1}|{2}", bundleName , newfile , md5));  //bundleName
                }
    //            UpdateProgress(i, luaPaths.Length, "Recording lua md5...");
            }
    //        EditorUtility.ClearProgressBar();
    //        AssetDatabase.Refresh();

            //重写记录文件
            string luaMd5File = BuilderPreference.BUILD_PATH + "/luamd5.txt";
            File.WriteAllText(luaMd5File, sb.ToString());
        }

        /// <summary>
        /// 从上一版本替换不需要增量更新的lua bundle文件
        /// </summary>
        private void CopyNoChangeLuaFiles(HashSet<string> copyLuaFiles)
        {
            if (copyLuaFiles.Count <= 0) return;

            int index = 0;
            
            foreach (var file in copyLuaFiles)
            {
                string fromPath = string.Concat(BuilderPreference.TEMP_ASSET_PATH , "/lua/" , file);
                string toPath = string.Concat(BuilderPreference.BUILD_PATH , "/lua/" , file);

                if (File.Exists(fromPath)) File.Copy(fromPath, toPath, true);
    //                UpdateProgress(index++, luaMd5Dict.Count, "Copy no change files...");
            }
    //            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

    #endregion
    }

}
