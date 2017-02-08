using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3D_LayoutOpt
{
    public class candidate
    {
        internal bool gauss;
        internal bool small_move;
        private double[] objectiveFunction;
        private double[] x;
        public double[] new_obj_values;
        public List<component> components;
    }

    public  class component
    {
        public double temp;
        public double tempcrit;
    }

    public class FDAThermalAnalysis
    {
        public void Simulate(candidate c)
        {
            if ((c.gauss) && (c.small_move))
            {
                calc_thermal_matrix_gauss(c);
                c.small_move = false;
            }
            else if (c.gauss)
            {
                calc_thermal_matrix_LU(c);
            }
            else calc_thermal_matrix_app(c);
            /*calc_thermal_matrix_LU(c);*/
            c.new_obj_values[3] = 0.0;
            c.new_obj_values[4] = 0.0;
            foreach (var comp in c.components)
            {
                if (comp.temp>comp.tempcrit)
                c.new_obj_values[3] += (comp.temp - comp.tempcrit) * (comp.temp - comp.tempcrit) / (c.components.Count);
                c.new_obj_values[4] += (comp.temp * comp.temp) / (comp.tempcrit * comp.tempcrit * c.components.Count);

            }
        }
        
        private void calc_thermal_matrix_app(candidate candidate)
        {
            throw new NotImplementedException();
        }

        private void calc_thermal_matrix_LU(candidate candidate)
        {
            throw new NotImplementedException();
        }

        private void calc_thermal_matrix_gauss(candidate candidate)
        {
            throw new NotImplementedException();
        }
    }
}
