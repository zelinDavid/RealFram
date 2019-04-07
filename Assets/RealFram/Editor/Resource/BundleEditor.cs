﻿using System.Text;
using System.Net.Mail;
using System.Xml.Linq;
using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.Profiling;

public class BundleEditor
{
    private static string m_BunleTargetPath = Application.dataPath+"/../AssetBundle/" + EditorUserBuildSettings.activeBuildTarget.ToString();
    private static string ABCONFIGPATH = "Assets/RealFram/Editor/Resource/ABConfig.asset";
    private static string ABBYTEPATH = RealConfig.GetRealFram().m_ABBytePath;
    //key是ab包名，value是路径，所有文件夹ab包dic
    private static Dictionary<string, string> m_AllFileDir = new Dictionary<string, string>();
    //过滤的list
    private static List<string> m_AllFileAB = new List<string>();
    //单个prefab的ab包
    private static Dictionary<string, List<string>> m_AllPrefabDir = new Dictionary<string, List<string>>();
    //储存所有有效路径
    private static List<string> m_ConfigFil = new List<string>();

    [MenuItem("Tools/SV打包")]
    public static  void SVBuild(){

        m_ConfigFil.Clear();
        m_AllFileAB.Clear();
        m_AllFileDir.Clear();
        m_AllPrefabDir.Clear();
        
        ABConfig config =  AssetDatabase.LoadAssetAtPath<ABConfig>(ABCONFIGPATH);
        List<string> prefabList = config.m_AllPrefabPath;
        List<ABConfig.FileDirABName> fileDirList = config.m_AllFileDirAB;

        //将dirPath存放对应数据结构
        foreach (ABConfig.FileDirABName dirPath in fileDirList)
        {
            EditorUtility.DisplayProgressBar("查找Prefab", "Prefab:",0.9f);

            if (m_AllFileDir.ContainsKey(dirPath.ABName))
            {
                Debug.LogError("AB包名称重复");
            }else{
                m_AllFileDir.Add(dirPath.ABName, dirPath.Path);
                m_AllFileAB.Add(dirPath.Path);
                m_ConfigFil.Add(dirPath.Path);
            }
        }

        //将prefab path存放对应数据结构
 
        string[] prefabIDs = AssetDatabase.FindAssets("t:Prefab", config.m_AllPrefabPath.ToArray());
        // Debug.Log("prefabs: " + prefabs.Length);
        foreach ( string uID in prefabIDs)
        {
            // Debug.LogWarning("uID " + uID);
            string path = AssetDatabase.GUIDToAssetPath(uID);
            //  Debug.LogWarning("uid path " + path);
            if (ConatinABName(path, m_ConfigFil.ToArray()))
            {
                continue;
            }
            GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            string[] allDepend = AssetDatabase.GetDependencies(path);
            List<string> dependPrefabs = new List<string>();

            foreach (string dependPath in allDepend)
            {
                if (!ContainAllFileAB(dependPath) && !dependPath.EndsWith(".cs") )
                { 
                    m_AllFileAB.Add(dependPath);
                    dependPrefabs.Add(dependPath);
                }
            }
            
            m_AllPrefabDir.Add(obj.name, dependPrefabs);
            m_ConfigFil.Add(path);
        }


        //将对应资源赋值assetbundle name

        foreach (string name in m_AllFileDir.Keys)
        {
            SetABName(name, m_AllFileDir[name]);
        }

        foreach (string name in m_AllPrefabDir.Keys)
        {
            SetABName(name, m_AllPrefabDir[name]);
        }
        //删除之前废弃的文件
        SVDelete();
        
 
        //生成对应配置列表
        SVGenerateConfigeFiles();
        //打包
        SVBuildBundles();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(); //刷新工程视图

        EditorUtility.ClearProgressBar();
 
    }

