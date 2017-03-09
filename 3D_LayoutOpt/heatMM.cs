using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3D_LayoutOpt
{
    class heatMM
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

            set_up_tfield(design, node_dim);
            hbw = node_dim[1]*node_dim[2];
            width = 2*hbw + 1;
            tot_nodes = node_dim[0]*hbw;


            find_contained_nodes(design, hbw, node_dim[2]);

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
            set_up_flux(design, flux, tot_nodes);
            set_up_coef_matrix(design, flux, R, tot_nodes, hbw, node_dim[2]);
            /*if (design.gauss) {
    design.gauss = 0;
    gauss_seidel(design, R, flux, tot_nodes, hbw, node_dim[2]);
  }
  else*/
            LU_Decomp(design, R, flux, tot_nodes, hbw);
            find_comp_temp(design);

            //for (i = 0; i<tot_nodes; i++)
            //    free(R[i]);
            //    free(R);
            //    free(flux);
        }

/* ---------------------------------------------------------------------------------- */
/* This function initializes the temperature field.  This field is a vector not a     */
/* matrix.  This is because it corresponds to the x in Ax = b.  The comp in the       */
/* tfield structure refers to the number of the component in the list.  If zero, then */
/* corresponds to a simple resistor junction.                                         */
/* ---------------------------------------------------------------------------------- */

        static void set_up_tfield(Design design, int[] node_dim)
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
                    if (not_duplicate(comp.coord[m], xx[m], i[m]))
                        xx[m][++i[m]] = comp.coord[m];
                }
            }
         
            for (m = 0; m < Constants.DIMENSION; m++)
            {
                node_dim[m] = i[m];
                picksort(node_dim[m], xx[m]);
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
                refinemesh(node_dim, xx[m], m, design.min_node_space);
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
/* This function finds if there already is such a coordinate in the array.            */
/* It returns TRUE if no duplicates and FALSE if duplicates                           */
/* ---------------------------------------------------------------------------------- */

        static bool not_duplicate(double num, double[] arr, int n)
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
/* This function is pick sort from NUMERICAL RECIPES in C.  It sorts the arrays in    */
/* ascending order.                                                                   */
/* ---------------------------------------------------------------------------------- */

        static void picksort(int n, double[] arr)
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
/* This function adds nodes in gaps where the space is bigger than MIN_NODE_SPACE.    */
/*                                                                                    */
/* ---------------------------------------------------------------------------------- */

        static void refinemesh(int[] node_dim, double[] arr, int vector, double min_node_space)
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

        static void find_if_comp_center(Component first_comp, Temperature_field[] tfield, int k)
        {
            Component comp;

            comp = first_comp;
            while (comp != null)
            {
                if ((Math.Abs(comp.coord[0] - tfield[k].coord[0]) < Constants.CLOSE_NODE) &&
                    (Math.Abs(comp.coord[1] - tfield[k].coord[1]) < Constants.CLOSE_NODE) &&
                    (Math.Abs(comp.coord[2] - tfield[k].coord[2]) < Constants.CLOSE_NODE))
                {
                    comp.node_center = k;
                    return;
                }
                comp = comp.next_comp;
            }
        }
         
         */

/* ---------------------------------------------------------------------------------- */
/* This function finds all the nodes that are contained in components and inner       */
/* component heat sources.                                                            */
/* ---------------------------------------------------------------------------------- */

        static void find_contained_nodes(Design design, int hbw, int znodes)
        {
            int k = 0;
            Component comp;

            for (int i = 0; i < design.comp_count; i++)
            {
                comp = design.components[k];
                design.tfield[comp.node_center].comp = comp;
                comp.nodes = 1;
                find_neighbors(design.tfield, comp, comp.node_center, comp.nodes, hbw, znodes, 0);
            }
        }

/* ---------------------------------------------------------------------------------- */
/* This recursive function finds which neighboring nodes are contained within the     */
/* component.                                                                         */
/* ---------------------------------------------------------------------------------- */

        static void find_neighbors(Temperature_field[] tfield, Component comp, int k, int n, int hbw, int znodes, int from)
        {
            /* This will check neighbors to the west so long as it didn't come FROM the west.*/
            if ((from != -1) && ((Math.Abs(tfield[k - hbw].coord[0] - comp.coord[0])) < (comp.dim[0]/2)) &&
                (tfield[k - hbw].comp == null))
            {
                tfield[k - hbw].comp = comp;
                ++n;
                find_neighbors(tfield, comp, k - hbw, n, hbw, znodes, 1);
            }
            /* Checks to the east. */
            if ((from != 1) && ((Math.Abs(tfield[k + hbw].coord[0] - comp.coord[0])) < (comp.dim[0]/2)) &&
                (tfield[k + hbw].comp == null))
            {
                tfield[k + hbw].comp = comp;
                ++n;
                find_neighbors(tfield, comp, k + hbw, n, hbw, znodes, -1);
            }
            /* Checks to the south. */
            if ((from != -2) && ((Math.Abs(tfield[k - znodes].coord[1] - comp.coord[1])) < (comp.dim[1]/2)) &&
                (tfield[k - znodes].comp == null))
            {
                tfield[k - znodes].comp = comp;
                ++n;
                find_neighbors(tfield, comp, k - znodes, n, hbw, znodes, 2);
            }
            /* Checks to the north. */
            if ((from != 2) && ((Math.Abs(tfield[k + znodes].coord[1] - comp.coord[1])) < (comp.dim[1]/2)) &&
                (tfield[k + znodes].comp == null))
            {
                tfield[k + znodes].comp = comp;
                ++n;
                find_neighbors(tfield, comp, k + znodes, n, hbw, znodes, -2);
            }
            /* Checks down. */
            if ((from != -3) && ((Math.Abs(tfield[k - 1].coord[2] - comp.coord[2])) < (comp.dim[2]/2)) &&
                (tfield[k - 1].comp == null))
            {
                tfield[k - 1].comp = comp;
                ++n;
                find_neighbors(tfield, comp, k - 1, n, hbw, znodes, 3);
            }
            /* Checks up. */
            if ((from != 3) && ((Math.Abs(tfield[k + 1].coord[2] - comp.coord[2])) < (comp.dim[2]/2)) &&
                (tfield[k + 1].comp == null))
            {
                tfield[k + 1].comp = comp;
                ++n;
                find_neighbors(tfield, comp, k + 1, n, hbw, znodes, -3);
            }
        }

/* ---------------------------------------------------------------------------------- */
/* This function initializes the heat flux vector.  This vector corresponds to the    */
/* b in Ax = b.  If no component at node then it equals zero, however if there is     */
/* a component at the node then it equals comp.q divided by the number of nodes      */
/* inside the component.                                                              */
/* ---------------------------------------------------------------------------------- */

        static void set_up_flux(Design design, double[] flux, int tot_nodes)
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
/* This function sets up the cooefficient matrix R.  This matrix corresponds to the   */
/* A in Ax = b.  Calculations of all the thermal resistances and boundary conditions  */
/* are done in this subroutine.                                                       */
/* ---------------------------------------------------------------------------------- */

        static void set_up_coef_matrix(Design design, double[] flux, double[][] R, int tot_nodes, int hbw, int znodes)
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


                calc_resistances(design, flux, R, hbw, ((u + d)*(n + s)), w, k, -hbw, 0);

                calc_resistances(design, flux, R, hbw, ((u + d)*(n + s)), e, k, hbw, 0);

                calc_resistances(design, flux, R, hbw, ((u + d)*(e + w)), s, k, -znodes, 1);

                calc_resistances(design, flux, R, hbw, ((u + d)*(e + w)), n, k, znodes, 1);

                calc_resistances(design, flux, R, hbw, ((n + s)*(e + w)), d, k, -1, 2);

                calc_resistances(design, flux, R, hbw, ((n + s)*(e + w)), u, k, 1, 2);
            }
        }

