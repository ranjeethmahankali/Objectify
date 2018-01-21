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
    //this is the custom parameter class for the objectify component
    public class memberInput : GH_Param<IGH_Goo>
    {
        //constructors
        public memberInput():
            base("Member Input", "Label", "Input data", "Data", "Objectify", GH_ParamAccess.list)
        {
            this.DataMapping = GH_DataMapping.Flatten;
            this.Optional = true;
            this.Access = GH_ParamAccess.list;

            this.option = new Dictionary<string, bool>();
            option.Add("Visible", true);
            option.Add("Bakable", true);

            this.isGeometry = false;
        }
        public memberInput(string nickname):this()//using the 0 arg constructor and building on top of it.
        {
            this.NickName = nickname;
        }
        /// <summary>
        /// Since this parameter wraps IGH_Goo (which is an interface), this method needs
        /// to be overridden, for GH can't instantiate interfaces.
        /// </summary>
        /// <returns>Default data to be stored.</returns>
        protected override IGH_Goo InstantiateT()
        {
            return new GH_ObjectWrapper();
        }
        //properties
        public Dictionary<string, bool> option;
        public bool isGeometry;

        //overriding the options shown in the menu
        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            this.Menu_AppendDisconnectWires(menu);
            foreach (string opName in option.Keys)
            {
                Menu_AppendItem(menu, opName, optionClickHandler, isGeometry, isGeometry && option[opName]);
            }
        }
        //this is what happens when the option is clicked
        private void optionClickHandler(Object sender, EventArgs e)
        {
            //do sth when clicked
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            if (item == null)
            {
                return; // Weird...
            }

            if (!this.option.ContainsKey(item.Text))
            {
                return; //unrecognized option
            }

            //recording undo event before changing option state. This allows the user to undo this action
            string desc = "Changed "+item.Text+" of "+this.NickName;
            RecordUndoEvent(desc);
            //changing the option now.
            this.option[item.Text] = !(this.option[item.Text]);
            this.OnDisplayExpired(true);
            this.ExpireSolution(true);
        }

        //these methods are for (de)serialization - for when reading and writing to file
        //this tells GH how to write a file containing your component and how to read from it
        public override bool Write(GH_IWriter writer)
        {
            try
            {
                // converting the options and states to json to be saved
                string jsonStr = JsonConvert.SerializeObject(this.option);
                writer.SetString("Options", jsonStr);
            }catch(Exception e)
            {
                Debug.WriteLine(e.Message);
                Rhino.RhinoApp.WriteLine(e.Message);
            }

            return base.Write(writer);
        }
        public override bool Read(GH_IReader reader)
        {
            // reading the json string and then deserializing it to set the options
            string optString = reader.GetString("Options");
            if (optString == null)
            {
                //the options were not serialized when the file was saved last time
                return base.Read(reader);
            }
            this.option = JsonConvert.DeserializeObject<Dictionary<string, bool>>(optString);

            return base.Read(reader);
        }

        //This controls what user can see
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.hidden; }
        }
        //this is the unique guid don't change this after the component is published
        public override Guid ComponentGuid
        {
            get { return new Guid("{20bb1984-b89d-4414-98ab-37ac6e413198}"); }
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
            memberInput param = new memberInput("Label_1");
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
            memberInput param = new memberInput("Label_" + paramNum);

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
            if (obj.dataCount == 0)
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

                //now we try to extract all that data
                //Rhino.RhinoApp.WriteLine(obj_in[0].GetType().ToString());
                memberInput param = Params.Input[i] as memberInput;
                param.isGeometry = false;
                if (obj_in[0].GetType() == typeof(GH_Number))
                {
                    //Rhino.RhinoApp.WriteLine("Number !");
                    List<double> nums = new List<double>();
                    DA.GetDataList<double>(i, nums);
                    numDict.Add(param.NickName, nums);
                }
                else if (obj_in[0].GetType() == typeof(GH_String))
                {
                    //Rhino.RhinoApp.WriteLine("Text !");
                    List<string> text = new List<string>();
                    DA.GetDataList<string>(i, text);
                    textDict.Add(param.NickName, text);
                }
                else if (obj_in[0].GetType() == typeof(GH_Vector))
                {
                    //Rhino.RhinoApp.WriteLine("Vector!");
                    List<GH_Vector> vecs = new List<GH_Vector>();
                    DA.GetDataList<GH_Vector>(i, vecs);
                    vecDict.Add(param.NickName, vecs);
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
                            dataDict.Add(param.NickName, grp);
                            param.isGeometry = true;
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

            this.obj = new GeomObject(this.NickName, dataDict);
            this.obj.number = numDict;
            this.obj.text = textDict;
            this.obj.vector = vecDict;
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
                memberInput param = Params.Input[i] as memberInput;
                this.obj.Bakability.Add(param.NickName, param.option["Bakable"]);
                this.obj.Visibility.Add(param.NickName, param.option["Visible"]);
            }
        }

        //checks if the data in the list is all of the same type
        public static bool validDatatypes(List<Object> objList)
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

    //this class is the custom parameter class that shows options in the context menu
    public class memberSelect : GH_Param<GeomObjGoo>
    {
        //constructors
        public memberSelect(string nickname):
            base("Member", nickname, "Member", "Data","Objectify", GH_ParamAccess.item)
        {
            this.options = new List<string>();
            this.MutableNickName = false;
        }
        public memberSelect(List<string> keys):
            this("Member")
        {
            this.options = keys;
            this.NickName = options[0];
        }
        public memberSelect() : this("")
        {
        }
        
        //properties - options to show in the context menu
        public List<string> options { get;}
        
        //overriding the options shown in the menu
        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            this.Menu_AppendDisconnectWires(menu);
            for (int i = 0; i < this.options.Count; i++)
            {
                Menu_AppendItem(menu, this.options[i], optionClickHandler, true, (this.options[i] == this.NickName));
            }
        }
        //this is what happens when the option is clicked
        private void optionClickHandler(Object sender,  EventArgs e)
        {
            //do sth when clicked
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            this.NickName = item.Text;
            this.OnDisplayExpired(true);
            this.ExpireSolution(true);
        }
        
        //this updates the context menu to member names and then sets the current member to the first one if it is unset
        public void update(GeomObject obj)
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
        public void reset(GeomObjGoo goo)
        {
            GeomObject obj = goo.Value;
            this.update(obj);
            this.OnDisplayExpired(true);
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

        //This controls what user can see
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.hidden; }
        }
        //this is the unique guid don't change this after the component is published
        public override Guid ComponentGuid
        {
            get { return new Guid("{36c82c59-bc1a-4e70-9215-553181d31ad3}"); }
        }
    }

    //this is the object reader component
    public class ObjectMember : GH_Component
    {
        //constructor that calls the base class constructor
        public ObjectMember()
          : base("Object Member", "M",
              "Reads an Object",
              "Data", "Objectify")
        {
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
            GeomObjGoo objGoo = new GeomObjGoo();

            if (!DA.GetData(0, ref objGoo))
            {
                mainParam.reset(objGoo);
                return;
            }

            GeomObject obj = objGoo.Value;
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
            ExpireSolution(false);
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
                return Properties.Resources.ObjectMember;
            }
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

        //dont change this Guid after publishing the plugin
        public override Guid ComponentGuid
        {  
            get { return new Guid("{0f8fad5b-d9cb-469f-a165-70867728950e}"); }
        }
    }

    //this component can mutate an object - change one or more of its members
    public class MutateObject:GH_Component
    {
        //constructor
        public MutateObject():
            base("Mutate Object", "Mutate", "Mutate an object by changing one or more members", "Data", "Objectify")
        {
            obj = new GeomObject(this.NickName);
            Params.ParameterChanged += new GH_ComponentParamServer.ParameterChangedEventHandler(OnParameterChanged);
        }

        //properties
        private GeomObject obj { get; set; }        
        
        //this function forces GH to recompute the component - bound to change events
        protected virtual void OnParameterChanged(object sender, GH_ParamServerEventArgs e)
        {
            ExpireSolution(true);
        }

        /// Registers all the input parameters for this component.
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            memberSelect objToMutate = new memberSelect("O");
            objToMutate.Optional = true;
            pManager.AddParameter(objToMutate, "", "", "Object - Member to Mutate", GH_ParamAccess.item);

            memberInput newMember = new memberInput("R");
            newMember.Name = "New Member Value";
            newMember.Description = "Replacement Value for the member in the parameter above";
            newMember.MutableNickName = false;
            pManager.AddParameter(newMember);
        }
        // Registers all the output parameters for this component.
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Object", "O", "Mutated Object", GH_ParamAccess.item);
        }
        /// This is the method that actually does the work.
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GeomObjGoo objGoo = new GeomObjGoo();

            if (!DA.GetData(0, ref objGoo))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No Object received");
                return;
            }

            this.obj = objGoo.Value.fresh(true);
            if (obj.dataCount == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "The Object is empty");
                return;
            }
            //making a copy of the object in case mutation fails
            GeomObject obj_original = obj.fresh(true);

            memberSelect param0 = Params.Input[0] as memberSelect;
            param0.update(obj);

            //now mutating the object
            List<Object> obj_in = new List<object>();
            if (!DA.GetDataList<Object>(1, obj_in))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "The member was not replaced: No replacement received");
                DA.SetData(0, new GeomObjGoo(obj_original));
                return;
            }
            //Debug.WriteLine(obj_in[0] == null);
            //here check if all data are of same type within the list of this param
            if (!ObjectifyComponent.validDatatypes(obj_in))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "All data in an object member should be of the same type!");
                return;
            }

            memberInput param1 = Params.Input[1] as memberInput;
            if (!obj.hasMember(param0.NickName))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The object does not have a member with this name !");
                return;
            }

            //deleting the old member
            obj.removeMember(param0.NickName);
            param1.isGeometry = false;
            if (obj_in[0].GetType() == typeof(GH_Number))
            {
                //Rhino.RhinoApp.WriteLine("Number !");
                List<double> nums = new List<double>();
                DA.GetDataList<double>(1, nums);
                obj.number.Add(param0.NickName, nums);
            }
            else if (obj_in[0].GetType() == typeof(GH_String))
            {
                //Rhino.RhinoApp.WriteLine("Text !");
                List<string> text = new List<string>();
                DA.GetDataList<string>(1, text);
                obj.text.Add(param0.NickName, text);
            }
            else if (obj_in[0].GetType() == typeof(GH_Vector))
            {
                //Rhino.RhinoApp.WriteLine("Vector!");
                List<GH_Vector> vecs = new List<GH_Vector>();
                DA.GetDataList<GH_Vector>(1, vecs);
                obj.vector.Add(param0.NickName, vecs);
            }
            else
            {
                try
                {
                    List<IGH_GeometricGoo> geom = new List<IGH_GeometricGoo>();
                    if (DA.GetDataList<IGH_GeometricGoo>(1, geom))
                    {
                        GH_GeometryGroup grp = new GH_GeometryGroup();
                        for (int j = 0; j < geom.Count; j++)
                        {
                            grp.Objects.Add(geom[j]);
                        }
                        obj.data.Add(param0.NickName, grp);
                        param1.isGeometry = true;
                    }
                    else
                    {
                        //if we get here then there is something wrong with the data
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Something is not right with the data you provide");
                    }
                }
                catch (Exception e)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Objectofy does not support this data type");
                    DA.SetData(0, new GeomObjGoo(obj_original));
                    return;
                }
            }

            //now updating the visibility and bakability settings
            this.obj.Visibility[param0.NickName] = param1.option["Visible"];
            this.obj.Bakability[param0.NickName] = param1.option["Bakable"];

            DA.SetData(0, new GeomObjGoo(obj));
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

        //this is the guid of the component, once you publish the plugin u cannot change this
        public override Guid ComponentGuid
        {
            get { return new Guid("{c3430f1a-0d01-47ae-b5be-84a9408ad73a}"); }
        }

        //this is the icon for this component
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // Returning the Icon for this component
                return Properties.Resources.MutateObject;
            }
        }
    }
}