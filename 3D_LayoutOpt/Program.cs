using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL;

namespace _3D_LayoutOpt
{
    class Program
    {

        private static readonly string[] FileNames = {
            "../../../TestFiles/ABF.ply"
            // "../../../TestFiles/Beam_Boss.STL",
            // //"../../../TestFiles/bigmotor.amf",
            // //"../../../TestFiles/DxTopLevelPart2.shell",
            };

        static void Main(string[] args)
        {
            int i, which, end_time;
            double eval, h, w, l;
            char wait;
            Design design = new Design();
            Component comp;


            Directory.SetCurrentDirectory("../../workspace");
            int start_time = get_time();
            setseed();

            


            // IMPORTING CAD MODELS, COMPONENT AND CONTAINER FEATURES
            readwrite.ImportData(design);
            Console.WriteLine("{0} components were read in from the file.\n",design.comp_count);

            //INITIALIZING THE PROCESS

            initialize(design);
            Console.WriteLine("Sampling points in design space\n");
            Schedules.sample_space(design);  


            anneal_alg.anneal(design);                        /* This function is in anneal_alg.c */
            readwrite.save_design(design);
            readwrite.save_container(design);
            readwrite.save_tfield(design);

            /* downhill(design, MIN_MOVE_DIST);      */
            end_time = get_time();

            using (StreamWriter writetext = new StreamWriter("results"))
            {
                if (design.new_obj_values[1] != 0.0)
                {
                    writetext.WriteLine("*** THE FINAL OVERLAP WAS NOT ZERO!!!");
                    Console.WriteLine("*** THE FINAL OVERLAP WAS NOT ZERO!!!");
                }
                writetext.WriteLine("The elapsed time was {0} seconds", (end_time - start_time));
                Console.WriteLine("The elapsed time was {0} seconds", (end_time - start_time));
            }


        }

