using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3D_LayoutOpt
{
	class Node
	{
		private int id;
		private bool status;
		private List<Node> neighbors;
		private double[] coordinates;

		public int Id { get => id; set => id = value; }
		public bool Status { get => status; set => status = value; }
		public double[] Coordinates { get => coordinates; set => coordinates = value; }
		internal List<Node> Neighbors { get => neighbors; set => neighbors = value; }

		public Node(int id, bool status, double[] coordinates)
		{
			Id = id;
			Status = status;
			Coordinates = coordinates;
		}
	}
}
