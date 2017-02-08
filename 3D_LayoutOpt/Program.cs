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
            int comp_num, i, which, end_time;
            double eval, h, w, l;
            char wait;
            Design design = new Design();
            Component comp;

            int start_time = Program.get_time();
            Program.setseed();

            /* The component data is read in from a file and the number of components is returned */
            comp_num = getdata(design);
            /*  Console.WriteLine("%d components were read in from the file.\n\n",comp_num);*/
            if (comp_num != Constants.COMP_NUM)
            {
                Console.WriteLine("The number of components read in is not the number expected.\n");
                //exit();
            }

            Program.initializations(design);

            /*  Console.WriteLine("Sampling points in design space\n\n");*/
            sample_space(design);  /* IN SCHEDULE.C */

        #ifdef WAIT
            Console.WriteLine("\nHit return to continue.\n\n");
            getchar(wait);
        #endif

        #ifdef BOTH
            Console.WriteLine("Problem set up as minimization of weighted sum of area_ratio and overlap.\n");
        #endif

            /*  Console.WriteLine("Ready to begin search using simulated annealing algorithm.\n\n");*/

        #ifdef WAIT
            Console.WriteLine("\nHit return to continue.\n\n");
            getchar(wait);
        #endif

            anneal(design);                        /* This function is in anneal_alg.c */
            save_design(design);
            save_container(design);
            save_tfield(design);

            /* downhill(design, MIN_MOVE_DIST);      */
            end_time = get_time();
            fptr = fopen("results","a");
            if (design.new_obj_values[1] != 0.0)
            {
                fprintf(fptr,"*** THE FINAL OVERLAP WAS NOT ZERO!!!\n");
                Console.WriteLine("*** THE FINAL OVERLAP WAS NOT ZERO!!!\n");
            }
            fprintf(fptr, "The elapsed time was %d seconds\n",(end_time - start_time));
            Console.WriteLine("The elapsed time was %d seconds\n",(end_time - start_time));
            fclose(fptr);
 

        }

        public static void setseed()
        {
            int seconds;
            seconds = get_time();
            Console.WriteLine("\nSetting seed for random number generator to %d.\n\n", seconds);
            using (StreamWriter writetext = new StreamWriter("output/seed.out"))
            {
                writetext.WriteLine("The seed is %d\n\n", seconds);
            }

            using (StreamWriter writetext = new StreamWriter("results"))
            {
                writetext.WriteLine("\nThe seed is %d\n", seconds);
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
        public static int getdata(Design design)
        {
            int i;
            double x_dim, y_dim, z_dim, tempcrit, q, k, pi;
            char[] name = new char[Constants.MAX_NAME_LENGTH];
            char[] type = new char[Constants.MAX_NAME_LENGTH];
            char[] shape = new char[5];

            pi = 4.0*Math.Atan(1.0);
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
                        design.h[0] = Convert.ToDouble(items[5]);
                        design.h[0] = Convert.ToDouble(items[6]);
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
                        string[] items = line.Split('\t',' ');
                        Component comp_ptr = new Component();
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
                            comp_ptr.mass = comp_ptr.dim_initial[0] * comp_ptr.dim_initial[1] * comp_ptr.dim_initial[2];
                        }
                        else
                        {
                            comp_ptr.half_area = (pi * comp_ptr.dim_initial[0] * comp_ptr.dim_initial[0] / 4.0) + (pi * comp_ptr.dim_initial[2] * comp_ptr.dim_initial[0] / 2.0);
                            comp_ptr.volume = pi * comp_ptr.dim_initial[0] * comp_ptr.dim_initial[0] * comp_ptr.dim_initial[2] / 4.0;
                            comp_ptr.mass = pi * comp_ptr.dim_initial[0] * comp_ptr.dim_initial[0] * comp_ptr.dim_initial[2] / 4.0;
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
            return design.components.Count();
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
          init_overlaps(design);          /* This function is in obj_function.c */

          Console.WriteLine("Initializing weights.\n\n");
          init_weights(design);

          Console.WriteLine("Initializing heat parameters.\n\n");
          init_heat_param(design);        /* This function is in heat.c */
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
            int i, j;
            Component temp_comp;

        #ifdef LOCATE
            Console.WriteLine("Entering init_locations\n");
        #endif

            i = 0;
            temp_comp = design.components[i];
            while (i <= Constants.COMP_NUM - 1)
            {
                temp_comp.orientation = my_random(1, 6);
                temp_comp.orientation = 1;
                update_dim(temp_comp);
                j = -1;
                while (++j <= Constants.DIMENSION -1)
	                temp_comp.coord[j] = my_double_random(-Constants.INITIAL_BOX_SIZE, Constants.INITIAL_BOX_SIZE);
                if (Constants.DIMENSION == 2)
	                temp_comp.coord[2] = 0.0;
                Console.WriteLine("%d Dimensional Initial Placement\n", Constants.DIMENSION);
                ++i;
                temp_comp = design.components[i];
            }

        /* Set the initial max and min bounding box dimensions */
          i = 0;
          while (i <= 2)
          {
              design.box_min[i] = temp_comp.coord[i];
              design.box_max[i] = temp_comp.coord[i];
              ++i;
          }

        #ifdef LOCATE
            Console.WriteLine("Leavinging init_locations\n");
        #endif

        }

        /* ---------------------------------------------------------------------------------- */
        /* This function initializes the bounds for the bounding box.                         */
        /* ---------------------------------------------------------------------------------- */
        static void init_bounds(Design design)
        {
            int i, j;
            Component comp;

            i = 0;
            comp = design.components[i];
            while (++i <= Constants.COMP_NUM)
            {
                j = 0;
                while (j <=2)
	            {

                    update_min_bounds(design, comp, j);
                    update_max_bounds(design, comp, j);
	                ++j;
	            }
                comp = design.components[i];
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function returns a random integer between integers rndmin and rndmax.         */
        /* ---------------------------------------------------------------------------------- */
        static int my_random(int rndmin, int rndmax)
        {
            int t = get_time();
            Random r = new Random(t);
            return r.Next(rndmin, rndmax+1); //for ints
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function returns a random integer between integers rndmin and rndmax.         */
        /* ---------------------------------------------------------------------------------- */
        static double my_double_random(double rndmin, double rndmax)
        {
            int t = get_time();
            Random random = new Random(t);
            return random.NextDouble() * (rndmax - rndmin) + rndmin;
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function updates the x-y-z dimensions of a component based in its initial     */
        /* dimensions and its current orientation.                                            */
        /* ---------------------------------------------------------------------------------- */
        static void update_dim(Component comp)
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
        void update_bounds(Design design, Component comp)
        {
        int i, j;
        Component temp_comp;
        char wait;

        #ifdef LOCATE
          Console.WriteLine("Entering update_bounds\n");
        #endif

        /* First test to see if we are moving the min_comp.  If we are, not, we just update   */
        /* min bounds for this component.  If we are, we have to update bounds for all the    */
        /* elements to find the new one (which may the the same as the current one).  To      */
        /* correctly update the bounds, we reset the box_min (since we've moved the min_comp  */
        /* the old value is no longer valid).                                                 */
        i = -1;
          while (++i <= 2)
            {

        /*      Console.WriteLine("i = %d\n",i);
              Console.WriteLine("The min and max comps are\n%s, %s, %s\n%s, %s, %s\n",design.min_comp[0].comp_name,
	         design.min_comp[1].comp_name,design.min_comp[2].comp_name,
	         design.max_comp[0].comp_name,design.max_comp[1].comp_name,
	         design.max_comp[2].comp_name);
        */

              if (comp != design.min_comp[i])

            update_min_bounds(design, comp, i);
              else
	        {
        /*	  Console.WriteLine("Min comp may have changed - recomputing min bounds\n");
        */
	          design.box_min[i] = comp.coord[i];
	          j = 0;
	          temp_comp = design.first_comp;
	          while (++j <= Constants.COMP_NUM)
	            {

                  update_min_bounds(design, temp_comp, i);
	              if (j< Constants.COMP_NUM)

                temp_comp = temp_comp.next_comp;
	            }
	        }
            }

        /* Now do the same for the max_comp. */
          i = -1;
          while (++i <= 2)
            {
              if (comp != design.max_comp[i])

            update_max_bounds(design, comp, i);
              else
	        {
        /*	  Console.WriteLine("Max comp may have changed - recomputing max bounds\n");
        */
	          design.box_max[i] = comp.coord[i];
	          j = 0;
	          temp_comp = design.first_comp;
	          while (++j <= Constants.COMP_NUM)
	            {

                  update_max_bounds(design, temp_comp, i);
	              if (j< Constants.COMP_NUM)

                temp_comp = temp_comp.next_comp;
	            }
	        }
            }

        #ifdef LOCATE
          Console.WriteLine("Leaving update_bounds\n");
        #endif

        }

        /* ---------------------------------------------------------------------------------- */
        /* This function updates the  min x, y and z bounds for the bounding box.             */
        /* ---------------------------------------------------------------------------------- */
        static void update_min_bounds(Design design, Component comp, int i)
        {
            double location;

        #ifdef LOCATE
            Console.WriteLine("Entering update_min_bounds\n");
        #endif

        /*  Console.WriteLine("updating min bounds %d for %s\n",i,comp.comp_name);
        */

            location = comp.coord[i] - comp.dim[i]/2.0;
            if (location<design.box_min[i])
            {
	            design.box_min[i] = location;
	            design.min_comp[i] = comp;
            }

        # ifdef LOCATE
            Console.WriteLine("Leaving update_min_bounds\n");
        #endif

        }

        /* ---------------------------------------------------------------------------------- */
        /* This function updates the max and min x, y and z bounds for the bounding box.      */
        /* ---------------------------------------------------------------------------------- */
        static void update_max_bounds(Design design, Component comp, int i)
        {

        #ifdef LOCATE
            Console.WriteLine("Entering update_max_bounds\n");
        #endif

        /*  Console.WriteLine("updating max bounds %d for %s\n",i,comp.comp_name);
        */

            double location = comp.coord[i] + comp.dim[i]/2.0;
            if (location > design.box_max[i])
            {
	            design.box_max[i] = location;
	            design.max_comp[i] = comp;
            }

        #ifdef LOCATE
            Console.WriteLine("Leaving update_max_bounds\n");
        #endif

        }


        /* ---------------------------------------------------------------------------------- */
        /* This function calculates the center of gravity.                                    */
        /* ---------------------------------------------------------------------------------- */
        void calc_c_grav(Design design)
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
                    restore_design(design);
                    downhill(design, move_size);
                    eval = evaluate(design);
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
