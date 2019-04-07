using System;
using System.Collections.Generic;
public class SVClassPool
{
    protected Dictionary<Type,object> m_classPoolDict = new Dictionary<Type, object>();

    // public SVClassObjectPool<T> GetOrCreatClassPool<T>(int maxcount) where T: class, new(){
    //     object pool;
    //     if(m_classPoolDict.TryGetValue(typeof(T),out pool)){

    //     }
        
    // }
}