using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3D_LayoutOpt
{
	class Edge
	{
		private int id;
		private bool status;
		private double length;
		private Node[] adjNodes;

		public int Id { get => id; set => id = value; }
		public bool Status { get => status; set => status = value; }
		public double Length { get => length; set => length = value; }
		internal Node[] AdjNodes { get => adjNodes; set => adjNodes = value; }
	}
}
