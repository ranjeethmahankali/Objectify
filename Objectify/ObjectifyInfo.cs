using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace Objectify
{
    public class ObjectifyInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "Objectify";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("dd2460d9-dc69-4d1b-8d96-a4c8b520b658");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "";
            }
        }
    }
}
