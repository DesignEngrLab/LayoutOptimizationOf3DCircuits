using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3D_LayoutOpt
{
    class HeatSs
    {

        /* ---------------------------------------------------------------------------------- */
        /*                                                                                    */
        /*                             HEATSS.C -- Sub-Space Method                           */
        /*                                                                                    */
        /* ---------------------------------------------------------------------------------- */
        public static void thermal_analysis_SS(Design design)
        {
            double[] flux;
            double[][] r;
            var ssDim = new double[3];
            var fringe = new double[3];
            int i, j, totNodes;
            var divisions = new int[3];

            totNodes = (design.Choice)*100;
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

            define_divisions(design, divisions, ssDim, totNodes, fringe);
            totNodes = divisions[0]* divisions[1]* divisions[2];
            /* total nodes might change from predicted amount. */
            /*  These commands set up the flux vector and R matrix using dynamic memory */
            /*  allocation.                                                             */
            flux = new double[totNodes]; 
            for (i = 0; i<totNodes; i++)
	            flux[i] = 0.0;
            r =new double[totNodes][];
            for (i = 0; i<totNodes; i++)
            {
	            for (j = 0; j< (2* divisions[1]* divisions[2] +1); ++j)
	            r[i][j] = 0.0;
            }
            find_k_and_q(design, divisions, totNodes, ssDim, flux, fringe);
            set_up_SS_matrix(design, flux, r, ssDim, divisions, totNodes);
            HeatMm.LU_Decomp(design, r, flux, totNodes, divisions[1]*divisions[2]);
            find_avg_temp(design, totNodes, ssDim);
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION DEFINES THE DIVISIONS OF THE SIDES AND THE LENGTHS OF THE DIVISIONS. */
        /* ---------------------------------------------------------------------------------- */
        public static void define_divisions(Design design, int[] divisions, double[] ssDim, int totNodes, double[] fringe)
        {
            double ratio;
            var containDim = new double[3];
            int i;
 
            for (i = 0; i< 3; i++)
            {
	            containDim[i] = (2* fringe[i] + design.BoxMax[i] - design.BoxMin[i]);
            }
            ratio = Math.Pow((totNodes/(containDim[0]*containDim[1]*containDim[2])), (1.0/3.0));
            for (i = 0; i< 3; i++)
            {
	            divisions[i] = ((int) (containDim[i]* ratio + 0.5));
	            ssDim[i] = containDim[i]/divisions[i];
            }
    
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION INITIALIZES THE TEMPERATURE FIELD.  THIS FIELD IS A VECTOR NOT A     */
        /* MATRIX.  THIS IS BECAUSE IT CORRESPONDS TO THE X IN AX = B.  THE COMP IN THE       */
        /* TFIELD STRUCTURE REFERS TO THE NUMBER OF THE COMPONENT IN THE LIST.  IF ZERO, THEN */
        /* CORRESPONDS TO A SIMPLE RESISTOR JUNCTION.                                         */
        /* ---------------------------------------------------------------------------------- */
        public static void find_k_and_q(Design design, int[] divisions, int totNodes, double[] ssDim, double[] flux, double[] fringe)
        {
            Component comp;
            double subspaceVol, ktot, vol, compVol;
            int i, j, k, m = 0;

            subspaceVol = ssDim[0]* ssDim[1]* ssDim[2];
            for (i = 0; i<divisions[0]; i++)
            {
	            for (j = 0; j<divisions[1]; j++)
                {
	                for (k = 0; k<divisions[2]; k++)
                    {
		                design.Tfield[m].Coord[0] = (design.BoxMin[0] - fringe[0]) + (0.5* ssDim[0] + i* ssDim[0]);
		                design.Tfield[m].Coord[1] = (design.BoxMin[1] - fringe[1]) + (0.5* ssDim[1] + j* ssDim[1]);
		                design.Tfield[m].Coord[2] = (design.BoxMin[2] - fringe[2]) + (0.5* ssDim[2] + k* ssDim[2]);

		                ktot = 0.0;
		                vol = 0.0;
		                flux[m] = 0.0;

                        for (var n = 0; n < design.CompCount; n++)
                        {
                            comp = design.Components[n];
                            compVol = find_vol(comp, design.Tfield[m].Coord, ssDim);
                            flux[m] += (comp.Q) * ((compVol) / ((comp.Ts.XMax - comp.Ts.XMin) * (comp.Ts.YMax - comp.Ts.YMin) * (comp.Ts.ZMax - comp.Ts.ZMin)));
                            ktot += (compVol) * (comp.K);
                            vol += compVol;
                        }

		                design.Tfield[m].Vol = vol;
		                design.Tfield[m].K = (ktot/subspaceVol) + (design.Kb)*(1 - (vol/subspaceVol));
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
        public static double find_vol(Component comp, double[] sscoord, double[] ssDim)
        {
            double dx, dy, dz;
            dx = ((comp.Ts.XMax - comp.Ts.XMin) + ssDim[0])/2.0 - Math.Abs(comp.Ts.Center[0] - sscoord[0]);
            if (dx < 0) dx = 0;
            if (dx > (comp.Ts.XMax - comp.Ts.XMin)) dx = (comp.Ts.XMax - comp.Ts.XMin);
            if (dx > ssDim[0]) dx = ssDim[0];
            dy = ((comp.Ts.YMax - comp.Ts.YMin) + ssDim[1])/2.0 - Math.Abs(comp.Ts.Center[1] - sscoord[1]);
            if (dy< 0) dy = 0;
            if (dy > (comp.Ts.YMax - comp.Ts.YMin)) dy = (comp.Ts.YMax - comp.Ts.YMin);
            if (dy > ssDim[1]) dy = ssDim[1];
            dz = ((comp.Ts.ZMax - comp.Ts.ZMin) + ssDim[2])/2.0 - Math.Abs(comp.Ts.Center[2] - sscoord[2]);
            if (dz< 0) dz = 0;
            if (dz > (comp.Ts.ZMax - comp.Ts.ZMin)) dz = (comp.Ts.ZMax - comp.Ts.ZMin);
            if (dz > ssDim[2]) dz = ssDim[2];

            return(dx* dy* dz);
        }    

        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION SETS UP THE COOEFFICIENT MATRIX R.  THIS MATRIX CORRESPONDS TO THE   */
        /* A IN AX = B.  CALCULATIONS OF ALL THE THERMAL RESISTANCES AND BOUNDARY CONDITIONS  */
        /* ARE DONE IN THIS SUBROUTINE.                                                       */
        /* ---------------------------------------------------------------------------------- */
        public static void set_up_SS_matrix(Design design, double[] flux, double[][] r, double[] ssDim, int[] divisions, int totNodes)
        {
            int i, j, k, c;

            totNodes = divisions[0]* divisions[1]* divisions[2];
            c = divisions[1]* divisions[2];
            for (i = 0; i<totNodes; ++i) 
	        for (j = 0; j< (2* divisions[1]* divisions[2] +1); j++) 
	    r[i][j] = 0.0;
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
    for (i = 0; i<totNodes; ++i) {
	/* West */
	if ((i-divisions[1]* divisions[2]) >= 0) {
	    r[i][c] += 2*(design.Tfield[i].K)*(design.Tfield[(i - divisions[1] * divisions[2])].K)*(ssDim[1]* ssDim[2])/
			((ssDim[0])*(design.Tfield[i].K + design.Tfield[(i - divisions[1] * divisions[2])].K));
	    r[i][(c - divisions[1] * divisions[2])] = -2*(design.Tfield[i].K)*(design.Tfield[(i - divisions[1] * divisions[2])].K)*(ssDim[1]* ssDim[2])/
			((ssDim[0])*(design.Tfield[i].K + design.Tfield[(i - divisions[1] * divisions[2])].K));
	} else {
	    r[i][c] += 2*(design.Tfield[i].K)*(design.H[0])*(ssDim[1]* ssDim[2])/
			((ssDim[0])*(design.H[0]) + 2*(design.Tfield[i].K));
	    flux[i] += (design.Tamb)*2*(design.Tfield[i].K)*(design.H[0])*(ssDim[1]* ssDim[2])/
			((ssDim[0])*(design.H[0]) + 2*(design.Tfield[i].K));
	}
	/* East */
	if ((i+divisions[1]* divisions[2]) < totNodes) {
	    r[i][c] += 2*(design.Tfield[i].K)*(design.Tfield[(i + divisions[1] * divisions[2])].K)*(ssDim[1]* ssDim[2])/
			((ssDim[0])*(design.Tfield[i].K + design.Tfield[(i + divisions[1] * divisions[2])].K));
	    r[i][(c + divisions[1] * divisions[2])] = -2*(design.Tfield[i].K)*(design.Tfield[(i + divisions[1] * divisions[2])].K)*(ssDim[1]* ssDim[2])/
			((ssDim[0])*(design.Tfield[i].K + design.Tfield[(i + divisions[1] * divisions[2])].K));
	} else {
	    r[i][c] += 2*(design.Tfield[i].K)*(design.H[0])*(ssDim[1]* ssDim[2])/
			((ssDim[0])*(design.H[0]) + 2*(design.Tfield[i].K));
	    flux[i] += (design.Tamb)*2*(design.Tfield[i].K)*(design.H[0])*(ssDim[1]* ssDim[2])/
			((ssDim[0])*(design.H[0]) + 2*(design.Tfield[i].K));
	}
	/* South */
	if ((i % (divisions[1]* divisions[2])) >= divisions[2]) {
	    r[i][c] += 2*(design.Tfield[i].K)*(design.Tfield[(i - divisions[2])].K)*(ssDim[0]* ssDim[2])/
			((ssDim[1])*(design.Tfield[i].K + design.Tfield[(i - divisions[2])].K));
	    r[i][(c - divisions[2])] = -2*(design.Tfield[i].K)*(design.Tfield[(i - divisions[2])].K)*(ssDim[0]* ssDim[2])/
			((ssDim[1])*(design.Tfield[i].K + design.Tfield[(i - divisions[2])].K));
	} else {
	    r[i][c] += 2*(design.Tfield[i].K)*(design.H[1])*(ssDim[0]* ssDim[2])/
			((ssDim[1])*(design.H[1]) + 2*(design.Tfield[i].K));
	    flux[i] += (design.Tamb)*2*(design.Tfield[i].K)*(design.H[1])*(ssDim[0]* ssDim[2])/
			((ssDim[1])*(design.H[1]) + 2*(design.Tfield[i].K));
	}
	/* North */
	if (((i+divisions[2]) % (divisions[1]* divisions[2])) >= divisions[2]) {
	    r[i][c] += 2*(design.Tfield[i].K)*(design.Tfield[(i + divisions[2])].K)*(ssDim[0]* ssDim[2])/
			((ssDim[1])*(design.Tfield[i].K + design.Tfield[(i + divisions[2])].K));
	    r[i][(c + divisions[2])] = -2*(design.Tfield[i].K)*(design.Tfield[(i + divisions[2])].K)*(ssDim[0]* ssDim[2])/
			((ssDim[1])*(design.Tfield[i].K + design.Tfield[(i + divisions[2])].K));
	} else {
	    r[i][c] += 2*(design.Tfield[i].K)*(design.H[1])*(ssDim[0]* ssDim[2])/
			((ssDim[1])*(design.H[1]) + 2*(design.Tfield[i].K));
	    flux[i] += (design.Tamb)*2*(design.Tfield[i].K)*(design.H[1])*(ssDim[0]* ssDim[2])/
			((ssDim[1])*(design.H[1]) + 2*(design.Tfield[i].K));
	}
	/* Down */
	if ((i % divisions[2]) != 0) {
	    r[i][c] += 2*(design.Tfield[i].K)*(design.Tfield[(i - 1)].K)*(ssDim[0]* ssDim[1])/
			((ssDim[2])*(design.Tfield[i].K + design.Tfield[(i - 1)].K));
	    r[i][(c - 1)] = -2*(design.Tfield[i].K)*(design.Tfield[(i - 1)].K)*(ssDim[0]* ssDim[1])/
			((ssDim[2])*(design.Tfield[i].K + design.Tfield[(i - 1)].K));
	} else {
	    r[i][c] += 2*(design.Tfield[i].K)*(design.H[2])*(ssDim[0]* ssDim[1])/
			((ssDim[2])*(design.H[2]) + 2*(design.Tfield[i].K));
	    flux[i] += (design.Tamb)*2*(design.Tfield[i].K)*(design.H[2])*(ssDim[0]* ssDim[1])/
			((ssDim[2])*(design.H[2]) + 2*(design.Tfield[i].K));
	}
	/* Up */
	if (((i+1) % divisions[2]) != 0) {
	    r[i][c] += 2*(design.Tfield[i].K)*(design.Tfield[(i + 1)].K)*(ssDim[0]* ssDim[1])/
			((ssDim[2])*(design.Tfield[i].K + design.Tfield[(i + 1)].K));
	    r[i][(c + 1)] = -2*(design.Tfield[i].K)*(design.Tfield[(i + 1)].K)*(ssDim[0]* ssDim[1])/
			((ssDim[2])*(design.Tfield[i].K + design.Tfield[(i + 1)].K));
	} else {
	    r[i][c] += 2*(design.Tfield[i].K)*(design.H[2])*(ssDim[0]* ssDim[1])/
			((ssDim[2])*(design.H[2]) + 2*(design.Tfield[i].K));
	    flux[i] += (design.Tamb)*2*(design.Tfield[i].K)*(design.H[2])*(ssDim[0]* ssDim[1])/
			((ssDim[2])*(design.H[2]) + 2*(design.Tfield[i].K));
	}
    }
}

/* ---------------------------------------------------------------------------
 * This function returns the average temperature of the components based on the
 * weighted average of the sub spaces that they occupy.
 */
        public static void find_avg_temp(Design design, int totNodes,double[] ssDim)
        {
            int i;
            double totVol, totTemp;
            double tempAvg, vol;
            Component comp;


            for (var j = 0; j < design.CompCount; j++)
            {
                comp = design.Components[j];
                totVol = 0.0;
                totTemp = 0.0;
                for (i = 0; i < totNodes; i++)
                {
                    vol = find_vol(comp, design.Tfield[i].Coord, ssDim);
                    totTemp += (vol) * (design.Tfield[i].Temp);
                    totVol += vol;
                }
                tempAvg = totTemp / totVol;
                comp.Temp = design.Hcf * tempAvg;

            }
        }

/* ---------------------------------------------------------------------------------- */
/* This function corrects the approximation method by comparing it to the LU method.  */
/* The value for design.hcf (heat correction factor) will be used by the app_method. */
/* ---------------------------------------------------------------------------------- */
        public static void correct_SS_by_LU(Design design)
        {
            Component comp;
            double tempSs, tempMm;

            tempMm = 0.0;
            tempSs = 0.0;
            design.Hcf = 1.0;
            design.Gauss = 0;


            thermal_analysis_SS(design);

            for (var i = 0; i < design.CompCount; i++)
            {
                comp = design.Components[i];
                tempSs += comp.Temp / design.CompCount;
            }

       
            tempSs = design.Components[0].Temp;
            HeatMm.thermal_analysis_MM(design);

            for (var i = 0; i < design.CompCount; i++)
            {
                comp = design.Components[i];
                tempMm += comp.Temp / design.CompCount;
            }
            
            design.Hcf = tempMm/tempSs;
        }
    }
}
