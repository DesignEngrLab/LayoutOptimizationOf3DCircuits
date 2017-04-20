using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using TVGL;
using TVGL.IOFunctions;


namespace _3D_LayoutOpt
{
    internal static class IO
    {

        private static readonly string[] CompNames =
        {
            "Designs/IC1.STL",
            "Designs/R1.STL",
            "Designs/R2.STL",
            "Designs/R3.STL",
            "Designs/C1.STL",
            "Designs/D1.STL"
        };
        private static readonly string ContainerName =
            "Designs/Container1.STL";


        public static void ImportData(Design design)
        {
            ImportComponents(design);
            ImportContainer(design);
            ImportNetlist(design);
        }

        private static void ImportComponents(Design design)
        {
            ImportFootprints(design);
            ImportCompModels(design);
            //ImportCompFeatures(design);
        }

        private static void ImportContainer(Design design)
        {
            ImportContModel(design);
            //ImportContFeatures(design);
        }

        private static void ImportContModel(Design design)
        {
            var filename = ContainerName;
            Console.WriteLine("Attempting: " + filename);
            Stream fileStream;
            TessellatedSolid ts;
            using (fileStream = File.OpenRead(filename))
                ts = TVGL.IOFunctions.IO.Open(fileStream, filename)[0];
            var name = GetNameFromFileName(filename);
            var container = new Container(name, ts);
            design.Container = container;
        }

        private static void ImportCompFeatures(Design design)
        {
            try
            {
                using (var readtext = new StreamReader("datafile1"))
                {
                    Console.WriteLine("Reading component data from file.");
                    string line;
                    while ((line = readtext.ReadLine()) != null)
                    {
                        var items = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        var compname = items[0];
                        var tempcrit = Convert.ToDouble(items[1]);
                        var q = Convert.ToDouble(items[2]);
                        var k = Convert.ToDouble(items[3]);
                        var comp = design.Components.Find(x => x.Name == compname);
                        comp.Tempcrit = tempcrit;
                        comp.Q = q;
                        comp.K = k;
                    }
                    Console.WriteLine("EOF reached in the input file.\n");
                }
            }
            catch (IOException ex)
            {
            }

        }

        private static void ImportContFeatures(Design design)
        {
            try
            {
                using (var readtext = new StreamReader("datafile2"))
                {
                    Console.WriteLine("Reading container dimensions from file.");
                    string line;
                    while ((line = readtext.ReadLine()) != null)
                    {
                        var items = line.Split(' ');
                        var kb = Convert.ToDouble(items[0]);
                        var h0 = Convert.ToDouble(items[1]);
                        var h1 = Convert.ToDouble(items[2]);
                        var h2 = Convert.ToDouble(items[3]);
                        var tamb = Convert.ToDouble(items[4]);

                        design.Kb = kb;
                        design.H[0] = h0;
                        design.H[1] = h1;
                        design.H[2] = h2;
                        design.Tamb = tamb;

                    }
                }
            }
            catch (IOException ex)
            {

            }

        }


        public static void ImportFootprints(Design design)
        {

            var doc = XDocument.Load("Designs/555LED.sch");
            var Sheets = doc.Element("eagle").Element("drawing").Element("schematic").Element("sheets");
            var Parts = doc.Element("eagle").Element("drawing").Element("schematic").Element("parts");
            var parts = Parts.Elements("part");
            if (Sheets != null)
            {
                var sheets = Sheets.Elements("sheet");
                var sheet = sheets.First();
                var instances = sheet.Element("instances").Elements("instance");
                var index = 0;
                foreach (var instance in instances) 
                {
                    var partName = instance.FirstAttribute.Value;
                    var xElement = doc.Element("eagle");
                    if (xElement != null)
                    {
                        var part = (xElement.Element("drawing").Element("schematic").Element("parts").Elements("part").Where(n => n.Attribute("name").Value == partName)).First();
                        var library = (xElement.Element("drawing").Element("schematic").Element("libraries").Elements("library").Where(n => n.Attribute("name").Value == part.Attribute("library").Value)).First();
                        var deviceset = library.Element("devicesets").Elements("deviceset").First(n => n.Attribute("name").Value == part.Attribute("deviceset").Value);
                        var device = deviceset.Element("devices").Elements("device").First(n => n.Attribute("name").Value == part.Attribute("device").Value);
                        if (device.Attribute("package") != null)
                        {

                            var packages = library.Element("packages");
                            if (packages.HasElements)
                            {
                                var package = packages.Elements("package").First(n => n.Attribute("name").Value == device.Attribute("package").Value);
                                var smDs = package.Elements("smd");
                                if (smDs.Count() != 0)
                                {
                                    var smDlsit = new List<Smd>();
                                    foreach (var smd in smDs)
                                    {
                                        var smDname = smd.Attribute("name").Value;
                                        double[] coords = { Convert.ToDouble(smd.Attribute("x").Value), Convert.ToDouble(smd.Attribute("y").Value), 0};
                                        double[] dims = { Convert.ToDouble(smd.Attribute("dx").Value), Convert.ToDouble(smd.Attribute("dy").Value) };
                                        var SMD = new Smd(smDname, coords, dims);
                                        smDlsit.Add(SMD);
                                    }

                                    var fPname = package.Attribute("name").Value;
                                    var footprint = new Footprint(fPname, smDlsit);

                                    var comp = new Component(partName, footprint, index);
                                    design.add_comp(comp);
                                    index++;
                                }
                            }
                                               
                        }
                    }
                }
                design.CompCount = index;
            }

        }

        