/* ---------------------------------------------------------------------------------- */
/* This function calculates the resistances between points.  The reason this function */
/* is so complicated is that a connection between a node can either be in open space, */
/* one node contained within a component, both nodes within separate components, or   */
/* both nodes within the same component.                                              */
/* ---------------------------------------------------------------------------------- */

        static void calc_resistances(Design design, double[] flux, double[][] R, int hbw, double area, double x, int k,
            int step, int dir)
        {
            double xc, nx, kc, kn;
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
                    xc = (design.tfield[k].comp.dim[dir]/2) - Math.Abs(design.tfield[k].comp.coord[dir] -

                                                                       design.tfield[k].coord[dir]);
                }
                else
                {
                    kc = design.kb;
                    xc = 0.0;
                }
                if (design.tfield[k + step].comp != null)
                {
                    kn = design.tfield[k + step].comp.k;
                    nx = (design.tfield[k + step].comp.dim[dir]/2) - Math.Abs(design.tfield[k + step].comp.coord[dir] -

                                                                              design.tfield[k + step].coord[dir]);
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
/* This function puts the temperatures of the node that lie on components into the    */
/* the component data structure for analysis by eval_part_5.                          */
/* ---------------------------------------------------------------------------------- */

        static void find_comp_temp(Design design)
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
/* This is the matrix solver for the vector of nodes in tfield.  It uses LU decomp.   */
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
/* This is an iterative matrix solver for the vector of nodes in tfield.              */
/* ---------------------------------------------------------------------------------- */

        void gauss_seidel(Design design, double[][] R, double[] flux, int tot_nodes, int hbw, int znodes)
        {
            int i, k;
            int[] pos = {1, 0, 0};
            Int16 iter = 0;
            double tol, rtot;

            Console.WriteLine("GS ");
            pos[1] = znodes;
            pos[2] = hbw;

            get_initial_guess(design, tot_nodes, hbw);

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
/* This is function establishes the intial guesses used by Gauss-Seidel.  These       */
/* are from the previous iteration.                                                   */
/* ---------------------------------------------------------------------------------- */

        void get_initial_guess(Design design, int tot_nodes, int hbw)
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
