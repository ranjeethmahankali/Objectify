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
    public class MemberInput : GH_Param<IGH_Goo>
    {
        //constructors
        public MemberInput() :
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
        public MemberInput(string nickname) : this()//using the 0 arg constructor and building on top of it.
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
            string desc = "Changed " + item.Text + " of " + this.NickName;
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
            }
            catch (Exception e)
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
}
