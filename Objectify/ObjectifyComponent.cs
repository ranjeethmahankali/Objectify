﻿using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Parameters;
//using Newtonsoft.Json;
using Grasshopper.Kernel.Types;
//using Grasshopper.Kernel.Special;
using System.Windows.Forms;

namespace Objectify
{
    //my custom datatype
    public class geomObject
    {
        //constructors
        public geomObject(string nameStr, Dictionary<string, GH_GeometryGroup> geomDictionary)
        {
            name = nameStr;
            data = geomDictionary;
        }
        public geomObject(string nameStr)
        {
            name = nameStr;
            data = new Dictionary<string, GH_GeometryGroup>();
        }
        public geomObject()
        {
            name = "None";//this is the default value for name
            data = new Dictionary<string, GH_GeometryGroup>();//this is the default empty dictionary
        }

        //object properties
        public string name;
        public Dictionary<string, GH_GeometryGroup> data;
        
        //this function gets all the geometry as a group (nested if the members are already groups themselves))
        public GH_GeometryGroup getGeometryGroup()
        {
            GH_GeometryGroup geoGrp = new GH_GeometryGroup();
            foreach (string key in data.Keys)
            {
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
            foreach (string key in data.Keys)
            {
                if (counter != 0)
                {
                    output += ", ";
                }
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
            foreach(string key in data.Keys)
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
            if(obj == null) { obj = new geomObject(); }
            this.Value = obj;
        }
        //I have to implement these properties for using the GH_Geometric_Goo class
        public override BoundingBox Boundingbox
        {
            get
            {
                if(this.Value == null) { return BoundingBox.Empty; }
                GH_GeometryGroup geoGrp = this.Value.getGeometryGroup();
                if (geoGrp == null) { return BoundingBox.Empty; }
                return geoGrp.Boundingbox;
            }
        }
        public override string TypeDescription { get { return "This is the Main data type used by the Objectify Component. It inherits the from GH_GeometricGoo"; } }
        public override string TypeName{get{return "Geometry Object";}}

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
    
    //main component
    public class ObjectifyComponent : GH_Component, IGH_VariableParameterComponent
    {
        //constructor
        public ObjectifyComponent()
          : base("Objectify", "Object",
              "Creates an object out of the inpupt geometry",
              "Data", "Objectify")
        {
            obj = new geomObject(this.NickName);
            Params.ParameterChanged += new GH_ComponentParamServer.ParameterChangedEventHandler(OnParameterChanged);
        }

        //this is the geomObject which will hold the data being outputted by this component
        private geomObject obj{get; set;}
        
        /// Registers all the input parameters for this component.
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geometry", "Label_1", "Geometry for this label", GH_ParamAccess.list);
            Params.Input[0].DataMapping = GH_DataMapping.Flatten;
        }
        
        /// Registers all the output parameters for this component.
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Object", "O", "Object", GH_ParamAccess.item);
            //pManager.AddGeometryParameter("Geometry", "G", "This is the geometry in the object", GH_ParamAccess.item);
            //pManager.AddTextParameter("Debugging Data", "D", "Debug", GH_ParamAccess.item);
        }

        //this function forces GH to recompute the component - bound to change events
        protected virtual void OnParameterChanged(object sender, GH_ParamServerEventArgs e)
        {
            ExpireSolution(true);
        }

        //this makes the decision of whether to allow the user to insert parameters or not
        public bool CanInsertParameter(GH_ParameterSide side, int index)
        {
            if (side == GH_ParameterSide.Input)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //this makes the decision of whether to allow the user to remove parameters or not
        public bool CanRemoveParameter(GH_ParameterSide side, int index)
        {
            if (side == GH_ParameterSide.Input && Params.Input.Count > 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //The parameter returned by this func is what is added to the component when user creates a component
        public IGH_Param CreateParameter(GH_ParameterSide side, int index  )
        {
            int paramNum = Params.Input.Count + 1;
            Param_Geometry param = new Param_Geometry();
            param.Name = "Geometry for this label";
            param.NickName = "Label_"+paramNum;
            param.DataMapping = GH_DataMapping.Flatten;
            param.Access = GH_ParamAccess.list;

            return param;
        }
        
        //this is same as remove parameter though I am not sure
        public bool DestroyParameter(GH_ParameterSide side, int index)
        {
            if(side == GH_ParameterSide.Input && Params.Input.Count > 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //this is any maintenenance that needs to be done - not really sure what it is for but I was forced to implement it
        public void VariableParameterMaintenance()
        {
            //doc.NewSolution(false);
        }

        /// This is the method that actually does the work.
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            UpdateData(DA);
            // We should now validate the data and warn the user if invalid data is supplied.
            if (obj.data.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "There is no Geometry");
                //return;
            }

            // Finally assign the spiral to the output parameter.
            DA.SetData(0, new geomObjGoo(obj));
            //DA.SetDataList(1, obj.getGeometry());
        }

        //this updates the data to the obj of this instance
        private void UpdateData(IGH_DataAccess DA)
        {
            Dictionary<string, GH_GeometryGroup> dataDict = new Dictionary<string, GH_GeometryGroup>();
            for (int i = 0; i < Params.Input.Count; i++)
            {
                List<IGH_GeometricGoo> geom = new List<IGH_GeometricGoo>();
                if (DA.GetDataList<IGH_GeometricGoo>(i, geom))
                {
                    GH_GeometryGroup grp = new GH_GeometryGroup();
                    for (int j = 0; j < geom.Count; j++)
                    {
                        grp.Objects.Add(geom[j]);
                    }
                    dataDict.Add(Params.Input[i].NickName, grp);
                }
                //else {}
            }

            this.obj = new geomObject(this.NickName, dataDict);
        }

        //I have no idea what this is for - came with the template - find out later
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }
    
        //If you make this property return an image (loaded from a path) then that will be the component logo
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        //this is the guid of the component, once you publish the plugin u cannot change this
        public override Guid ComponentGuid
        {
            get { return new Guid("{1b99b61b-34e7-4912-955e-54fd914b4200}"); }
        }
    }

    //this class is the custom parameter class that shows options in the context menu
    public class memberSelect : GH_Param<geomObjGoo>
    {
        //constructors
        public memberSelect(string nickname):
            base("Member", nickname, "Member", "Data","Objectify", GH_ParamAccess.item)
        {
            this.options = new List<string>();
            this.MutableNickName = false;
        }
        public memberSelect(List<string> keys):
            base("Member", "Member", "Member", "Data", "Objectify", GH_ParamAccess.item)
        {
            this.options = keys;
            this.MutableNickName = false;
            this.NickName = options[0];
        }
        
        //properties - options to show in the context menu
        public List<string> options { get;}
        
        //overriding the options shown in the menu
        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            this.Menu_AppendDisconnectWires(menu);
            for (int i = 0; i < this.options.Count; i++)
            {
                Menu_AppendItem(menu, this.options[i], optionClickHandler);
            }
        }
        //this is what happens when the option is clicked
        private void optionClickHandler(Object sender,  EventArgs e)
        {
            //do sth when clicked
            this.NickName = sender.ToString();
            this.OnDisplayExpired(true);
            this.ExpireSolution(true);
        }
        
        //this updates the context menu to member names and then sets the current member to the first one if it is unset
        public void update(geomObject obj)
        {
            this.options.Clear();
            foreach(string key in obj.data.Keys)
            {
                this.options.Add(key);
            }
            if(this.NickName == "")
            {
                this.NickName = options[0];
            }
        }
        //this resets the parameter - clears the context menu options and sets the current member name to empty string
        public void reset()
        {
            this.options.Clear();
            this.NickName = "";
            this.OnDisplayExpired(true);
        }
        //this is the unique guid don't change this after the component is published
        public override Guid ComponentGuid
        {
            get { return new Guid("{36c82c59-bc1a-4e70-9215-553181d31ad3}"); }
        }
    }

    //this is the object reader
    public class ObjectMember : GH_Component
    {
        //constructor that calls the base class constructor
        public ObjectMember()
          : base("Object Member", "M",
              "Reads an Object",
              "Data", "Objectify")
        {
            Params.ParameterChanged += new GH_ComponentParamServer.ParameterChangedEventHandler(OnParameterChanged);
            mainParam.Optional = true;
        }
        
        //properties - this is the main parameter that I will add inside the registerinputparams function
        private memberSelect mainParam = new memberSelect("");
        
        // Registers all the input parameters for this component.
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(mainParam, "", "", "", GH_ParamAccess.item);
        }

        // Registers all the output parameters for this component.
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Output", "O", "This is the member in the object", GH_ParamAccess.item);
        }

