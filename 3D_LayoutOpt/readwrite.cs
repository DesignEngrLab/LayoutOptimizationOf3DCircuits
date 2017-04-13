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
    static class readwrite
    {
        private static readonly string[] CompNames =
        {
            "../../TestFiles/DxTopLevelPart2.shell"
        };
        private static readonly string ContainerName =
            "../../TestFiles/DxTopLevelPart2.shell";


        public static void ImportData(Design design)
        {
            ImportComponents(design);
            ImportContainer(design);
        }

        static void ImportComponents(Design design)
        {
            ImportFootprints(design);
            ImportCompModels(design);
            ImportCompFeatures(design);
        }

        static void ImportContainer(Design design)
        {
            ImportContModel(design);
            ImportContFeatures(design);
        }

        static void ImportContModel(Design design)
        {
            var filename = ContainerName;
            Console.WriteLine("Attempting: " + filename);
            Stream fileStream;
            List<TessellatedSolid> ts;
            using (fileStream = File.OpenRead(filename))
                ts = IO.Open(fileStream, filename);
            string name = GetNameFromFileName(filename);
            var container = new Container(name, ts);
            design.container = container;
        }

        static void ImportCompFeatures(Design design)
        {
            try
            {
                using (StreamReader readtext = new StreamReader("datafile1"))
                {
                    Console.WriteLine("Reading component data from file.");
                    string line;
                    while ((line = readtext.ReadLine()) != null)
                    {
                        string[] items = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        var compname = items[0];
                        var tempcrit = Convert.ToDouble(items[1]);
                        var q = Convert.ToDouble(items[2]);
                        var k = Convert.ToDouble(items[3]);
                        var comp = design.components.Find(x => x.name == compname);
                        comp.tempcrit = tempcrit;
                        comp.q = q;
                        comp.k = k;
                    }
                    Console.WriteLine("EOF reached in the input file.\n");
                }
            }
            catch (IOException ex)
            {
            }

        }

        static void ImportContFeatures(Design design)
        {
            try
            {
                using (StreamReader readtext = new StreamReader("datafile2"))
                {
                    Console.WriteLine("Reading container dimensions from file.");
                    string line;
                    while ((line = readtext.ReadLine()) != null)
                    {
                        string[] items = line.Split(' ');
                        var kb = Convert.ToDouble(items[0]);
                        var h0 = Convert.ToDouble(items[1]);
                        var h1 = Convert.ToDouble(items[2]);
                        var h2 = Convert.ToDouble(items[3]);
                        var tamb = Convert.ToDouble(items[4]);

                        design.kb = kb;
                        design.h[0] = h0;
                        design.h[1] = h1;
                        design.h[2] = h2;
                        design.tamb = tamb;

                    }
                }
            }
            catch (IOException ex)
            {

            }

        }


        public static void ImportFootprints(Design design)
        {

            XDocument doc = XDocument.Load("arduino_Uno.sch");
            XElement Sheets = doc.Element("eagle").Element("drawing").Element("schematic").Element("sheets");
            XElement Parts = doc.Element("eagle").Element("drawing").Element("schematic").Element("parts");
            IEnumerable<XElement> parts = Parts.Elements("part");
            if (Sheets != null)
            {
                IEnumerable<XElement> sheets = Sheets.Elements("sheet");
                var sheet = sheets.First();
                IEnumerable<XElement> instances = sheet.Element("instances").Elements("instance");
                int index = 0;
                foreach (var instance in instances) 
                {
                    string PartName = instance.FirstAttribute.Value;
                    var part = (doc.Element("eagle").Element("drawing").Element("schematic").Element("parts").Elements("part").Where(n => n.Attribute("name").Value == PartName)).First();
                    var library = (doc.Element("eagle").Element("drawing").Element("schematic").Element("libraries").Elements("library").Where(n => n.Attribute("name").Value == part.Attribute("library").Value)).First();
                    var deviceset = library.Element("devicesets").Elements("deviceset").Where(n => n.Attribute("name").Value == part.Attribute("deviceset").Value).First();
                    var device = deviceset.Element("devices").Elements("device").Where(n => n.Attribute("name").Value == part.Attribute("device").Value).First();
                    if (device.Attribute("package") != null)
                    {

                        var Packages = library.Element("packages");
                        if (Packages.HasElements)
                        {
                            var package = Packages.Elements("package").Where(n => n.Attribute("name").Value == device.Attribute("package").Value).First();
                            var SMDs = package.Elements("smd");
                            if (SMDs.Count() != 0)
                            {
                                List<SMD> SMDlsit = new List<SMD>();
                                foreach (var smd in SMDs)
                                {
                                    string SMDname = smd.Attribute("name").Value;
                                    double[] coords = new double[] { Convert.ToDouble(smd.Attribute("x").Value), Convert.ToDouble(smd.Attribute("y").Value),  };
                                    double[] dims = new double[] { Convert.ToDouble(smd.Attribute("dx").Value), Convert.ToDouble(smd.Attribute("dy").Value) };
                                    var SMD = new SMD(SMDname, coords, dims);
                                    SMDlsit.Add(SMD);
                                }

                                string FPname = package.Attribute("name").Value;
                                var Footprint = new Footprint(FPname, SMDlsit);

                                var comp = new Component(PartName, Footprint, index);
                                design.add_comp(comp);
                                index++;
                            }
                        }
                                               
                    }
                    
                }
                design.comp_count = index;
            }

        }

        static void ImportCompModels(Design design)
        {
            for (var i = 0; i < CompNames.Count(); i++)
            {
                var filename = CompNames[i];
                Console.WriteLine("Attempting: " + filename);
                Stream fileStream;
                List<TessellatedSolid> ts;
                using (fileStream = File.OpenRead(filename))
                    ts = IO.Open(fileStream, filename);
                string name = GetNameFromFileName(filename);
                var comp = design.components.Find(x => x.name == name);
                comp.ts = ts;
                foreach (var smd in comp.footprint.pads)
                {
                    smd.coord[2] = comp.ts[0].ZMin;
                }
            }
        }

        static string GetNameFromFileName(string filename)
        {
            var startIndex = filename.LastIndexOf('/') + 1;
            if (startIndex == -1) startIndex = filename.LastIndexOf('\\') + 1;
            var endIndex = filename.IndexOf('.', startIndex);
            if (endIndex == -1) endIndex = filename.Length - 1;
            return filename.Substring(startIndex, endIndex - startIndex);
        }



        public static void ImportNetlist(Design desgin)
        {
            XDocument doc = XDocument.Load("arduino_Uno.sch");
            XElement Sheets = doc.Element("eagle").Element("drawing").Element("schematic").Element("sheets");
            if (Sheets != null)
            {
                IEnumerable<XElement> sheets = Sheets.Elements("sheet");
                var sheet = sheets.First();
                IEnumerable<XElement> nets = sheet.Element("nets").Elements("net");
                foreach (var net in nets)
                {
                    Net Net = new Net();
                    Net.Netname = net.Attribute("name").Value;
                    var segments = net.Elements("segment");
                    foreach (var segment in segments)
                    {
                        var pinrefs = segment.Elements("pinref");
                        foreach (var pinref in pinrefs)
                        {
                            PinRef Pinref = new PinRef(pinref.Attribute("part").Value, pinref.Attribute("pin").Value);
                            Net.PinRefs.Add(Pinref);
                        }
                    }
                    desgin.Netlist.Add(Net);
                }
            }
        }


            /* ---------------------------------------------------------------------------------- */
            /* This function gets component data from a file.                                     */
            /* ---------------------------------------------------------------------------------- */




            /* ---------------------------------------------------------------------------------- */
            /*                                                                                    */
            /*                                    READWRITE.C                                      */
            /*                                                                                    */
            /* ---------------------------------------------------------------------------------- */

            /* ---------------------------------------------------------------------------------- */
            /* This function prints out component information.                                    */
            /* ---------------------------------------------------------------------------------- */

            static void fprint_data(Design design, int which)
        {
            int i;
            Component comp;

            using (StreamWriter writetext = new StreamWriter("/comp.out"))
            {
                comp = design.components[0];
                i = 1;
                while (++i <= which)
                    comp = design.components[i];


                writetext.WriteLine("\nComponent name is {0} and the orientation is {1}", comp.name, comp.orientation);

                i = -1;
                while (++i < 3)
                    writetext.WriteLine("dim {0} is {1}", i, comp.dim[i]);

                i = -1;
                while (++i < 3)
                    writetext.WriteLine("dim_initial {0} is {1}", i, comp.dim_initial[i]);

                i = -1;
                while (++i < 3)
                    writetext.WriteLine("current dim {0} is {1}", i, comp.dim[i]);

                i = -1;
                while (++i < 3)
                    writetext.WriteLine("coord {0} is {1}", i, comp.coord[i]);

                writetext.WriteLine("");
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function writes Accepted steps to a file.                                     */
        /* ---------------------------------------------------------------------------------- */

        static void write_step(Design design, int iteration, int flag)
        {

            if (iteration == 0)         //????????????????????
            {
                StreamWriter F1 = new StreamWriter("/ratio.out");
                StreamWriter F2 = new StreamWriter("/container.out");
                StreamWriter F3 = new StreamWriter("/overlap.out");
                StreamWriter F4 = new StreamWriter("/coef.out");
                StreamWriter F5 = new StreamWriter("/overlap2.out");


                if (flag != 0)
                {

                    F1.WriteLine("", iteration, design.new_obj_values[0]);

                    F3.WriteLine("", iteration,
                        (design.new_obj_values[1] * design.weight[1]));
                }

                F1.Close();
                F2.Close();
                F3.Close();
                F4.Close();
                F5.Close();
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function writes data regarding the last temperature to a file.                */
        /* ---------------------------------------------------------------------------------- */

        public static void WriteLoopData(double t, int steps_at_t, int Accept_count, int bad_Accept_count, int gen_limit, int flag)
        {
            using (StreamWriter writetext = new StreamWriter("/temperature.out"))
            {
                if (flag == 1)
                {
                    writetext.WriteLine("Temperature set at {0}", t);
                    writetext.WriteLine("At this temperature {0} steps were taken.  {1} were Accepted",
                        steps_at_t, Accept_count);
                    writetext.WriteLine("Of the Accepted steps, {0} were inferior steps", bad_Accept_count);
                    writetext.WriteLine("Equilibrium was ");
                    if (steps_at_t > gen_limit)

                        writetext.WriteLine("not ");
                    writetext.WriteLine("reached at this temperature\n");
                }
                else if (flag == 2)
                    writetext.WriteLine("Design is now frozen\n\n");
                else if (flag == 3)
                {
                    writetext.WriteLine("Temperature set at infinity (DownHill search)");
                    writetext.WriteLine("{0} steps were taken.  {1} were Accepted\n\n", steps_at_t, Accept_count);
                }
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function prints out component information.                                    */
        /* ---------------------------------------------------------------------------------- */

        static void print_data(Design design, int which)
        {
            int i;
            Component comp;

            comp = design.components[0];
            i = 1;
            while (++i <= which)
                comp = design.components[i];


            Console.WriteLine("\nComponent name is {0} and the orientation is {1}", comp.name, comp.orientation);

            i = -1;
            while (++i < 3)
                Console.WriteLine("dim {0} is {1}", i, comp.dim[i]);

            i = -1;
            while (++i < 3)
                Console.WriteLine("dim_initial {0} is {1}", i, comp.dim_initial[i]);

            i = -1;
            while (++i < 3)
                Console.WriteLine("coord {0} is {1}", i, comp.coord[i]);
            Console.WriteLine("");
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function writes component data to a file.                                     */
        /* ---------------------------------------------------------------------------------- */

        static void print_comp_data(Design design)
        {
            int i;

            i = 0;
            while (++i <= Constants.COMP_NUM)
                fprint_data(design, i);
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function prints out overlap data.                                             */
        /* ---------------------------------------------------------------------------------- */

        static void print_overlaps(Design design)
        {
            int i, j;

            i = -1;
            while (++i < Constants.COMP_NUM)
            {
                j = -1;
                while (++j <= i)
                    Console.WriteLine("{0} ", design.overlap[i, j]);
                Console.WriteLine("");
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function writes design data to a file.                                        */
        /* ---------------------------------------------------------------------------------- */

        public static void save_design(Design design)
        {
            int i, j;
            double avg_old_value;
            Component comp;

            Console.WriteLine("Saving current design");

            i = -1;
            avg_old_value = 0.0;
            while (++i < Constants.BALANCE_AVG)
                avg_old_value += design.old_obj_values[1, i];
            avg_old_value /= Constants.BALANCE_AVG;

            using (StreamWriter writetext = new StreamWriter("/design.data"))
            {
                i = 0;
                comp = design.components[0];
                while (++i <= Constants.COMP_NUM)
                {
                    //writetext.WriteLine("{0} {1} {2}", comp.name, comp.shape_type, comp.orientation);
                    //writetext.WriteLine("{0} {1} {2}", comp.dim_initial[0], comp.dim_initial[1], comp.dim_initial[2]);
                    //writetext.WriteLine("{0} {1} {2}", comp.dim[0], comp.dim[1], comp.dim[2]);
                    //writetext.WriteLine("{0} {1} {2}", comp.coord[0], comp.coord[1], comp.coord[2]);
                    //writetext.WriteLine("{0} {1}  {2}", comp.half_area, comp.mass, comp.temp);
                    if (i < Constants.COMP_NUM)

                        comp = design.components[i];
                }
                writetext.WriteLine("{0} {1} {2} {3}", design.new_obj_values[1], design.coef[1],
                    design.weight[1], avg_old_value);

            }

        }

        /* ---------------------------------------------------------------------------------- */
        /* This function writes container data to a file.                                        */
        /* ---------------------------------------------------------------------------------- */

        public static void save_container(Design design)
        {
            int i, j;
            double avg_old_value;
            double[] box_dim = new double[3];
            Component comp;

            Console.WriteLine("Saving current container");

            using (StreamWriter writetext = new StreamWriter("/container.data"))
            {
                box_dim[0] = design.box_max[0] - design.box_min[0];
                box_dim[1] = design.box_max[1] - design.box_min[1];
                box_dim[2] = design.box_max[2] - design.box_min[2];

                writetext.WriteLine("container B {0}", 1);
                writetext.WriteLine("{0} {1} {2}", box_dim[0], box_dim[1], box_dim[2]);
                writetext.WriteLine("{0} {1} {2}", box_dim[0], box_dim[1], box_dim[2]);
                writetext.WriteLine("{0} {1} {2}", (design.box_min[0] + box_dim[0] / 2),
                    (design.box_min[1] + box_dim[1] / 2),
                    (design.box_min[2] + box_dim[2] / 2));
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function reads design data from a file.                                       */
        /* ---------------------------------------------------------------------------------- */

        public static void RestoreDesign(Design design)
        {
            int i, j;
            double avg_old_value;
            Component comp;

            Console.WriteLine("Restoring saved design");

            using (StreamReader readtext = new StreamReader("/design.data"))
            {
                i = 0;
                comp = design.components[0];
                string line;
                while (++i <= Constants.COMP_NUM)
                {
                    line = readtext.ReadLine();
                    string[] items = line.Split(' ');
                    comp.name = items[0];
                    comp.shape_type = items[1];
                    comp.orientation = Convert.ToInt16(items[2]);

                    line = readtext.ReadLine();
                    items = line.Split(' ');
                    comp.dim_initial[0] = Convert.ToDouble(items[0]);
                    comp.dim_initial[1] = Convert.ToDouble(items[1]);
                    comp.dim_initial[2] = Convert.ToDouble(items[2]);

                    line = readtext.ReadLine();
                    items = line.Split(' ');
                    comp.dim[0] = Convert.ToDouble(items[0]);
                    comp.dim[1] = Convert.ToDouble(items[1]);
                    comp.dim[2] = Convert.ToDouble(items[2]);

                    line = readtext.ReadLine();
                    items = line.Split(' ');
                    comp.coord[0] = Convert.ToDouble(items[0]);
                    comp.coord[1] = Convert.ToDouble(items[1]);
                    comp.coord[2] = Convert.ToDouble(items[2]);

                    line = readtext.ReadLine();
                    items = line.Split(' ');
                    comp.half_area = Convert.ToDouble(items[0]);
                    comp.mass = Convert.ToDouble(items[1]);
                    comp.temp = Convert.ToDouble(items[2]);
                    if (i < Constants.COMP_NUM)
                        comp = design.components[i];
                }
                line = readtext.ReadLine();
                string[] items2 = line.Split(' ');
                design.new_obj_values[1] = Convert.ToDouble(items2[0]);
                design.coef[1] = Convert.ToDouble(items2[1]);
                design.weight[1] = Convert.ToDouble(items2[2]);
                avg_old_value = Convert.ToDouble(items2[3]);
            }

            Program.init_bounds(design);
            obj_function.InitOverlaps(design);
            i = -1;
            while (++i < Constants.OBJ_NUM)
            {
                j = -1;
                while (++j < Constants.BALANCE_AVG)
                    design.old_obj_values[i, j] = avg_old_value;
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function writes to a file: the current Move probabilities, the percentage     */
        /* change in delta_c due to each Move, and the percentage of attempts for each Move.  */
        /* ---------------------------------------------------------------------------------- */

        public static void write_probs(Hustin hustin, double temp)
        {
            int i, total_attempts;
            double total_delta_c;

            total_attempts = 0;
            total_delta_c = 0;

            i = -1;
            while (++i < Constants.MOVE_NUM)
            {
                total_attempts += hustin.attempts[i];
                total_delta_c += hustin.delta_c[i];
            }

            StreamWriter F1 = new StreamWriter("/probs.out");
            StreamWriter F2 = new StreamWriter("/delta_c.out");
            StreamWriter F3 = new StreamWriter("/attempts.out");


            F1.WriteLine(temp);
            F2.WriteLine(temp);
            F3.WriteLine(temp);

            i = -1;
            while (++i < Constants.MOVE_NUM)
            {
                F1.WriteLine(hustin.prob[i]);
                F2.WriteLine(hustin.delta_c[i] / total_delta_c);
                F3.WriteLine(1 * hustin.attempts[i] / total_attempts);
            }
            F1.WriteLine("");
            F2.WriteLine("");
            F3.WriteLine("");
            F1.Close();
            F2.Close();
            F3.Close();
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function writes the final temperature field to a file.                        */
        /* ---------------------------------------------------------------------------------- */

        public static void save_tfield(Design design)
        {
            int k = 0;
            Console.WriteLine("Saving current tfield");
            StreamWriter F1 = new StreamWriter("/tfield.data");

            while (design.tfield[k].temp != 0.0)
            {
                F1.WriteLine("", design.tfield[k].coord[0],
                    design.tfield[k].coord[1], design.tfield[k].coord[2],
                    design.tfield[k].temp);
                /*if (design.tfield[k].coord[2] == 0.0) 
    fprintf(fptr, "%lf %lf %lf", design.tfield[k].coord[0], 
    design.tfield[k].coord[1], design.tfield[k].temp);*/
                ++k;
            }
            F1.Close();
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function restores the temperature field to a old_temp's.                       */
        /* ---------------------------------------------------------------------------------- */

        static void restore_tfield(Design design)
        {
            int k = 0;
            Console.WriteLine("Restoring tfield");

            using (StreamReader readtext = new StreamReader("datafile1"))
            {
                string line;
                while ((line = readtext.ReadLine()) != null)
                {
                    string[] items = line.Split('\t', ' ');
                    design.tfield[k].coord[0] = Convert.ToDouble(items[0]);
                    design.tfield[k].coord[1] = Convert.ToDouble(items[1]);
                    design.tfield[k].coord[2] = Convert.ToDouble(items[2]);
                    design.tfield[k].old_temp = Convert.ToDouble(items[3]);
                }
            }

        }
    }
}