        private static void ImportCompModels(Design design)
        {
            for (var i = 0; i < CompNames.Count(); i++)
            {
                var filename = CompNames[i];
                Console.WriteLine("Attempting: " + filename);
                Stream fileStream;
                TessellatedSolid ts;
                using (fileStream = File.OpenRead(filename))
                      ts = TVGL.IOFunctions.IO.Open(fileStream, filename)[0];
                var name = GetNameFromFileName(filename);
                var comp = design.Components.Find(x => x.Name == name);
                comp.Ts = ts;
                foreach (var smd in comp.Footprint.Pads)
                {
                    smd.Coord[0] += smd.Coord[0] + comp.Ts.Center[0];
                    smd.Coord[1] += smd.Coord[1] + comp.Ts.Center[1];
                    smd.Coord[2] = comp.Ts.ZMin;
                }
            }
        }

        private static string GetNameFromFileName(string filename)
        {
            var startIndex = filename.LastIndexOf('/') + 1;
            if (startIndex == -1) startIndex = filename.LastIndexOf('\\') + 1;
            var endIndex = filename.IndexOf('.', startIndex);
            if (endIndex == -1) endIndex = filename.Length - 1;
            return filename.Substring(startIndex, endIndex - startIndex);
        }



        public static void ImportNetlist(Design desgin)
        {
            var doc = XDocument.Load("Designs/555LED.sch");
            var Sheets = doc.Element("eagle").Element("drawing").Element("schematic").Element("sheets");
            if (Sheets != null)
            {
                var sheets = Sheets.Elements("sheet");
                var sheet = sheets.First();
                var nets = sheet.Element("nets").Elements("net");
                foreach (var net in nets)
                {
                    var Net = new Net();
                    Net.Netname = net.Attribute("name").Value;
                    var segments = net.Elements("segment");
                    foreach (var segment in segments)
                    {
                        var pinrefs = segment.Elements("pinref");
                        foreach (var pinref in pinrefs)
                        {
                            var Pinref = new PinRef(pinref.Attribute("part").Value, pinref.Attribute("pin").Value);
                            Net.PinRefs.Add(Pinref);
                        }
                    }
                    desgin.Netlist.Add(Net);
                }
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function prints out component information.                                    */
        /* ---------------------------------------------------------------------------------- */

        private static void FprintData(Design design, Component comp)
        {
            using (var writetext = new StreamWriter("/comp.out"))
            {
                writetext.WriteLine("\nComponent name is {0} and the orientation is {1}", comp.Name);
                for (var i = 0; i < 3; i++)
                {
                    Console.WriteLine("coord {0} is {1}", i, comp.Ts.Center[i]);
                }
                writetext.WriteLine("");
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function writes Accepted steps to a file.                                     */
        /* ---------------------------------------------------------------------------------- */

        private static void WriteStep(Design design, int iteration, int flag)
        {

            if (iteration == 0)         //????????????????????
            {
                var f1 = new StreamWriter("/ratio.out");
                var f2 = new StreamWriter("/container.out");
                var f3 = new StreamWriter("/overlap.out");
                var f4 = new StreamWriter("/coef.out");
                var f5 = new StreamWriter("/overlap2.out");


                if (flag != 0)
                {

                    f1.WriteLine("", iteration, design.NewObjValues[0]);

                    f3.WriteLine("", iteration,
                        (design.NewObjValues[1] * design.Weight[1]));
                }

                f1.Close();
                f2.Close();
                f3.Close();
                f4.Close();
                f5.Close();
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function writes data regarding the last temperature to a file.                */
        /* ---------------------------------------------------------------------------------- */

        public static void WriteLoopData(double t, int stepsAtT, int acceptCount, int badAcceptCount, int genLimit, int flag)
        {
            using (var writetext = new StreamWriter("/temperature.out"))
            {
                if (flag == 1)
                {
                    writetext.WriteLine("Temperature set at {0}", t);
                    writetext.WriteLine("At this temperature {0} steps were taken.  {1} were Accepted",
                        stepsAtT, acceptCount);
                    writetext.WriteLine("Of the Accepted steps, {0} were inferior steps", badAcceptCount);
                    writetext.WriteLine("Equilibrium was ");
                    if (stepsAtT > genLimit)

                        writetext.WriteLine("not ");
                    writetext.WriteLine("reached at this temperature\n");
                }
                else if (flag == 2)
                    writetext.WriteLine("Design is now frozen\n\n");
                else if (flag == 3)
                {
                    writetext.WriteLine("Temperature set at infinity (DownHill search)");
                    writetext.WriteLine("{0} steps were taken.  {1} were Accepted\n\n", stepsAtT, acceptCount);
                }
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION PRINTS OUT COMPONENT INFORMATION.                                    */
        /* ---------------------------------------------------------------------------------- */

        private static void PrintData(Design design, Component comp)
        {

            Console.WriteLine("\nComponent name is {0} and the orientation is {1}", comp.Name);

            //i = -1;
            //while (++i < 3)
            //    Console.WriteLine("dim {0} is {1}", i, comp.dim[i]);

            //i = -1;
            //while (++i < 3)
            //    Console.WriteLine("dim_initial {0} is {1}", i, comp.dim_initial[i]);

            for (var i = 0; i < 3; i++)
            {
                Console.WriteLine("coord {0} is {1}", i, comp.Ts.Center[i]);
            }
            Console.WriteLine("");
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function writes component data to a file.                                     */
        /* ---------------------------------------------------------------------------------- */

        private static void PrintCompData(Design design)
        {

            foreach (var comp in design.Components)
            {
                FprintData(design, comp);
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function prints out overlap data.                                             */
        /* ---------------------------------------------------------------------------------- */

        private static void PrintOverlaps(Design design)
        {
            int i, j;

            i = -1;
            while (++i < design.CompCount)
            {
                j = -1;
                while (++j <= i)
                    Console.WriteLine("{0} ", design.Overlap[i, j]);
                Console.WriteLine("");
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function writes design data to a file.                                        */
        /* ---------------------------------------------------------------------------------- */

        public static void SaveDesign(Design design)
        {
            int i, j;
            double avgOldValue;
            Component comp;

            Console.WriteLine("Saving current design");

            i = -1;
            avgOldValue = 0.0;
            while (++i < Constants.BalanceAvg)
                avgOldValue += design.OldObjValues[1, i];
            avgOldValue /= Constants.BalanceAvg;

            using (var writetext = new StreamWriter("/design.data"))
            {
                i = 0;
                comp = design.Components[0];
                while (++i <= design.CompCount)
                {
                    //writetext.WriteLine("{0} {1} {2}", comp.name, comp.shape_type, comp.orientation);
                    //writetext.WriteLine("{0} {1} {2}", comp.dim_initial[0], comp.dim_initial[1], comp.dim_initial[2]);
                    //writetext.WriteLine("{0} {1} {2}", comp.dim[0], comp.dim[1], comp.dim[2]);
                    //writetext.WriteLine("{0} {1} {2}", comp.ts[0].Center[0], comp.ts[0].Center[1], comp.ts[0].Center[2]);
                    //writetext.WriteLine("{0} {1}  {2}", comp.half_area, comp.mass, comp.temp);
                    if (i < design.CompCount)

                        comp = design.Components[i];
                }
                writetext.WriteLine("{0} {1} {2} {3}", design.NewObjValues[1], design.Coef[1],
                    design.Weight[1], avgOldValue);

            }

        }

        /* ---------------------------------------------------------------------------------- */
        /* This function writes container data to a file.                                        */
        /* ---------------------------------------------------------------------------------- */

        public static void SaveContainer(Design design)
        {
            int i, j;
            double avgOldValue;
            var boxDim = new double[3];
            Component comp;

            Console.WriteLine("Saving current container");

            using (var writetext = new StreamWriter("/container.data"))
            {
                boxDim[0] = design.BoxMax[0] - design.BoxMin[0];
                boxDim[1] = design.BoxMax[1] - design.BoxMin[1];
                boxDim[2] = design.BoxMax[2] - design.BoxMin[2];

                writetext.WriteLine("container B {0}", 1);
                writetext.WriteLine("{0} {1} {2}", boxDim[0], boxDim[1], boxDim[2]);
                writetext.WriteLine("{0} {1} {2}", boxDim[0], boxDim[1], boxDim[2]);
               
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION READS DESIGN DATA FROM A FILE.                                       */
        /* ---------------------------------------------------------------------------------- */

        public static void RestoreDesign(Design design)
        {
            int i, j;
            double avgOldValue;
            Component comp;

            Console.WriteLine("Restoring saved design");

            using (var readtext = new StreamReader("/design.data"))
            {
                i = 0;
                comp = design.Components[0];
                string line;
                while (++i <= design.CompCount)
                {
                    line = readtext.ReadLine();
                    var items = line.Split(' ');
                    comp.Name = items[0];
                    //comp.shape_type = items[1];
                    //comp.orientation = Convert.ToInt16(items[2]);

                    line = readtext.ReadLine();
                    items = line.Split(' ');
                    //comp.dim_initial[0] = Convert.ToDouble(items[0]);
                    //comp.dim_initial[1] = Convert.ToDouble(items[1]);
                    //comp.dim_initial[2] = Convert.ToDouble(items[2]);

                    line = readtext.ReadLine();
                    items = line.Split(' ');
                    //comp.dim[0] = Convert.ToDouble(items[0]);
                    //comp.dim[1] = Convert.ToDouble(items[1]);
                    //comp.dim[2] = Convert.ToDouble(items[2]);

                    line = readtext.ReadLine();
                    items = line.Split(' ');
                    //comp.ts[0].Center[0] = Convert.ToDouble(items[0]);
                    //comp.ts[0].Center[1] = Convert.ToDouble(items[1]);
                    //comp.ts[0].Center[2] = Convert.ToDouble(items[2]);

                    line = readtext.ReadLine();
                    items = line.Split(' ');
                    //comp.half_area = Convert.ToDouble(items[0]);
                    //comp.mass = Convert.ToDouble(items[1]);
                    comp.Temp = Convert.ToDouble(items[2]);
                    if (i < design.CompCount)
                        comp = design.Components[i];
                }
                line = readtext.ReadLine();
                var items2 = line.Split(' ');
                design.NewObjValues[1] = Convert.ToDouble(items2[0]);
                design.Coef[1] = Convert.ToDouble(items2[1]);
                design.Weight[1] = Convert.ToDouble(items2[2]);
                avgOldValue = Convert.ToDouble(items2[3]);
            }

            //Program.InitBounds(design);
            //ObjFunction.InitOverlaps(design);
            i = -1;
            while (++i < Constants.ObjNum)
            {
                j = -1;
                while (++j < Constants.BalanceAvg)
                    design.OldObjValues[i, j] = avgOldValue;
            }
        }

      
        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION WRITES THE FINAL TEMPERATURE FIELD TO A FILE.                        */
        /* ---------------------------------------------------------------------------------- */

        public static void SaveTfield(Design design)
        {
            var k = 0;
            Console.WriteLine("Saving current tfield");
            var f1 = new StreamWriter("/tfield.data");

            while (design.Tfield[k].Temp != 0.0)
            {
                f1.WriteLine("", design.Tfield[k].Coord[0],
                    design.Tfield[k].Coord[1], design.Tfield[k].Coord[2],
                    design.Tfield[k].Temp);
                /*if (design.tfield[k].coord[2] == 0.0) 
    fprintf(fptr, "%lf %lf %lf", design.tfield[k].coord[0], 
    design.tfield[k].coord[1], design.tfield[k].temp);*/
                ++k;
            }
            f1.Close();
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function restores the temperature field to a old_temp's.                       */
        /* ---------------------------------------------------------------------------------- */

        private static void RestoreTfield(Design design)
        {
            var k = 0;
            Console.WriteLine("Restoring tfield");

            using (var readtext = new StreamReader("datafile1"))
            {
                string line;
                while ((line = readtext.ReadLine()) != null)
                {
                    var items = line.Split('\t', ' ');
                    design.Tfield[k].Coord[0] = Convert.ToDouble(items[0]);
                    design.Tfield[k].Coord[1] = Convert.ToDouble(items[1]);
                    design.Tfield[k].Coord[2] = Convert.ToDouble(items[2]);
                    design.Tfield[k].OldTemp = Convert.ToDouble(items[3]);
                }
            }

        }
    }
}
