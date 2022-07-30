using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Autodesk.Fbx;

public class FbxPostProcessor : AssetPostprocessor
{
    private static Dictionary<FbxAttributeAttribute, Type> autoPostProcess;
    private const string extension = ".asset";

    private void OnPostprocessModel(GameObject go)
    {
        //Work only on FBX files
        if (!(assetPath.EndsWith(".fbx") || assetPath.EndsWith(".FBX")))
            return;


        //Lists all classes inheriting from FbxScriptableObject and implementing the FbxAttribute attribute
        if (autoPostProcess == null || autoPostProcess.Count == 0)
            FillAutoPostProcessDictionary();


        //Check if a FbxScriptableObject supports the FBX file
        Match match;
        foreach (var item in autoPostProcess)
        {
            match = Regex.Match(go.name, item.Key.filenameFilter);
            if (match.Success)
            {
                string attributesFilePath = GetAttributeFilePath(assetPath, item.Key.suffix);
                EditorApplication.delayCall += () => GenerateAttributeFile(assetPath, attributesFilePath, item.Value);
            }
        }
    }

    /// <summary>
    /// Generates a file of type fbxObjectType at the location attributesFilePath for the file fbx at the location fbxFilePath.
    /// </summary>
    public static void GenerateAttributeFile(string fbxFilePath, string attributesFilePath, Type fbxObjectType)
    {
        GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(fbxFilePath);

        //Get or create attribute file
        ScriptableObject target;
        if (File.Exists(attributesFilePath))
        {
            target = AssetDatabase.LoadAssetAtPath<ScriptableObject>(attributesFilePath);
            Type fbxScriptableObjectDefinition = typeof(FbxScriptableObject<IMeshAttributes>).GetGenericTypeDefinition();

            //Another file type already exists at this location.
            if (target == null || !IsSubclassOfRawGeneric(target.GetType(), fbxScriptableObjectDefinition))
                throw new Exception($"A file of different type already exists on the path:\n\"{attributesFilePath}\"\nUnable to generate an attribute file for the model: \"{go.name}\".");
        }
        else
        {
            target = ScriptableObject.CreateInstance(fbxObjectType);
            AssetDatabase.CreateAsset(target, attributesFilePath);
        }

        //Assign source gameObject
        target.GetType().GetField("source").SetValue(target, go);

        //Create a list of IMeshAttributes and complete the attributes in it.
        Type genType = GetGenericTypeOf(target.GetType());
        var list = createList(genType);
        ReadAttributes(go, fbxFilePath, genType, list);

        //Apply the result to the scriptable object and re-import the file.
        FieldInfo info = target.GetType().GetField("childs", BindingFlags.Instance | BindingFlags.Public);
        info.SetValue(target, list);
        EditorUtility.SetDirty(target);
        AssetDatabase.ImportAsset(attributesFilePath, ImportAssetOptions.ForceUpdate);

        //Calls a callback that could be useful.
        target.GetType().GetMethod("OnPostImport", BindingFlags.Public | BindingFlags.Instance).Invoke(target, null);
    }


    /// <summary>
    /// Generate the path of the attribute file based on the path of the fbx file
    /// </summary>
    private string GetAttributeFilePath(string fbxPath, string suffix)
    {
        string directory = Path.GetDirectoryName(fbxPath);
        string name = Path.GetFileNameWithoutExtension(fbxPath);
        return $"{directory}\\{name}{suffix}{extension}";
    }


