using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FbxScriptableObject<T> : ScriptableObject where T : IMeshAttributes
{
    public GameObject source;
    public List<T> childs = new List<T>();

    public virtual void OnPostImport() { }
}