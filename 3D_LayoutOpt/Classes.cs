﻿using System.Collections.Generic;
using TVGL;
using OptimizationToolbox;
using System;
using System.Linq;
using StarMathLib;

namespace _3D_LayoutOpt
{

    public class Component
    {
        public TessellatedSolid Ts = null;
        public TessellatedSolid BackupTs = null;
        public Footprint Footprint = null;
        public Footprint BackupFootprint = null;
        public int NodeCenter, Nodes;                        //TO DO: WHAT ARE NODE and NODE CENTER?
        public double Temp, Tempcrit, Q, K;
        public string Name;
        public int Index;


        public Component(string cmpName, Footprint fp, int cmpIndex)
        {
            Name = cmpName;
            Footprint = fp;
            Index = cmpIndex;
        }

        public void BackupComponent()
        {
            BackupTs = Ts;
            BackupFootprint = Footprint;
        }

        public void RevertComponent()
        {
            Ts = BackupTs;
            Footprint = BackupFootprint;
        }

        internal void SetCompToZero()
        {
            double[,] backTransformMatrix = null;
            Ts = Ts.SetToOriginAndSquareTesselatedSolid(out backTransformMatrix);
            foreach (var smd in Footprint.Pads)
            {
                var tempCoord = backTransformMatrix.inverse().multiply(new[] { smd.Coord[0], smd.Coord[1], smd.Coord[2], 1 });
                smd.Coord = tempCoord.Take(tempCoord.Count() - 1).ToArray();
            }


        }

        internal void Update(double[] coord)
        {
            var x = coord[0];
            var y = coord[1];
            var z = coord[2];
            var thetaX = coord[3];
            var thetaY = coord[4];
            var thetaZ = coord[5];
            var center = Ts.Center;


            var translateMatrix1 = new double[,]
            {
                {1, 0, 0, -center[0]},
                {0, 1, 0, -center[1]},
                {0, 0, 1, -center[2]},
                {0, 0, 0, 1},
            };
            Ts.Transform(translateMatrix1);

            var transformMatrix = new double[,]
                 {
                    {
                         Math.Cos(thetaX) * Math.Cos(thetaY),
                         Math.Cos(thetaX) * Math.Sin(thetaY) * Math.Sin(thetaZ) - Math.Sin(thetaX) * Math.Cos(thetaZ),
                         Math.Cos(thetaX) * Math.Sin(thetaY) * Math.Cos(thetaZ) + Math.Sin(thetaX) * Math.Sin(thetaZ),
                         0 },
                    {
                         Math.Sin(thetaX) * Math.Cos(thetaY),
                         Math.Sin(thetaX) * Math.Sin(thetaY) * Math.Sin(thetaZ) + Math.Cos(thetaX) * Math.Cos(thetaZ),
                         Math.Sin(thetaX) * Math.Sin(thetaY) * Math.Cos(thetaZ) - Math.Cos(thetaX) * Math.Sin(thetaZ),
                         0 },
                    {
                         -1 * Math.Sin(thetaY),
                         Math.Cos(thetaY) * Math.Sin(thetaZ),
                         Math.Cos(thetaY) * Math.Cos(thetaZ),
                         0 },
                    {0.0, 0.0, 0.0, 1.0}
                 };
            Ts.Transform(transformMatrix);

            var translateMatrix2 = new double[,]
            {
                {1, 0, 0, center[0] + coord[0]},
                {0, 1, 0, center[1] + coord[1]},
                {0, 0, 1, center[2] + coord[2]},
                {0, 0, 0, 1},
            };
            Ts.Transform(translateMatrix2);

            //UPDATING THE PIN COORDINATES
            foreach (var smd in Footprint.Pads)
            {
                smd.Coord = translateMatrix1.multiply(new[] { smd.Coord[0], smd.Coord[1], smd.Coord[2], 1 });
                smd.Coord = transformMatrix.multiply(new[] { smd.Coord[0], smd.Coord[1], smd.Coord[2], 1 });
                smd.Coord = translateMatrix2.multiply(new[] { smd.Coord[0], smd.Coord[1], smd.Coord[2], 1 });
            }
        }
    }

    public class Container
    {
        public string Name;
        public TessellatedSolid Ts;

        public Container(string containerName, TessellatedSolid tessellatedSolid)
        {
            Name = containerName;
            Ts = tessellatedSolid;
        }

        internal void SetToZero()
        {
            var transformMatrix = new double[,]
            {
                {1, 0, 0, -Ts.Center[0]},
                {0, 1, 0, -Ts.Center[1]},
                {0, 0, 1, -Ts.Center[2]},
                {0, 0, 0, 1},
            };
            Ts.Transform(transformMatrix);
        }
    }

