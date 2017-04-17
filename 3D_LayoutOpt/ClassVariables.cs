using System.Collections.Generic;
using TVGL;
using OptimizationToolbox;
using System;


namespace _3D_LayoutOpt
{

    public class Component
    {
        public List<TessellatedSolid> ts = null;
        public List<TessellatedSolid> backup_ts = null;
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

    public class Design : IDependentAnalysis
    {
        public double[][] DesignVars;
        public double[][] OldDesignVars;
        public List<Net> Netlist = null;


        /* Box_min and box_max are the x, y, z bounds for the bounding box.  Overlap is a     */
        /* matrix which contains the overlap between the ith and the jth components.  Note    */
        /* that only the top half of the overlap matrix is used.  half_area is half of the    */
        /* total of all the surface areas of the components.                                  */

        public int comp_count;
        public double[] old_orientation;
        public double[] box_min = new double[3];
        public double[] box_max = new double[3];
        public double[,] overlap;
        public double[] old_coord = new double[3];
        public double[] old_dim = new double[3];
        public double[] c_grav = new double[3];
        //public double[] container = new double[3];                                             //ENCLOSURE DIMENSIONS READ FROM FILE
        public Container container;
        public double half_area, volume, mass;

        /* First comp is a pointer to the first component in the component list.  Min_comp    */
        /* and max_comp are pointers to the components which contribute to the x, y, z max    */
        /* and min bounds (i.e. box_min and box_max). Backup is a pointer to a component that */
        /* contains backup information in case we reject a step and need to revert to a       */
        /* previous design.                                                                   */

        public Component[] min_comp = new Component[3];
        public Component[] max_comp = new Component[3];
        public List<Component> components = new List<Component>();

        /* THESE HAVE BEEN ADDED FOR BALANCING THE COMPONENTS OF THE OBJECTIVE FUNCTION       */
        /* new_obj_values are the latest values of the components of the objective function.  */
        /* They are copied to a column of old_obj_values matrix (only on improvement steps).  */
        /* old_obj_values keeps track of past values of the components of the objective       */
        /* function.  Currently, the 0th line in the matrix are values of the area ratio and  */
        /* the 1st line in the matrix are values of the overlap, coef is an array of          */
        /* coefficients which the components of the objective function get multiplied by to   */
        /* balance it.  (The 0th coefficient is ignored.).  The weights are also multiplied   */
        /* with the components of the objective function, to specify relative importance of   */
        /* the different components.  Backup_obj_values is used to back up the array of       */
        /* new_obj_values before taking a step.  When a step is rejected, the revert function */
        /* copies the backed up values to new_obj_values to restore the previous state.       */

        public double[] new_obj_values = new double[Constants.OBJ_NUM];
        public double[,] old_obj_values = new double[Constants.OBJ_NUM, Constants.BALANCE_AVG];
        public double[] backup_obj_values = new double[Constants.OBJ_NUM];
        public double[] coef = new double[Constants.OBJ_NUM];
        public double[] weight = new double[Constants.OBJ_NUM];

        /* These variables have been added for the thermal analysis.                          */
        /* tfield is the vector of temperature nodes.  It is constrained to NODE_NUM in each  */
        /* direction.                                                                         */
        /* kb is the conductivity of the board.                                               */
        /* h(x,y,z) is the heat transfer coefficient for the convective boundary conditions.  */
        /* tamb is the ambient temperature of the boundary.                                   */
        /* tolerance is the adjustable tolerance used by the Gauss-Seidel solver that         */
        /* the temperatures are solved to.                                                    */
        /* min_node_space is the minimum allowable node spacing.                              */
        /* hcf is the Approximation Method Correction Factor established using Matrix Method. */
        /* hcf_per_temp is the number of times App gets corrected per temperature.            */
        /* gaussMove is the Move size under which GS is used instead of LU for translations.  */
        /* gauss is the flag that says to use GS.  GS is always used on rotations and Swaps.  */
        /* max_iter is the maximum number of iterations for GS.                               */
        /* analysis_switch[] is the vector of t/initial t steps that the thermal              */
        /* analysis switches.                                                                 */
        /* choice is the flag for the HeatEval H.T. chooser.                                 */

        //public TemperatureNode[] tfield = new TemperatureNode[Constants.NODE_NUM * Constants.NODE_NUM * Constants.NODE_NUM];
        public TemperatureNode[] tfield = InitializeArray<TemperatureNode>(Constants.NODE_NUM * Constants.NODE_NUM * Constants.NODE_NUM);
        public double kb, tamb, tolerance, min_node_space;
        public double hcf, gaussMove;
        public double[] h = new double[3];
        public double[] analysis_switch = new double[Constants.SWITCH_NUM];
        public int hcf_per_temp, gauss, max_iter, choice;

        static T[] InitializeArray<T>(int length) where T : new()
        {
            T[] array = new T[length];
            for (int i = 0; i < length; ++i)
            {
                array[i] = new T();
            }

            return array;
        }

        public void add_comp(Component comp)
        {
            components.Add(comp);
        }

        public void calculate(double[] x)
        {
            OldDesignVars = (double[][])DesignVars.Clone();
            int k = 0;
            foreach (var comp in DesignVars)
            {
                for (int i = 0; i < 6; i++)
                {
                    comp[i] = x[k++];
                }
            }
        }
    }

    public class Schedule
    {
        public int mgl, within_target, max_tolerance, in_count, out_count, equilibrium, problem_size;
        public double t_initial, sigma, c_avg, delta, c_min, c_max, max_delta_c, delta_c;
    }

    public class Hustin
    {
        /* attemts is the number of attempts made for each Move, which_Move is the Move taken */
        /* so we know which Move to attribute a cost change to, delta_c is the cumulative     */
        /* change in cost (absolute value) due to each Move, quality is the quality factor    */
        /* for each Move, probability is the probability if selecting each Move, and Move_    */
        /* size is the distance for each of the translate Moves, usable_prob is the "usable"  */
        /* portion of the probability.  The "unusable" portion (i.e. MIN_PROB * MOVE_NUM) is  */
        /* set aside to give each Move a minimum probability.                                 */
        public int[] attempts = new int[Constants.MOVE_NUM];
        public int which_Move;
        public double[] delta_c = new double[Constants.MOVE_NUM];
        public double[] quality = new double[Constants.MOVE_NUM];
        public double[] prob = new double[Constants.MOVE_NUM];
        public double[] Move_size = new double[Constants.TRANS_NUM];
        public double[] rot_size = new double[Constants.ROT_NUM];
        public double usable_prob;
    }
}
