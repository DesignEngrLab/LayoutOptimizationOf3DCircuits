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
    internal static class Io
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
            ImportCompFeatures(design);
        }

        private static void ImportContainer(Design design)
        {
            ImportContModel(design);
            ImportContFeatures(design);
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
                        var connects = device.Element("connects"); 
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
                                        var smdName = smd.Attribute("name").Value;
                                        double[] coords = { Convert.ToDouble(smd.Attribute("x").Value), Convert.ToDouble(smd.Attribute("y").Value), 0};
                                        double[] dims = { Convert.ToDouble(smd.Attribute("dx").Value), Convert.ToDouble(smd.Attribute("dy").Value) };
                                        var pinName = connects.Elements("connect").First(n => n.Attribute("pad").Value == smd.Attribute("name").Value).Attribute("pin").Value;        
                                        var SMD = new Smd(pinName, smdName, coords, dims);
                                        smDlsit.Add(SMD);
                                    }

                                    var fPname = package.Attribute("name").Value;
                                    var footprint = new Footprint(fPname, smDlsit);

                                    var comp = new Component(partName, footprint, index);
                                    design.AddComp(comp);
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
                    smd.Coord[0] = smd.Coord[0] + comp.Ts.Center[0];
                    smd.Coord[1] = smd.Coord[1] + comp.Ts.Center[1];
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



        public static void ImportNetlist(Design design)
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
                            var comp =
                                design.Components.Find(component => component.Name == pinref.Attribute("part").Value);
                            var Pinref = new PinRef(comp, pinref.Attribute("pin").Value);
                            Net.PinRefs.Add(Pinref);
                        }
                    }
                    design.Netlist.Add(Net);
                }
            }
        }
    }
}
