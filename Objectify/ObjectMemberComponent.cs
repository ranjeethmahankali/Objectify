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

namespace Objectify
{
    //this is the object reader component
    public class ObjectMember : GH_Component, IGH_VariableParameterComponent
    {
        //constructor that calls the base class constructor
        public ObjectMember()
          : base("Object Member", "M",
              "Reads an Object",
              "Data", "Objectify") { }

        private static MemberSelect GetNewInputParameter(string name = "")
        {
            MemberSelect select = new MemberSelect(name);
            select.Optional = true;
            return select;
        }

        // Registers all the input parameters for this component.
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(GetNewInputParameter(), "", "", "", GH_ParamAccess.item);
        }

        // Registers all the output parameters for this component.
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Output", "O", "This is the member in the object", GH_ParamAccess.item);
        }

        // This is the method that actually does the work.
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<IGH_Goo> members = new List<IGH_Goo>();
            for(int i = 0; i < Params.Input.Count; i++)
            {
                GeomObjGoo objGoo = new GeomObjGoo();
                MemberSelect curParam = Params.Input[i] as MemberSelect;
                if(curParam == null) { continue; }
                if (!DA.GetData(i, ref objGoo))
                {
                    curParam.Reset(objGoo);
                    continue;
                }

                GeomObject obj = objGoo.Value;
                if (obj.MemberDict.Count == 0)
                {
                    //AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Nothing to show");
                    curParam.Reset(objGoo);
                    continue;
                }

                curParam.Update(obj);
                string key = curParam.NickName;
                if (obj.MemberDict.ContainsKey(key))
                {
                    members.AddRange(obj.MemberDict[key]);
                }
            }

            //returning as a list or as a single item based how many things need to be returned
            if(members.Count == 1)
            {
                Params.Output[0].Access = GH_ParamAccess.item;
                DA.SetData(0, members.First());
            }
            else
            {
                Params.Output[0].Access = GH_ParamAccess.item;
                DA.SetDataList(0, members);
            }
        }

        // this function forces GH to recompute the component - bound to change events
        protected virtual void OnParameterChanged(object sender, GH_ParamServerEventArgs e)
        {
            //curParam.reset();
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

        #region IGH_VariableParameterComponenet Implementation
        public bool CanInsertParameter(GH_ParameterSide side, int index)
        {
            return (side == GH_ParameterSide.Input);
        }

        public bool CanRemoveParameter(GH_ParameterSide side, int index)
        {
            return (side == GH_ParameterSide.Input && Params.Input.Count > 1);
        }

        public IGH_Param CreateParameter(GH_ParameterSide side, int index)
        {
            return GetNewInputParameter();
        }

        public bool DestroyParameter(GH_ParameterSide side, int index)
        {
            return true;
        }

        public void VariableParameterMaintenance()
        {
            // This is where you make sure all your parameters are set up correctly.
            for (int i = 0; i < Params.Input.Count; i++)
            {
                IGH_Param param = Params.Input[i];
                param.Optional = true;
            }
        }
        #endregion

        //dont change this Guid after publishing the plugin
        public override Guid ComponentGuid
        {
            get { return new Guid("{0f8fad5b-d9cb-469f-a165-70867728950e}"); }
        }
    }
}