        public static void setseed()
        {
            int seconds;
            seconds = get_time();
            Console.WriteLine("\nSetting seed for random number generator to {0}", seconds);
            using (StreamWriter writetext = new StreamWriter("/seed.out"))
            {
                writetext.WriteLine("The seed is {0}\n", seconds);
            }

            using (StreamWriter writetext = new StreamWriter("results"))
            {
                writetext.WriteLine("\nThe seed is {0}", seconds);
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function returns the current time in seconds.                                 */
        /* ---------------------------------------------------------------------------------- */
        public static int get_time()
        {
            int seconds = (int)(DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalSeconds;
            return seconds;
        }

        

        /* ---------------------------------------------------------------------------------- */
        /* This function calls several other initialization functions.                        */
        /* ---------------------------------------------------------------------------------- */
        public static void initialize(Design design)
        {
          Console.WriteLine("Initializing locations.\n");
          init_locations(design);

          //Console.WriteLine("Initializing box bounds.\n");
          //init_bounds(design);

          Console.WriteLine("Initializing overlaps.\n");
          obj_function.init_overlaps(design);          

          Console.WriteLine("Initializing weights.\n");
          init_weights(design);

          Console.WriteLine("Initializing heat parameters.\n");
          heatbasic.init_heat_param(design);        
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function sets the initial objective function weights to 1.0.                  */
        /* ---------------------------------------------------------------------------------- */
        static void init_weights(Design design)
        {
            int i;
            for (i = 0; i< Constants.OBJ_NUM; ++i)
                design.weight[i] = 1.0;
            design.weight[3] = 0.01;
            design.weight[1] = 2.5;

        }

        /* ---------------------------------------------------------------------------------- */
        /* This function initializes the component locations.                                 */
        /* ---------------------------------------------------------------------------------- */
        static void init_locations(Design design)
        {
            Component temp_comp = null;
            Console.WriteLine("Placing components in randomly in 3D space");

            foreach (var comp in design.components)
            {
                var TessellatedSolids = comp.ts;
                foreach (var TessellatedSolid in TessellatedSolids)
                {
                    double[,] BackTransformMatrix = null; 
                    var NewTessellatedSolid = TessellatedSolid.SetToOriginAndSquareTesselatedSolid(out BackTransformMatrix);
                    TessellatedSolids.Remove(TessellatedSolid);
                    TessellatedSolids.Add(NewTessellatedSolid);
                }
                comp.ts = TessellatedSolids;
                temp_comp = comp;
                design.DesignVars[comp.index] = new double[] { 0, 0, 0, 0, 0, 0 };
            }
   
            //for (int i = 0; i < design.comp_count; i++)
            //{
            //    temp_comp = design.components[i];
            //    //temp_comp.orientation = my_random(1, 6);            //TO DO: WHY SET IT TO 1 AFTER RANDOM?
            //    temp_comp.orientation = 1;
            //    update_dim(temp_comp);
            //    for (int j = 0; j < Constants.DIMENSION; j++)
            //    {
            //        temp_comp.coord[j] = my_double_random(-Constants.INITIAL_BOX_SIZE, Constants.INITIAL_BOX_SIZE);
            //    }
            //    if (Constants.DIMENSION == 2)
            //        temp_comp.coord[2] = 0.0;
            //    Console.WriteLine("{0} Dimensional Initial Placement ", Constants.DIMENSION);                 
                
            //}

            /* Set the initial max and min bounding box dimensions to the last component dimensioins */
            design.box_min[0] = temp_comp.ts[1].XMin; design.box_max[0] = temp_comp.ts[1].XMax;
            design.box_min[1] = temp_comp.ts[1].YMin; design.box_max[1] = temp_comp.ts[1].YMax;
            design.box_min[2] = temp_comp.ts[1].ZMin; design.box_max[2] = temp_comp.ts[1].ZMax;

            ///* Set the initial max and min bounding box dimensions */
            //for (int i = 0; i < 3; i++)
            //{
            //    design.box_min[i] = temp_comp.coord[i];
            //    design.box_max[i] = temp_comp.coord[i];
            //}

        #if LOCATE
            Console.WriteLine("Leavinging init_locations");
        #endif

        }

        ///* ---------------------------------------------------------------------------------- */
        ///* This function initializes the bounds for the bounding box.                         */
        ///* ---------------------------------------------------------------------------------- */
        //public static void init_bounds(Design design)
        //{
        //    Component comp;  
        //    for (int i = 0; i < design.comp_count; i++)
        //    {
        //        comp = design.components[i];
        //        update_min_bounds(design, comp);
        //        update_max_bounds(design, comp);   
        //    }
        //}


        /* ---------------------------------------------------------------------------------- */
        /* This function returns a random integer between integers rndmin and rndmax.         */
        /* ---------------------------------------------------------------------------------- */
        public static int my_random(int rndmin, int rndmax)
        {
            int t = get_time();
            Random r = new Random(t);
            return r.Next(rndmin, rndmax+1); //for ints
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function returns a random integer between integers rndmin and rndmax.         */
        /* ---------------------------------------------------------------------------------- */
        public static double my_double_random(double rndmin, double rndmax)
        {
            int t = get_time();
            Random random = new Random();
            return random.NextDouble() * (rndmax - rndmin) + rndmin;
        }

        ///* ---------------------------------------------------------------------------------- */
        ///* This function updates the x-y-z dimensions of a component based in its initial     */
        ///* dimensions and its current orientation.                                            */
        ///* ---------------------------------------------------------------------------------- */
        //public static void update_dim(Component comp)
        //{
        //  switch(comp.orientation)
        //  {
        //    case 1:
        //      comp.dim[0] = comp.dim_initial[0];
        //      comp.dim[1] = comp.dim_initial[1];
        //      comp.dim[2] = comp.dim_initial[2];
        //      break;
        //    case 2:
        //      comp.dim[0] = comp.dim_initial[0];
        //      comp.dim[1] = comp.dim_initial[2];
        //      comp.dim[2] = comp.dim_initial[1];
        //      break;
        //    case 3:
        //      comp.dim[0] = comp.dim_initial[1];
        //      comp.dim[1] = comp.dim_initial[0];
        //      comp.dim[2] = comp.dim_initial[2];
        //      break;
        //    case 4:
        //      comp.dim[0] = comp.dim_initial[1];
        //      comp.dim[1] = comp.dim_initial[2];
        //      comp.dim[2] = comp.dim_initial[0];
        //      break;
        //    case 5:
        //      comp.dim[0] = comp.dim_initial[2];
        //      comp.dim[1] = comp.dim_initial[0];
        //      comp.dim[2] = comp.dim_initial[1];
        //      break;
        //    case 6:
        //      comp.dim[0] = comp.dim_initial[2];
        //      comp.dim[1] = comp.dim_initial[1];
        //      comp.dim[2] = comp.dim_initial[0];
        //      break;
        //    default:
        //      Console.WriteLine("\nCase error in update_dim");
        //      break;
        //  }
        //}

        ///* ---------------------------------------------------------------------------------- */
        ///* This function updates the max and min x, y and z bounds for the bounding box.      */
        ///* ---------------------------------------------------------------------------------- */
        //public static void update_bounds(Design design, Component comp)
        //{
        //    Component temp_comp;
        //    char wait;

        //#if LOCATE
        //    Console.WriteLine("Entering update_bounds");
        //#endif

        ///* First test to see if we are moving the min_comp.  If we are, not, we just update   */
        ///* min bounds for this component.  If we are, we have to update bounds for all the    */
        ///* elements to find the new one (which may the the same as the current one).  To      */
        ///* correctly update the bounds, we reset the box_min (since we've moved the min_comp  */
        ///* the old value is no longer valid).                                                 */

        //    for (int i = 0; i < 3; i++)
        //    {
        //        if (comp != design.min_comp[i])
        //            update_min_bounds(design, comp);
        //        else
        //        {
        //            /*	  Console.WriteLine("Min comp may have changed - recomputing min bounds");
        //            */
        //            design.box_min[i] = comp.coord[i];

        //            for (int j = 0; j < design.comp_count; j++)
        //            {
        //                temp_comp = design.components[j];
        //                update_min_bounds(design, temp_comp);
        //            }

        //        }
        //    }


        ///* Now do the same for the max_comp. */
        //    for (int i = 0; i < 3; i++)
        //    {
        //        if (comp != design.max_comp[i])
        //            update_max_bounds(design, comp);
        //        else
        //        {
        //            /*	  Console.WriteLine("Max comp may have changed - recomputing max bounds");
        //            */
        //            design.box_max[i] = comp.coord[i];
        //            for (int j = 0; j < design.comp_count; j++)
        //            {
        //                temp_comp = design.components[j];
        //                update_max_bounds(design, temp_comp);
        //            }
        //        }
        //    }

        //#if LOCATE
        //    Console.WriteLine("Leaving update_bounds");
        //#endif

        //}

//        /* ---------------------------------------------------------------------------------- */
//        /* This function updates the  min x, y and z bounds for the bounding box.             */
//        /* ---------------------------------------------------------------------------------- */
//        static void update_min_bounds(Design design, Component comp)
//        {
//            double location;

//        #if LOCATE
//            Console.WriteLine("Entering update_min_bounds");
//#endif

//            /*  Console.WriteLine("updating min bounds %d for %s",i,comp.comp_name);
//            */
//            for (int i = 0; i < 3; i++)
//            {
//                location = comp.coord[i] - comp.dim[i] / 2.0;
//                if (location < design.box_min[i])
//                {
//                    design.box_min[i] = location;
//                    design.min_comp[i] = comp;
//                }
//            }
            

//        #if LOCATE
//            Console.WriteLine("Leaving update_min_bounds");
//        #endif

//        }

//        /* ---------------------------------------------------------------------------------- */
//        /* This function updates the max and min x, y and z bounds for the bounding box.      */
//        /* ---------------------------------------------------------------------------------- */
//        static void update_max_bounds(Design design, Component comp)
//        {
//            double location;

//#if LOCATE
//            Console.WriteLine("Entering update_max_bounds");
//#endif

//            for (int i = 0; i < 3; i++)
//            {
//                location = comp.coord[i] + comp.dim[i] / 2.0;
//                if (location > design.box_max[i])
//                {
//                    design.box_max[i] = location;
//                    design.max_comp[i] = comp;
//                }
//            }

//        #if LOCATE
//            Console.WriteLine("Leaving update_max_bounds");
//        #endif

//        }


        /* ---------------------------------------------------------------------------------- */
        /* This function calculates the center of gravity.                                    */
        /* ---------------------------------------------------------------------------------- */
        public static void calc_c_grav(Design design)
        {
            double mass;
            double[] sum = new double[3];
            Component comp;

            mass = 0.0;
            sum[0] = 0.0;
            sum[1] = 0.0;
            sum[2] = 0.0;

            
            for (int i = 0; i < design.comp_count; i++)
            {
                comp = design.components[i];
                mass += comp.mass;
                for (int j = 0; j < 3; j++)
                {
                    sum[j] += comp.mass * comp.coord[j];
                }
            }

            for (int i = 0; i < 3; i++)
            {
                design.c_grav[i] = sum[i] / mass;
            }

        }

        /* ---------------------------------------------------------------------------------- */
        /* This function finds a good downhill step size.                                     */
        /* ---------------------------------------------------------------------------------- */
        void find_step(Design design)
        {
            double eval;
            double move_size = 0.05;
            double min_eval = 5;

            using (StreamWriter writetext = new StreamWriter("size.dat"))
            {
                while (move_size <= 1.25)
                {
                    writetext.WriteLine(move_size);
                    readwrite.restore_design(design);
                    anneal_alg.downhill(design, move_size);
                    eval = obj_function.evaluate(design, 0, 0);
                    writetext.WriteLine("{0}", eval);
                    if (eval < min_eval)
                        min_eval = eval;
                    move_size += .05;
                }
                writetext.WriteLine("THE MIN WAS {0}", min_eval);
            }
        }
    }
}
