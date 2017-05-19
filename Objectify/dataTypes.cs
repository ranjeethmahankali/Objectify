using System;
using System.Collections.Generic;
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

//my custom datatype
public class geomObject
{
    //constructors
    public geomObject()
    {
        name = "None";//this is the default value for name
        data = new Dictionary<string, GH_GeometryGroup>();//this is the default empty dictionary
        number = new Dictionary<string, List<double>>();
        text = new Dictionary<string, List<string>>();
        vector = new Dictionary<string, List<GH_Vector>>();
        Visibility = new Dictionary<string, bool>();
        Bakability = new Dictionary<string, bool>();
    }
    public geomObject(string nameStr) : this()
    {
        name = nameStr;
    }
    public geomObject(string nameStr, Dictionary<string, GH_GeometryGroup> geomDictionary):this(nameStr)
    {
        data = geomDictionary;
    }
    //this constructor is for the cases where this object has to be reconstructed from
    //a dictionary with json strings of all its fields
    public geomObject(Dictionary<string, string> jsonDict)
    {
        name = jsonDict["name"];//this is the default value for name
        data = JsonConvert.DeserializeObject<Dictionary<string, GH_GeometryGroup>>(jsonDict["data"]);
        number = JsonConvert.DeserializeObject<Dictionary<string, List<double>>>(jsonDict["number"]);
        text = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(jsonDict["text"]);
        vector = JsonConvert.DeserializeObject<Dictionary<string, List<GH_Vector>>>(jsonDict["vector"]);
        
        //now getting the visibility and bakability settings
        Visibility = JsonConvert.DeserializeObject<Dictionary<string, bool>>(jsonDict["Visibility"]);
        Bakability = JsonConvert.DeserializeObject<Dictionary<string, bool>>(jsonDict["Bakability"]);
    }
    
    //object properties
    public string name;
    public Dictionary<string, GH_GeometryGroup> data;
    public Dictionary<string, List<double>> number;
    public Dictionary<string, List<string>> text;
    public Dictionary<string, List<GH_Vector>> vector;
    public int dataCount { get { return (this.data.Count + this.number.Count + this.text.Count + this.vector.Count); } }
    public Dictionary<string, bool> Visibility;
    public Dictionary<string, bool> Bakability;

    //this copies the object - all data except geometry unless the bool is true
    public geomObject fresh(bool withGeom = false)
    {
        //have to manually recreate the object to avoid references and create a true deep copy
        geomObject nObj = new geomObject(this.name);

        if (withGeom)
        {
            foreach (string key in this.data.Keys) { nObj.data.Add(key, this.data[key]); }
        }

        foreach (string key in this.number.Keys) { nObj.number.Add(key, this.number[key]); }
        foreach (string key in this.text.Keys) { nObj.text.Add(key, this.text[key]); }
        foreach (string key in this.vector.Keys) { nObj.vector.Add(key, this.vector[key]); }

        foreach (string key in this.Visibility.Keys) { nObj.Visibility.Add(key, this.Visibility[key]); }
        foreach (string key in this.Bakability.Keys) { nObj.Bakability.Add(key, this.Bakability[key]); }

        return nObj;
    }
    //returns the type of any key (member name) that is passed as param
    public Type getTypeOf(string key)
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
    public void removeMember(string key)
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
    public bool hasMember(string key)
    {
        return (
            this.data.ContainsKey(key) || 
            this.number.ContainsKey(key)|| 
            this.text.ContainsKey(key) || 
            this.vector.ContainsKey(key)
            );
    }