    public class Footprint
    {
        public string Name;
        public List<Smd> Pads = null;

        public Footprint(string fpName, List<Smd> smdPads)
        {
            Name = fpName;
            Pads = smdPads;
        }
    }

    public class Smd
    {
        public string pinName;
        public string Name;
        public double[] Coord;
        public double[] Dim;

        public Smd(string pinname, string smDname, double[] coordinates, double[] dimensions)
        {
            pinName = pinname;
            Name = smDname;
            Coord = coordinates;
            Dim = dimensions;
        }
    }

    public class PinRef
    {
        public Component Comp;
        public string PinName;

        public PinRef(Component component, string pin)
        {
            Comp = component;
            PinName = pin;
        }
    }

    public class Net
    {
        public string Netname;
        public List<PinRef> PinRefs = new List<PinRef>();
        public double NetLength = 0;
		private Dictionary<Tuple<Smd, Smd>, double> TerminalNodesSortedQueue;

		public void CreateEuclidianDistanceQueue(Design design)
		{
			var TerminalNodesQueue = new Dictionary<Tuple<Smd, Smd>, double>();
			for (int i = 0; i < PinRefs.Count - 1; i++)
			{
				for (int j = i + 1; j < PinRefs.Count; j++)
				{
					var pinJ = design.Components[PinRefs[j].Comp.Index].Footprint.Pads.Find(smd => smd.pinName == PinRefs[j].PinName);
					var pinI = design.Components[PinRefs[i].Comp.Index].Footprint.Pads.Find(smd => smd.pinName == PinRefs[i].PinName);
					var distance = Math.Sqrt(
						    (pinJ.Coord[0] - pinI.Coord[0]) * (pinJ.Coord[0] - pinI.Coord[0]) 
						  + (pinJ.Coord[1] - pinI.Coord[1]) * (pinJ.Coord[1] - pinI.Coord[1]) 
						  + (pinJ.Coord[2] - pinI.Coord[2]) * (pinJ.Coord[2] - pinI.Coord[2])
						  );
					var key = new Tuple<Smd, Smd>(pinI, pinJ);
					TerminalNodesQueue.Add(key, distance);
				}
			}
			TerminalNodesSortedQueue = TerminalNodesQueue.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
		}

		public void Route(Design design)
		{
            List<Smd> visitedTermianlNodes = new List<Smd>();
            NetLength = 0;
			foreach (var item in TerminalNodesSortedQueue)
			{
                if (visitedTermianlNodes.Count == PinRefs.Count)
                    break;
                var nodeA = item.Key.Item1;
				var nodeB = item.Key.Item2;
				var netSegment = new double[] {nodeA.Coord[0], nodeA.Coord[1], nodeA.Coord[2], nodeB.Coord[0], nodeB.Coord[1], nodeB.Coord[2]};
				design.RatsNest.Add(netSegment);
				NetLength += item.Value;
                if (!visitedTermianlNodes.Contains(nodeA))
                    visitedTermianlNodes.Add(nodeA);

                if (!visitedTermianlNodes.Contains(nodeB))
                    visitedTermianlNodes.Add(nodeB);

				
			}
            TerminalNodesSortedQueue.Clear();
		}

		public void CalcNetDirectLineLength(Design design)
        {
            for (var i = 0; i < PinRefs.Count - 1; i++)
            {
                for (var j = i + 1; j < PinRefs.Count; j++)
                {
                    var pinJ = design.Components[PinRefs[j].Comp.Index].Footprint.Pads.Find(smd => smd.pinName == PinRefs[j].PinName);
                    var pinI = design.Components[PinRefs[i].Comp.Index].Footprint.Pads.Find(smd => smd.pinName == PinRefs[i].PinName);
                    var d = (pinJ.Coord[0] - pinI.Coord[0]) * (pinJ.Coord[0] - pinI.Coord[0]) + (pinJ.Coord[1] - pinI.Coord[1]) * (pinJ.Coord[1] - pinI.Coord[1]) + (pinJ.Coord[2] - pinI.Coord[2]) * (pinJ.Coord[2] - pinI.Coord[2]);
                    NetLength += Math.Sqrt(d);
                }
            }
        }
    }



    public class TemperatureNode
    {
        /* A matrix of such structures mark each node of the temperature field.  There will   */
        /* be a total of COMP_NUM squared of these components.  A null value in the comp variable   */
        /* means that the node does not refer to the center of a component but merely to a    */
        /* a resistor junction.  A non-zero number refers to that component in the list of    */
        /* components.                                                                        */
        public Component Comp, Innercomp;
        public double[] Coord = new double[3];
        public double PrevTemp, OldTemp, Temp;
        public double Vol, K;
    }
}