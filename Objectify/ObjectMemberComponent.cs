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
        private MemberSelect mainParam = new MemberSelect("");

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
            if (obj.DataCount == 0)
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
}
