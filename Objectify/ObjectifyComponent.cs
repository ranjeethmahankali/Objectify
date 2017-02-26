using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Parameters;
using Newtonsoft.Json;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Special;

public class geomObject:GH_GeometryGroup
{
    //constructors
    public geomObject(string nameStr, Dictionary<string, IGH_GeometricGoo> geomDictionary)
    {
        name = nameStr;
        data = geomDictionary;
        updateGroup();
    }

    public geomObject(string nameStr)
    {
        name = nameStr;
        data =  new Dictionary<string, IGH_GeometricGoo>();
    }
    public geomObject()
    {
        name = "None";//this is the default value for name
        data = new Dictionary<string, IGH_GeometricGoo>();//this is the default empty dictionary
    }

    //object properties
    public string name;
    public Dictionary<string, IGH_GeometricGoo> data;

    //functions
    //this one returns all the geometry as a list
    public void updateGroup()
    {
        //removing all old elements
        Objects.Clear();
        foreach(string key in data.Keys)
        {
            Objects.Add(data[key]);
        }
    }

    //this is for when the user prints the object onto a panel
    public override string ToString()
    {
        string output = name+" object with " + data.Count.ToString() + " members:{";
        int counter = 0;
        foreach(string key in data.Keys)
        {
            if(counter != 0)
            {
                output += ", ";
            }
            output += key;
            counter++;
        }

        output += "}";

        return output;
    }
}

namespace Objectify
{   
    
    public class ObjectifyComponent : GH_Component, IGH_VariableParameterComponent
    {
        public ObjectifyComponent()
          : base("Objectify", "Object",
              "Creates an object out of the inpupt geometry",
              "Data", "Objectify")
        {
            obj = new geomObject(this.NickName);
        }

        private geomObject obj
        {
            get; set;
        }
        
        /// Registers all the input parameters for this component.
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geometry", "Label_1", "Geometry for this label", GH_ParamAccess.item);
        }
        
        /// Registers all the output parameters for this component.
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Object", "O", "Object", GH_ParamAccess.item);
            //pManager.AddGeometryParameter("Geometry", "G", "This is the geometry in the object", GH_ParamAccess.item);
            //pManager.AddTextParameter("Debugging Data", "D", "Debug", GH_ParamAccess.item);
        }

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

        public IGH_Param CreateParameter(GH_ParameterSide side, int index  )
        {
            int paramNum = Params.Input.Count + 1;
            Param_Geometry param = new Param_Geometry();
            param.Name = "Geometry for this label";
            param.NickName = "Label_"+paramNum;

            return param;
        }

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
        public void VariableParameterMaintenance()
        {
            //Params.Input[0].
        }

        /// This is the method that actually does the work.
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // First, we need to retrieve all data from the input parameters.
            // We'll start by declaring variables and assigning them starting values.
            // Then we need to access the input parameters individually. 
            // When data cannot be extracted from a parameter, we should abort this method.
            UpdateData(DA);
            // We should now validate the data and warn the user if invalid data is supplied.
            if (obj.data.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "There is no Geometry");
                //return;
            }

            // We're set to create the spiral now. To keep the size of the SolveInstance() method small, 
            // The actual functionality will be in a different method:

            // Finally assign the spiral to the output parameter.
            DA.SetData(0, obj);
            //DA.SetDataList(1, obj.getGeometry());
        }

        private void UpdateData(IGH_DataAccess DA)
        {
            Dictionary<string, IGH_GeometricGoo> dataDict = new Dictionary<string, IGH_GeometricGoo>();
            for (int i = 0; i < Params.Input.Count; i++)
            {
                IGH_GeometricGoo geom = null;
                if (DA.GetData<IGH_GeometricGoo>(i, ref geom))
                {
                    dataDict.Add(Params.Input[i].NickName, geom);
                }
                //else {}
            }

            this.obj = new geomObject(this.NickName, dataDict);
        }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{1b99b61b-34e7-4912-955e-54fd914b4200}"); }
        }
    }

    //this is the object reader
    public class ReadObjComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ReadObjComponent()
          : base("Read Object", "ReadObj",
              "Reads an Object",
              "Data", "Objectify")
        {
        }

        /// Registers all the input parameters for this component.
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            
            pManager.AddGenericParameter("Geometry", "Label_1", "Geometry for this label", GH_ParamAccess.item);
        }

        /// Registers all the output parameters for this component.
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geometry", "G", "This is the geometry in the object", GH_ParamAccess.item);
            //pManager.AddTextParameter("Debugging Data", "D", "Debug", GH_ParamAccess.item);
        }

        /// This is the method that actually does the work.
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            geomObject obj = new geomObject();
            string key = Params.Input[0].NickName;

            if (!DA.GetData(0, ref obj))
            {
                return;
            }

            if (obj.data.Count == 0)
            {
                //AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Nothing to show");
                return;
            }

            if (!obj.data.ContainsKey(key))
            {
                return;
            }


            DA.SetData(0, obj.data[key]);
        }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            
            get { return new Guid("{0f8fad5b-d9cb-469f-a165-70867728950e}"); }
        }
    }
}
