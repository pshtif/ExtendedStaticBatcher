/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public enum ExtendedStaticBatcherInitializationType
{
    AWAKE,
    START,
    METHOD
}

public class ExtendedStaticBatcher : MonoBehaviour
{
    public ExtendedStaticBatcherInitializationType initializationType = ExtendedStaticBatcherInitializationType.START;
    public bool isGlobal = false;
    public LayerMask includeLayers = ~0;
    public bool useReflectionInternalPreChecks = true;
    public bool warnOnNegativeScale = true;

    private PropertyInfo _staticBatchIndex;
    private PropertyInfo _disableBatching;
    
    void CacheReflection()
    {
        if (_staticBatchIndex == null)
        {
            _staticBatchIndex =
                typeof(Renderer).GetProperty("staticBatchIndex", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        if (_disableBatching == null)
        {
            _disableBatching =
                typeof(Shader).GetProperty("disableBatching", BindingFlags.Instance | BindingFlags.NonPublic);
        }
    }
    
    void Awake()
    {
        if (initializationType == ExtendedStaticBatcherInitializationType.AWAKE)
        {
            Batch();
        }
    }
    
    void Start()
    {
        if (initializationType == ExtendedStaticBatcherInitializationType.START)
        {
            Batch();
        }
    }

    public void Batch()
    {
        StaticBatchingUtility.Combine(EnumerateObjectForBatching(), gameObject);
    }

    GameObject[] EnumerateObjectForBatching()
    {
        List<GameObject> objects = new List<GameObject>();

        MeshFilter[] filters;
        if (!isGlobal)
        {
            filters = transform.GetComponentsInChildren<MeshFilter>();
        } else
        {
            filters = GameObject.FindObjectsOfType<MeshFilter>();
        }
        
        foreach (var filter in filters)
        {
            // Discard all objects not included in specified layers
            if ((includeLayers & (1<<filter.gameObject.layer)) == 0)
                continue;

            Mesh sharedMesh = filter.sharedMesh;

            // Invalid mesh
            if (sharedMesh.vertexCount == 0)
            {
                Debug.LogWarning("Trying to static batch mesh with 0 vertices.");
                continue;
            }

            // Mesh needs to readable for static batching
            if (!sharedMesh.isReadable)
            {
                Debug.LogWarning("Trying to static batch non readable geometry.");
                continue;
            }

            if (filter.transform.localToWorldMatrix.determinant < 0.0 && warnOnNegativeScale)
            {
                Debug.LogWarning("Batching object will negative scaling will break into multiple batches.");
            }

            Renderer renderer = filter.GetComponent<Renderer>(); 
            // Static batcher would discard them anyway so lets do it here as well
            if (renderer == null || !renderer.enabled)
            {
                Debug.LogWarning("Trying to static batch mesh without or disabled renderer.");
                continue;
            }

            // Check for some stuff that batcher will check internally so we can granularize debugging of issues
            if (useReflectionInternalPreChecks)
            {
                CacheReflection();
                
                if ((uint)(int)_staticBatchIndex.GetValue(renderer) > 0U)
                {
                    Debug.Log("StaticBatchIndex");
                    continue;
                }
                
                Material[] source = renderer.sharedMaterials;
                if (source.Any<Material>(m =>
                    {
                        return m != null && m.shader != null &&
                               (uint)(int)_disableBatching.GetValue(m.shader) > 0U;
                    }))
                {
                    Debug.Log("Trying to batch renderer that has material with forced disabled batching.");
                    continue;
                }
            }
            
            objects.Add(filter.gameObject);
        } 

        return objects.ToArray();
    }
    
}
