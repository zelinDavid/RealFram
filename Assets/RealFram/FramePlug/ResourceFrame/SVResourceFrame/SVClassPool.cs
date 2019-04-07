using System;
using System.Collections.Generic;
public class SVClassPool:Singleton<SVClassPool>
{
    protected Dictionary<Type,object> m_classPoolDict = new Dictionary<Type, object>();

    public SVClassObjectPool<T> GetOrCreatClassPool<T>(int maxcount) where T: class, new(){
        object pool;
        Type type = typeof(T);
        if(!m_classPoolDict.TryGetValue(type,out pool) || pool == null){
            pool = new SVClassObjectPool<T>(maxcount);
            m_classPoolDict.Add(type, pool);
        }

        return pool as SVClassObjectPool<T>;
    }
}