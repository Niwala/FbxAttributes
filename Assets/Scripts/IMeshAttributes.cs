using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMeshAttributes
{
    public Mesh mesh { get; set; }
    public string[] materials { get; set; }
}