using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AssetBundleBuilder
{
    public class SVNUtility
    {
        private const string TORTOISEPROC_NAME = "TortoiseProc.exe";
        private const string PYTHONPROC_NAME = "python";

        public static void Update(string path)
        {
            string args = string.Format("/command:update /path:{0}", path);
            Process p = Process.Start(TORTOISEPROC_NAME, args);
            p.WaitForExit();
        }


        public static void Update(string[] paths)
        {
            Update(string.Join("*" , paths));
        }

        public static void Commit(string path , string svnLog)
        {
            string args = string.Format("/command:commit /path:{0} /logmsg:{1}", path , svnLog);
            Process p = Process.Start(TORTOISEPROC_NAME, args);
            p.WaitForExit();
        }
        
        public static void Commit(string[] paths, string svnLog)
        {
            Commit(string.Join("*" , paths) , svnLog);
        }

        public static void Upload(string args)
        {
            Process p = Process.Start(PYTHONPROC_NAME, args);
            p.WaitForExit();
        }


    }
}