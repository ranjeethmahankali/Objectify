using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using GH_IO.Serialization;
//using Grasshopper.Kernel.Special;
using System.Windows.Forms;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Objectify
{
    //main component
    public class ObjectifyComponent : GH_Component, IGH_VariableParameterComponent
    {
        //constructor
        public ObjectifyComponent()
          : base("Objectify", "Object",
              "Creates an object out of the input data",
              "Data", "Objectify")
        {
            obj = new GeomObject(this.NickName);
            Params.ParameterChanged += new GH_ComponentParamServer.ParameterChangedEventHandler(OnParameterChanged);
        }

        //this is the geomObject which will hold the data being outputted by this component
        private GeomObject obj{get; set;}
        private List<string> allKeys
        {
            get
            {
                List<string> keyList = new List<string>();
                for (int i = 0; i < Params.Input.Count; i++)
                {
                    keyList.Add(Params.Input[i].NickName);
                }

                return keyList;
            }
        }

        /// Registers all the input parameters for this component.
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            MemberInput param = new MemberInput("Label_1");
            //pManager.AddParameter(, "Label", "L1", "This is the descriptions", GH_ParamAccess.list);
            pManager.AddParameter(param);
            //pManager.AddGenericParameter("", "Label_1", "", GH_ParamAccess.list);
        }
        
        // Registers all the output parameters for this component.
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
            return (side == GH_ParameterSide.Input);
        }
        //this makes the decision of whether to allow the user to remove parameters or not
        public bool CanRemoveParameter(GH_ParameterSide side, int index)
        {
            return (side == GH_ParameterSide.Input && Params.Input.Count > 1);
        }
        //The parameter returned by this func is what is added to the component when user creates a component
        public IGH_Param CreateParameter(GH_ParameterSide side, int index  )
        {
            int paramNum = Params.Input.Count + 1;
            //memberInput param = newParam("Label_" + paramNum, index);
            MemberInput param = new MemberInput("Label_" + paramNum);

            return param;
        }
        //this is same as remove parameter though I am not sure
        public bool DestroyParameter(GH_ParameterSide side, int index)
        {
            return true;
        }
        //this is any maintenenance that needs to be done - not really sure what it is for but I was forced to implement it
        public void VariableParameterMaintenance()
        {
            // This is where you make sure all your parameters are set up correctly.
            for (int i = 0; i < Params.Input.Count; i++)
            {
                IGH_Param param = Params.Input[i];
                param.Optional = true;
                param.Access = GH_ParamAccess.list;
            }
        }

        /// This is the method that actually does the work.
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //now we check that all the member names are unique
            if (allKeys.Distinct().Count() != allKeys.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Member names cannot be repeated !");
                return;
            }

            UpdateData(DA);
            // We should now validate the data and warn the user if invalid data is supplied.
            if (obj.MemberDict.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "No data received");
                //return;
            }

            // Finally assign the spiral to the output parameter.
            DA.SetData(0, new GeomObjGoo(obj));
            //DA.SetDataList(1, obj.getGeometry());
        }
        //this updates the data to the obj of this instance
        private void UpdateData(IGH_DataAccess DA)
        {
            Dictionary<string, List<IGH_Goo>> dataDict = new Dictionary<string, List<IGH_Goo>>();
            for (int i = 0; i < Params.Input.Count; i++)
            {
                List<IGH_Goo> obj_in = new List<IGH_Goo>();//this is for before you decide the type
                if (!DA.GetDataList(i, obj_in)) return;
                //here check if all data are of same type within the list of this param
                if (!validDatatypes(obj_in))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "All data in an object member should be of the same type!");
                    return;
                }

                MemberInput param = (MemberInput)Params.Input[i];
                param.HasGeometry = typeof(IGH_GeometricGoo).IsAssignableFrom(obj_in[0].GetType());

                dataDict.Add(Params.Input[i].NickName, obj_in);
            }

            this.obj = new GeomObject(this.NickName, dataDict);
            updateSettings();
            //Rhino.RhinoApp.WriteLine(this.obj.dataCount.ToString());
        }
        //update settings
        private void updateSettings()
        {
            this.obj.Bakability.Clear();
            this.obj.Visibility.Clear();
            for (int i = 0; i < Params.Input.Count; i++)
            {
                MemberInput param = Params.Input[i] as MemberInput;
                this.obj.Bakability.Add(param.NickName, param.Settings["Bakable"]);
                this.obj.Visibility.Add(param.NickName, param.Settings["Visible"]);
            }
        }

        //checks if the data in the list is all of the same type
        public static bool validDatatypes(List<IGH_Goo> objList)
        {
            if(objList.Count == 0) { return true; }
            for (int i = 1; i < objList.Count; i++)
            {
                if(objList[i].GetType() != objList[0].GetType())
                {
                    return false;
                }
            }
            return true;
        }

        //I have no idea what this is for - came with the template - find out later
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        //these methods are for (de)serialization - for when reading and writing to file
        //this tells GH how to write a file containing your component and how to read from it
        public override bool Write(GH_IWriter writer)
        {
            return base.Write(writer);
        }
        public override bool Read(GH_IReader reader)
        {
            return base.Read(reader);
        }

        //this is the icon for this component
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // Returning the Icon for this component
                return Properties.Resources.Objectify;
            }
        }

        //this is the guid of the component, once you publish the plugin u cannot change this
        public override Guid ComponentGuid
        {
            get { return new Guid("{1b99b61b-34e7-4912-955e-54fd914b4200}"); }
        }
    }
}