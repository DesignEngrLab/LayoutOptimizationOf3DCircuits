using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OptimizationToolbox;
using StarMathLib;
using TVGL;

namespace _3D_LayoutOpt.Functions
{
    class ComponentToComponentOverlap : IInequality
    {
        private Design _design;

        internal ComponentToComponentOverlap(Design design)
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

			for (var i = 0; i < _design.CompCount; i++)
            {
                var comp0 = _design.Components[i];
                var ts0 = comp0.Ts;
                for (var j = i+1; j < _design.CompCount; j++)
                {

					var comp1 = _design.Components[j];
					var ts1 = comp1.Ts;
                    if (!BoundingBoxOverlap(ts0, ts1))
                        _design.Overlap[j, i] = 0;
                    else if (!ConvexHullOverlap(ts0, ts1))
                        _design.Overlap[j, i] = 0;
                    else
                    {
                        List<Vertex> ts1VertsInts0, ts0VertsInts1;
                        List<Vertex> ts1VertsOutts0, ts0VertsOutts1;
                        TVGL.MiscFunctions.FindSolidIntersections(ts0, ts1, out ts0VertsInts1,
                            out ts0VertsOutts1, out ts1VertsInts0, out ts1VertsOutts0, true);

                       
                        ts1VertsInts0.AddRange(ts0VertsInts1);
                        if (ts1VertsInts0.Count < 8)
                            _design.Overlap[j, i] = (ts1VertsInts0.Count/(ts0.Vertices.Count()+ts1.Vertices.Count())) * (ts0.Volume + ts1.Volume);
                        else
                        {
                            var convexHull = new TVGLConvexHull(ts1VertsInts0, 0.000001);
                            var vol = convexHull.Volume;
                            /*_design.Overlap[j, i] = vol / (ts0.Volume + ts1.Volume);*/       //USING OVERLAP VOLUME PERCENTAGE
                            _design.Overlap[j, i] = vol;
                        }
                    }


                    //List<Vertex> ts1VertsInts0, ts0VertsInts1;
                    //List<Vertex> ts1VertsOutts0, ts0VertsOutts1;
                    //TVGL.MiscFunctions.FindSolidIntersections(ts0, ts1, out ts0VertsInts1,
                    //    out ts0VertsOutts1, out ts1VertsInts0, out ts1VertsOutts0, true);
                    //ts1VertsInts0.AddRange(ts0VertsInts1);
                    //if (ts1VertsInts0.Count == 0)
                    //    _design.Overlap[j, i] = 0;
                    //else
                    //{
                    //    var convexHull = new TVGLConvexHull(ts1VertsInts0, 0.000001);
                    //    var vol = convexHull.Volume;
                    //    /*_design.Overlap[j, i] = vol / (ts0.Volume + ts1.Volume);*/       //USING OVERLAP VOLUME PERCENTAGE
                    //    _design.Overlap[j, i] = vol;
                    //}
                }
            }
            var sum = 0.0;

            for (var i = 0; i < _design.CompCount; i++)
            {
                for (var j = i; j < _design.CompCount; j++)
                {
                    sum += _design.Overlap[j, i];
                }
            }

            Console.Write("c2c = {0};  ", sum);

            return sum;
        }
    }
}
