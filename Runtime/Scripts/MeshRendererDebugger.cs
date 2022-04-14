using System.Collections;
using System.Collections.Generic;
using PixelFederation.Common.Attributes;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class MeshRendererDebugger : MonoBehaviour
{
    [Button()]
    public void Test()
    {
        Debug.Log(GetComponent<MeshRenderer>().subMeshStartIndex);
    }
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
