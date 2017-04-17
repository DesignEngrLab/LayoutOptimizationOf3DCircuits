using System.Collections.Generic;
using TVGL;
using OptimizationToolbox;
using System;


namespace _3D_LayoutOpt
{
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
            box_max = new[] { double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity };
            box_min = new[] { double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity };
            OldDesignVars = (double[][])DesignVars.Clone();
            int k = 0;
            for (int i = 0; i < comp_count; i++)
            {
                var move = new double[6];
                for (int j = 0; j < 6; j++)
                {
                    DesignVars[i][j] = x[k++];
                    move[j] = DesignVars[i][j] - OldDesignVars[i][j];
                }
                components[i].Update(move);
                for (int j = 0; j < 3; j++)
                {
                    if (box_min[j] < components[j].ts.Bounds[j][0])
                        box_min[j] = components[j].ts.Bounds[j][0];
                    if (box_max[j] > components[j].ts.Bounds[j][1])
                        box_max[j] = components[j].ts.Bounds[j][1];
                }
            }
        }
    }
}