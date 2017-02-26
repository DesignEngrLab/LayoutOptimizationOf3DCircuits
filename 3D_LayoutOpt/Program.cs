using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3D_LayoutOpt
{
    class Program
    {
        static void Main(string[] args)
        {
            int i, which, end_time;
            double eval, h, w, l;
            char wait;
            Design design = new Design();
            Component comp;

            int start_time = Program.get_time();
            Program.setseed();

            /* The component data is read in from a file and the number of components is returned */
            getdata(design);
            design.store_component_cnt();
            Console.WriteLine("{0} components were read in from the file.\n\n",design.comp_count);


            Program.initializations(design);

            /*  Console.WriteLine("Sampling points in design space\n\n");*/
            Schedules.sample_space(design);  /* IN SCHEDULE.C */

        #if WAIT
            Console.WriteLine("\nHit return to continue.\n\n");
            getchar(wait);
        #endif

        #if BOTH
            Console.WriteLine("Problem set up as minimization of weighted sum of area_ratio and overlap.\n");
        #endif

            /*  Console.WriteLine("Ready to begin search using simulated annealing algorithm.\n\n");*/

        #if WAIT
            Console.WriteLine("\nHit return to continue.\n\n");
            getchar(wait);
        #endif

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
                    writetext.WriteLine("*** THE FINAL OVERLAP WAS NOT ZERO!!!\n");
                    Console.WriteLine("*** THE FINAL OVERLAP WAS NOT ZERO!!!\n");
                }
                writetext.WriteLine("The elapsed time was %d seconds\n", (end_time - start_time));
                Console.WriteLine("The elapsed time was %d seconds\n", (end_time - start_time));
            }


        }

        public static void setseed()
        {
            int seconds;
            seconds = get_time();
            Console.WriteLine("\nSetting seed for random number generator to {0}", seconds);
            using (StreamWriter writetext = new StreamWriter("output/seed.out"))
            {
                writetext.WriteLine("The seed is {0}\n\n", seconds);
            }

            using (StreamWriter writetext = new StreamWriter("results"))
            {
                writetext.WriteLine("\nThe seed is {0}\n", seconds);
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
        /* This function gets component data from a file.                                     */
        /* ---------------------------------------------------------------------------------- */
        public static void getdata(Design design)
        {
            int i;
            double x_dim, y_dim, z_dim, tempcrit, q, k, pi;
            char[] name = new char[Constants.MAX_NAME_LENGTH];
            char[] type = new char[Constants.MAX_NAME_LENGTH];
            char[] shape = new char[5];
            
            try
            {
                using (StreamReader readtext = new StreamReader("datafile2"))
                {
                    Console.WriteLine("Reading container dimensions from file.\n");
                    string line;
                    while ((line = readtext.ReadLine()) != null)
                    {
                        string[] items = line.Split(' ');
                        design.container[0] = Convert.ToDouble(items[0]);
                        design.container[1] = Convert.ToDouble(items[1]);
                        design.container[2] = Convert.ToDouble(items[2]);
                        design.kb = Convert.ToDouble(items[3]);
                        design.h[0] = Convert.ToDouble(items[4]);
                        design.h[1] = Convert.ToDouble(items[5]);
                        design.h[2] = Convert.ToDouble(items[6]);
                        design.tamb = Convert.ToDouble(items[7]);
                    }
                }
            }
            catch (IOException ex)
            {
                
            }

            try
            {
                using (StreamReader readtext = new StreamReader("datafile1"))
                {
                    Console.WriteLine("Reading component data from file.\n");
                    design.half_area = 0;
                    design.volume = 0.0;
                    design.mass = 0.0;
                    i = 0;
                    string line;
                    while ((line = readtext.ReadLine()) != null)
                    {
                        string[] items = line.Split(new char[]{ ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        Component comp_ptr = new Component();                   //TO DO: change comp_ptr to comp
                        comp_ptr.comp_name = items[0].ToCharArray();
                        comp_ptr.shape_type = items[1].ToCharArray();
                        comp_ptr.dim_initial[0] = Convert.ToDouble(items[2]);
                        comp_ptr.dim_initial[1] = Convert.ToDouble(items[3]);
                        comp_ptr.dim_initial[2] = Convert.ToDouble(items[4]);
                        comp_ptr.tempcrit = Convert.ToDouble(items[5]);
                        comp_ptr.q = Convert.ToDouble(items[6]);
                        comp_ptr.k = Convert.ToDouble(items[7]);
                        comp_ptr.orientation = 0;
                        if (comp_ptr.shape_type[0] == 'B')
                        {
                            comp_ptr.half_area = (comp_ptr.dim_initial[0] * comp_ptr.dim_initial[1]) + (comp_ptr.dim_initial[1] * comp_ptr.dim_initial[2]) + (comp_ptr.dim_initial[0] * comp_ptr.dim_initial[2]);
                            comp_ptr.volume = comp_ptr.dim_initial[0] * comp_ptr.dim_initial[1] * comp_ptr.dim_initial[2];
                            comp_ptr.mass = comp_ptr.dim_initial[0] * comp_ptr.dim_initial[1] * comp_ptr.dim_initial[2];                    // TO DO: add density so that mass = density * volume
                        }
                        else
                        {
                            comp_ptr.half_area = (Math.PI * comp_ptr.dim_initial[0] * comp_ptr.dim_initial[0] / 4.0) + (Math.PI * comp_ptr.dim_initial[2] * comp_ptr.dim_initial[0] / 2.0);
                            comp_ptr.volume = Math.PI * comp_ptr.dim_initial[0] * comp_ptr.dim_initial[0] * comp_ptr.dim_initial[2] / 4.0;
                            comp_ptr.mass = Math.PI * comp_ptr.dim_initial[0] * comp_ptr.dim_initial[0] * comp_ptr.dim_initial[2] / 4.0;
                        }
                        design.half_area += comp_ptr.half_area;
                        design.volume += comp_ptr.volume;
                        design.mass += comp_ptr.mass;
                        design.components.Add(comp_ptr);
                    }
                    Console.WriteLine("EOF reached in the input file.\n\n");
                }
            }
            catch (IOException ex)
            {
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function calls several other initialization functions.                        */
        /* ---------------------------------------------------------------------------------- */
        public static void initializations(Design design)
        {
          Console.WriteLine("Initializing locations.\n\n");
          init_locations(design);

          Console.WriteLine("Initializing box bounds.\n\n");
          init_bounds(design);

          Console.WriteLine("Initializing overlaps.\n\n");
          obj_function.init_overlaps(design);          /* This function is in obj_function.c */

          Console.WriteLine("Initializing weights.\n\n");
          init_weights(design);

          Console.WriteLine("Initializing heat parameters.\n\n");
          heatbasic.init_heat_param(design);        /* This function is in heat.c */
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

        #if LOCATE
            Console.WriteLine("Entering init_locations\n");
        #endif
   
            for (int i = 0; i < design.comp_count; i++)
            {
                temp_comp = design.components[i];
                temp_comp.orientation = my_random(1, 6);            //TO DO: WHY SET IT TO 1 AFTER RANDOM?
                temp_comp.orientation = 1;
                update_dim(temp_comp);
                for (int j = 0; j < Constants.DIMENSION; j++)
                {
                    temp_comp.coord[j] = my_double_random(-Constants.INITIAL_BOX_SIZE, Constants.INITIAL_BOX_SIZE);
                }
                if (Constants.DIMENSION == 2)
                    temp_comp.coord[2] = 0.0;
                Console.WriteLine("%d Dimensional Initial Placement {0}\n", Constants.DIMENSION);                 
                
            }

            /* Set the initial max and min bounding box dimensions */
            for (int i = 0; i < 3; i++)
            {
                design.box_min[i] = temp_comp.coord[i];
                design.box_max[i] = temp_comp.coord[i];
            }

        #if LOCATE
            Console.WriteLine("Leavinging init_locations\n");
        #endif

        }

        /* ---------------------------------------------------------------------------------- */
        /* This function initializes the bounds for the bounding box.                         */
        /* ---------------------------------------------------------------------------------- */
        public static void init_bounds(Design design)
        {
            Component comp;

            
            for (int i = 0; i < design.comp_count; i++)
            {
                comp = design.components[i];
                update_min_bounds(design, comp);
                update_max_bounds(design, comp);   
            }
        }

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
            Random random = new Random(t);
            return random.NextDouble() * (rndmax - rndmin) + rndmin;
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function updates the x-y-z dimensions of a component based in its initial     */
        /* dimensions and its current orientation.                                            */
        /* ---------------------------------------------------------------------------------- */
        public static void update_dim(Component comp)
        {
          switch(comp.orientation)
          {
            case 1:
              comp.dim[0] = comp.dim_initial[0];
              comp.dim[1] = comp.dim_initial[1];
              comp.dim[2] = comp.dim_initial[2];
              break;
            case 2:
              comp.dim[0] = comp.dim_initial[0];
              comp.dim[1] = comp.dim_initial[2];
              comp.dim[2] = comp.dim_initial[1];
              break;
            case 3:
              comp.dim[0] = comp.dim_initial[1];
              comp.dim[1] = comp.dim_initial[0];
              comp.dim[2] = comp.dim_initial[2];
              break;
            case 4:
              comp.dim[0] = comp.dim_initial[1];
              comp.dim[1] = comp.dim_initial[2];
              comp.dim[2] = comp.dim_initial[0];
              break;
            case 5:
              comp.dim[0] = comp.dim_initial[2];
              comp.dim[1] = comp.dim_initial[0];
              comp.dim[2] = comp.dim_initial[1];
              break;
            case 6:
              comp.dim[0] = comp.dim_initial[2];
              comp.dim[1] = comp.dim_initial[1];
              comp.dim[2] = comp.dim_initial[0];
              break;
            default:
              Console.WriteLine("\nCase error in update_dim\n");
              break;
          }
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function updates the max and min x, y and z bounds for the bounding box.      */
        /* ---------------------------------------------------------------------------------- */
        public static void update_bounds(Design design, Component comp)
        {
            Component temp_comp;
            char wait;

        #if LOCATE
            Console.WriteLine("Entering update_bounds\n");
        #endif

        /* First test to see if we are moving the min_comp.  If we are, not, we just update   */
        /* min bounds for this component.  If we are, we have to update bounds for all the    */
        /* elements to find the new one (which may the the same as the current one).  To      */
        /* correctly update the bounds, we reset the box_min (since we've moved the min_comp  */
        /* the old value is no longer valid).                                                 */

            for (int i = 0; i < 3; i++)
            {
                if (comp != design.min_comp[i])
                    update_min_bounds(design, comp);
                else
                {
                    /*	  Console.WriteLine("Min comp may have changed - recomputing min bounds\n");
                    */
                    design.box_min[i] = comp.coord[i];

                    for (int j = 0; j < design.comp_count; j++)
                    {
                        temp_comp = design.components[j];
                        update_min_bounds(design, temp_comp);
                    }

                }
            }


        /* Now do the same for the max_comp. */
            for (int i = 0; i < 3; i++)
            {
                if (comp != design.max_comp[i])
                    update_max_bounds(design, comp);
                else
                {
                    /*	  Console.WriteLine("Max comp may have changed - recomputing max bounds\n");
                    */
                    design.box_max[i] = comp.coord[i];
                    for (int j = 0; j < design.comp_count; j++)
                    {
                        temp_comp = design.components[j];
                        update_max_bounds(design, temp_comp);
                    }
                }
            }

        #if LOCATE
            Console.WriteLine("Leaving update_bounds\n");
        #endif

        }

        /* ---------------------------------------------------------------------------------- */
        /* This function updates the  min x, y and z bounds for the bounding box.             */
        /* ---------------------------------------------------------------------------------- */
        static void update_min_bounds(Design design, Component comp)
        {
            double location;

        #if LOCATE
            Console.WriteLine("Entering update_min_bounds\n");
#endif

            /*  Console.WriteLine("updating min bounds %d for %s\n",i,comp.comp_name);
            */
            for (int i = 0; i < 3; i++)
            {
                location = comp.coord[i] - comp.dim[i] / 2.0;
                if (location < design.box_min[i])
                {
                    design.box_min[i] = location;
                    design.min_comp[i] = comp;
                }
            }
            

        #if LOCATE
            Console.WriteLine("Leaving update_min_bounds\n");
        #endif

        }

        /* ---------------------------------------------------------------------------------- */
        /* This function updates the max and min x, y and z bounds for the bounding box.      */
        /* ---------------------------------------------------------------------------------- */
        static void update_max_bounds(Design design, Component comp)
        {
            double location;

#if LOCATE
            Console.WriteLine("Entering update_max_bounds\n");
#endif

            for (int i = 0; i < 3; i++)
            {
                location = comp.coord[i] + comp.dim[i] / 2.0;
                if (location > design.box_max[i])
                {
                    design.box_max[i] = location;
                    design.max_comp[i] = comp;
                }
            }

        #if LOCATE
            Console.WriteLine("Leaving update_max_bounds\n");
        #endif

        }


        /* ---------------------------------------------------------------------------------- */
        /* This function calculates the center of gravity.                                    */
        /* ---------------------------------------------------------------------------------- */
        public static void calc_c_grav(Design design)
        {
            int i, j;
            double mass;
            double[] sum = new double[3];
            Component comp;

            mass = 0.0;
            sum[0] = 0.0;
            sum[1] = 0.0;
            sum[2] = 0.0;

            i = 0;
            comp = design.components[i];
            while (++i <= Constants.COMP_NUM)
            {
                mass += comp.mass;
                j = -1;
                while (++j <= 2)
	            sum[j] += comp.mass* comp.coord[j];
                comp = design.components[i];
            }
            i = -1;
            while (++i <= 2)
            design.c_grav[i] = sum[i]/mass;
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
                    writetext.WriteLine("%lf", move_size);
                    readwrite.restore_design(design);
                    anneal_alg.downhill(design, move_size);
                    eval = obj_function.evaluate(design, 0, 0);
                    writetext.WriteLine(" %lf\n", eval);
                    if (eval < min_eval)
                        min_eval = eval;
                    move_size += .05;
                }
                writetext.WriteLine("THE MIN WAS %lf", min_eval);
            }
        }
    }
}