        // This is the method that actually does the work.
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            geomObjGoo objGoo = new geomObjGoo();

            if (!DA.GetData(0, ref objGoo))
            {
                mainParam.reset();
                return;
            }

            geomObject obj = objGoo.Value;
            if (obj.data.Count == 0)
            {
                //AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Nothing to show");
                mainParam.reset();
                return;
            }

            mainParam.update(obj);
            string key = mainParam.NickName;
            if (!obj.data.ContainsKey(key))
            {
                //mainParam.reset();
                return;
            }

            if(obj.data[key].Objects.Count == 1)
            {
                Params.Output[0].Access = GH_ParamAccess.item;
                DA.SetData(0, obj.data[key].Objects[0]);
            }
            else
            {
                Params.Output[0].Access = GH_ParamAccess.list;
                DA.SetDataList(0, obj.data[key].Objects);
            }
        }

        // this function forces GH to recompute the component - bound to change events
        protected virtual void OnParameterChanged(object sender, GH_ParamServerEventArgs e)
        {
            mainParam.reset();
            ExpireSolution(true);
        }

        //came with the template - no clue what it is - find out later
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }
    
        //same story - make this function load and image from a path and return it, and your icon will be displayed (24 x 24)
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        //dont change this Guid after publishing the plugin
        public override Guid ComponentGuid
        {  
            get { return new Guid("{0f8fad5b-d9cb-469f-a165-70867728950e}"); }
        }
    }
}
