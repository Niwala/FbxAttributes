# Fbx Attributes
Small utility scripts to easily extract attributes from FBX file meshes and get them into unity.

![Maya & Unity Example](Media/Image_01.png?raw=true)

This project serves to quickly set up a scriptable object that will automatically receive the attributes of an FBX mesh.
(During procedural generation for example, it allows to add a lot of meta-data on the different Meshes.)

![Maya & Unity Example](Media/Image_02.gif?raw=true)

&nbsp;

## Usage
The whole thing is quite simple to use. An example is available in the project.

To use the project, you have to make two scripts:

 - The first one will contain the attribute data. It must have the [System.Serializable] attribute and implement the IMeshAttributes interface. Then, put the variables you want to extract from the FBX file.

```csharp
//Simplified example on purpose - See the FbxMeshExample.cs script for more details
using UnityEngine;

[System.Serializable]
public class MyFbxMeshAttributes : IMeshAttributes
{
    //Required data 
    public Mesh mesh { get; set; }
    public string[] materials { get; set; }

    //Attributes that will be read from the fbx file
    public MyEnum myEnum;
    public string myString;
    public int myInteger;
    public float myFloat;
    public Color myColor;
    public Vector3 myVector;
}
```
&nbsp;
 - The second class is the scriptable object which will contain a list of instances of the previous class and will be automatically generated. 
It must have the attribute FbxAttribute(string filenameFilter) and inherit from FbxScriptableObject<MyFbxMeshAttributes>.

```csharp
[FbxAttribute("Tile", suffix = "_Attributes")]
public class MyFbxObject : FbxScriptableObject<MyFbxMeshAttributes>
{
}
```
The filenameFilter is used to indicate which objects should have a postImport on them. Only objects containing the filter will be processed. You can use regex functions.
  
The suffix is optional, it is used to add a suffix to the generated file, this can easily avoid clashes.