    //this function gets all the geometry as a group (nested if the members are already groups themselves))
    public GH_GeometryGroup getGeometryGroup(string filter = "all")
    {
        //return rmpty group if the filter is invalid
        List<string> validFilters = new List<string>(new string[] {"visible","bakable","both","all"});
        if (!validFilters.Contains(filter)) { return new GH_GeometryGroup(); }

        GH_GeometryGroup geoGrp = new GH_GeometryGroup();
        foreach (string key in data.Keys)
        {
            if(!this.Visibility.ContainsKey(key) || !this.Bakability.ContainsKey(key)) {continue;}
            if(filter == "visible" && !this.Visibility[key]) {continue;}
            if(filter == "bakable" && !this.Bakability[key]) { continue; }
            if(filter == "both" && (!this.Bakability[key] && !this.Visibility[key])) { continue; }
            
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
        string output = name + " object with " + this.dataCount.ToString() + " members:{";
        int counter = 0;
        //all the geometry members
        foreach (string key in data.Keys)
        {
            if (counter != 0){output += ", ";}
            output += key;
            counter++;
        }
        //all the numbers
        foreach (string key in number.Keys)
        {
            if (counter != 0){output += ", ";}
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
        foreach(string key in vector.Keys)
        {
            if (counter != 0) { output += ", "; }
            output += key;
            counter++;
        }

        output += "}";

        return output;
    }

    //all kinds of operations done on this have to be defined
    //this is for transform
    public geomObject Transform(Transform xform)
    {
        geomObject xObj = fresh();
        foreach (string key in data.Keys)
        {
            GH_GeometryGroup xGeom = (GH_GeometryGroup)data[key].Transform(xform);
            xObj.data.Add(key, xGeom);
        }

        return xObj;
    }
    //this is for morphing - dont even know what it is, but I dont have to
    public geomObject Morph(SpaceMorph morph)
    {
        geomObject mObj = fresh();
        foreach (string key in data.Keys)
        {
            GH_GeometryGroup mGeom = (GH_GeometryGroup)data[key].Morph(morph);
            mObj.data.Add(key, mGeom);
        }

        return mObj;
    }
    //this is for duplication
    public geomObject DuplicateGeometry()
    {
        geomObject dObj = fresh();
        foreach (string key in data.Keys)
        {
            GH_GeometryGroup dGeom = (GH_GeometryGroup)data[key].DuplicateGeometry();
            dObj.data.Add(key, dGeom);
        }

        return dObj;
    }
}

//GeometricGoo wrapper
public class geomObjGoo : GH_GeometricGoo<geomObject>, IGH_PreviewData, IGH_BakeAwareData
{
    //all the different constructors
    public geomObjGoo()
    {
        this.Value = new geomObject();
    }
    public geomObjGoo(geomObject obj)
    {
        if (obj == null) { obj = new geomObject(); }
        this.Value = obj;
    }
    public geomObjGoo(Dictionary<string, string> jsonStr)
    {
        geomObject obj;
        if (jsonStr == null) { obj = new geomObject(); }
        else{obj = new geomObject(jsonStr);}

        this.Value = obj;
    }
    //I have to implement these properties for using the GH_Geometric_Goo class
    public override BoundingBox Boundingbox
    {
        get
        {
            if (this.Value == null) { return BoundingBox.Empty; }
            GH_GeometryGroup geoGrp = this.Value.getGeometryGroup("bakable");
            if (geoGrp == null) { return BoundingBox.Empty; }
            return geoGrp.Boundingbox;
        }
    }
    public override string TypeDescription { get { return "This is the Main data type used by the Objectify Component. It inherits the from GH_GeometricGoo"; } }
    public override string TypeName { get { return "Geometry Object"; } }

    //these are my implementations / overrides for the geometric operations
    //this is the bounding box
    public override BoundingBox GetBoundingBox(Transform xform)
    {
        return this.Value.getGeometryGroup("bakable").GetBoundingBox(xform);
    }
    //this is the transformations
    public override IGH_GeometricGoo Transform(Transform xform)
    {
        return new geomObjGoo(this.Value.Transform(xform));
    }
    //this is the morph function
    public override IGH_GeometricGoo Morph(SpaceMorph morph)
    {
        return new geomObjGoo(this.Value.Morph(morph));
    }
    //this is for duplication
    public override IGH_GeometricGoo DuplicateGeometry()
    {
        return new geomObjGoo(this.Value.DuplicateGeometry());
    }
    //this is for all printing purposes
    public override string ToString()
    {
        return this.Value.ToString();
    }

    //these are all the implementations for using IGH_previewData class - to tell GH how to show my object in the viewport
    //this is the clippng box
    public BoundingBox ClippingBox
    {
        get { return Boundingbox; }
    }
    public void DrawViewportMeshes(GH_PreviewMeshArgs args)
    {
        this.Value.getGeometryGroup("visible").DrawViewportMeshes(args);
    }
    public void DrawViewportWires(GH_PreviewWireArgs args)
    {
        this.Value.getGeometryGroup("visible").DrawViewportWires(args);
    }

    //these are for casting my datatype into others
    public override bool CastTo<Q>(out Q target)
    {
        //whatever Q is, if a GH_GeometryGroup can be cast into it, then we are happy
        //we use GH_GeometryGroup as out mediator type
        if (typeof(Q).IsAssignableFrom(typeof(GH_GeometryGroup)))
        {
            target = (Q)(Object)this.Value.getGeometryGroup("both");
            //casting was successful
            return true;
        }
        else if (typeof(Q).IsAssignableFrom(typeof(Dictionary<string, string>)))
        {
            Dictionary<string, string> castDict = new Dictionary<string, string>();
            castDict.Add("name", this.Value.name);//the name of the object

            //now adding data, numbers, vectors and text
            castDict.Add("data", JsonConvert.SerializeObject(this.Value.data));
            castDict.Add("number", JsonConvert.SerializeObject(this.Value.number));
            castDict.Add("text", JsonConvert.SerializeObject(this.Value.text));
            castDict.Add("vector", JsonConvert.SerializeObject(this.Value.vector));

            //now adding visibility and bakability settings
            castDict.Add("Visibility", JsonConvert.SerializeObject(this.Value.Visibility));
            castDict.Add("Bakability", JsonConvert.SerializeObject(this.Value.Bakability));

            target = (Q)(Object)castDict;
            return true;
        }
        else
        {
            //we cannot cast it so we give up
            target = default(Q);
            return false;
        }
    }

    //this is when user decides to bake our object using the interface BakeAwareData
    public bool BakeGeometry(Rhino.RhinoDoc doc, Rhino.DocObjects.ObjectAttributes att, out Guid obj_guid)
    {
        obj_guid = new Guid();
        return this.Value.getGeometryGroup("bakable").BakeGeometry(doc, att, ref obj_guid);
    }
}