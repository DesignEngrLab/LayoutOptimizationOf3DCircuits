using OptimizationToolbox;
using StarMathLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TVGL;

namespace _3D_LayoutOpt.Functions
{
    /* ---------------------------------------------------------------------------------- */
    /* THIS FUNCTION SETS THE VALUE OF THE THIRD PART OF THE OBJECTIVE FUNCTION, WHICH    */
    /* IS THE AMOUNT OF OVERLAP WITH THE CONTAINER.                                       */
    /* ---------------------------------------------------------------------------------- */

    class ComponentToContainerOverlap : IInequality
    {
        private Design _design;

        public ComponentToContainerOverlap(Design design)
        {
            this._design = design;
        }

		internal static bool BoundingBoxOverlap(TessellatedSolid a, TessellatedSolid b)
		{
			var aveXLength = (Math.Abs(a.XMax - a.XMin) + Math.Abs(b.XMax - b.XMin)) / 2.0;
			var aveYLength = (Math.Abs(a.YMax - a.YMin) + Math.Abs(b.YMax - b.YMin)) / 2.0;
			var aveZLength = (Math.Abs(a.ZMax - a.ZMin) + Math.Abs(b.ZMax - b.ZMin)) / 2.0;
			if (a.XMin > b.XMax
				|| a.YMin > b.YMax
				|| a.ZMin > b.ZMax
				|| b.XMin > a.XMax
				|| b.YMin > a.YMax
				|| b.ZMin > a.ZMax)
				return false;
			return true;
		}

		internal static bool ConvexHullOverlap(TessellatedSolid a, TessellatedSolid b)
		{
			foreach (var f in a.ConvexHull.Faces)
			{
				var dStar = (f.Normal.dotProduct(f.Vertices[0].Position));
				if (b.ConvexHull.Vertices.All(pt => (f.Normal.dotProduct(pt.Position)) > dStar + 0.1)) // 0.001
					return false;
			}
			foreach (var f in b.ConvexHull.Faces)
			{
				var dStar = (f.Normal.dotProduct(f.Vertices[0].Position));
				if (a.ConvexHull.Vertices.All(pt => (f.Normal.dotProduct(pt.Position)) > dStar + 0.1)) // 0.001
					return false;
			}
			return true;
		}

		public double calculate(double[] x)
        {
            var volPenalty = 0.0;
            var totCompVolume = 0.0;
            var containerPenalty = 0.0;
            var ts0 = _design.Container.Ts;
			var vol = 0.0;
			foreach (var comp in _design.Components)
            {
                var ts1 = comp.Ts;
                totCompVolume += ts1.Volume;
                if (!BoundingBoxOverlap(ts0, ts1))
                    vol = ts1.Volume;
                else if (!ConvexHullOverlap(ts0, ts1))
                    vol = ts1.Volume;
                else
                {
                    List<Vertex> ts1VertsInts0, ts0VertsInts1;
                    List<Vertex> ts1VertsOutts0, ts0VertsOutts1;
                    TVGL.MiscFunctions.FindSolidIntersections(ts0, ts1, out ts0VertsInts1,
                                out ts0VertsOutts1, out ts1VertsInts0, out ts1VertsOutts0, false);
                    if (ts1VertsOutts0.Count() < 6)
                        vol = (ts1VertsOutts0.Count() / ts1.Vertices.Count()) * ts1.Volume;
                    else
                    {
                        var convexHull = new TVGLConvexHull(ts1VertsOutts0, 0.000001);
                        vol = convexHull.Volume;
                    }                
                }
                volPenalty += vol;
                containerPenalty = volPenalty / totCompVolume;
                //var ts1 = comp.Ts;
                //List<Vertex> ts1VertsInts0, ts0VertsInts1;
                //List<Vertex> ts1VertsOutts0, ts0VertsOutts1;
                //TVGL.MiscFunctions.FindSolidIntersections(ts0, ts1, out ts0VertsInts1,
                //            out ts0VertsOutts1, out ts1VertsInts0, out ts1VertsOutts0, false);
                //ts1VertsOutts0.AddRange(ts0VertsOutts1);
                //var convexHull = new TVGLConvexHull(ts1VertsOutts0, 0.000001);
                //vol = convexHull.Volume;
                //boxPenalty += vol;
            }
			_design.NewObjValues[2] = 4*containerPenalty;  //MANUALLY APPLYING A WEIGHT OF 2
            Console.Write("c2b = {0};  ", 4*containerPenalty);

            return 4*containerPenalty;
        }
    }
}
