using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _3D_LayoutOpt;
using System.IO;

namespace importTester
{
    class Program
    {
        static void Main(string[] args)
        {
            Directory.SetCurrentDirectory("../../workspace");
            Design design = new Design();
            readwrite.ImportFootprints(design);
            
        }
    }
}
