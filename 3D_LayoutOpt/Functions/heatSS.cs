using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3D_LayoutOpt
{
    class HeatSS
    {

        /* ---------------------------------------------------------------------------------- */
        /*                                                                                    */
        /*                             HEATSS.C -- Sub-Space Method                           */
        /*                                                                                    */
        /* ---------------------------------------------------------------------------------- */
        public static void thermal_analysis_SS(Design design)
        {
            double[] flux;
            double[][] R;
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
            R =new double[tot_nodes][];
            for (i = 0; i<tot_nodes; i++)
            {
	            for (j = 0; j< (2* divisions[1]* divisions[2] +1); ++j)
	            R[i][j] = 0.0;
            }
            find_k_and_q(design, divisions, tot_nodes, ss_dim, flux, fringe);
            set_up_SS_matrix(design, flux, R, ss_dim, divisions, tot_nodes);
            HeatMM.LU_Decomp(design, R, flux, tot_nodes, divisions[1]*divisions[2]);
            find_avg_temp(design, tot_nodes, ss_dim);
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION DEFINES THE DIVISIONS OF THE SIDES AND THE LENGTHS OF THE DIVISIONS. */
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
        /* THIS FUNCTION INITIALIZES THE TEMPERATURE FIELD.  THIS FIELD IS A VECTOR NOT A     */
        /* MATRIX.  THIS IS BECAUSE IT CORRESPONDS TO THE X IN AX = B.  THE COMP IN THE       */
        /* TFIELD STRUCTURE REFERS TO THE NUMBER OF THE COMPONENT IN THE LIST.  IF ZERO, THEN */
        /* CORRESPONDS TO A SIMPLE RESISTOR JUNCTION.                                         */
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

		                ktot = 0.0;
		                vol = 0.0;
		                flux[m] = 0.0;

                        for (int n = 0; n < design.comp_count; n++)
                        {
                            comp = design.components[n];
                            comp_vol = find_vol(comp, design.tfield[m].coord, ss_dim);
                            flux[m] += (comp.q) * ((comp_vol) / ((comp.ts.XMax - comp.ts.XMin) * (comp.ts.YMax - comp.ts.YMin) * (comp.ts.ZMax - comp.ts.ZMin)));
                            ktot += (comp_vol) * (comp.k);
                            vol += comp_vol;
                        }

		                design.tfield[m].vol = vol;
		                design.tfield[m].k = (ktot/subspace_vol) + (design.kb)*(1 - (vol/subspace_vol));
		                ++m;
	                }
	            }
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION SETS UP THE COOEFFICIENT MATRIX R.  THIS MATRIX CORRESPONDS TO THE   */
        /* A IN AX = B.  CALCULATIONS OF ALL THE THERMAL RESISTANCES AND BOUNDARY CONDITIONS  */
        /* ARE DONE IN THIS SUBROUTINE.                                                       */
        /* ---------------------------------------------------------------------------------- */
        public static double find_vol(Component comp, double[] sscoord, double[] ss_dim)
        {
            double dx, dy, dz;
            dx = ((comp.ts.XMax - comp.ts.XMin) + ss_dim[0])/2.0 - Math.Abs(comp.ts.Center[0] - sscoord[0]);
            if (dx < 0) dx = 0;
            if (dx > (comp.ts.XMax - comp.ts.XMin)) dx = (comp.ts.XMax - comp.ts.XMin);
            if (dx > ss_dim[0]) dx = ss_dim[0];
            dy = ((comp.ts.YMax - comp.ts.YMin) + ss_dim[1])/2.0 - Math.Abs(comp.ts.Center[1] - sscoord[1]);
            if (dy< 0) dy = 0;
            if (dy > (comp.ts.YMax - comp.ts.YMin)) dy = (comp.ts.YMax - comp.ts.YMin);
            if (dy > ss_dim[1]) dy = ss_dim[1];
            dz = ((comp.ts.ZMax - comp.ts.ZMin) + ss_dim[2])/2.0 - Math.Abs(comp.ts.Center[2] - sscoord[2]);
            if (dz< 0) dz = 0;
            if (dz > (comp.ts.ZMax - comp.ts.ZMin)) dz = (comp.ts.ZMax - comp.ts.ZMin);
            if (dz > ss_dim[2]) dz = ss_dim[2];

            return(dx* dy* dz);
        }    

        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION SETS UP THE COOEFFICIENT MATRIX R.  THIS MATRIX CORRESPONDS TO THE   */
        /* A IN AX = B.  CALCULATIONS OF ALL THE THERMAL RESISTANCES AND BOUNDARY CONDITIONS  */
        /* ARE DONE IN THIS SUBROUTINE.                                                       */
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


            for (int j = 0; j < design.comp_count; j++)
            {
                comp = design.components[j];
                tot_vol = 0.0;
                tot_temp = 0.0;
                for (i = 0; i < tot_nodes; i++)
                {
                    vol = find_vol(comp, design.tfield[i].coord, ss_dim);
                    tot_temp += (vol) * (design.tfield[i].temp);
                    tot_vol += vol;
                }
                temp_avg = tot_temp / tot_vol;
                comp.temp = design.hcf * temp_avg;

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

            for (int i = 0; i < design.comp_count; i++)
            {
                comp = design.components[i];
                tempSS += comp.temp / design.comp_count;
            }

       
            tempSS = design.components[0].temp;
            HeatMM.thermal_analysis_MM(design);

            for (int i = 0; i < design.comp_count; i++)
            {
                comp = design.components[i];
                tempMM += comp.temp / design.comp_count;
            }
            
            design.hcf = tempMM/tempSS;
        }
    }
}
