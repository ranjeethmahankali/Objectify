using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
//using Newtonsoft.Json;
using Grasshopper.Kernel.Types;
//using Grasshopper.Kernel.Special;
using System.Windows.Forms;

namespace Objectify
{
    //this is the custom parameter class for the objectify component
    public class memberParam : GH_Param<IGH_Goo>
    {
        //constructors
        public memberParam(string nickname):
            base("Member", nickname, "Member", "Data", "Objectify", GH_ParamAccess.list)
        {
            this.DataMapping = GH_DataMapping.Flatten;
            this.Access = GH_ParamAccess.list;  
            this.NickName = nickname;
            this.Optional = true;

            this.options = new List<string>();
            options.Add("Visible");
            options.Add("Bakable");

            this.opState = new Dictionary<string, bool>();
            opState.Add("Visible", true);
            opState.Add("Bakable", true);
        }

        //properties
        private List<string> options;
        public Dictionary<string, bool> opState;

        //overriding the options shown in the menu
        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            this.Menu_AppendDisconnectWires(menu);
            for (int i = 0; i < this.options.Count; i++)
            {
                if (opState[this.options[i]])
                {
                    Menu_AppendItem(menu, this.options[i], optionClickHandler, true, true);
                }
                else
                {
                    Menu_AppendItem(menu, this.options[i], optionClickHandler);
                }
            }
        }
        //this is what happens when the option is clicked
        private void optionClickHandler(Object sender, EventArgs e)
        {
            //do sth when clicked
            opState[sender.ToString()] = !(opState[sender.ToString()]);
            this.OnDisplayExpired(true);
            this.ExpireSolution(true);
        }

        //this is the unique guid don't change this after the component is published
        public override Guid ComponentGuid
        {
            get { return new Guid("{0e51fcfc-078e-4fa3-a41e-9e6b57c8d1ef}"); }
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
        private List<memberParam> paramList = new List<memberParam>();

        //this safely creates and returns a new parameter - does not add it to the component
        private memberParam newParam(string nickName, int index = -1)
        {
            if (index == -1) { index = Params.Input.Count; }
            memberParam param = new memberParam(nickName);
            paramList.Insert(index, param);

            return param;
        }
        /// Registers all the input parameters for this component.
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            memberParam param = newParam("Label_1");
            pManager.AddParameter(param);
            /*pManager.AddGenericParameter("Geometry", "Label_1", "Geometry for this label", GH_ParamAccess.list);
            Params.Input[0].DataMapping = GH_DataMapping.Flatten;
            Params.Input[0].Optional = true;*/
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
            memberParam param = newParam("Label_" + paramNum.ToString(), index);

            return param;
        }
        
        //this is same as remove parameter though I am not sure
        public bool DestroyParameter(GH_ParameterSide side, int index)
        {
            if(side == GH_ParameterSide.Input && Params.Input.Count > 1)
            {
                paramList.RemoveAt(index);
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
            if (obj.dataCount == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "No data received");
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
            Dictionary<string, List<double>> numDict = new Dictionary<string, List<double>>();
            Dictionary<string, List<string>> textDict = new Dictionary<string, List<string>>();
            Dictionary<string, List<GH_Vector>> vecDict = new Dictionary<string, List<GH_Vector>>();
            for (int i = 0; i < Params.Input.Count; i++)
            {
                List<Object> obj_in = new List<Object>();//this is for before you decide the type
                if (!DA.GetDataList<Object>(i, obj_in)) return;
                //here check if all data are of same type within the list of this param
                if (!validDatatypes(obj_in))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "All data in an object member should be of the same type!");
                    return;
                }
                //now we check that all the member names are unique
                if (allKeys.Distinct().Count() != allKeys.Count)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Member names cannot be repeated !");
                    return;
                }

