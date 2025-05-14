using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> where T : MonoBehaviour
{
    private readonly T prefab;
    private readonly LinkedList<T> pool = new LinkedList<T>();
    private readonly Transform parentTransform;

    public ObjectPool(T prefab, int initialSize, Transform parent = null)
    {
        this.prefab = prefab;
        this.parentTransform = parent;

        for (int i = 0; i < initialSize; i++)
        {
            T obj = Object.Instantiate(prefab, parent);
            //SetupMaterial(obj);
            obj.gameObject.SetActive(false);
            pool.AddLast(obj);
        }
    }

    public T Get()
    {
        T obj;

        if (pool.Count > 0)
        {
            obj = pool.First.Value;
            pool.RemoveFirst();
        }
        else
        {
            obj = Object.Instantiate(prefab, parentTransform);
            //SetupMaterial(obj);
        }

        obj.gameObject.SetActive(true);
        return obj;
    }

    public void Release(T obj)
    {
        //CleanupMaterial(obj);
        obj.gameObject.SetActive(false);
        pool.AddLast(obj); // Normal release to back
    }

    public void ReleaseToFront(T obj)
    {
        //CleanupMaterial(obj);
        obj.gameObject.SetActive(false);
        pool.AddFirst(obj); // Optional method to add to front
    }

    private void SetupMaterial(T obj)
    {
        var renderer = obj.GetComponent<MeshRenderer>();
        if (renderer != null && renderer.sharedMaterial != null)
        {
            var instancedMaterial = new Material(renderer.sharedMaterial)
            {
                name = renderer.sharedMaterial.name + " (Instance)"
            };
            renderer.material = instancedMaterial;
        }
    }

    private void CleanupMaterial(T obj)
    {
        var renderer = obj.GetComponent<MeshRenderer>();
        if (renderer != null && renderer.material != null)
        {
            Object.Destroy(renderer.material);
        }
    }
}
