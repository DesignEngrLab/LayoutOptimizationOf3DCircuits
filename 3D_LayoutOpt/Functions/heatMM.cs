using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3D_LayoutOpt
{
    class HeatMM
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

        public static void thermal_analysis_MM(Design design)
        {
            double[] flux;
            double[][] R;
            int i, j, tot_nodes, hbw, width;
            int[] node_dim = new int[Constants.DIMENSION];

            SetUpTfield(design, node_dim);
            hbw = node_dim[1]*node_dim[2];
            width = 2*hbw + 1;
            tot_nodes = node_dim[0]*hbw;


            FindContainedNodes(design, hbw, node_dim[2]);

            /* These commands set up the flux vector and R matrix using dynamic memory */
            /* allocation.                                                             */
            flux = new double[tot_nodes];
            for (i = 0; i < tot_nodes; i++)
                flux[i] = 0.0;
            R = new double[tot_nodes][];
            for (i = 0; i < tot_nodes; i++)
            {
                R[i] = new double[width];
                for (j = 0; j < width; ++j)
                    R[i][j] = 0.0;
            }
            SetupFlux(design, flux, tot_nodes);
            SetupCoefMatrix(design, flux, R, tot_nodes, hbw, node_dim[2]);
            /*if (design.gauss) {
    design.gauss = 0;
    GaussSeidel(design, R, flux, tot_nodes, hbw, node_dim[2]);
  }
  else*/
            LU_Decomp(design, R, flux, tot_nodes, hbw);
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

        static void SetUpTfield(Design design, int[] node_dim)
        {
            Component comp;
            double[][] xx = new double[3][];
            double[] fringe = new double[3];
            int j, k, m;
            int[] i = new int[3];
            for (m = 0; m < Constants.DIMENSION; m++)
            {
                i[m] = 0;
            }

            for (int n = 0; n < design.comp_count; n++)
            {
                comp = design.components[n];
                for (m = 0; m < Constants.DIMENSION; m++)
                {
                    if (NotDuplicate(comp.ts[0].Center[m], xx[m], i[m]))
                        xx[m][++i[m]] = comp.ts[0].Center[m];
                }
            }
         
            for (m = 0; m < Constants.DIMENSION; m++)
            {
                node_dim[m] = i[m];
                PickSort(node_dim[m], xx[m]);
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

            for (m = 0; m < Constants.DIMENSION; m++)
            {
                xx[m][0] = design.box_min[m] - fringe[m];
                xx[m][(++node_dim[m])] = design.box_max[m] + fringe[m];
                RefineMesh(node_dim, xx[m], m, design.min_node_space);
                ++node_dim[m];
            }
            /*Console.WriteLine("%d %d %d  ", node_dim[0], node_dim[1], node_dim[2]);*/
/* Put coordinates and component number in each node.                                 */
            k = 0;
            for (i[0] = 0; i[0] < node_dim[0]; ++i[0])
            {
                for (i[1] = 0; i[1] < node_dim[1]; ++i[1])
                {
                    for (i[2] = 0; i[2] < node_dim[2]; ++i[2])
                    {
                        for (m = 0; m < Constants.DIMENSION; m++)
                        {
                            design.tfield[k].coord[m] = xx[m][i[m]];
                        }
                        design.tfield[k].comp = null;

                        //find_if_comp_center(design.first_comp, design.tfield, k);
                        ++k;
                    }
                }
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION FINDS IF THERE ALREADY IS SUCH A COORDINATE IN THE ARRAY.            */
        /* IT RETURNS TRUE IF NO DUPLICATES AND FALSE IF DUPLICATES                           */
        /* ---------------------------------------------------------------------------------- */

        static bool NotDuplicate(double num, double[] arr, int n)
        {
            int m;

            for (m = 1; m <= n; ++m)
            {
                if (Math.Abs(num - arr[m]) < Constants.CLOSE_NODE)
                    return false;
            }
            return true;
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION IS PICK SORT FROM NUMERICAL RECIPES IN C.  IT SORTS THE ARRAYS IN    */
        /* ASCENDING ORDER.                                                                   */
        /* ---------------------------------------------------------------------------------- */

        static void PickSort(int n, double[] arr)
        {
            int i, j;
            double a;

            for (j = 2; j <= n; j++)
            {
                a = arr[j];
                i = j - 1;
                while (i > 0 && arr[i] > a)
                {
                    arr[i + 1] = arr[i];
                    i--;
                }
                arr[i + 1] = a;
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION ADDS NODES IN GAPS WHERE THE SPACE IS BIGGER THAN MIN_NODE_SPACE.    */
        /*                                                                                    */
        /* ---------------------------------------------------------------------------------- */

        static void RefineMesh(int[] node_dim, double[] arr, int vector, double min_node_space)
        {
            int i, j;

            i = node_dim[vector];
            while ((i > 0) && (1 + node_dim[vector] < Constants.NODE_NUM))
            {
                if ((arr[i] - arr[i - 1]) > min_node_space)
                {
                    ++node_dim[vector];
                    for (j = node_dim[vector]; j > i; --j)
                        arr[j] = arr[j - 1];
                    arr[i] = 0.5*(arr[i + 1] - arr[i - 1]) + arr[i - 1];
                    i += 2;
                }
                --i;
            }
        }

/* ---------------------------------------------------------------------------------- */
/* This function returns the index of the component specified by the coordinates      */
/* x and y.  Returns zero if no such component.                                       */
/* ---------------------------------------------------------------------------------- */

        /*

        static void find_if_comp_center(Component first_comp, TemperatureNode[] tfield, int k)
        {
            Component comp;

            comp = first_comp;
            while (comp != null)
            {
                if ((Math.Abs(comp.ts[0].Center[0] - tfield[k].coord[0]) < Constants.CLOSE_NODE) &&
                    (Math.Abs(comp.ts[0].Center[1] - tfield[k].coord[1]) < Constants.CLOSE_NODE) &&
                    (Math.Abs(comp.ts[0].Center[2] - tfield[k].coord[2]) < Constants.CLOSE_NODE))
                {
                    comp.node_center = k;
                    return;
                }
                comp = comp.next_comp;
            }
        }
         
         */

        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION FINDS ALL THE NODES THAT ARE CONTAINED IN COMPONENTS AND INNER       */
        /* COMPONENT HEAT SOURCES.                                                            */
        /* ---------------------------------------------------------------------------------- */

        static void FindContainedNodes(Design design, int hbw, int znodes)
        {
            int k = 0;
            Component comp;

            for (int i = 0; i < design.comp_count; i++)
            {
                comp = design.components[k];
                design.tfield[comp.node_center].comp = comp;
                comp.nodes = 1;
                FindNeighbors(design.tfield, comp, comp.node_center, comp.nodes, hbw, znodes, 0);
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* This recursive function finds which neighboring nodes are contained within the     */
        /* component.                                                                         */
        /* ---------------------------------------------------------------------------------- */

        static void FindNeighbors(TemperatureNode[] tfield, Component comp, int k, int n, int hbw, int znodes, int from)
        {
            /* This will check neighbors to the west so long as it didn't come FROM the west.*/
            if ((from != -1) && ((Math.Abs(tfield[k - hbw].coord[0] - comp.ts[0].Center[0])) < (comp.ts[0].XMax - comp.ts[0].XMin)/2) &&
                (tfield[k - hbw].comp == null))
            {
                tfield[k - hbw].comp = comp;
                ++n;
                FindNeighbors(tfield, comp, k - hbw, n, hbw, znodes, 1);
            }
            /* Checks to the east. */
            if ((from != 1) && ((Math.Abs(tfield[k + hbw].coord[0] - comp.ts[0].Center[0])) < (comp.ts[0].XMax - comp.ts[0].XMin) / 2) &&
                (tfield[k + hbw].comp == null))
            {
                tfield[k + hbw].comp = comp;
                ++n;
                FindNeighbors(tfield, comp, k + hbw, n, hbw, znodes, -1);
            }
            /* Checks to the south. */
            if ((from != -2) && ((Math.Abs(tfield[k - znodes].coord[1] - comp.ts[0].Center[1])) < (comp.ts[0].YMax - comp.ts[0].YMin) / 2) &&
                (tfield[k - znodes].comp == null))
            {
                tfield[k - znodes].comp = comp;
                ++n;
                FindNeighbors(tfield, comp, k - znodes, n, hbw, znodes, 2);
            }
            /* Checks to the north. */
            if ((from != 2) && ((Math.Abs(tfield[k + znodes].coord[1] - comp.ts[0].Center[1])) < (comp.ts[0].YMax - comp.ts[0].YMin) / 2) &&
                (tfield[k + znodes].comp == null))
            {
                tfield[k + znodes].comp = comp;
                ++n;
                FindNeighbors(tfield, comp, k + znodes, n, hbw, znodes, -2);
            }
            /* Checks down. */
            if ((from != -3) && ((Math.Abs(tfield[k - 1].coord[2] - comp.ts[0].Center[2])) < (comp.ts[0].ZMax - comp.ts[0].ZMin) / 2) &&
                (tfield[k - 1].comp == null))
            {
                tfield[k - 1].comp = comp;
                ++n;
                FindNeighbors(tfield, comp, k - 1, n, hbw, znodes, 3);
            }
            /* Checks up. */
            if ((from != 3) && ((Math.Abs(tfield[k + 1].coord[2] - comp.ts[0].Center[2])) < (comp.ts[0].ZMax - comp.ts[0].ZMin) / 2) &&
                (tfield[k + 1].comp == null))
            {
                tfield[k + 1].comp = comp;
                ++n;
                FindNeighbors(tfield, comp, k + 1, n, hbw, znodes, -3);
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION INITIALIZES THE HEAT FLUX VECTOR.  THIS VECTOR CORRESPONDS TO THE    */
        /* B IN AX = B.  IF NO COMPONENT AT NODE THEN IT EQUALS ZERO, HOWEVER IF THERE IS     */
        /* A COMPONENT AT THE NODE THEN IT EQUALS COMP.Q DIVIDED BY THE NUMBER OF NODES      */
        /* INSIDE THE COMPONENT.                                                              */
        /* ---------------------------------------------------------------------------------- */

        static void SetupFlux(Design design, double[] flux, int tot_nodes)
        {
            Component comp;
            int k;
            
            /*  flux[(comp.node_center)] = comp.q;*/


            for (int i = 0; i < design.comp_count; i++)
            {
                comp = design.components[i];
                for (k = 0; k < tot_nodes; k++)
                {
                    if (design.tfield[k].comp == comp)
                        flux[k] = comp.q / comp.nodes;
                }
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION SETS UP THE COOEFFICIENT MATRIX R.  THIS MATRIX CORRESPONDS TO THE   */
        /* A IN AX = B.  CALCULATIONS OF ALL THE THERMAL RESISTANCES AND BOUNDARY CONDITIONS  */
        /* ARE DONE IN THIS SUBROUTINE.                                                       */
        /* ---------------------------------------------------------------------------------- */

        static void SetupCoefMatrix(Design design, double[] flux, double[][] R, int tot_nodes, int hbw, int znodes)
        {
            int k, i;
            double n, e, w, s, u, d;

/*  Go through each node to fill in each row of the R-matrix  */
            for (k = 0; k < tot_nodes; ++k)
            {
/* Find dimension n, e, w, s, u, d to surrounding points (0 if boundary).   */
                if ((k - hbw) >= 0)
                {
                    w = design.tfield[k].coord[0] - design.tfield[(k - hbw)].coord[0];
                }
                else
                    w = 0.0;

                if ((k + hbw) < tot_nodes)
                {
                    e = design.tfield[(k + hbw)].coord[0] - design.tfield[k].coord[0];
                }
                else
                    e = 0.0;

                if ((k%hbw) >= znodes)
                {
                    s = design.tfield[k].coord[1] - design.tfield[k - znodes].coord[1];
                }
                else
                    s = 0.0;

                if (((k + znodes)%hbw) >= znodes)
                {
                    n = design.tfield[k + znodes].coord[1] - design.tfield[k].coord[1];
                }
                else
                    n = 0.0;

                if ((k%znodes) != 0)
                {
                    d = design.tfield[k].coord[2] - design.tfield[k - 1].coord[2];
                }
                else
                    d = 0.0;

                if (((k + 1)%znodes) != 0)
                {
                    u = design.tfield[k + 1].coord[2] - design.tfield[k].coord[2];
                }
                else
                    u = 0.0;


                CalcResistances(design, flux, R, hbw, ((u + d)*(n + s)), w, k, -hbw, 0);

                CalcResistances(design, flux, R, hbw, ((u + d)*(n + s)), e, k, hbw, 0);

                CalcResistances(design, flux, R, hbw, ((u + d)*(e + w)), s, k, -znodes, 1);

                CalcResistances(design, flux, R, hbw, ((u + d)*(e + w)), n, k, znodes, 1);

                CalcResistances(design, flux, R, hbw, ((n + s)*(e + w)), d, k, -1, 2);

                CalcResistances(design, flux, R, hbw, ((n + s)*(e + w)), u, k, 1, 2);
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION CALCULATES THE RESISTANCES BETWEEN POINTS.  THE REASON THIS FUNCTION */
        /* IS SO COMPLICATED IS THAT A CONNECTION BETWEEN A NODE CAN EITHER BE IN OPEN SPACE, */
        /* ONE NODE CONTAINED WITHIN A COMPONENT, BOTH NODES WITHIN SEPARATE COMPONENTS, OR   */
        /* BOTH NODES WITHIN THE SAME COMPONENT.                                              */
        /* ---------------------------------------------------------------------------------- */

        static void CalcResistances(Design design, double[] flux, double[][] R, int hbw, double area, double x, int k, int step, int dir)
        {
            double xc = 0, nx = 0, kc, kn;
            int i;

            if (x == 0.0)
            {
                flux[k] += 0.25*(design.tamb)*(design.h[dir])*area;
                R[k][hbw] += 0.25*(design.h[dir])*area;
            }
            else
            {
                if (design.tfield[k].comp != null)
                {
                    kc = design.tfield[k].comp.k;
                    switch (dir)
                    {
                        case 0:
                            xc = ((design.tfield[k].comp.ts[0].XMax - design.tfield[k].comp.ts[0].XMin) / 2) - Math.Abs(design.tfield[k].comp.ts[0].Center[dir] - design.tfield[k].coord[dir]);
                            break;
                        case 1:
                            xc = ((design.tfield[k].comp.ts[0].YMax - design.tfield[k].comp.ts[0].YMin) / 2) - Math.Abs(design.tfield[k].comp.ts[0].Center[dir] - design.tfield[k].coord[dir]);
                            break;
                        case 2:
                            xc = ((design.tfield[k].comp.ts[0].ZMax - design.tfield[k].comp.ts[0].ZMin) / 2) - Math.Abs(design.tfield[k].comp.ts[0].Center[dir] - design.tfield[k].coord[dir]);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    kc = design.kb;
                    xc = 0.0;
                }
                if (design.tfield[k + step].comp != null)
                {
                    kn = design.tfield[k + step].comp.k;
                    switch (dir)
                    {
                        case 0:
                            xc = ((design.tfield[k + step].comp.ts[0].XMax - design.tfield[k + step].comp.ts[0].XMin) / 2) - Math.Abs(design.tfield[k + step].comp.ts[0].Center[dir] - design.tfield[k + step].coord[dir]);
                            break;
                        case 1:
                            xc = ((design.tfield[k + step].comp.ts[0].YMax - design.tfield[k + step].comp.ts[0].YMin) / 2) - Math.Abs(design.tfield[k + step].comp.ts[0].Center[dir] - design.tfield[k + step].coord[dir]);
                            break;
                        case 2:
                            xc = ((design.tfield[k + step].comp.ts[0].ZMax - design.tfield[k + step].comp.ts[0].ZMin) / 2) - Math.Abs(design.tfield[k + step].comp.ts[0].Center[dir] - design.tfield[k + step].coord[dir]);
                            break;
                        default:
                            break;
                    }

                }
                else
                {
                    kn = design.kb;
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
                R[k][hbw] += (0.25*(design.kb)*kc*kn*area)/(((design.kb)*kn*xc) + (kc*kn*x) + (kc*(design.kb)*nx));
                R[k][(hbw + step)] = -(0.25*(design.kb)*kc*kn*area)/
                                     (((design.kb)*kn*xc) + (kc*kn*x) + (kc*(design.kb)*nx));
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION PUTS THE TEMPERATURES OF THE NODE THAT LIE ON COMPONENTS INTO THE    */
        /* THE COMPONENT DATA STRUCTURE FOR ANALYSIS BY EVAL_PART_5.                          */
        /* ---------------------------------------------------------------------------------- */

        static void FindCompTemp(Design design)
        {
            Component comp;

            for (int i = 0; i < design.comp_count; i++)
            {
                comp = design.components[i];
                comp.temp = design.tfield[comp.node_center].temp;
                /*Console.WriteLine("Component %d temperature = %.2f", ++i, comp.temp);*/
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS IS THE MATRIX SOLVER FOR THE VECTOR OF NODES IN TFIELD.  IT USES LU DECOMP.   */
        /* ---------------------------------------------------------------------------------- */

        public static void LU_Decomp(Design design, double[][] R, double[] flux, int n, int hbw)
        {
            int i, j, k, c = hbw;
            double sum;

            Console.WriteLine("LU ");
/*   L-U Decomposition of Banded Matrix; returning values to the R banded matrix  */
            for (i = 1; i <= hbw; ++i)
                R[0][c + i] /= R[0][c];

            for (k = 1; k < (n - 1); ++k)
            {
                for (i = 0; i <= hbw; ++i)
                {
                    if ((k + i) < n)
                    {
                        sum = R[k + i][c - i];
                        for (j = 1; j <= (hbw - i); ++j)
                            if (j <= k) sum -= R[k - j][c + j]*R[k + i][c - i - j];
                        R[k + i][c - i] = sum;
                    }
                }
                for (i = 1; i <= hbw; ++i)
                {
                    sum = R[k][c + i];
                    for (j = 1; j <= (hbw - i); ++j)
                        if (j <= k) sum -= R[k - j][c + j + i]*R[k][c - j];
                    R[k][c + i] = sum/R[k][c];
                }
            }
            sum = R[n - 1][c];
            for (i = 1; i <= hbw; ++i)
                sum -= R[n - 1][c - i]*R[n - 1 - i][c + i];
            R[n - 1][c] = sum;

/*   L-U Back-Substition to get temperatures.   */
            for (k = 0; k < n; ++k)
            {
                sum = flux[k];
                for (i = 1; i <= hbw; ++i)
                    if (i <= k) sum -= R[k][c - i]*flux[k - i];
                flux[k] = sum/R[k][c];
            }
            for (k = n - 1; k >= 0; --k)
            {
                sum = flux[k];
                for (i = 1; i <= hbw; ++i)
                    if ((k + i) < n) sum -= R[k][c + i]*(design.tfield[k + i].temp);
                design.tfield[k].temp = sum;
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS IS AN ITERATIVE MATRIX SOLVER FOR THE VECTOR OF NODES IN TFIELD.              */
        /* ---------------------------------------------------------------------------------- */

        void GaussSeidel(Design design, double[][] R, double[] flux, int tot_nodes, int hbw, int znodes)
        {
            int i, k;
            int[] pos = {1, 0, 0};
            Int16 iter = 0;
            double tol, rtot;

            Console.WriteLine("GS ");
            pos[1] = znodes;
            pos[2] = hbw;

            GetInitialGuess(design, tot_nodes, hbw);

            /*      Guass - Seidel iteration with SOR      */
            do
            {
                ++iter;
                tol = 0.0;
                for (k = 0; k < tot_nodes; ++k)
                {
                    design.tfield[k].prev_temp = design.tfield[k].temp;
                    rtot = flux[k];
                    for (i = 0; i <= 2; ++i)
                    {
                        if ((k - pos[i]) >= 0)
                        {
                            rtot -= R[k][hbw - pos[i]]*design.tfield[k - pos[i]].temp;
                        }
                        if ((k + pos[i]) < tot_nodes)
                        {
                            rtot -= R[k][hbw + pos[i]]*design.tfield[k + pos[i]].temp;
                        }
                    }
                    design.tfield[k].temp = rtot/(R[k][hbw]);
                    /*   SOR   */
                    design.tfield[k].temp = Constants.OMEGA*design.tfield[k].temp +
                                            (1 - Constants.OMEGA)*design.tfield[k].prev_temp;
                    /* Absolute Tolerance tabulation. */
                    tol += Math.Abs(design.tfield[k].temp - design.tfield[k].prev_temp)/tot_nodes;
                }
                /*Console.WriteLine("Iteration %d: Component #1 temperature = %.2f  %f", iter, 
	design.tfield[0].temp, tol);*/
            } while ((tol > design.tolerance) && (iter < design.max_iter));
            /*Console.WriteLine("Iterations = %d: Component #1 temperature = %.2f  %f", iter, 
	design.tfield[design.first_comp.node_center].temp, tol);*/
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS IS FUNCTION ESTABLISHES THE INTIAL GUESSES USED BY GAUSS-SEIDEL.  THESE       */
        /* ARE FROM THE PREVIOUS ITERATION.                                                   */
        /* ---------------------------------------------------------------------------------- */

        void GetInitialGuess(Design design, int tot_nodes, int hbw)
        {
            int k;
            Component comp;

            for (k = 0; k < tot_nodes; k++)
            {
                design.tfield[k].temp = design.tfield[k].old_temp;
            }
            
            for (int i = 0; i < design.comp_count; i++)
            {
                comp = design.components[i];
                design.tfield[comp.node_center].temp = comp.temp;
            }
        }
    }
}
