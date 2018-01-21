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

namespace Objectify
{
    //GeometricGoo wrapper
    public class GeomObjGoo : GH_GeometricGoo<GeomObject>, IGH_PreviewData, IGH_BakeAwareData
    {
        //all the different constructors
        public GeomObjGoo()
        {
            this.Value = new GeomObject();
        }
        public GeomObjGoo(GeomObject obj)
        {
            if (obj == null) { obj = new GeomObject(); }
            this.Value = obj;
        }
        public GeomObjGoo(Dictionary<string, string> jsonStr)
        {
            GeomObject obj;
            if (jsonStr == null) { obj = new GeomObject(); }
            else { obj = new GeomObject(jsonStr); }

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
            return new GeomObjGoo(this.Value.Transform(xform));
        }
        //this is the morph function
        public override IGH_GeometricGoo Morph(SpaceMorph morph)
        {
            return new GeomObjGoo(this.Value.Morph(morph));
        }
        //this is for duplication
        public override IGH_GeometricGoo DuplicateGeometry()
        {
            return new GeomObjGoo(this.Value.DuplicateGeometry());
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
                castDict.Add("data", GeomObject.SerializeToString(this.Value.data));
                castDict.Add("number", GeomObject.SerializeToString(this.Value.number));
                castDict.Add("text", GeomObject.SerializeToString(this.Value.text));
                castDict.Add("vector", GeomObject.SerializeToString(this.Value.vector));

                //now adding visibility and bakability settings
                castDict.Add("Visibility", GeomObject.SerializeToString(this.Value.Visibility));
                castDict.Add("Bakability", GeomObject.SerializeToString(this.Value.Bakability));

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
}