    /// <summary>
    /// Adds an instance inheriting from IMeshAttributes to the list for each mesh found.
    /// </summary>
    public static void ReadAttributes(GameObject go, string path, Type type, IList list)
    {
        //Init data holders
        Dictionary<string, (Mesh, string[])> meshes = new Dictionary<string, (Mesh, string[])>();


        //Load meshes
        MeshFilter[] filters = go.GetComponentsInChildren<MeshFilter>();
        for (int i = 0; i < filters.Length; i++)
        {
            MeshRenderer rend = filters[i].GetComponent<MeshRenderer>();
            string[] materials = new string[rend.sharedMaterials.Length];
            for (int j = 0; j < materials.Length; j++)
                materials[j] = rend.sharedMaterials[j].name;

            if (filters[i].sharedMesh != null)
            {
                meshes.Add(filters[i].sharedMesh.name, (filters[i].sharedMesh, materials));
            }
        }


        //Import FBX file
        using (FbxManager fbxManager = FbxManager.Create())
        {
            //Get the fields exposed in the class inheriting from IMeshAttributes
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);


            fbxManager.SetIOSettings(FbxIOSettings.Create(fbxManager, Globals.IOSROOT));
            using (FbxImporter importer = FbxImporter.Create(fbxManager, "importer"))
            {
                //Import scene
                bool status = importer.Initialize(path, -1, fbxManager.GetIOSettings());
                FbxScene scene = FbxScene.Create(fbxManager, "scene");
                importer.Import(scene);

                //Recursively gets all attributes 
                GetAttributes(scene.GetRootNode());
                void GetAttributes(FbxNode node)
                {
                    //bool valid = false;
                    IMeshAttributes att = (IMeshAttributes) Activator.CreateInstance(type);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        FbxProperty prop = node.FindPropertyHierarchical(fields[i].Name);
                        if (prop.IsValid())
                        {
                            Type type = fields[i].FieldType;

                            if (type == typeof(bool))
                            {
                                fields[i].SetValue(att, prop.GetBool());
                            }
                            else if (type == typeof(int))
                            {
                                fields[i].SetValue(att, prop.GetInt());
                            }
                            else if (type == typeof(string))
                            {
                                fields[i].SetValue(att, prop.GetString());
                            }
                            else if (type == typeof(float))
                            {
                                if (prop.GetType() == typeof(double))
                                    fields[i].SetValue(att, (float)prop.GetDouble());
                                else
                                    fields[i].SetValue(att, prop.GetFloat());
                            }
                            else if (type == typeof(Color))
                            {
                                FbxColor c = prop.GetFbxColor();
                                fields[i].SetValue(att, new Color((float)c.mRed, (float)c.mGreen, (float)c.mBlue, (float)c.mAlpha));
                            }
                            else if (type == typeof(Vector3))
                            {
                                FbxDouble3 c = prop.GetFbxDouble3();
                                fields[i].SetValue(att, new Vector3((float)c.X, (float)c.Y, (float)c.Z));
                            }
                            else if (type.BaseType == typeof(Enum))
                            {
                                fields[i].SetValue(att, prop.GetInt());
                            }
                        }
                    }

                    string name = node.GetName();
                    if (meshes.ContainsKey(name))
                    {
                        att.mesh = meshes[name].Item1;
                        att.materials = meshes[name].Item2;
                        list.Add(att);
                    }

                    for (int i = 0; i < node.GetChildCount(); i++)
                        GetAttributes(node.GetChild(i));
                }
            }
        }
    }


    /// <summary>
    /// Create a dictionary indicating all the FbxScriptablObject that can be generated automatically when importing an FBX file.
    /// </summary>
    private static void FillAutoPostProcessDictionary()
    {
        autoPostProcess = new Dictionary<FbxAttributeAttribute, Type>();
        IEnumerable<(FbxAttributeAttribute attribute, Type type)> childs = 
            GetTypesWithAttribute(Assembly.GetAssembly(typeof(FbxAttributeAttribute))).ToArray();
        foreach (var item in childs)
            autoPostProcess.Add(item.attribute, item.type);
    }


    /// <summary>
    /// Gets all the classes that can be filled in automatically on import:
    /// Must inherit from class FbxScriptableObject<IMeshAttributes> and have attribute FbxAttribute(string filenameFilter).
    /// </summary>
    private static IEnumerable<(FbxAttributeAttribute, Type)> GetTypesWithAttribute(Assembly assembly)
    {
        Type fbxScriptableObjectDefinition = typeof(FbxScriptableObject<IMeshAttributes>).GetGenericTypeDefinition();

        foreach (Type type in assembly.GetTypes())
        {
            object[] attrs = type.GetCustomAttributes(typeof(FbxAttributeAttribute), true);
            foreach (var attr in attrs)
            {
                if (attr is FbxAttributeAttribute attribute)
                {
                    if (!string.IsNullOrEmpty(attribute.filenameFilter) && 
                        IsSubclassOfRawGeneric(type, fbxScriptableObjectDefinition))
                        yield return (attribute, type);
                }
            }
        }
    }


    /// <summary>
    /// Checks if a class inherits from the definition of a generic type.
    /// </summary>
    private static bool IsSubclassOfRawGeneric(Type type, Type genericTypeDefinition)
    {
        while (type != null && type != typeof(object) && type.IsClass)
        {
            Type cur = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            if (genericTypeDefinition == cur)
                return true;
            type = type.BaseType;
        }
        return false;
    }

    private static Type GetGenericTypeOf(Type type)
    {
        while (!type.IsGenericType && type != typeof(object))
            type = type.BaseType;

        if (!type.IsGenericType)
            return null;

        return type.GenericTypeArguments[0];
    }

    private static IList createList(Type myType)
    {
        Type genericListType = typeof(List<>).MakeGenericType(myType);
        return (IList)Activator.CreateInstance(genericListType);
    }
}