using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FbxMeshExample : IMeshAttributes
{
    //------- The mesh and materials properties below are necessary for the system to work properly. -------
    /// <summary> Source mesh - Allows to identify to which mesh the data of this instance are bind. </summary>
    public Mesh mesh { get => _mesh; set => _mesh = value; }
    [SerializeField] private Mesh _mesh;  //Layout set up only for the purpose of making the field easily visible in the inspector. This variable can be removed and mesh var can use a simpler get set.

    /// <summary> The name of the materials. Not necessary but can be very useful during procedural generation (among others). </summary>
    public string[] materials { get; set; }



    //-------     The attributes of your mesh: Put here all the attributes you want to collect.     -------
    //Use the same name and type between the variables below and the attributes on the FBX file mesh: The fields should be filled automatically
    public MyEnum myEnum;
    public string myString;
    public int myInteger;
    public float myFloat;
    public Color myColor;
    public Vector3 myVector;


    //The enum must be similar to the one in your FBX file.
    public enum MyEnum
    {
        Green,
        Blue,
        Red
    }
}