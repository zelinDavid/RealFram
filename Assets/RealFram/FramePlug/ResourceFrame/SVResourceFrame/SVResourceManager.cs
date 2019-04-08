using System;
 
using System.Collections;
using System.Collections.Generic;
 using UnityEngine;

public class SVResourceManager : Singleton<SVResourceManager>
{
    protected long m_Guid = 0;
    public bool m_LoadFormAssetBundle = true;
    //缓存使用的资源列表
    public Dictionary<uint,SVResourceItem> AssetDic = new Dictionary<uint, SVResourceItem>();
    //缓存引用计数为零的资源列表，达到缓存组大的时候释放这个列表里最早没用的资源。
    protected SVCMapList<SVResourceItem> m_NoRefrenceAssetMapList = new SVCMapList<SVResourceItem>();




}


public enum SVLoadResPriority
{
    RES_HIGHT = 0,
    RES_MID = 0,
    RES_SLOW,
    RES_NUM,
}

public class SVResouceObj
{
    //路径对应CRC
    public uint m_Crc = 0;
    //存ResouceItem
    public SVResourceItem m_ResItem = null;
    //实例化出来的GameObject
    public GameObject m_CloneObj = null;
    //是否跳场景清除
    public bool m_bClear = true;
    //储存GUID
    public long m_Guid = 0;
    //是否已经放回对象池
    public bool m_Already = false;
    //--------------------------------
    //是否放到场景节点下面
    public bool m_SetSceneParent = false;
    //实例化资源加载完成回调
    
    public SVOnAsyncFinsih m_DealFinish = null;
    //异步参数
    public object m_Param1, m_Param2, m_Param3 = null;
    //离线数据
    public OfflineData m_OfflineData = null;

    public void Reset()
    {
        m_Crc = 0;
        m_CloneObj = null;
        m_bClear = true;
        m_Guid = 0;
        m_ResItem = null;
        m_Already = false;
        m_SetSceneParent = false;
        m_DealFinish = null;
        m_Param1 = m_Param2 = m_Param3 = null;
        m_OfflineData = null;
    }
}

public class SVAsyncLoadResParam
{
    public List<SVAsyncCallBack> m_CallBackList = new List<SVAsyncCallBack>();
    public uint m_Crc;
    public string m_Path;
    public bool m_Sprite = false;
    public SVLoadResPriority m_Priority = SVLoadResPriority.RES_SLOW;

    public void Reset()
    {
        m_CallBackList.Clear();
        m_Crc = 0;
        m_Path = "";
        m_Sprite = false;
        m_Priority = SVLoadResPriority.RES_SLOW;
    }
}

public class SVAsyncCallBack
{
    //加载完成的回调(针对ObjectManager)
    public SVOnAsyncFinsih m_DealFinish = null;
    //加载完成的回调
    public SVOnAsyncObjFinish m_DealObjFinish = null;
    //ObjectManger对应的中间类
    public SVResouceObj m_ResObj = null;
    //回调参数
    public object m_Param1 = null, m_Param2 = null, m_Param3 = null;

    public void Reset(){
        m_DealObjFinish = null;
        m_DealFinish = null;
        m_Param1 = null;
        m_Param2 = null;
        m_Param3 = null;
        m_ResObj = null;
    }
}


//资源加载完成回调
public delegate void SVOnAsyncObjFinish(string path, Object obj, object param1 = null, object param2 = null, object param3 = null);
//实例化对象加载完成回调
public delegate void SVOnAsyncFinsih(string path, ResouceObj resObj, object param1 = null, object param2 = null, object param3 = null);


//双向链表结构节点
public class SVDoubleLinkedListNode<T> where T: class, new()
{
    public SVDoubleLinkedListNode<T> prev = null;
    public SVDoubleLinkedListNode<T> next = null;
    public T t = null;

    public void  Reset(){
        prev = next = null;
        t = null;
    }
}

public class SVDoubleLinedList<T> where T: class, new()
{
    //表头
    public SVDoubleLinkedListNode<T> Head = null;
    //表尾
    public SVDoubleLinkedListNode<T> Tail = null;