    public static void SVGenerateConfigeFiles(){
        // 生成abbundleConfige.xml 及 二进制文件

        string[] bundleNames = AssetDatabase.GetAllAssetBundleNames();
        // Debug.LogWarning("bundleName: " + bundleNames.Length);

        Dictionary<string, string> resList = new Dictionary<string, string> ();
        AssetBundleConfig config = new AssetBundleConfig();
        config.ABList = new List<ABBase>();
        foreach (string name in bundleNames)
        {
         
            string[] pathNames = AssetDatabase.GetAssetPathsFromAssetBundle(name);
            foreach (string path in pathNames)
            {
                if (!ValidPath(path))
                {
                    continue;
                }
                resList.Add(path, name);
            }
        }

        foreach (string path in resList.Keys)
        {
            ABBase aBase = new ABBase();
            List<string> temDepends = new List<string>();
            aBase.ABDependce = temDepends;
            aBase.ABName = resList[path];
            aBase.AssetName = path.Remove(0, path.LastIndexOf("/") + 1);
            aBase.Crc = Crc32.GetCrc32(path);
            aBase.Path = path;

            string[] depends = AssetDatabase.GetDependencies(path);
            foreach (string bundlePath in depends)
            {
                if(bundlePath.EndsWith(".cs"))
                    continue;
                if (resList.TryGetValue(bundlePath, out string temBundleName))
                {
                    if(aBase.ABName == temBundleName)
                        continue;
                    
                    temDepends.Add(temBundleName);
                }
            }

            config.ABList.Add(aBase);
        }

    //写入xml
        string xmlPath = Application.dataPath + "/AssetbundleConfig.xml";
        if(File.Exists(xmlPath)) File.Delete(xmlPath);
        using (FileStream fileStream = new FileStream(xmlPath, FileMode.Create, FileAccess.ReadWrite) )
        {
            using (StreamWriter sr = new StreamWriter(fileStream, Encoding.UTF8))
            {
                XmlSerializer xmlSer = new XmlSerializer(typeof(AssetBundleConfig));
                xmlSer.Serialize(sr,config);
                sr.Close();
                fileStream.Close();
            }

        }
            
        

        //写入二进制
        foreach (ABBase abBase in config.ABList)
        {
            abBase.Path = "";
        }
         using (FileStream fileStream = new FileStream(ABBYTEPATH, FileMode.Create, FileAccess.ReadWrite) )
        {
            fileStream.Seek(0, SeekOrigin.Begin);
            fileStream.SetLength(0);
             BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(fileStream, config);
            fileStream.Close();
        }
        SetABName("assetbundleconfig", ABBYTEPATH);

        Debug.LogWarning("generate configfile success");

    }
    
    public static void SVDelete(){
         string[] bundles =  AssetDatabase.GetAllAssetBundleNames();
        DirectoryInfo diction = new DirectoryInfo(m_BunleTargetPath);
        FileInfo[] files =  diction.GetFiles("*", SearchOption.AllDirectories);
        foreach (FileInfo file in files)

        {
            if (ConatinABName(file.Name, bundles) || file.Name.EndsWith(".meta") || file.Name.EndsWith(".manifest")|| file.Name.EndsWith(".assetbundleconfig"))
            {
                continue;
            }else {
                if (File.Exists(file.FullName))
                {
                    File.Delete(file.FullName);
                }
                if (File.Exists(file.FullName + ".manifest") )
                {
                    File.Delete(file.FullName + ".manifest");
                }
            }
        }
 
    }
    
