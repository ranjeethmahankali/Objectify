using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace Objectify
{
  public class OptionInputParameter : GH_Param<IGH_Goo>
  {
    #region constructor
    public OptionInputParameter()
      : base("Member input", "Mem", "Input data", "Data", "Objectify", GH_ParamAccess.item)
    {
      _options = new string[2];
      _states = new bool[_options.Length];

      _options[0] = "Visible";
      _options[1] = "Bakeable";
      _states[0] = true;
      _states[1] = true;
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
    #endregion

    #region option logic
    private readonly string[] _options;
    private readonly bool[] _states;

    /// <summary>
    /// Enumerate over all the option+state pairs in this parameter.
    /// It is better to expose private fields via properties or methods.
    /// Makes the code more future-proof.
    /// </summary>
    public IEnumerable<KeyValuePair<string, bool>> Options
    {
      get
      {
        for (int i = 0; i < _options.Length; i++)
          yield return new KeyValuePair<string, bool>(_options[i], _states[i]);
      }
    }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      Menu_AppendDisconnectWires(menu);
      foreach (KeyValuePair<string, bool> option in Options)
        Menu_AppendItem(menu, option.Key, OptionClick, true, option.Value);
    }
    private void OptionClick(object sender, EventArgs e)
    {
      ToolStripMenuItem item = sender as ToolStripMenuItem;
      if (item == null)
        return; // Weird...

      int index = Array.IndexOf(_options, item.Text);
      if (index < 0)
        return; // Also weird, unrecognised option name.

      // Record the current state, then change the selected option.
      RecordUndoEvent("Option change");
      _states[index] = !_states[index];

      ExpireSolution(true);
    }
    #endregion

    #region properties
    public static readonly Guid _id = new Guid("{20bb1984-b89d-4414-98ab-37ac6e413198}");
    public override Guid ComponentGuid
    {
      get { return _id; }
    }
    public override GH_Exposure Exposure
    {
      get { return GH_Exposure.hidden; }
    }
    #endregion

    #region (de)serialisation
    public override bool Write(GH_IWriter writer)
    {
      // Write option names and option states.
      for (int i = 0; i < _states.Length; i++)
      {
        writer.SetString("InputOption", i, _options[i]);
        writer.SetBoolean("InputState", i, _states[i]);
      }

      return base.Write(writer);
    }
    public override bool Read(GH_IReader reader)
    {
      // Only read option states.
      for (int i = 0; i < _states.Length; i++)
        _states[i] = reader.GetBoolean("InputState", i);

      return base.Read(reader);
    }
    #endregion
  }

  public class OptionTestComponent : GH_Component, IGH_VariableParameterComponent
  {
    //constructor
    public OptionTestComponent()
      : base("Test Component", "TestComp", "Creates an object out of the input geometry", "Data", "Testing")
    {

    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddParameter(new OptionInputParameter(), "Parameter 1", "P:1", "First input", GH_ParamAccess.list);
      pManager[0].Optional = true;
    }
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddTextParameter("Object", "O", "Object", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
      StringBuilder sb = new StringBuilder();
      for (int i = 0; i < Params.Input.Count; i++)
      {
        List<IGH_Goo> list = new List<IGH_Goo>();
        if (!da.GetDataList(i, list))
          continue;

        OptionInputParameter param = Params.Input[i] as OptionInputParameter;
        if (param == null)
          sb.AppendLine("Unrecognised input parameter type.");
        else
        {
          sb.AppendLine(string.Format("Option input parameter #{0}", i + 1));
          foreach (KeyValuePair<string, bool> option in param.Options)
            sb.AppendLine(string.Format("{0} = {1}", option.Key, option.Value));
        }
        sb.AppendLine("{");
        for (int k = 0; k < list.Count; k++)
          sb.AppendLine(string.Format("  {0}: {1}", k, list[i].ToString()));
        sb.AppendLine("}");
        sb.AppendLine();
      }

      da.SetData(0, sb.ToString());
    }

    public override GH_Exposure Exposure
    {
      get { return GH_Exposure.primary; }
    }
    //If you make this property return an image (loaded from a path) then that will be the component logo
    protected override Bitmap Icon
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
      get { return new Guid("{5bef3db8-ccbe-4074-b616-9bb5debcdcef}"); }
    }

    #region variable parameter logic
    public bool CanInsertParameter(GH_ParameterSide side, int index)
    {
      return side == GH_ParameterSide.Input;
    }
    public bool CanRemoveParameter(GH_ParameterSide side, int index)
    {
      return side == GH_ParameterSide.Input && Params.Input.Count > 1;
    }
    public IGH_Param CreateParameter(GH_ParameterSide side, int index)
    {
      return new OptionInputParameter();
    }
    public bool DestroyParameter(GH_ParameterSide side, int index)
    {
      // You don't have to do anything here, it only for when you want to take action due to a parameter being removed.
      return true;
    }
    public void VariableParameterMaintenance()
    {
      // This is where you make sure all your parameters are set up correctly.
      for (int i = 0; i < Params.Input.Count; i++)
      {
        IGH_Param param = Params.Input[i];
        param.Optional = true;
        param.NickName = "P:" + (i + 1);
        param.MutableNickName = false;
        param.Access = GH_ParamAccess.list;
      }
    }
    #endregion
  }
}
