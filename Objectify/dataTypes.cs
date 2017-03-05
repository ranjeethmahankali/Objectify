using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Parameters;
//using Newtonsoft.Json;
using Grasshopper.Kernel.Types;
//using Grasshopper.Kernel.Special;

//my custom datatype
public class geomObject
{
    //constructors
    public geomObject(string nameStr, Dictionary<string, GH_GeometryGroup> geomDictionary)
    {
        name = nameStr;
        data = geomDictionary;
        number = new Dictionary<string, List<double>>();
        text = new Dictionary<string, List<string>>();
        vector = new Dictionary<string, List<GH_Vector>>();
        Visibility = new Dictionary<string, bool>();
        Bakability = new Dictionary<string, bool>();
    }
    public geomObject(string nameStr)
    {
        name = nameStr;
        data = new Dictionary<string, GH_GeometryGroup>();
        number = new Dictionary<string, List<double>>();
        text = new Dictionary<string, List<string>>();
        vector = new Dictionary<string, List<GH_Vector>>();
        Visibility = new Dictionary<string, bool>();
        Bakability = new Dictionary<string, bool>();
    }
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

    //object properties
    public string name;
    public Dictionary<string, GH_GeometryGroup> data;
    public Dictionary<string, List<double>> number;
    public Dictionary<string, List<string>> text;
    public Dictionary<string, List<GH_Vector>> vector;
    public int dataCount { get { return (this.data.Count + this.number.Count + this.text.Count); } }
    public Dictionary<string, bool> Visibility;
    public Dictionary<string, bool> Bakability;

    //this function gets all the geometry as a group (nested if the members are already groups themselves))
    public GH_GeometryGroup getGeometryGroup(string set = "all")
    {
        //if set is visible then only visible geometry is included
        //if set is bakable then only bakable geometry is included
        //if set is both then both visible and bakable are included
        //if set is not provided, the default value will include all geometry
        GH_GeometryGroup geoGrp = new GH_GeometryGroup();
        foreach (string key in data.Keys)
        {
            if (set == "visible" && (!Visibility[key])) { continue; }
            if (set == "bakable" && (!Bakability[key])) { continue; }
            if (set == "both" && (!Visibility[key]) && (!Bakability[key])) { continue;}
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
        string output = name + " object with " + data.Count.ToString() + " members:{";
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
        geomObject xObj = new geomObject(this.name);
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
        geomObject mObj = new geomObject(this.name);
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
        geomObject dObj = new geomObject(this.name);
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
    //I have to implement these properties for using the GH_Geometric_Goo class
    public override BoundingBox Boundingbox
    {
        get
        {
            if (this.Value == null) { return BoundingBox.Empty; }
            GH_GeometryGroup geoGrp = this.Value.getGeometryGroup();
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
        return this.Value.getGeometryGroup().GetBoundingBox(xform);
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
        this.Value.getGeometryGroup().DrawViewportMeshes(args);
    }
    public void DrawViewportWires(GH_PreviewWireArgs args)
    {
        this.Value.getGeometryGroup().DrawViewportWires(args);
    }

    //these are for casting my datatype into others
    public override bool CastTo<Q>(out Q target)
    {
        //whatever Q is, if a GH_GeometryGroup can be cast into it, then we are happy
        //we use GH_GeometryGroup as out mediator type
        if (typeof(Q).IsAssignableFrom(typeof(GH_GeometryGroup)))
        {
            target = (Q)(Object)this.Value.getGeometryGroup();
            //casting was successful
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
        return this.Value.getGeometryGroup().BakeGeometry(doc, att, ref obj_guid);
    }
}