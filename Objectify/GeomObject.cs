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
    BAKE,
    VISIBLE,
    VISIBLE_AND_BAKABLE
}

//my custom datatype
public class GeomObject
{
    #region fields
    public string name;
    public Dictionary<string, GH_GeometryGroup> data;
    public Dictionary<string, List<double>> number;
    public Dictionary<string, List<string>> text;
    public Dictionary<string, List<GH_Vector>> vector;
    public Dictionary<string, bool> _visibility;
    public Dictionary<string, bool> _bakability;
    #endregion
    #region properties
    public int DataCount { get { return (this.data.Count + this.number.Count + this.text.Count + this.vector.Count); } }
    #endregion
    #region constructors
    public GeomObject()
    {
        name = "None";//this is the default value for name
        data = new Dictionary<string, GH_GeometryGroup>();//this is the default empty dictionary
        number = new Dictionary<string, List<double>>();
        text = new Dictionary<string, List<string>>();
        vector = new Dictionary<string, List<GH_Vector>>();
        _visibility = new Dictionary<string, bool>();
        _bakability = new Dictionary<string, bool>();
    }
    public GeomObject(string nameStr) : this()
    {
        name = nameStr;
    }
    public GeomObject(string nameStr, Dictionary<string, GH_GeometryGroup> geomDictionary) : this(nameStr)
    {
        data = geomDictionary;
    }
    //this constructor is for the cases where this object has to be reconstructed from
    //a dictionary with json strings of all its fields
    public GeomObject(Dictionary<string, string> xmlDict)
    {
        name = xmlDict["name"];//this is the default value for name
        data = DeserializeFromString<Dictionary<string, GH_GeometryGroup>>(xmlDict["data"]);
        number = DeserializeFromString<Dictionary<string, List<double>>>(xmlDict["number"]);
        text = DeserializeFromString<Dictionary<string, List<string>>>(xmlDict["text"]);
        vector = DeserializeFromString<Dictionary<string, List<GH_Vector>>>(xmlDict["vector"]);

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
    public GeomObject Fresh(bool withGeom = false)
    {
        //have to manually recreate the object to avoid references and create a true deep copy
        GeomObject nObj = new GeomObject(this.name);

        if (withGeom)
        {
            foreach (string key in this.data.Keys) { nObj.data.Add(key, this.data[key]); }
        }

        foreach (string key in this.number.Keys) { nObj.number.Add(key, this.number[key]); }
        foreach (string key in this.text.Keys) { nObj.text.Add(key, this.text[key]); }
        foreach (string key in this.vector.Keys) { nObj.vector.Add(key, this.vector[key]); }

        foreach (string key in this._visibility.Keys) { nObj._visibility.Add(key, this._visibility[key]); }
        foreach (string key in this._bakability.Keys) { nObj._bakability.Add(key, this._bakability[key]); }

        return nObj;
    }
    //returns the type of any key (member name) that is passed as param
    public Type GetTypeOf(string key)
    {
        Type listType = null;
        if (this.data.ContainsKey(key))
        {
            Type[] args = this.data.GetType().GetGenericArguments();
            return args[1];
        }
        else if (this.number.ContainsKey(key))
        {
            Type[] args = this.number.GetType().GetGenericArguments();
            listType = args[1];
        }
        else if (this.text.ContainsKey(key))
        {
            Type[] args = this.text.GetType().GetGenericArguments();
            listType = args[1];
        }
        else if (this.vector.ContainsKey(key))
        {
            Type[] args = this.vector.GetType().GetGenericArguments();
            listType = args[1];
        }
        else
        {
            return null;
        }

        //Debug.WriteLine(listType.ToString());
        Type itemType = listType.GetGenericArguments().Single();

        return itemType;
    }
    //removes the data with that key from this object
    public void RemoveMember(string key)
    {
        if (this.data.ContainsKey(key))
        {
            this.data.Remove(key);
            return;
        }
        else if (this.number.ContainsKey(key))
        {
            this.number.Remove(key);
            return;
        }
        else if (this.text.ContainsKey(key))
        {
            this.text.Remove(key);
            return;
        }
        else if (this.vector.ContainsKey(key))
        {
            this.vector.Remove(key);
            return;
        }
    }
    //returns whether this object has a member with that name
    public bool HasMember(string key)
    {
        return (
            this.data.ContainsKey(key) ||
            this.number.ContainsKey(key) ||
            this.text.ContainsKey(key) ||
            this.vector.ContainsKey(key)
            );
    }

    //this function gets all the geometry as a group (nested if the members are already groups themselves))
    public GH_GeometryGroup GetGeometryGroup(string filter = "all")
    {
        //return rmpty group if the filter is invalid
        List<string> validFilters = new List<string>(new string[] { "visible", "bakable", "both", "all" });
        if (!validFilters.Contains(filter)) { return new GH_GeometryGroup(); }

        GH_GeometryGroup geoGrp = new GH_GeometryGroup();
        foreach (string key in data.Keys)
        {
            if (!this._visibility.ContainsKey(key) || !this._bakability.ContainsKey(key)) { continue; }
            if (filter == "visible" && !this._visibility[key]) { continue; }
            if (filter == "bakable" && !this._bakability[key]) { continue; }
            if (filter == "both" && (!this._bakability[key] && !this._visibility[key])) { continue; }

            GH_GeometryGroup subGrp = new GH_GeometryGroup();
            for (int i = 0; i < data[key].Objects.Count; i++)
            {
                subGrp.Objects.Add(data[key].Objects[i]);
            }
            geoGrp.Objects.Add(subGrp);
        }
        return geoGrp;
    }

    //this is for when the user prints the object onto a panel
    public override string ToString()
    {
        string output = name + " object with " + this.DataCount.ToString() + " members:{";
        int counter = 0;
        //all the geometry members
        foreach (string key in data.Keys)
        {
            if (counter != 0) { output += ", "; }
            output += key;
            counter++;
        }
        //all the numbers
        foreach (string key in number.Keys)
        {
            if (counter != 0) { output += ", "; }
            output += key;
            counter++;
        }
        //all the text
        foreach (string key in text.Keys)
        {
            if (counter != 0) { output += ", "; }
            output += key;
            counter++;
        }
        //all the vectors
        foreach (string key in vector.Keys)
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
        GeomObject xObj = Fresh();
        foreach (string key in data.Keys)
        {
            GH_GeometryGroup xGeom = (GH_GeometryGroup)data[key].Transform(xform);
            xObj.data.Add(key, xGeom);
        }

        return xObj;
    }
    //this is for morphing - dont even know what it is, but I dont have to
    public GeomObject Morph(SpaceMorph morph)
    {
        GeomObject mObj = Fresh();
        foreach (string key in data.Keys)
        {
            GH_GeometryGroup mGeom = (GH_GeometryGroup)data[key].Morph(morph);
            mObj.data.Add(key, mGeom);
        }

        return mObj;
    }
    //this is for duplication
    public GeomObject DuplicateGeometry()
    {
        GeomObject dObj = Fresh();
        foreach (string key in data.Keys)
        {
            GH_GeometryGroup dGeom = (GH_GeometryGroup)data[key].DuplicateGeometry();
            dObj.data.Add(key, dGeom);
        }

        return dObj;
    }
    #endregion    
}