                //now we try to extract all that data
                //Rhino.RhinoApp.WriteLine(obj_in[0].GetType().ToString());
                if (obj_in[0].GetType() == typeof(GH_Number))
                {
                    //Rhino.RhinoApp.WriteLine("Number !");
                    List<double> nums = new List<double>();
                    DA.GetDataList<double>(i, nums);
                    numDict.Add(Params.Input[i].NickName, nums);
                }
                else if (obj_in[0].GetType() == typeof(GH_String))
                {
                    //Rhino.RhinoApp.WriteLine("Text !");
                    List<string> text = new List<string>();
                    DA.GetDataList<string>(i, text);
                    textDict.Add(Params.Input[i].NickName, text);
                }
                else if (obj_in[0].GetType() == typeof(GH_Vector))
                {
                    //Rhino.RhinoApp.WriteLine("Vector!");
                    List<GH_Vector> vecs = new List<GH_Vector>();
                    DA.GetDataList<GH_Vector>(i, vecs);
                    vecDict.Add(Params.Input[i].NickName, vecs);
                }
                else
                {
                    try
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
                        else
                        {
                            //if we get here then there is something wrong with the data
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Something is not right with the data you provide");
                        }
                    }catch(Exception e)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Objectofy does not support this data type");
                    }
                }
            }

            this.obj = new geomObject(this.NickName, dataDict);
            this.obj.number = numDict;
            this.obj.text = textDict;
            this.obj.vector = vecDict;
            updateSettings();
            //Rhino.RhinoApp.WriteLine(this.obj.dataCount.ToString());
        }

        //checks if the data in the list is all of the same type
        private bool validDatatypes(List<Object> objList)
        {
            for (int i = 1; i < objList.Count; i++)
            {
                if(objList[i].GetType() != objList[0].GetType())
                {
                    return false;
                }
            }
            return true;
        }

        //this one updates the visibility and bakability settings from all params
        private void updateSettings()
        {
            //clearing all the previous settings
            this.obj.Bakability.Clear();
            this.obj.Visibility.Clear();
            foreach(memberParam param in paramList)
            {
                this.obj.Visibility.Add(param.NickName, param.opState["Visible"]);
                this.obj.Bakability.Add(param.NickName, param.opState["Bakable"]);
            }
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
            foreach (string key in obj.number.Keys)
            {
                this.options.Add(key);
            }
            foreach (string key in obj.text.Keys)
            {
                this.options.Add(key);
            }
            foreach (string key in obj.vector.Keys)
            {
                this.options.Add(key);
            }

            //if nickname not set then empty string
            if (this.NickName == "" && options.Count > 0)
            {
                this.NickName = options[0];
            }
        }
        //this resets the parameter - clears the context menu options and sets the current member name to empty string
        public void reset(geomObjGoo goo)
        {
            geomObject obj = goo.Value;
            this.update(obj);
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
                mainParam.reset(objGoo);
                return;
            }

            geomObject obj = objGoo.Value;
            if (obj.dataCount == 0)
            {
                //AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Nothing to show");
                mainParam.reset(objGoo);
                return;
            }

            mainParam.update(obj);
            string key = mainParam.NickName;
            if (obj.data.ContainsKey(key))
            {
                if (obj.data[key].Objects.Count == 1)
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
            else if (obj.number.ContainsKey(key))
            {
                if (obj.number[key].Count == 1)
                {
                    Params.Output[0].Access = GH_ParamAccess.item;
                    DA.SetData(0, obj.number[key][0]);
                }
                else
                {
                    Params.Output[0].Access = GH_ParamAccess.list;
                    DA.SetDataList(0, obj.number[key]);
                }
            }
            else if (obj.text.ContainsKey(key))
            {
                if (obj.text[key].Count == 1)
                {
                    Params.Output[0].Access = GH_ParamAccess.item;
                    DA.SetData(0, obj.text[key][0]);
                }
                else
                {
                    Params.Output[0].Access = GH_ParamAccess.list;
                    DA.SetDataList(0, obj.text[key]);
                }
            }
            else if (obj.vector.ContainsKey(key))
            {
                if (obj.vector[key].Count == 1)
                {
                    Params.Output[0].Access = GH_ParamAccess.item;
                    DA.SetData(0, obj.vector[key][0]);
                }
                else
                {
                    Params.Output[0].Access = GH_ParamAccess.list;
                    DA.SetDataList(0, obj.vector[key]);
                }
            }
        }

        // this function forces GH to recompute the component - bound to change events
        protected virtual void OnParameterChanged(object sender, GH_ParamServerEventArgs e)
        {
            //mainParam.reset();
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