    //双向链表结构类对象池
    protected SVClassObjectPool<SVDoubleLinkedListNode<T>> m_DoubleLinkNodePool = SVClassPool.Instance.GetOrCreatClassPool<SVDoubleLinkedListNode<T>>(500);

    protected int m_Count = 0;
    public int Count{
        get{return m_Count;}
    }

    public SVDoubleLinkedListNode<T> AddToHeader(T t){
        SVDoubleLinkedListNode<T> node = m_DoubleLinkNodePool.Spawn(true);
        node.prev = null;
        node.next = null;
        node.t = t;
      return AddToHeader(node);  
    }

    public SVDoubleLinkedListNode<T> AddToHeader(SVDoubleLinkedListNode<T> node){
        if (node == null)
            return null;
        //如果m_count == 0,则表头表尾都是它
        //如果m_count > 0, 则表头是它
        if(m_Count == 0){
            Head = node;
            Tail = node;
        }else{
            node.next = Head;
            Head.prev = node;
            Head = node;
        }
        m_Count ++;
        return Head;
    }

    public SVDoubleLinkedListNode<T> AddToTail(T t)
    {
        SVDoubleLinkedListNode<T> pList = m_DoubleLinkNodePool.Spawn(true);
        pList.next = null;
        pList.prev = null;
        pList.t = t;
        return AddToTail(pList);
    }

    public SVDoubleLinkedListNode<T> AddToTail(SVDoubleLinkedListNode<T> pNode)
    {
        if (pNode == null)
            return null;

        pNode.next = null;
        if (Tail == null)
        {
            Head = Tail = pNode;
        }
        else
        {
            pNode.prev = Tail;
            Tail.next = pNode;
            Tail = pNode;
        }
        m_Count++;
        return Tail;
    }

    public void RemoveNode(SVDoubleLinkedListNode<T> pNode)
    {
        if(pNode == null)
            return;

        if (pNode == Head)
            Head = pNode.next;

        if (pNode == Tail)
            Tail = pNode.prev;

        if (pNode.prev != null)
            pNode.prev.next = pNode.next;

        if (pNode.next != null)
            pNode.next.prev = pNode.prev;

        m_Count --;
        pNode.Reset();
        m_DoubleLinkNodePool.Recycle(pNode);

    }

    public void MoveToHead(SVDoubleLinkedListNode<T> pNode){
        if (pNode == null || pNode == Head)
            return;
        if (pNode.prev == null && pNode.next == null)
            return;
        var t = pNode.t;   
        RemoveNode(pNode);
        AddToHeader(t);
    }
}


public class SVCMapList<T> where T : class, new()
{
    SVDoubleLinedList<T> m_DLink = new SVDoubleLinedList<T>();
    Dictionary<T,SVDoubleLinkedListNode<T>> m_FindMap = new Dictionary<T, SVDoubleLinkedListNode<T>>();

    ~SVCMapList(){
        Clear();
    }

    public void Clear(){
        while (m_DLink.Tail != null)
        {
            Remove(m_DLink.Tail.t);
        }
    }

    public void Remove(T t){
        SVDoubleLinkedListNode<T> node = null;
        if (!m_FindMap.TryGetValue(t, out node) || node == null)
        {
         return ;   
        }
        m_DLink.RemoveNode(node);
        m_FindMap.Remove(t);
    }

    public T Back()
    {
        return m_DLink.Tail == null ? null : m_DLink.Tail.t;
    }

     public int Size()
    {
        return m_FindMap.Count;
    }

   /// <summary>
    /// 查找是否存在该节点
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public bool Find(T t)
    {
        SVDoubleLinkedListNode<T> node = null;
        if (!m_FindMap.TryGetValue(t, out node) || node == null)
            return false;

        return true;
    }

    /// <summary>
    /// 刷新某个节点，把节点移动到头部
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public bool Reflesh(T t)
    {
        SVDoubleLinkedListNode<T> node = null;
        if (!m_FindMap.TryGetValue(t, out node) || node == null)
            return false;

        m_DLink.MoveToHead(node);
        return true;
    }

}