    public static void SVBuildBundles(){
        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(m_BunleTargetPath, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
        if (manifest == null)
        {
           Debug.LogError("打包失败"); 
        }else{
            Debug.LogWarning("打包成功");
        }

    }


    [MenuItem("Tools/打包")]
    public static void Build()
    {
        DataEditor.AllXmlToBinary();
        m_ConfigFil.Clear();
        m_AllFileAB.Clear();
        m_AllFileDir.Clear();
        m_AllPrefabDir.Clear();
 
        ABConfig abConfig = AssetDatabase.LoadAssetAtPath<ABConfig>(ABCONFIGPATH);
        foreach (ABConfig.FileDirABName fileDir in abConfig.m_AllFileDirAB)
        {
            if (m_AllFileDir.ContainsKey(fileDir.ABName))
            {
                Debug.LogError("AB包配置名字重复，请检查！");
            }
            else
            {
                m_AllFileDir.Add(fileDir.ABName, fileDir.Path);
                m_AllFileAB.Add(fileDir.Path);
                m_ConfigFil.Add(fileDir.Path);
            }
        }

        string[] allStr = AssetDatabase.FindAssets("t:Prefab", abConfig.m_AllPrefabPath.ToArray());
        for (int i = 0; i < allStr.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(allStr[i]);
            EditorUtility.DisplayProgressBar("查找Prefab", "Prefab:" + path, i * 1.0f / allStr.Length);
            m_ConfigFil.Add(path);
            if (!ContainAllFileAB(path))
            {
                Debug.LogWarning("origan path: " + path );
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Debug.LogWarning("origan obj: " + obj );

                string[] allDepend = AssetDatabase.GetDependencies(path);
                List<string> allDependPath = new List<string>();
                for (int j = 0; j < allDepend.Length; j++)
                {
                    if (!ContainAllFileAB(allDepend[j]) && !allDepend[j].EndsWith(".cs"))
                    {
                        m_AllFileAB.Add(allDepend[j]);
                        allDependPath.Add(allDepend[j]);
                    }
                }
                if (m_AllPrefabDir.ContainsKey(obj.name))
                {
                    Debug.LogError("存在相同名字的Prefab！名字：" + obj.name);
                }
                else
                {
                    m_AllPrefabDir.Add(obj.name, allDependPath);
                }
            }
        }
      

        foreach (string name in m_AllFileDir.Keys)
        {
            SetABName(name, m_AllFileDir[name]);
        }

        foreach (string name in m_AllPrefabDir.Keys)
        {
            SetABName(name, m_AllPrefabDir[name]);
        }

        BunildAssetBundle();

        string[] oldABNames = AssetDatabase.GetAllAssetBundleNames();
        for (int i = 0; i < oldABNames.Length; i++)
        {
            AssetDatabase.RemoveAssetBundleName(oldABNames[i], true);
            EditorUtility.DisplayProgressBar("清除AB包名", "名字：" + oldABNames[i], i * 1.0f / oldABNames.Length);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(); //刷新工程视图
        EditorUtility.ClearProgressBar();
    }

    static void SetABName(string name, string path)
    {
        AssetImporter importer = AssetImporter.GetAtPath(path);
       if (importer != null)
       {
           importer.assetBundleName = name;
       }
    }

    static void SetABName(string name, List<string> paths)
    {
        for (int i = 0; i < paths.Count; i++)
        {
            SetABName(name, paths[i]);
        }
    }

    static void BunildAssetBundle()
    {
        string[] allBundles = AssetDatabase.GetAllAssetBundleNames();
 
        //key为全路径，value为包名
        Dictionary<string, string> resPathDic = new Dictionary<string, string>();
        for (int i = 0; i < allBundles.Length; i++)
        {
            string[] allBundlePath = AssetDatabase.GetAssetPathsFromAssetBundle(allBundles[i]);
            for (int j = 0; j < allBundlePath.Length; j++)
            {
                if (allBundlePath[j].EndsWith(".cs"))
                    continue;

                Debug.Log("此AB包：" + allBundles[i] + "下面包含的资源文件路径：" + allBundlePath[j]);
                resPathDic.Add(allBundlePath[j], allBundles[i]);
            }
        }
 
        if (!Directory.Exists(m_BunleTargetPath))
        {
            Directory.CreateDirectory(m_BunleTargetPath);
        }

        DeleteAB();
        //生成自己的配置表
        WriteData(resPathDic);

        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(m_BunleTargetPath, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
        if (manifest == null)
        {
            Debug.LogError("AssetBundle 打包失败！");
        }
        else
        {
            Debug.Log("AssetBundle 打包完毕");
        }
    }

    static void WriteData(Dictionary<string ,string> resPathDic)
    {
        AssetBundleConfig config = new AssetBundleConfig();
        config.ABList = new List<ABBase>();
        foreach (string path in resPathDic.Keys)
        {
            if (!ValidPath(path)){ //m_ConfigFil
                // Debug.LogError("invalidPath: "+ path + "____" + resPathDic[path]);
                continue;
            }

            ABBase abBase = new ABBase();
            abBase.Path = path;
            abBase.Crc = Crc32.GetCrc32(path);
            abBase.ABName = resPathDic[path];
            abBase.AssetName = path.Remove(0, path.LastIndexOf("/") + 1);
            abBase.ABDependce = new List<string>();
            string[] resDependce = AssetDatabase.GetDependencies(path);
            for (int i = 0; i < resDependce.Length; i++)
            {
                string tempPath = resDependce[i];
                if (tempPath == path || path.EndsWith(".cs"))
                    continue;

                string abName = "";
                if (resPathDic.TryGetValue(tempPath, out abName))
                {
                    if (abName == resPathDic[path])
                        continue;

                    if (!abBase.ABDependce.Contains(abName))
                    {
                        abBase.ABDependce.Add(abName);
                    }
                }
            }
            config.ABList.Add(abBase);
        }

        //写入xml
        string xmlPath = Application.dataPath + "/AssetbundleConfig.xml";
        if (File.Exists(xmlPath)) File.Delete(xmlPath);
        FileStream fileStream = new FileStream(xmlPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        StreamWriter sw = new StreamWriter(fileStream, System.Text.Encoding.UTF8);
        XmlSerializer xs = new XmlSerializer(config.GetType());
        xs.Serialize(sw, config);
        sw.Close();
        fileStream.Close();

        //写入二进制
        foreach (ABBase abBase in config.ABList)
        {
            abBase.Path = "";
        }
        FileStream fs = new FileStream(ABBYTEPATH, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        fs.Seek(0, SeekOrigin.Begin);
        fs.SetLength(0);
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(fs, config);
        fs.Close();
        AssetDatabase.Refresh();
        SetABName("assetbundleconfig", ABBYTEPATH);
    }

    /// <summary>
    /// 删除无用AB包
    /// </summary>
    static void DeleteAB()
    {
        string[] allBundlesName = AssetDatabase.GetAllAssetBundleNames();
        DirectoryInfo direction = new DirectoryInfo(m_BunleTargetPath);
        FileInfo[] files = direction.GetFiles("*", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            if (ConatinABName(files[i].Name, allBundlesName) || files[i].Name.EndsWith(".meta")|| files[i].Name.EndsWith(".manifest") || files[i].Name.EndsWith("assetbundleconfig"))
            {
                continue;
            }
            else
            {
                Debug.Log("此AB包已经被删或者改名了：" + files[i].Name);
                if (File.Exists(files[i].FullName))
                {
                    File.Delete(files[i].FullName);
                }
                if(File.Exists(files[i].FullName + ".manifest"))
                {
                    File.Delete(files[i].FullName + ".manifest");
                }
            }
        }
    }

    /// <summary>
    /// 遍历文件夹里的文件名与设置的所有AB包进行检查判断
    /// </summary>
    /// <param name="name"></param>
    /// <param name="strs"></param>
    /// <returns></returns>
    static bool ConatinABName(string name, string[] strs)
    {
        for (int i = 0; i < strs.Length; i++)
        {
            if (name == strs[i])
                return true;
        }
        return false;
    }

    /// <summary>
    /// 是否包含在已经有的AB包里，做来做AB包冗余剔除
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    static bool ContainAllFileAB(string path)
    {
        for (int i = 0; i < m_AllFileAB.Count; i++)
        {
            if (path == m_AllFileAB[i] || (path.Contains(m_AllFileAB[i]) && (path.Replace(m_AllFileAB[i],"")[0] == '/')))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 是否是指定打包的相关资源路径，如果是 资源依赖路径，则return false 
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    static bool ValidPath(string path)
    {
        for (int i = 0; i < m_ConfigFil.Count; i++)
        {
            if (path.Contains(m_ConfigFil[i]))
            {
                return true;
            }
        }
        return false;
    }
}
