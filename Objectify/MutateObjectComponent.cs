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
    //this component can mutate an object - change one or more of its members
    public class MutateObject : GH_Component
    {
        //constructor
        public MutateObject() :
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
            MemberSelect objToMutate = new MemberSelect("O");
            objToMutate.Optional = true;
            pManager.AddParameter(objToMutate, "", "", "Object - Member to Mutate", GH_ParamAccess.item);

            MemberInput newMember = new MemberInput("R");
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

            this.obj = objGoo.Value.Duplicate();
            if (obj.MemberDict.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "The Object is empty");
                return;
            }
            //making a copy of the object in case mutation fails
            GeomObject obj_original = obj.Duplicate();

            MemberSelect param0 = Params.Input[0] as MemberSelect;
            param0.Update(obj);

            //now mutating the object
            List<IGH_Goo> obj_in = new List<IGH_Goo>();
            if (!DA.GetDataList(1, obj_in))
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

            MemberInput param1 = Params.Input[1] as MemberInput;
            param1.HasGeometry = typeof(IGH_GeometricGoo).IsAssignableFrom(obj_in[0].GetType());
            if (!obj.MemberDict.ContainsKey(param0.NickName))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The object does not have a member with this name !");
                return;
            }

            //deleting the old member
            obj.MemberDict.Remove(param0.NickName);
            obj.MemberDict.Add(param0.NickName, obj_in);

            //now updating the visibility and bakability settings
            this.obj.Visibility[param0.NickName] = param1.Settings["Visible"];
            this.obj.Bakability[param0.NickName] = param1.Settings["Bakable"];

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
