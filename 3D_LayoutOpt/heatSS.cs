using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3D_LayoutOpt
{
    class heatSS
    {

        /* ---------------------------------------------------------------------------------- */
        /*                                                                                    */
        /*                             HEATSS.C -- Sub-Space Method                           */
        /*                                                                                    */
        /* ---------------------------------------------------------------------------------- */
        public static void thermal_analysis_SS(Design design)
        {
            double[] flux;
           double[,] R;
            double[] ss_dim = new double[3];
            double[] fringe = new double[3];
            int i, j, tot_nodes;
            int[] divisions = new int[3];

            tot_nodes = (design.choice)*100;
#if SFRINGE
            for (i = 0; i<Constants.DIMENSION; i++)
            {
	            fringe[i] = SFRINGE;
            }
#endif

#if CFRINGE
            for (i = 0; i<Constants.DIMENSION; i++)
            {
	            fringe[i] = (design.container[i] - (design.box_max[i] - design.box_min[i]))/2;
	            if (fringe[i] < 0)
	                fringe[i] = Constants.CFRINGE;
            }
#endif

            define_divisions(design, divisions, ss_dim, tot_nodes, fringe);
            tot_nodes = divisions[0]* divisions[1]* divisions[2];
            /* total nodes might change from predicted amount. */
            /*  These commands set up the flux vector and R matrix using dynamic memory */
            /*  allocation.                                                             */
            flux = new double[tot_nodes]; 
            for (i = 0; i<tot_nodes; i++)
	            flux[i] = 0.0;
            R =new double[tot_nodes, 2 * divisions[1] * divisions[2] + 1];
            for (i = 0; i<tot_nodes; i++)
            {
	            for (j = 0; j< (2* divisions[1]* divisions[2] +1); ++j)
	            R[i][j] = 0.0;
            }
            find_k_and_q(design, divisions, tot_nodes, ss_dim, flux, fringe);
            set_up_SS_matrix(design, flux, R, ss_dim, divisions, tot_nodes);
            LU_Decomp(design, R, flux, tot_nodes, divisions[1]*divisions[2]);
            find_avg_temp(design, tot_nodes, ss_dim);
    
            for (i = 0; i<tot_nodes; i++)
            free(R[i]);
            free(R);
            free(flux);
        }

/* ---------------------------------------------------------------------------------- */
/* This function defines the divisions of the sides and the lengths of the divisions. */
/* ---------------------------------------------------------------------------------- */
        public static void define_divisions(Design design, int[] divisions, double[] ss_dim, int tot_nodes, double[] fringe)
        {
            double ratio;
            double[] contain_dim = new double[3];
            int i;
 
            for (i = 0; i< 3; i++)
            {
	            contain_dim[i] = (2* fringe[i] + design.box_max[i] - design.box_min[i]);
            }
            ratio = Math.Pow((tot_nodes/(contain_dim[0]*contain_dim[1]*contain_dim[2])), (1.0/3.0));
            for (i = 0; i< 3; i++)
            {
	            divisions[i] = ((int) (contain_dim[i]* ratio + 0.5));
	            ss_dim[i] = contain_dim[i]/divisions[i];
            }
    
        }

/* ---------------------------------------------------------------------------------- */
/* This function initializes the temperature field.  This field is a vector not a     */
/* matrix.  This is because it corresponds to the x in Ax = b.  The comp in the       */
/* tfield structure refers to the number of the component in the list.  If zero, then */
/* corresponds to a simple resistor junction.                                         */
/* ---------------------------------------------------------------------------------- */
        public static void find_k_and_q(Design design, int[] divisions, int tot_nodes, double[] ss_dim, double[] flux, double[] fringe)
        {
            Component comp;
            double subspace_vol, ktot, vol, comp_vol;
            int i, j, k, m = 0;

            subspace_vol = ss_dim[0]* ss_dim[1]* ss_dim[2];
            for (i = 0; i<divisions[0]; i++)
            {
	            for (j = 0; j<divisions[1]; j++)
                {
	                for (k = 0; k<divisions[2]; k++)
                    {
		                design.tfield[m].coord[0] = (design.box_min[0] - fringe[0]) + (0.5* ss_dim[0] + i* ss_dim[0]);
		                design.tfield[m].coord[1] = (design.box_min[1] - fringe[1]) + (0.5* ss_dim[1] + j* ss_dim[1]);
		                design.tfield[m].coord[2] = (design.box_min[2] - fringe[2]) + (0.5* ss_dim[2] + k* ss_dim[2]);
		                comp = design.first_comp;
		                ktot = 0.0;
		                vol = 0.0;
		                flux[m] = 0.0;
		                while (comp != null)
                        {		    
		                    comp_vol = find_vol(comp, design.tfield[m].coord, ss_dim);
                            flux[m] += (comp.q)*((comp_vol) / (comp.dim[0]* comp.dim[1]* comp.dim[2]));
		                    ktot += (comp_vol)*(comp.k);
		                    vol += comp_vol;
		                    comp = comp.next_comp;
		                }
		                design.tfield[m].vol = vol;
		                design.tfield[m].k = (ktot/subspace_vol) + (design.kb)*(1 - (vol/subspace_vol));
		                ++m;
	                }
	            }
            }
        }

/* ---------------------------------------------------------------------------------- */
/* This function sets up the cooefficient matrix R.  This matrix corresponds to the   */
/* A in Ax = b.  Calculations of all the thermal resistances and boundary conditions  */
/* are done in this subroutine.                                                       */
/* ---------------------------------------------------------------------------------- */
        public static double find_vol(Component comp, double[] sscoord, double[] ss_dim)
        {
            double dx, dy, dz;

            dx = (comp.dim[0] + ss_dim[0])/2.0 - Math.Abs(comp.coord[0] - sscoord[0]);
            if (dx< 0) dx = 0;
            if (dx > comp.dim[0]) dx = comp.dim[0];
            if (dx > ss_dim[0]) dx = ss_dim[0];
            dy = (comp.dim[1] + ss_dim[1])/2.0 - Math.Abs(comp.coord[1] - sscoord[1]);
            if (dy< 0) dy = 0;
            if (dy > comp.dim[1]) dy = comp.dim[1];
            if (dy > ss_dim[1]) dy = ss_dim[1];
            dz = (comp.dim[2] + ss_dim[2])/2.0 - Math.Abs(comp.coord[2] - sscoord[2]);
            if (dz< 0) dz = 0;
            if (dz > comp.dim[2]) dz = comp.dim[2];
            if (dz > ss_dim[2]) dz = ss_dim[2];

            return(dx* dy* dz);
        }    

/* ---------------------------------------------------------------------------------- */
/* This function sets up the cooefficient matrix R.  This matrix corresponds to the   */
/* A in Ax = b.  Calculations of all the thermal resistances and boundary conditions  */
/* are done in this subroutine.                                                       */
/* ---------------------------------------------------------------------------------- */
        public static void set_up_SS_matrix(Design design, double[] flux, double[][] R, double[] ss_dim, int[] divisions, int tot_nodes)
        {
            int i, j, k, c;

            tot_nodes = divisions[0]* divisions[1]* divisions[2];
            c = divisions[1]* divisions[2];
            for (i = 0; i<tot_nodes; ++i) 
	        for (j = 0; j< (2* divisions[1]* divisions[2] +1); j++) 
	    R[i][j] = 0.0;
/* The combination of simple resistances is of the form:
 *     2*k1*k2*A
 * R = ---------     for resistances between two subspaces
 *     l(k1 + k2)    - where k1, k2 are the average conductivities
 *                     of the two subspaces.
 *		     - l is the distance between centers
 *                   - A is the area between
 * Similarly for boundary nodes:
 *	2*k1*h*A
 *  R = --------    -h is the boundary convection coefficient
 *	2*k1+h*l 
 */
    for (i = 0; i<tot_nodes; ++i) {
	/* West */
	if ((i-divisions[1]* divisions[2]) >= 0) {
	    R[i][c] += 2*(design.tfield[i].k)*(design.tfield[(i - divisions[1] * divisions[2])].k)*(ss_dim[1]* ss_dim[2])/
			((ss_dim[0])*(design.tfield[i].k + design.tfield[(i - divisions[1] * divisions[2])].k));
	    R[i][(c - divisions[1] * divisions[2])] = -2*(design.tfield[i].k)*(design.tfield[(i - divisions[1] * divisions[2])].k)*(ss_dim[1]* ss_dim[2])/
			((ss_dim[0])*(design.tfield[i].k + design.tfield[(i - divisions[1] * divisions[2])].k));
	} else {
	    R[i][c] += 2*(design.tfield[i].k)*(design.h[0])*(ss_dim[1]* ss_dim[2])/
			((ss_dim[0])*(design.h[0]) + 2*(design.tfield[i].k));
	    flux[i] += (design.tamb)*2*(design.tfield[i].k)*(design.h[0])*(ss_dim[1]* ss_dim[2])/
			((ss_dim[0])*(design.h[0]) + 2*(design.tfield[i].k));
	}
	/* East */
	if ((i+divisions[1]* divisions[2]) < tot_nodes) {
	    R[i][c] += 2*(design.tfield[i].k)*(design.tfield[(i + divisions[1] * divisions[2])].k)*(ss_dim[1]* ss_dim[2])/
			((ss_dim[0])*(design.tfield[i].k + design.tfield[(i + divisions[1] * divisions[2])].k));
	    R[i][(c + divisions[1] * divisions[2])] = -2*(design.tfield[i].k)*(design.tfield[(i + divisions[1] * divisions[2])].k)*(ss_dim[1]* ss_dim[2])/
			((ss_dim[0])*(design.tfield[i].k + design.tfield[(i + divisions[1] * divisions[2])].k));
	} else {
	    R[i][c] += 2*(design.tfield[i].k)*(design.h[0])*(ss_dim[1]* ss_dim[2])/
			((ss_dim[0])*(design.h[0]) + 2*(design.tfield[i].k));
	    flux[i] += (design.tamb)*2*(design.tfield[i].k)*(design.h[0])*(ss_dim[1]* ss_dim[2])/
			((ss_dim[0])*(design.h[0]) + 2*(design.tfield[i].k));
	}
	/* South */
	if ((i % (divisions[1]* divisions[2])) >= divisions[2]) {
	    R[i][c] += 2*(design.tfield[i].k)*(design.tfield[(i - divisions[2])].k)*(ss_dim[0]* ss_dim[2])/
			((ss_dim[1])*(design.tfield[i].k + design.tfield[(i - divisions[2])].k));
	    R[i][(c - divisions[2])] = -2*(design.tfield[i].k)*(design.tfield[(i - divisions[2])].k)*(ss_dim[0]* ss_dim[2])/
			((ss_dim[1])*(design.tfield[i].k + design.tfield[(i - divisions[2])].k));
	} else {
	    R[i][c] += 2*(design.tfield[i].k)*(design.h[1])*(ss_dim[0]* ss_dim[2])/
			((ss_dim[1])*(design.h[1]) + 2*(design.tfield[i].k));
	    flux[i] += (design.tamb)*2*(design.tfield[i].k)*(design.h[1])*(ss_dim[0]* ss_dim[2])/
			((ss_dim[1])*(design.h[1]) + 2*(design.tfield[i].k));
	}
	/* North */
	if (((i+divisions[2]) % (divisions[1]* divisions[2])) >= divisions[2]) {
	    R[i][c] += 2*(design.tfield[i].k)*(design.tfield[(i + divisions[2])].k)*(ss_dim[0]* ss_dim[2])/
			((ss_dim[1])*(design.tfield[i].k + design.tfield[(i + divisions[2])].k));
	    R[i][(c + divisions[2])] = -2*(design.tfield[i].k)*(design.tfield[(i + divisions[2])].k)*(ss_dim[0]* ss_dim[2])/
			((ss_dim[1])*(design.tfield[i].k + design.tfield[(i + divisions[2])].k));
	} else {
	    R[i][c] += 2*(design.tfield[i].k)*(design.h[1])*(ss_dim[0]* ss_dim[2])/
			((ss_dim[1])*(design.h[1]) + 2*(design.tfield[i].k));
	    flux[i] += (design.tamb)*2*(design.tfield[i].k)*(design.h[1])*(ss_dim[0]* ss_dim[2])/
			((ss_dim[1])*(design.h[1]) + 2*(design.tfield[i].k));
	}
	/* Down */
	if ((i % divisions[2]) != 0) {
	    R[i][c] += 2*(design.tfield[i].k)*(design.tfield[(i - 1)].k)*(ss_dim[0]* ss_dim[1])/
			((ss_dim[2])*(design.tfield[i].k + design.tfield[(i - 1)].k));
	    R[i][(c - 1)] = -2*(design.tfield[i].k)*(design.tfield[(i - 1)].k)*(ss_dim[0]* ss_dim[1])/
			((ss_dim[2])*(design.tfield[i].k + design.tfield[(i - 1)].k));
	} else {
	    R[i][c] += 2*(design.tfield[i].k)*(design.h[2])*(ss_dim[0]* ss_dim[1])/
			((ss_dim[2])*(design.h[2]) + 2*(design.tfield[i].k));
	    flux[i] += (design.tamb)*2*(design.tfield[i].k)*(design.h[2])*(ss_dim[0]* ss_dim[1])/
			((ss_dim[2])*(design.h[2]) + 2*(design.tfield[i].k));
	}
	/* Up */
	if (((i+1) % divisions[2]) != 0) {
	    R[i][c] += 2*(design.tfield[i].k)*(design.tfield[(i + 1)].k)*(ss_dim[0]* ss_dim[1])/
			((ss_dim[2])*(design.tfield[i].k + design.tfield[(i + 1)].k));
	    R[i][(c + 1)] = -2*(design.tfield[i].k)*(design.tfield[(i + 1)].k)*(ss_dim[0]* ss_dim[1])/
			((ss_dim[2])*(design.tfield[i].k + design.tfield[(i + 1)].k));
	} else {
	    R[i][c] += 2*(design.tfield[i].k)*(design.h[2])*(ss_dim[0]* ss_dim[1])/
			((ss_dim[2])*(design.h[2]) + 2*(design.tfield[i].k));
	    flux[i] += (design.tamb)*2*(design.tfield[i].k)*(design.h[2])*(ss_dim[0]* ss_dim[1])/
			((ss_dim[2])*(design.h[2]) + 2*(design.tfield[i].k));
	}
    }
}

/* ---------------------------------------------------------------------------
 * This function returns the average temperature of the components based on the
 * weighted average of the sub spaces that they occupy.
 */
        public static void find_avg_temp(Design design, int tot_nodes,double[] ss_dim)
        {
            int i;
            double tot_vol, tot_temp;
            double temp_avg, vol;
            Component comp;

            comp = design.first_comp;
            int j = 0;
            while (comp != null)
            {
	            tot_vol = 0.0;
	            tot_temp = 0.0;
	            for (i = 0; i<tot_nodes; i++)
                {
	                vol = find_vol(comp, design.tfield[i].coord, ss_dim);
                    tot_temp += (vol)*(design.tfield[i].temp);
	                tot_vol += vol;
	            }
	            temp_avg = tot_temp/tot_vol;
	            comp.temp = design.hcf* temp_avg;
                i++;
                comp = design.components[i];
            }
        }

/* ---------------------------------------------------------------------------------- */
/* This function corrects the approximation method by comparing it to the LU method.  */
/* The value for design.hcf (heat correction factor) will be used by the app_method. */
/* ---------------------------------------------------------------------------------- */
        public static void correct_SS_by_LU(Design design)
        {
            Component comp;
            double tempSS, tempMM;

            tempMM = 0.0;
            tempSS = 0.0;
            design.hcf = 1.0;
            design.gauss = 0;


            thermal_analysis_SS(design);
            comp = design.first_comp;
            int i = 0;
            while (comp != null)
            {
	            tempSS += comp.temp/Constants.COMP_NUM;
                i++;
                comp = design.components[i];
            }   
            tempSS = design.first_comp.temp;


            thermal_analysis_MM(design);
            comp = design.first_comp;
            i = 0;
            while (comp != null)
            {
	            tempMM += comp.temp/Constants.COMP_NUM;
                /*if (tempMM < comp.temp) tempMM = comp.temp;*/
                i++;
                comp = design.components[i];
            }   
            design.hcf = tempMM/tempSS;
        }
    }
}
