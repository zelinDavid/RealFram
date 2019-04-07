using System.Collections.Generic;
 
 
public class SVABManager :Singleton<SVABManager>
{
    protected string m_ABConfigABName = "assetbundleconfig";
    
    protected Dictionary<uint, ResouceItem> m_ResouceItemDic = new Dictionary<uint, ResouceItem>();

    protected Dictionary<uint, AssetBundleItem> m_assetBundleItem = new Dictionary<uint, AssetBundleItem>();

    protected ClassObjectPool<AssetBundleItem> m_AssetBundleItemPool = ObjectManager.Instance.GetOrCreatClassPool<AssetBundleItem>();
    
}