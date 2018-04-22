using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
//using Grasshopper.Kernel.Parameters;
//using Newtonsoft.Json;
using Grasshopper.Kernel.Types;
//using Grasshopper.Kernel.Special;
//using System.Windows.Forms;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Xml.Serialization;
using System.IO;
using System.Text;

public enum GeometryFilter
{
    ALL,
    BAKABLE,
    VISIBLE,
    VISIBLE_AND_BAKABLE
}

//my custom datatype
public class GeomObject
{
    #region fields
    private string _name;
    private Dictionary<string, List<IGH_Goo>> _memberDict = new Dictionary<string, List<IGH_Goo>>();
    private Dictionary<string, bool> _visibility;
    private Dictionary<string, bool> _bakability;
    #endregion
    #region properties
    public string Name
    {
        get { return _name; }
        set { _name = value; }
    }
    //public int DataCount { get { return (this._data.Count + this.number.Count + this.text.Count + this.vector.Count); } }
    public Dictionary<string, List<IGH_Goo>> MemberDict { get { return _memberDict; } }
    public Dictionary<string, bool> Visibility { get { return _visibility; } }
    public Dictionary<string, bool> Bakability { get { return _bakability; } }
    #endregion
    #region constructors
    public GeomObject()
    {
        _name = "None";//this is the default value for name
        _visibility = new Dictionary<string, bool>();
        _bakability = new Dictionary<string, bool>();
    }
    public GeomObject(string nameStr) : this()
    {
        _name = nameStr;
    }
    public GeomObject(string nameStr, Dictionary<string, List<IGH_Goo>> dataDict) : this(nameStr)
    {
        _memberDict = dataDict;
    }
    //this constructor is for the cases where this object has to be reconstructed from
    //a dictionary with json strings of all its fields
    public GeomObject(Dictionary<string, string> xmlDict)
    {
        _name = xmlDict["name"];//this is the default value for name
        _memberDict = DeserializeFromString<Dictionary<string, List<IGH_Goo>>>(xmlDict["data"]);

        //now getting the visibility and bakability settings
        _visibility = DeserializeFromString<Dictionary<string, bool>>(xmlDict["Visibility"]);
        _bakability = DeserializeFromString<Dictionary<string, bool>>(xmlDict["Bakability"]);
        GH_GeometryGroup grp = new GH_GeometryGroup();
    }
    #endregion
    #region methods
    public static string SerializeToString(object obj)
    {
        XmlSerializer serializer = new XmlSerializer(obj.GetType());

        using (StringWriter writer = new StringWriter())
        {
            serializer.Serialize(writer, obj);

            return writer.ToString();
        }
    }
    public static T DeserializeFromString<T>(string xmlString)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(T));
        MemoryStream memStream = new MemoryStream(Encoding.UTF8.GetBytes(xmlString));
        T resultingMessage = (T)serializer.Deserialize(memStream);
        return resultingMessage;
    }

    //this copies the object - all data except geometry unless the bool is true
    public GeomObject Duplicate()
    {
        //have to manually recreate the object to avoid references and create a true deep copy
        GeomObject nObj = new GeomObject(this._name);

        foreach (string key in this._memberDict.Keys)
        {
            nObj._memberDict.Add(key, this._memberDict[key].Select((m) => m.Duplicate()).ToList());
        }
        foreach (string key in this._visibility.Keys) { nObj._visibility.Add(key, this._visibility[key]); }
        foreach (string key in this._bakability.Keys) { nObj._bakability.Add(key, this._bakability[key]); }

        return nObj;
    }
    //removes the data with that key from this object

    //this function gets all the geometry as a group (nested if the members are already groups themselves))
    public GH_GeometryGroup GetGeometryGroup(GeometryFilter filter = GeometryFilter.ALL)
    {
        GH_GeometryGroup geoGrp = new GH_GeometryGroup();
        foreach (string key in _memberDict.Keys)
        {
            if (!this._visibility.ContainsKey(key) || !this._bakability.ContainsKey(key)) { continue; }
            if (filter == GeometryFilter.VISIBLE && !this._visibility[key]) { continue; }
            if (filter == GeometryFilter.BAKABLE && !this._bakability[key]) { continue; }
            if (filter == GeometryFilter.VISIBLE_AND_BAKABLE && (!this._bakability[key] && !this._visibility[key])) { continue; }

            GH_GeometryGroup subGrp = new GH_GeometryGroup();
            foreach(var goo in _memberDict[key])
            {
                if (!typeof(IGH_GeometricGoo).IsAssignableFrom(goo.GetType())) { continue; }
                subGrp.Objects.Add((IGH_GeometricGoo)goo);
            }
            geoGrp.Objects.Add(subGrp);
        }
        return geoGrp;
    }

    //this is for when the user prints the object onto a panel
    public override string ToString()
    {
        string output = _name + "(object) with " + this.MemberDict.Count.ToString() + " members:{";
        int counter = 0;
        //all the members
        foreach (string key in _memberDict.Keys)
        {
            if (counter != 0) { output += ", "; }
            output += key;
            counter++;
        }

        output += "}";
        return output;
    }
    //this is for transform
    public GeomObject Transform(Transform xform)
    {
        GeomObject xObj = Duplicate();
        foreach (string key in xObj.MemberDict.Keys)
        {
            List<IGH_Goo> newData = new List<IGH_Goo>();
            for(int i = 0; i < xObj.MemberDict[key].Count; i++)
            {
                if (typeof(IGH_GeometricGoo).IsAssignableFrom(xObj.MemberDict[key][i].GetType()))
                {
                    IGH_GeometricGoo geom = (IGH_GeometricGoo)xObj.MemberDict[key][i];
                    xObj.MemberDict[key][i] = geom.Transform(xform);
                }
            }
        }

        return xObj;
    }
    //this is for morphing - dont even know what it is, but I dont have to
    public GeomObject Morph(SpaceMorph morph)
    {
        GeomObject mObj = Duplicate();
        foreach (string key in mObj.MemberDict.Keys)
        {
            List<IGH_Goo> newData = new List<IGH_Goo>();
            for (int i = 0; i < mObj.MemberDict[key].Count; i++)
            {
                if (typeof(IGH_GeometricGoo).IsAssignableFrom(mObj.MemberDict[key][i].GetType()))
                {
                    IGH_GeometricGoo geom = (IGH_GeometricGoo)mObj.MemberDict[key][i];
                    mObj.MemberDict[key][i] = geom.Morph(morph);
                }
            }
        }

        return mObj;
    }
    //this is for duplication
    public GeomObject DuplicateGeometry()
    {
        GeomObject dObj = Duplicate();
        foreach (string key in dObj.MemberDict.Keys)
        {
            List<IGH_Goo> newData = new List<IGH_Goo>();
            for (int i = 0; i < dObj.MemberDict[key].Count; i++)
            {
                if (typeof(IGH_GeometricGoo).IsAssignableFrom(dObj.MemberDict[key][i].GetType()))
                {
                    IGH_GeometricGoo geom = (IGH_GeometricGoo)dObj.MemberDict[key][i];
                    dObj.MemberDict[key][i] = geom.DuplicateGeometry();
                }
            }
        }

        return dObj;
    }
    #endregion    
}