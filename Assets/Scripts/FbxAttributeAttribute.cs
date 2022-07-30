using System;

/// <summary>
/// This attribute should be implemented on a class inheriting from the FbxScriptableObject<IMeshAttributes> class.
/// It allows to automatically generate a file containing the attributes of FBX files supporting the filter.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class FbxAttributeAttribute : Attribute
{
    /// <summary> 
    /// FBX files having a match with the filter will generate an instance of this class on import. 
    /// The filter support Regex
    /// </summary>
    public string filenameFilter;

    /// <summary> The name of the scriptableObject will be [The name of the file fbx][Suffix].asset </summary>
    public string suffix;

    private FbxAttributeAttribute() { }

    /// <summary>
    /// This attribute should be implemented on a class inheriting from the FbxScriptableObject<IMeshAttributes> class.
    /// It allows to automatically generate a file containing the attributes of FBX files supporting the filter.
    /// </summary>
    /// <param name="filenameFilter"> FBX files having a match with the filter will generate an instance of this class on import. The filter support Regex </param>
    public FbxAttributeAttribute(string filenameFilter)
    {
        this.filenameFilter = filenameFilter;
    }
}