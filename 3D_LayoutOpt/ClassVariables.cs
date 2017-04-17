using System.Collections.Generic;
using TVGL;
using OptimizationToolbox;
using System;
using StarMathLib;

namespace _3D_LayoutOpt
{

    public class Component
    {
        public TessellatedSolid ts = null;
        public TessellatedSolid backup_ts = null;
        public Footprint footprint = null;
        public Footprint backup_footprint = null;
        public int node_center, nodes;                        //TO DO: WHAT ARE NODE and NODE CENTER?
        public double temp, tempcrit, q, k;
        public string name;
        public int index;


        public Component(string CmpName, Footprint FP, int CmpIndex)
        {
            name = CmpName;
            footprint = FP;
            index = CmpIndex;
        }

        public void BackupComponent()
        {
            backup_ts = ts;
            backup_footprint = footprint;
        }

        public void RevertComponent()
        {
            ts = backup_ts;
            footprint = backup_footprint;
        }

        internal void Update(double[] coord)
        {
            var x = coord[0];
            var y = coord[1];
            var z = coord[2];
            var thetaX = coord[3];
            var thetaY = coord[4];
            var thetaZ = coord[5];
            var TransformMatrix = new double[,]
                 {
                    {
                         Math.Cos(thetaX) * Math.Cos(thetaY),
                         Math.Cos(thetaX) * Math.Sin(thetaY) * Math.Sin(thetaZ) - Math.Sin(thetaX) * Math.Cos(thetaZ),
                         Math.Cos(thetaX) * Math.Sin(thetaY) * Math.Cos(thetaZ) + Math.Sin(thetaX) * Math.Sin(thetaZ),
                         x },
                    {
                         Math.Sin(thetaX) * Math.Cos(thetaY),
                         Math.Sin(thetaX) * Math.Sin(thetaY) * Math.Sin(thetaZ) + Math.Cos(thetaX) * Math.Cos(thetaZ),
                         Math.Sin(thetaX) * Math.Sin(thetaY) * Math.Cos(thetaZ) - Math.Cos(thetaX) * Math.Sin(thetaZ),
                         y },
                    {
                         -1 * Math.Sin(thetaY),
                         Math.Cos(thetaY) * Math.Sin(thetaZ),
                         Math.Cos(thetaY) * Math.Cos(thetaZ),
                         z },
                    {0.0, 0.0, 0.0, 1.0}
                 };
            ts.Transform(TransformMatrix);
            //UPDATING THE PIN COORDINATES
            foreach (SMD smd in footprint.pads)
                smd.coord = TransformMatrix.multiply(new[] { smd.coord[0], smd.coord[0], smd.coord[0], 1 });
        }
    }

    public class Container
    {
        public string Name;
        public TessellatedSolid ts;

        public Container(string containername, TessellatedSolid tessellatedSolid)
        {
            Name = containername;
            ts = tessellatedSolid;
        }
    }

    public class Footprint
    {
        public string name;
        public List<SMD> pads = null;

        public Footprint(string FPname, List<SMD> SMDpads)
        {
            name = FPname;
            pads = SMDpads;
        }
    }

    public class SMD
    {
        public string name;
        public double[] coord;
        public double[] dim;

        public SMD(string SMDname, double[] coordinates, double[] dimensions)
        {
            name = SMDname;
            coord = coordinates;
            dim = dimensions;
        }
    }

    public class PinRef
    {
        public int CompIndex;
        public string CompName;
        public string PinName;

        public PinRef(string Component, string Pin)
        {
            CompName = Component;
            PinName = Pin;
        }
    }

    public class Net
    {
        public string Netname;
        public List<PinRef> PinRefs = null;
        public double NetLength = 0;

        public void CalcNetDirectLineLength(Design design)
        {
            for (int i = 0; i < PinRefs.Count - 1; i++)
            {
                for (int j = i + 1; j < PinRefs.Count; j++)
                {
                    SMD PinJ = design.components[PinRefs[j].CompIndex].footprint.pads.Find(smd => smd.name == PinRefs[j].PinName);
                    SMD PinI = design.components[PinRefs[i].CompIndex].footprint.pads.Find(smd => smd.name == PinRefs[i].PinName);
                    double d = (PinJ.coord[0] - PinI.coord[0]) * (PinJ.coord[0] - PinI.coord[0]) + (PinJ.coord[1] - PinI.coord[1]) * (PinJ.coord[1] - PinI.coord[1]) + (PinJ.coord[2] - PinI.coord[2]) * (PinJ.coord[2] - PinI.coord[2]);
                    NetLength += Math.Sqrt(d);
                }
            }
        }
    }



    public class TemperatureNode
    {
        /* A matrix of such structures mark each node of the temperature field.  There will   */
        /* be a total of COMP_NUM squared of these components.  A zero in the comp variable   */
        /* means that the node does not refer to the center of a component but merely to a    */
        /* a resistor junction.  A non-zero number refers to that component in the list of    */
        /* components.                                                                        */
        public Component comp, innercomp;
        public double[] coord = new double[3];
        public double prev_temp, old_temp, temp;
        public double vol, k;
    }
}