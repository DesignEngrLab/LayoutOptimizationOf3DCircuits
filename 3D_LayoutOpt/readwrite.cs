using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3D_LayoutOpt
{
    static class readwrite
    {

        /* ---------------------------------------------------------------------------------- */
        /*                                                                                    */
        /*                                    READWRITE.C                                      */
        /*                                                                                    */
        /* ---------------------------------------------------------------------------------- */

        /* ---------------------------------------------------------------------------------- */
        /* This function prints out component information.                                    */
        /* ---------------------------------------------------------------------------------- */

        static void fprint_data(Design design, int which)
        {
            int i;
            Component comp;

            using (StreamWriter writetext = new StreamWriter("/comp.out"))
            {
                comp = design.components[0];
                i = 1;
                while (++i <= which)
                    comp = design.components[i];


                writetext.WriteLine("\nComponent name is %s and the orientation is %d\n", comp.comp_name,
                    comp.orientation);

                i = -1;
                while (++i < 3)
                    writetext.WriteLine("dim %d is %lf\n", i, comp.dim[i]);

                i = -1;
                while (++i < 3)
                    writetext.WriteLine("dim_initial %d is %lf\n", i, comp.dim_initial[i]);

                i = -1;
                while (++i < 3)
                    writetext.WriteLine("current dim %d is %lf\n", i, comp.dim[i]);

                i = -1;
                while (++i < 3)
                    writetext.WriteLine("coord %d is %lf\n", i, comp.coord[i]);

                writetext.WriteLine("\n");
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function writes accepted steps to a file.                                     */
        /* ---------------------------------------------------------------------------------- */

        static void write_step(Design design, int iteration, int flag)
        {

            if (iteration == 0)         //????????????????????
            {
                StreamWriter F1 = new StreamWriter("/ratio.out");
                StreamWriter F2 = new StreamWriter("/container.out");
                StreamWriter F3 = new StreamWriter("/overlap.out");
                StreamWriter F4 = new StreamWriter("/coef.out");
                StreamWriter F5 = new StreamWriter("/overlap2.out");


                if (flag != 0)
                {

                    F1.WriteLine("\n", iteration, design.new_obj_values[0]);

                    F3.WriteLine("\n", iteration,
                        (design.new_obj_values[1]*design.weight[1]));
                }

/*      if (flag == 0)
	{
	  fprintf(fptr1,"%d %lf R\n",iteration, design.new_obj_values[0]);
	  fprintf(fptr2,"%d %lf R\n",iteration, 
		  (design.new_obj_values[2]*design.coef[2]*design.weight[2]));
	  fprintf(fptr3,"%d %lf R\n",iteration, (design.new_obj_values[1]*design.coef[1]));

	  fprintf(fptr5,"%d %lf R\n",iteration, design.new_obj_values[1]);

	  fprintf(fptr4,"%d %lf R\n",iteration, design.coef[1]);
	}
      else if (flag == 3)
	{
	  fprintf(fptr1,"%d %lf A  *\n",iteration, design.new_obj_values[0]);
	  fprintf(fptr2,"%d %lf A  *\n",iteration, 
		  (design.new_obj_values[2]*design.coef[2]*design.weight[2]));
	  fprintf(fptr3,"%d %lf A  *\n",iteration, (design.new_obj_values[1]*design.coef[1]));

	  fprintf(fptr5,"%d %lf A  *\n",iteration, design.new_obj_values[1]);

	  fprintf(fptr4,"%d %lf A  *\n",iteration, design.coef[1]);
	}
      else
	{
	  fprintf(fptr1,"%d %lf A\n",iteration, design.new_obj_values[0]);
	  fprintf(fptr2,"%d %lf A\n",iteration, 
		  (design.new_obj_values[2]*design.coef[2]*design.weight[2]));
	  fprintf(fptr3,"%d %lf A\n",iteration, (design.new_obj_values[1]*design.coef[1]));

	  fprintf(fptr5,"%d %lf A\n",iteration, design.new_obj_values[1]);

	  fprintf(fptr4,"%d %lf A\n",iteration, design.coef[1]);
	}
*/

                F1.Close();
                F2.Close();
                F3.Close();
                F4.Close();
                F5.Close();
            }
        }

/* ---------------------------------------------------------------------------------- */
/* This function writes data regarding the last temperature to a file.                */
/* ---------------------------------------------------------------------------------- */

        public static void write_loop_data(double t, int steps_at_t, int accept_count, int bad_accept_count, int gen_limit, int flag)
        {
            using (StreamWriter writetext = new StreamWriter("/temperature.out"))
            {
                if (flag == 1)
                {
                    writetext.WriteLine("Temperature set at %lf\n", t);
                    writetext.WriteLine("At this temperature %d steps were taken.  %d were accepted\n",
                        steps_at_t, accept_count);
                    writetext.WriteLine("Of the accepted steps, %d were inferior steps\n", bad_accept_count);
                    writetext.WriteLine("Equilibrium was ");
                    if (steps_at_t > gen_limit)

                        writetext.WriteLine("not ");
                    writetext.WriteLine("reached at this temperature\n\n");
                }
                else if (flag == 2)
                    writetext.WriteLine("Design is now frozen\n\n\n");
                else if (flag == 3)
                {
                    writetext.WriteLine("Temperature set at infinity (Downhill search)\n");
                    writetext.WriteLine("%d steps were taken.  %d were accepted\n\n\n", steps_at_t, accept_count);
                }
            }
        }

/* ---------------------------------------------------------------------------------- */
/* This function prints out component information.                                    */
/* ---------------------------------------------------------------------------------- */

        static void print_data(Design design, int which)
        {
            int i;
            Component comp;

            comp = design.components[0];
            i = 1;
            while (++i <= which)
                comp = design.components[i];


            Console.WriteLine("\nComponent name is %s and the orientation is %d\n", comp.comp_name, comp.orientation);

            i = -1;
            while (++i < 3)
                Console.WriteLine("dim %d is %lf\n", i, comp.dim[i]);

            i = -1;
            while (++i < 3)
                Console.WriteLine("dim_initial %d is %lf\n", i, comp.dim_initial[i]);

            i = -1;
            while (++i < 3)
                Console.WriteLine("coord %d is %lf\n", i, comp.coord[i]);
            Console.WriteLine("\n");
        }

/* ---------------------------------------------------------------------------------- */
/* This function writes component data to a file.                                     */
/* ---------------------------------------------------------------------------------- */

        static void print_comp_data(Design design)
        {
            int i;

            i = 0;
            while (++i <= Constants.COMP_NUM)
                fprint_data(design, i);
        }

/* ---------------------------------------------------------------------------------- */
/* This function prints out overlap data.                                             */
/* ---------------------------------------------------------------------------------- */

        static void print_overlaps(Design design)
        {
            int i, j;

            i = -1;
            while (++i < Constants.COMP_NUM)
            {
                j = -1;
                while (++j <= i)
                    Console.WriteLine("%lf ", design.overlap[i, j]);
                Console.WriteLine("\n");
            }
        }

/* ---------------------------------------------------------------------------------- */
/* This function writes design data to a file.                                        */
/* ---------------------------------------------------------------------------------- */

        public static void save_design(Design design)
        {
            int i, j;
            double avg_old_value;
            Component comp;

            Console.WriteLine("Saving current design\n");

            i = -1;
            avg_old_value = 0.0;
            while (++i < Constants.BALANCE_AVG)
                avg_old_value += design.old_obj_values[1,i];
            avg_old_value /= Constants.BALANCE_AVG;

            using (StreamWriter writetext = new StreamWriter("/design.data"))
            {
                i = 0;
                comp = design.components[0];
                while (++i <= Constants.COMP_NUM)
                {
                    writetext.WriteLine("%s %s %d\n", comp.comp_name, comp.shape_type, comp.orientation);
                    writetext.WriteLine("%lf %lf %lf\n", comp.dim_initial[0], comp.dim_initial[1], comp.dim_initial[2]);
                    writetext.WriteLine("%lf %lf %lf\n", comp.dim[0], comp.dim[1], comp.dim[2]);
                    writetext.WriteLine("%lf %lf %lf\n", comp.coord[0], comp.coord[1], comp.coord[2]);
                    writetext.WriteLine("%lf %lf   %lf\n", comp.half_area, comp.mass, comp.temp);
                    if (i < Constants.COMP_NUM)

                        comp = design.components[i];
                }
                writetext.WriteLine("%lf %lf %lf %lf\n", design.new_obj_values[1], design.coef[1],
                    design.weight[1], avg_old_value);

            }

        }

/* ---------------------------------------------------------------------------------- */
/* This function writes container data to a file.                                        */
/* ---------------------------------------------------------------------------------- */

        public static void save_container(Design design)
        {
            int i, j;
            double avg_old_value;
            double[] box_dim = new double[3];
            Component comp;

            Console.WriteLine("Saving current container\n");

            using (StreamWriter writetext = new StreamWriter("/container.data"))
            {
                box_dim[0] = design.box_max[0] - design.box_min[0];
                box_dim[1] = design.box_max[1] - design.box_min[1];
                box_dim[2] = design.box_max[2] - design.box_min[2];

                writetext.WriteLine("container B %d\n", 1);
                writetext.WriteLine("%lf %lf %lf\n", box_dim[0], box_dim[1], box_dim[2]);
                writetext.WriteLine("%lf %lf %lf\n", box_dim[0], box_dim[1], box_dim[2]);
                writetext.WriteLine("%lf %lf %lf\n", (design.box_min[0] + box_dim[0]/2),
                    (design.box_min[1] + box_dim[1]/2),
                    (design.box_min[2] + box_dim[2]/2));
            }
        }

/* ---------------------------------------------------------------------------------- */
/* This function reads design data from a file.                                       */
/* ---------------------------------------------------------------------------------- */

        public static void restore_design(Design design)
        {
            int i, j;
            double avg_old_value;
            Component comp;

            Console.WriteLine("Restoring saved design\n");

            using (StreamReader readtext = new StreamReader("/design.data"))
            {
                i = 0;
                comp = design.components[0];
                string line;
                while (++i <= Constants.COMP_NUM)
                {
                    line = readtext.ReadLine();
                    string[] items = line.Split(' ');
                    comp.comp_name = items[0];
                    comp.shape_type = items[1];
                    comp.orientation = Convert.ToInt16(items[2]);

                    line = readtext.ReadLine();
                    items = line.Split(' ');
                    comp.dim_initial[0] = Convert.ToDouble(items[0]);
                    comp.dim_initial[1] = Convert.ToDouble(items[1]);
                    comp.dim_initial[2] = Convert.ToDouble(items[2]);

                    line = readtext.ReadLine();
                    items = line.Split(' ');
                    comp.dim[0] = Convert.ToDouble(items[0]);
                    comp.dim[1] = Convert.ToDouble(items[1]);
                    comp.dim[2] = Convert.ToDouble(items[2]);

                    line = readtext.ReadLine();
                    items = line.Split(' ');
                    comp.coord[0] = Convert.ToDouble(items[0]);
                    comp.coord[1] = Convert.ToDouble(items[1]);
                    comp.coord[2] = Convert.ToDouble(items[2]);

                    line = readtext.ReadLine();
                    items = line.Split(' ');
                    comp.half_area = Convert.ToDouble(items[0]);
                    comp.mass = Convert.ToDouble(items[1]);
                    comp.temp = Convert.ToDouble(items[2]);
                    if (i < Constants.COMP_NUM)
                        comp = design.components[i];
                }
                line = readtext.ReadLine();
                string[] items2 = line.Split(' ');
                design.new_obj_values[1] = Convert.ToDouble(items2[0]);
                design.coef[1] = Convert.ToDouble(items2[1]);
                design.weight[1] = Convert.ToDouble(items2[2]);
                avg_old_value = Convert.ToDouble(items2[3]);
            }

            Program.init_bounds(design);
            obj_function.init_overlaps(design);
            i = -1;
            while (++i < Constants.OBJ_NUM)
            {
                j = -1;
                while (++j < Constants.BALANCE_AVG)
                    design.old_obj_values[i,j] = avg_old_value;
            }
        }

/* ---------------------------------------------------------------------------------- */
/* This function writes to a file: the current move probabilities, the percentage     */
/* change in delta_c due to each move, and the percentage of attempts for each move.  */
/* ---------------------------------------------------------------------------------- */

        public static void write_probs(Hustin hustin, double temp)
        {
            int i, total_attempts;
            double total_delta_c;

            total_attempts = 0;
            total_delta_c = 0;

            i = -1;
            while (++i < Constants.MOVE_NUM)
            {
                total_attempts += hustin.attempts[i];
                total_delta_c += hustin.delta_c[i];
            }

            StreamWriter F1 = new StreamWriter("/probs.out");
            StreamWriter F2 = new StreamWriter("/delta_c.out");
            StreamWriter F3 = new StreamWriter("/attempts.out");


            F1.WriteLine(temp);
            F2.WriteLine(temp);
            F3.WriteLine(temp);

            i = -1;
            while (++i < Constants.MOVE_NUM)
            {
                F1.WriteLine(hustin.prob[i]);
                F2.WriteLine(hustin.delta_c[i]/total_delta_c);
                F3.WriteLine(1*hustin.attempts[i]/total_attempts);
            }
            F1.WriteLine("\n");
            F2.WriteLine("\n");
            F3.WriteLine("\n");
            F1.Close();
            F2.Close();
            F3.Close();
        }

/* ---------------------------------------------------------------------------------- */
/* This function writes the final temperature field to a file.                        */
/* ---------------------------------------------------------------------------------- */

        public static void save_tfield(Design design)
        {
            int k = 0;
            Console.WriteLine("Saving current tfield\n");
            StreamWriter F1 = new StreamWriter("/tfield.data");

            while (design.tfield[k].temp != 0.0)
            {
                F1.WriteLine("\n", design.tfield[k].coord[0],
                    design.tfield[k].coord[1], design.tfield[k].coord[2],
                    design.tfield[k].temp);
                /*if (design.tfield[k].coord[2] == 0.0) 
	fprintf(fptr, "%lf %lf %lf\n", design.tfield[k].coord[0], 
	design.tfield[k].coord[1], design.tfield[k].temp);*/
                ++k;
            }
            F1.Close();
        }

/* ---------------------------------------------------------------------------------- */
/* This function restores the temperature field to a old_temp's.                       */
/* ---------------------------------------------------------------------------------- */

        static void restore_tfield(Design design)
        {
            int k = 0;
            Console.WriteLine("Restoring tfield\n");

            using (StreamReader readtext = new StreamReader("datafile1"))
            {
                string line;
                while ((line = readtext.ReadLine()) != null)
                {
                    string[] items = line.Split('\t', ' ');
                    design.tfield[k].coord[0] = Convert.ToDouble(items[0]);
                    design.tfield[k].coord[1] = Convert.ToDouble(items[1]);
                    design.tfield[k].coord[2] = Convert.ToDouble(items[2]);
                    design.tfield[k].old_temp = Convert.ToDouble(items[3]);
                }
            }

        }
    }
}
