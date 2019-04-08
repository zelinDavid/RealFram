using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Net.Mime;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class SVABManager :Singleton<SVABManager>
{
    protected string m_ABConfigABName = "assetbundleconfig";
    
    protected Dictionary<uint, SVResourceItem> m_ResouceItemDic = new Dictionary<uint, SVResourceItem>();

    protected Dictionary<uint, SVAssetBundleItem> m_AssetBundleItemDic = new Dictionary<uint, SVAssetBundleItem>();

    protected SVClassObjectPool<SVAssetBundleItem> m_AssetBundleItemPool = SVClassPool.Instance.GetOrCreatClassPool<SVAssetBundleItem>(500);

    protected string  ABLoadPath{
        get{
            return Application.streamingAssetsPath   + "/";
        }
    }

    //加载ab配置表
    public bool LoadABConfig(){
        m_ResouceItemDic.Clear();
        string configPath = ABLoadPath + m_ABConfigABName;
        AssetBundle configAB = AssetBundle.LoadFromFile(configPath);
        TextAsset textAsset = configAB.LoadAsset<TextAsset>(m_ABConfigABName);
        if(textAsset == null ){
            Debug.LogError("ABConfig is no exist!");
            return false;
        }
        AssetBundleConfig abCOnfig;
        using( MemoryStream stream = new MemoryStream(textAsset.bytes)){
            BinaryFormatter bf = new BinaryFormatter();
            abCOnfig = (AssetBundleConfig)bf.Deserialize(stream);
            stream.Close();
        }

        for (int i = 0; i <abCOnfig.ABList.Count; i++)
        {
            ABBase aBase = abCOnfig.ABList[i];
            SVResourceItem item = new SVResourceItem();
            item.m_ABName = aBase.ABName;
            item.m_AssetName = aBase.AssetName;
            item.m_Crc = aBase.Crc;
            item.m_DependAssetBundle = aBase.ABDependce;
            // item.m_Guid = 
            if(m_ResouceItemDic.ContainsKey(item.m_Crc)){
                Debug.LogError("重复的Crc 资源名为：");

            }else{
                m_ResouceItemDic.Add(item.m_Crc, item);
            }
        }
        return true ;
    }
      
    public SVResourceItem LoadResourceAB(uint cRc){
         if(!m_ResouceItemDic.TryGetValue(cRc, out SVResourceItem item) || item == null){
             Debug.LogError(string.Format("can't find cRc:{0}", cRc.ToString()));
             return null;
         }
 
        if (item.m_AssetBundle != null)
        {
            return item;
        }

        item.m_AssetBundle =  LoadAssetByName(item.m_ABName);

        if (item.m_DependAssetBundle != null)
        {
            foreach (string name in item.m_DependAssetBundle)
            {
                LoadAssetByName(name);
            }
        }
        return null;
    }

    public AssetBundle LoadAssetByName(string name){
        uint crc = Crc32.GetCrc32(name);
        if(m_AssetBundleItemDic.TryGetValue(crc, out SVAssetBundleItem bundle)){
            bundle.Refcount ++;
            return bundle.assetBundle;
        }else{
            string fullpath = ABLoadPath + name;
            AssetBundle ab = AssetBundle.LoadFromFile(fullpath);
            bundle = m_AssetBundleItemPool.Spawn(true);
            bundle.assetBundle = ab;
            bundle.Refcount ++;
            m_AssetBundleItemDic.Add(crc, bundle);
            return ab;
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    /// <param name="item"></param>
    public void ReleaseAsset(ResouceItem item){
         if (item == null)
         {
             return;
         }
         
        bool trueRelease = UnLoadAssetBundle(item.m_ABName);
        if (trueRelease)
        {
            item.m_AssetBundle = null;
        }

         foreach (string name in item.m_DependAssetBundle)
         {
             UnLoadAssetBundle(name);
         }
    }

    
    private bool UnLoadAssetBundle(string name)
    {
        uint crc = Crc32.GetCrc32(name);
        if(m_AssetBundleItemDic.TryGetValue(crc, out SVAssetBundleItem item) && item != null){
            item.Refcount --;
            if (item.Refcount < 0)
            {
                item.assetBundle.Unload(true);
                item.Reset();
                m_AssetBundleItemDic.Remove(crc);
                m_AssetBundleItemPool.Recycle(item);
                return true;
            }
        }  
        return false;
    }

    public SVResourceItem FindResourceItem(uint crc)
    {
        if(m_ResouceItemDic.TryGetValue(crc, out SVResourceItem item) && item != null){
            return item;
        } 
        return null;
    }
}


//----------------name- ----------

public class SVAssetBundleItem
{
    public AssetBundle assetBundle;
    public int Refcount;
    public void Reset(){
        assetBundle = null;
        Refcount = 0;
        
    }
}


public class SVResourceItem
{
   //资源路径的CRC
    public uint m_Crc = 0;
    //该资源的文件名
    public string m_AssetName = string.Empty;
    //该资源所在的AssetBundle
    public string m_ABName = string.Empty;
    //该资源所依赖的AssetBundle
    public List<string> m_DependAssetBundle = null;
    //该资源加载完的AB包
    public AssetBundle m_AssetBundle = null;
    //-----------------------------------------------------
    //资源对象
    public Object m_Obj = null;
    //资源唯一标识
    public int m_Guid = 0;
    //资源最后所使用的时间
    public float m_LastUseTime = 0.0f;
    //引用计数
    protected int m_RefCount = 0;
    //是否跳场景清掉
    public bool m_Clear = true;
    public int RefCount{
        set{
            m_RefCount = value; 
            if(m_RefCount <0){
                Debug.LogError("refcount < 0" + m_RefCount + " ," + (m_Obj != null ? m_Obj.name : "name is null" ));
            }
        }
    }

}