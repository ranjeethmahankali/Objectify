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
    //this class is the custom parameter class that shows options in the context menu
    public class MemberSelect : GH_Param<GeomObjGoo>
    {
        //constructors
        public MemberSelect(string nickname) :
            base("Member", nickname, "Member", "Data", "Objectify", GH_ParamAccess.item)
        {
            this.options = new List<string>();
            this.MutableNickName = false;
        }
        public MemberSelect(List<string> keys) :
            this("Member")
        {
            this.options = keys;
            this.NickName = options[0];
        }
        public MemberSelect() : this("")
        {
        }

        //properties - options to show in the context menu
        public List<string> options { get; }

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
        private void optionClickHandler(Object sender, EventArgs e)
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
            foreach (string key in obj.data.Keys)
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
}
