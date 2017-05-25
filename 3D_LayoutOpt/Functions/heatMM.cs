using System;
using System.Collections.Generic;
using System.Linq;

namespace _3D_LayoutOpt
{
    class HeatMm
    {
        /* ------------------------------------------------------------------------- */
        /*                                                                           */
        /*                       HEATMM.C -- Matrix Method                           */
        /*                                                                           */
        /* ------------------------------------------------------------------------- */
        /* ------------------------------------------------------------------------- */
        /* This function finds the temperature at the center of each component and   */
        /* places that value in comp.temp.  Because of the nature of this function  */
        /* it needs to re-calculated for each iteration, instead of just being       */
        /* updated.                                                                  */
        /* ------------------------------------------------------------------------- */

        public static void ThermalAnalysisMM(Design design)
        {
            int i;
            var nodeDim = new int[Constants.Dimension];
            SetUpTfield(design, out nodeDim);
            var hbw = nodeDim[1] * nodeDim[2];
            var width = 2 * hbw + 1;
            var totNodes = nodeDim[0] * hbw;


            FindContainedNodes(design, hbw, nodeDim[2]);

            /* These commands set up the flux vector and R matrix using dynamic memory */
            /* allocation.                                                             */
            var flux = new double[totNodes];
            for (i = 0; i < totNodes; i++)
                flux[i] = 0.0;
            var r = new double[totNodes][];
            for (i = 0; i < totNodes; i++)
            {
                r[i] = new double[width];
                int j;
                for (j = 0; j < width; ++j)
                    r[i][j] = 0.0;
            }
            SetupFlux(design, flux, totNodes);
            SetupCoefMatrix(design, flux, r, totNodes, hbw, nodeDim[2]);
            /*if (design.gauss) {
    design.gauss = 0;
    GaussSeidel(design, R, flux, tot_nodes, hbw, node_dim[2]);
  }
  else*/
            LU_Decomp(design, r, flux, totNodes, hbw);
            FindCompTemp(design);

            //for (i = 0; i<tot_nodes; i++)
            //    free(R[i]);
            //    free(R);
            //    free(flux);
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION INITIALIZES THE TEMPERATURE FIELD.  THIS FIELD IS A VECTOR NOT A     */
        /* MATRIX.  THIS IS BECAUSE IT CORRESPONDS TO THE X IN AX = B.  THE COMP IN THE       */
        /* TFIELD STRUCTURE REFERS TO THE NUMBER OF THE COMPONENT IN THE LIST.  IF ZERO, THEN */
        /* CORRESPONDS TO A SIMPLE RESISTOR JUNCTION.                                         */
        /* ---------------------------------------------------------------------------------- */

        static void SetUpTfield(Design design, out int[] nodeDimensions)
        {
            Component comp;
            List<double>[] xx = new List<double>[3];
            //var xx = new double[3][];
            var fringe = new double[3];
            int j, k, m;
            var dim = new int[3];
            for (m = 0; m < Constants.Dimension; m++)
            {
                dim[m] = 0;
                xx[m] = new List<double>();
            }

            for (var n = 0; n < design.CompCount; n++)
            {
                comp = design.Components[n];
                for (m = 0; m < Constants.Dimension; m++)
                {

                    if (IsNullOrEmpty(xx[m].ToArray()) || NotDuplicate(comp.Ts.Center[m], xx[m].ToArray()))
                        xx[m].Add(comp.Ts.Center[m]);
                }
            }

            for (m = 0; m < Constants.Dimension; m++)
            {
                xx[m].Sort();
            }


#if SFRINGE
            for (m = 0; m < Constants.DIMENSION; m++)
            {
                fringe[m] = Constants.SFRINGE;
            }
#endif

#if CFRINGE
            for (m = 0; m < Constants.DIMENSION; m++)
            {
                fringe[m] = (design.container[m] - (design.box_max[m] - design.box_min[m]))/2;
                if (fringe[m] < 0)
                    fringe[m] = Constants.CFRINGE;
            }
#endif
            // refine mesh
            for (m = 0; m < Constants.Dimension; m++)
            {
                xx[m].Insert(0, design.BoxMin[m] - fringe[m]);
                xx[m].Add(design.BoxMax[m] + fringe[m]);
                RefineMesh(xx[m], design.MinNodeSpace);
            }
            nodeDimensions = xx.Select(a => a.Count).ToArray();
            // Put coordinates and component number in each node.

            k = 0;
            for (dim[0] = 0; dim[0] < nodeDimensions[0]; dim[0]++)
            {
                for (dim[1] = 0; dim[1] < nodeDimensions[1]; dim[1]++)
                {
                    for (dim[2] = 0; dim[2] < nodeDimensions[2]; dim[2]++)
                    {
                        for (m = 0; m < Constants.Dimension; m++)
                        {
                            design.Tfield[k].Coord[m] = xx[m][dim[m]];
                        }
                        design.Tfield[k].Comp = null;
                        if ((k > nodeDimensions[0] * nodeDimensions[1]) && (k < nodeDimensions[0] * nodeDimensions[1] * nodeDimensions[2] - nodeDimensions[0] * nodeDimensions[1])) 
                            FindIfCompCenter(design, design.Tfield, k);          
                        k++;
                    }
                }
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION FINDS IF THERE ALREADY IS SUCH A COORDINATE IN THE ARRAY.            */
        /* IT RETURNS TRUE IF NO DUPLICATES AND FALSE IF DUPLICATES                           */
        /* ---------------------------------------------------------------------------------- */

        static bool NotDuplicate(double num, double[] arr)
        {

            for (int m = 0; m < arr.Length; m++)
            {
                if (Math.Abs(num - arr[m]) < Constants.CloseNode)
                    return false;
            }
            return true;
        }


        public static bool IsNullOrEmpty(Array array)
        {
            return (array == null || array.Length == 0);
        }



        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION ADDS NODES IN GAPS WHERE THE SPACE IS BIGGER THAN MIN_NODE_SPACE.    */
        /*                                                                                    */
        /* ---------------------------------------------------------------------------------- */

        static void RefineMesh(List<double> arr, double minNodeSpace)
        {
            int k = 1;
            while (k < arr.Count && arr.Count < Constants.NodeNum - 1)
            {
                if ((arr[k] - arr[k - 1]) > minNodeSpace)
                    arr.Insert(k, 0.5 * (arr[k] - arr[k - 1]) + arr[k - 1]);
                else k++;
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function returns the index of the component specified by the coordinates      */
        /* x and y.  Returns zero if no such component.                                       */
        /* ---------------------------------------------------------------------------------- */

        static void FindIfCompCenter(Design design, TemperatureNode[] tfield, int k)
        {
            foreach (var comp in design.Components)
            {
                if ((Math.Abs(comp.Ts.Center[0] - tfield[k].Coord[0]) < Constants.CloseNode) &&
                    (Math.Abs(comp.Ts.Center[1] - tfield[k].Coord[1]) < Constants.CloseNode) &&
                    (Math.Abs(comp.Ts.Center[2] - tfield[k].Coord[2]) < Constants.CloseNode))
                {
                    comp.NodeCenter = k;
                }
            }
        }


        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION FINDS ALL THE NODES THAT ARE CONTAINED IN COMPONENTS AND INNER       */
        /* COMPONENT HEAT SOURCES.                                                            */
        /* ---------------------------------------------------------------------------------- */

        static void FindContainedNodes(Design design, int hbw, int znodes)
        {
            var k = 0;
            Component comp;

            for (var i = 0; i < design.CompCount; i++)
            {
                comp = design.Components[k];
                design.Tfield[comp.NodeCenter].Comp = comp;
                comp.Nodes = 1;
                FindNeighbors(design.Tfield, comp, comp.NodeCenter, comp.Nodes, hbw, znodes, 0);
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* This recursive function finds which neighboring nodes are contained within the     */
        /* component.                                                                         */
        /* ---------------------------------------------------------------------------------- */

        static void FindNeighbors(TemperatureNode[] tfield, Component comp, int k, int n, int hbw, int znodes, int from)
        {
            if(k > hbw && k < hbw*znodes - hbw)
            {
                /* This will check neighbors to the west so long as it didn't come FROM the west.*/
                if ((from != -1) && ((Math.Abs(tfield[k - hbw].Coord[0] - comp.Ts.Center[0])) < (comp.Ts.XMax - comp.Ts.XMin) / 2) &&
                    (tfield[k - hbw].Comp == null))
                {
                    comp.Nodes++;
                    tfield[k - hbw].Comp = comp;
                    ++n;
                    FindNeighbors(tfield, comp, k - hbw, n, hbw, znodes, 1);
                }
                /* Checks to the east. */
                if ((from != 1) && ((Math.Abs(tfield[k + hbw].Coord[0] - comp.Ts.Center[0])) < (comp.Ts.XMax - comp.Ts.XMin) / 2) &&
                    (tfield[k + hbw].Comp == null))
                {
                    comp.Nodes++;
                    tfield[k + hbw].Comp = comp;
                    ++n;
                    FindNeighbors(tfield, comp, k + hbw, n, hbw, znodes, -1);
                }
                /* Checks to the south. */
                if ((from != -2) && ((Math.Abs(tfield[k - znodes].Coord[1] - comp.Ts.Center[1])) < (comp.Ts.YMax - comp.Ts.YMin) / 2) &&
                    (tfield[k - znodes].Comp == null))
                {
                    comp.Nodes++;
                    tfield[k - znodes].Comp = comp;
                    ++n;
                    FindNeighbors(tfield, comp, k - znodes, n, hbw, znodes, 2);
                }
                /* Checks to the north. */
                if ((from != 2) && ((Math.Abs(tfield[k + znodes].Coord[1] - comp.Ts.Center[1])) < (comp.Ts.YMax - comp.Ts.YMin) / 2) &&
                    (tfield[k + znodes].Comp == null))
                {
                    comp.Nodes++;
                    tfield[k + znodes].Comp = comp;
                    ++n;
                    FindNeighbors(tfield, comp, k + znodes, n, hbw, znodes, -2);
                }
                /* Checks down. */
                if ((from != -3) && ((Math.Abs(tfield[k - 1].Coord[2] - comp.Ts.Center[2])) < (comp.Ts.ZMax - comp.Ts.ZMin) / 2) &&
                    (tfield[k - 1].Comp == null))
                {
                    comp.Nodes++;
                    tfield[k - 1].Comp = comp;
                    ++n;
                    FindNeighbors(tfield, comp, k - 1, n, hbw, znodes, 3);
                }
                /* Checks up. */
                if ((from != 3) && ((Math.Abs(tfield[k + 1].Coord[2] - comp.Ts.Center[2])) < (comp.Ts.ZMax - comp.Ts.ZMin) / 2) &&
                    (tfield[k + 1].Comp == null))
                {
                    comp.Nodes++;
                    tfield[k + 1].Comp = comp;
                    ++n;
                    FindNeighbors(tfield, comp, k + 1, n, hbw, znodes, -3);
                }
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION INITIALIZES THE HEAT FLUX VECTOR.  THIS VECTOR CORRESPONDS TO THE    */
        /* B IN AX = B.  IF NO COMPONENT AT NODE THEN IT EQUALS ZERO, HOWEVER IF THERE IS     */
        /* A COMPONENT AT THE NODE THEN IT EQUALS COMP.Q DIVIDED BY THE NUMBER OF NODES      */
        /* INSIDE THE COMPONENT.                                                              */
        /* ---------------------------------------------------------------------------------- */

        static void SetupFlux(Design design, double[] flux, int totNodes)
        {
            /*  flux[(comp.node_center)] = comp.q;*/


            for (var i = 0; i < design.CompCount; i++)
            {
                var comp = design.Components[i];
                for (int k = 0; k < totNodes; k++)
                {
                    if (design.Tfield[k].Comp == comp)
                        flux[k] = comp.Q / comp.Nodes;
                }
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION SETS UP THE COOEFFICIENT MATRIX R.  THIS MATRIX CORRESPONDS TO THE   */
        /* A IN AX = B.  CALCULATIONS OF ALL THE THERMAL RESISTANCES AND BOUNDARY CONDITIONS  */
        /* ARE DONE IN THIS SUBROUTINE.                                                       */
        /* ---------------------------------------------------------------------------------- */

        static void SetupCoefMatrix(Design design, double[] flux, double[][] r, int totNodes, int hbw, int znodes)
        {
            int k, i;
            double n, e, w, s, u, d;

            /*  Go through each node to fill in each row of the R-matrix  */
            for (k = 0; k < totNodes; ++k)
            {
                /* Find dimension n, e, w, s, u, d to surrounding points (0 if boundary).   */
                if ((k - hbw) >= 0)
                {
                    w = design.Tfield[k].Coord[0] - design.Tfield[(k - hbw)].Coord[0];
                }
                else
                    w = 0.0;

                if ((k + hbw) < totNodes)
                {
                    e = design.Tfield[(k + hbw)].Coord[0] - design.Tfield[k].Coord[0];
                }
                else
                    e = 0.0;

                if ((k % hbw) >= znodes)
                {
                    s = design.Tfield[k].Coord[1] - design.Tfield[k - znodes].Coord[1];
                }
                else
                    s = 0.0;

                if (((k + znodes) % hbw) >= znodes)
                {
                    n = design.Tfield[k + znodes].Coord[1] - design.Tfield[k].Coord[1];
                }
                else
                    n = 0.0;

                if ((k % znodes) != 0)
                {
                    d = design.Tfield[k].Coord[2] - design.Tfield[k - 1].Coord[2];
                }
                else
                    d = 0.0;

                if (((k + 1) % znodes) != 0)
                {
                    u = design.Tfield[k + 1].Coord[2] - design.Tfield[k].Coord[2];
                }
                else
                    u = 0.0;


                CalcResistances(design, flux, r, hbw, ((u + d) * (n + s)), w, k, -hbw, 0);

                CalcResistances(design, flux, r, hbw, ((u + d) * (n + s)), e, k, hbw, 0);

                CalcResistances(design, flux, r, hbw, ((u + d) * (e + w)), s, k, -znodes, 1);

                CalcResistances(design, flux, r, hbw, ((u + d) * (e + w)), n, k, znodes, 1);

                CalcResistances(design, flux, r, hbw, ((n + s) * (e + w)), d, k, -1, 2);

                CalcResistances(design, flux, r, hbw, ((n + s) * (e + w)), u, k, 1, 2);
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION CALCULATES THE RESISTANCES BETWEEN POINTS.  THE REASON THIS FUNCTION */
        /* IS SO COMPLICATED IS THAT A CONNECTION BETWEEN A NODE CAN EITHER BE IN OPEN SPACE, */
        /* ONE NODE CONTAINED WITHIN A COMPONENT, BOTH NODES WITHIN SEPARATE COMPONENTS, OR   */
        /* BOTH NODES WITHIN THE SAME COMPONENT.                                              */
        /* ---------------------------------------------------------------------------------- */

        static void CalcResistances(Design design, double[] flux, double[][] r, int hbw, double area, double x, int k, int step, int dir)
        {
            double xc = 0, nx = 0, kc, kn;
            int i;

            if (x == 0.0)
            {
                flux[k] += 0.25 * (design.Tamb) * (design.H[dir]) * area;
                r[k][hbw] += 0.25 * (design.H[dir]) * area;
            }
            else
            {
                if (design.Tfield[k].Comp != null)
                {
                    kc = design.Tfield[k].Comp.K;
                    switch (dir)
                    {
                        case 0:
                            xc = ((design.Tfield[k].Comp.Ts.XMax - design.Tfield[k].Comp.Ts.XMin) / 2) - Math.Abs(design.Tfield[k].Comp.Ts.Center[dir] - design.Tfield[k].Coord[dir]);
                            break;
                        case 1:
                            xc = ((design.Tfield[k].Comp.Ts.YMax - design.Tfield[k].Comp.Ts.YMin) / 2) - Math.Abs(design.Tfield[k].Comp.Ts.Center[dir] - design.Tfield[k].Coord[dir]);
                            break;
                        case 2:
                            xc = ((design.Tfield[k].Comp.Ts.ZMax - design.Tfield[k].Comp.Ts.ZMin) / 2) - Math.Abs(design.Tfield[k].Comp.Ts.Center[dir] - design.Tfield[k].Coord[dir]);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    kc = design.Kb;
                    xc = 0.0;
                }
                if (design.Tfield[k + step].Comp != null)
                {
                    kn = design.Tfield[k + step].Comp.K;
                    switch (dir)
                    {
                        case 0:
                            xc = ((design.Tfield[k + step].Comp.Ts.XMax - design.Tfield[k + step].Comp.Ts.XMin) / 2) - Math.Abs(design.Tfield[k + step].Comp.Ts.Center[dir] - design.Tfield[k + step].Coord[dir]);
                            break;
                        case 1:
                            xc = ((design.Tfield[k + step].Comp.Ts.YMax - design.Tfield[k + step].Comp.Ts.YMin) / 2) - Math.Abs(design.Tfield[k + step].Comp.Ts.Center[dir] - design.Tfield[k + step].Coord[dir]);
                            break;
                        case 2:
                            xc = ((design.Tfield[k + step].Comp.Ts.ZMax - design.Tfield[k + step].Comp.Ts.ZMin) / 2) - Math.Abs(design.Tfield[k + step].Comp.Ts.Center[dir] - design.Tfield[k + step].Coord[dir]);
                            break;
                        default:
                            break;
                    }

                }
                else
                {
                    kn = design.Kb;
                    nx = 0.0;
                }
                /* These two 'if' statements account for the chance that the node and the */
                /* neighboring node might be in the same component.                       */
                /*Console.WriteLine("%f  %f  %f ", x, xc, nx);*/
                if (x + nx <= xc)
                {
                    xc = x;
                    nx = 0.0;
                }
                if (x + xc <= nx)
                {
                    nx = x;
                    xc = 0.0;
                }
                x -= xc + nx;
                if (x < 0.0) x = 0.0;
                /* These resistances are actually 'admittances' and therefore the total resistance   */
                /* is the reciprocal of the sum of the reciprocals.                                  */
                r[k][hbw] += (0.25 * (design.Kb) * kc * kn * area) / (((design.Kb) * kn * xc) + (kc * kn * x) + (kc * (design.Kb) * nx));
                r[k][(hbw + step)] = -(0.25 * (design.Kb) * kc * kn * area) /
                                     (((design.Kb) * kn * xc) + (kc * kn * x) + (kc * (design.Kb) * nx));
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION PUTS THE TEMPERATURES OF THE NODE THAT LIE ON COMPONENTS INTO THE    */
        /* THE COMPONENT DATA STRUCTURE FOR ANALYSIS BY EVAL_PART_5.                          */
        /* ---------------------------------------------------------------------------------- */

        static void FindCompTemp(Design design)
        {
            Component comp;

            for (var i = 0; i < design.CompCount; i++)
            {
                comp = design.Components[i];
                comp.Temp = design.Tfield[comp.NodeCenter].Temp;
                /*Console.WriteLine("Component %d temperature = %.2f", ++i, comp.temp);*/
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS IS THE MATRIX SOLVER FOR THE VECTOR OF NODES IN TFIELD.  IT USES LU DECOMP.   */
        /* ---------------------------------------------------------------------------------- */

        public static void LU_Decomp(Design design, double[][] r, double[] flux, int n, int hbw)
        {
            int i, j, k, c = hbw;
            double sum;

            Console.WriteLine("LU ");
            /*   L-U Decomposition of Banded Matrix; returning values to the R banded matrix  */
            for (i = 1; i <= hbw; ++i)
                r[0][c + i] /= r[0][c];

            for (k = 1; k < (n - 1); ++k)
            {
                for (i = 0; i <= hbw; ++i)
                {
                    if ((k + i) < n)
                    {
                        sum = r[k + i][c - i];
                        for (j = 1; j <= (hbw - i); ++j)
                            if (j <= k) sum -= r[k - j][c + j] * r[k + i][c - i - j];
                        r[k + i][c - i] = sum;
                    }
                }
                for (i = 1; i <= hbw; ++i)
                {
                    sum = r[k][c + i];
                    for (j = 1; j <= (hbw - i); ++j)
                        if (j <= k) sum -= r[k - j][c + j + i] * r[k][c - j];
                    r[k][c + i] = sum / r[k][c];
                }
            }
            sum = r[n - 1][c];
            for (i = 1; i <= hbw; ++i)
                sum -= r[n - 1][c - i] * r[n - 1 - i][c + i];
            r[n - 1][c] = sum;

            /*   L-U Back-Substition to get temperatures.   */
            for (k = 0; k < n; ++k)
            {
                sum = flux[k];
                for (i = 1; i <= hbw; ++i)
                    if (i <= k) sum -= r[k][c - i] * flux[k - i];
                flux[k] = sum / r[k][c];
            }
            for (k = n - 1; k >= 0; --k)
            {
                sum = flux[k];
                for (i = 1; i <= hbw; ++i)
                    if ((k + i) < n) sum -= r[k][c + i] * (design.Tfield[k + i].Temp);
                design.Tfield[k].Temp = sum;
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS IS AN ITERATIVE MATRIX SOLVER FOR THE VECTOR OF NODES IN TFIELD.              */
        /* ---------------------------------------------------------------------------------- */

        void GaussSeidel(Design design, double[][] r, double[] flux, int totNodes, int hbw, int znodes)
        {
            int i, k;
            int[] pos = { 1, 0, 0 };
            Int16 iter = 0;
            double tol, rtot;

            Console.WriteLine("GS ");
            pos[1] = znodes;
            pos[2] = hbw;

            GetInitialGuess(design, totNodes, hbw);

            /*      Guass - Seidel iteration with SOR      */
            do
            {
                ++iter;
                tol = 0.0;
                for (k = 0; k < totNodes; ++k)
                {
                    design.Tfield[k].PrevTemp = design.Tfield[k].Temp;
                    rtot = flux[k];
                    for (i = 0; i <= 2; ++i)
                    {
                        if ((k - pos[i]) >= 0)
                        {
                            rtot -= r[k][hbw - pos[i]] * design.Tfield[k - pos[i]].Temp;
                        }
                        if ((k + pos[i]) < totNodes)
                        {
                            rtot -= r[k][hbw + pos[i]] * design.Tfield[k + pos[i]].Temp;
                        }
                    }
                    design.Tfield[k].Temp = rtot / (r[k][hbw]);
                    /*   SOR   */
                    design.Tfield[k].Temp = Constants.Omega * design.Tfield[k].Temp +
                                            (1 - Constants.Omega) * design.Tfield[k].PrevTemp;
                    /* Absolute Tolerance tabulation. */
                    tol += Math.Abs(design.Tfield[k].Temp - design.Tfield[k].PrevTemp) / totNodes;
                }
                /*Console.WriteLine("Iteration %d: Component #1 temperature = %.2f  %f", iter, 
	design.tfield[0].temp, tol);*/
            } while ((tol > design.Tolerance) && (iter < design.MaxIter));
            /*Console.WriteLine("Iterations = %d: Component #1 temperature = %.2f  %f", iter, 
	design.tfield[design.first_comp.node_center].temp, tol);*/
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS IS FUNCTION ESTABLISHES THE INTIAL GUESSES USED BY GAUSS-SEIDEL.  THESE       */
        /* ARE FROM THE PREVIOUS ITERATION.                                                   */
        /* ---------------------------------------------------------------------------------- */

        void GetInitialGuess(Design design, int totNodes, int hbw)
        {
            int k;
            Component comp;

            for (k = 0; k < totNodes; k++)
            {
                design.Tfield[k].Temp = design.Tfield[k].OldTemp;
            }

            for (var i = 0; i < design.CompCount; i++)
            {
                comp = design.Components[i];
                design.Tfield[comp.NodeCenter].Temp = comp.Temp;
            }
        }
    }
}
