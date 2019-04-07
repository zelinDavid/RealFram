using System.Collections.Generic;

public class SVClassObjectPool<T> where T : class, new () {
    protected Stack<T> m_Pool = new Stack<T> ();

    protected int m_maxCount = 0;

    protected int m_NoRecycleCount = 0;

    public SVClassObjectPool (int maxCount) {
        m_maxCount = maxCount;
        for (int i = 0; i < maxCount; i++) {
            m_Pool.Push (new T ());
        }
    }

    //取对象
    /// <summary>
    /// 从池里面取类对象
    /// </summary>
    /// <param name="creatIfPoolEmpty">如果为空是否new出来</param>
    /// <returns></returns>
    public T Spawn (bool createIfEmpty) {
        if (m_Pool.Count > 0) {
            T t = m_Pool.Pop ();
            if (t != null) {
                m_NoRecycleCount++;
                return t;
            } else {
                if (createIfEmpty) {
                    t = new T ();
                    m_NoRecycleCount++;
                    return t;
                }
            }
        } else {
            if (createIfEmpty) {
                T t = new T ();
                m_NoRecycleCount++;
                return t;
            }
        }

        return null;
    }

    public bool Recycle (T obj) {
        if (obj == null)
            return false;

        m_NoRecycleCount--;

        if (m_Pool.Count >= m_maxCount && m_maxCount > 0)
        {
            obj = null;
            return false;
        }

        m_Pool.Push(obj);
        return true;
    }

    //回收对象

}