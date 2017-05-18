using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3D_LayoutOpt
{
	class RMST
	{
		private int numTerminals;
		private List<Node> GraphTerminalNodes;
		private List<Node> graphNodes;
		private List<Edge> graphEdges;
		Dictionary<Tuple<int, int>, double> TerminalNodesSortedQueue;
		public List<Edge> minLengthNet = null;


		public RMST(double[] coordinates)
		{
			CreateHananGraph(coordinates);
			CreateManhattanDistanceQueue(coordinates);
		}

		public void CreateHananGraph(double[] coordinates)
		{
			var id = 0;
			for (int i = 0; i < coordinates.Length / 3; i++)
			{
				for (int j = coordinates.Length / 3 + 1; i < 2 * coordinates.Length / 3; i++)
				{
					for (int k = 2 * coordinates.Length / 3 + 1; i < coordinates.Length; i++)
					{
						var nodeCoordinates = new double[] { coordinates[i], coordinates[j], coordinates[k] };
						var node = new Node(id, false, nodeCoordinates);
						graphNodes.Add(node);
					}
				}
			}
		}

		public List<Edge> Route()
		{
			//TO DO
			List<Node> visitedTermianlNodes = null;
			List<Node> remainingTerminalNodes = GraphTerminalNodes;
			foreach (var item in TerminalNodesSortedQueue)
			{
				var nodeA = GraphTerminalNodes[item.Key.Item1];
				var nodeB = GraphTerminalNodes[item.Key.Item1];
				FindSingularRectilinearPath(nodeA, nodeB, graphNodes);
				remainingTerminalNodes.Remove(nodeA);
				remainingTerminalNodes.Remove(nodeB);
				if (!remainingTerminalNodes.Any())
					break;
			}

			return minLengthNet;
		}

		public List<Edge> FindSingularRectilinearPath(Node nodeA, Node nodeB, List<Node> graphNodes)
		{
			List<Edge> path = null;
			bool reachedNodeB = false;
			var tempNode = nodeA;
			while (!reachedNodeB)
			{
				var neighbors = GetNodeNeighbors(tempNode);
				var minDist = double.MaxValue;
				foreach (var node in neighbors)
				{
					var dist =
						Math.Abs(node.Coordinates[0] - nodeB.Coordinates[0])
						+ Math.Abs(node.Coordinates[1] - nodeB.Coordinates[1])
						+ Math.Abs(node.Coordinates[2] - nodeB.Coordinates[2]);
					if (dist < minDist)
					{
						minDist = dist;
						tempNode = node;
					}
				}
			}

			//TO DO

			return path;
		}

		public void CreateManhattanDistanceQueue(double[] coordinates)
		{
			var TerminalNodesQueue = new Dictionary<Tuple<int, int>, double>();
			for (int i = 0; i < coordinates.Length/3 - 1; i++)
			{
				for (int j = i+1; j < coordinates.Length / 3; j++)
				{
					var distance = 
						  Math.Abs(coordinates[i] - coordinates[j])
						+ Math.Abs(coordinates[i+ coordinates.Length / 3] - coordinates[j+ coordinates.Length / 3]) 
						+ Math.Abs(coordinates[i+ 2*coordinates.Length / 3] - coordinates[j+ 2 * coordinates.Length / 3]);
					var key = new Tuple<int, int>(i, j);
					TerminalNodesQueue.Add(key, distance);
				}
			}
			TerminalNodesSortedQueue = TerminalNodesQueue.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
		}

		public List<Node> GetNodeNeighbors(Node node)
		{
			var neighbors = new List<Node>();
			neighbors.Add(graphNodes[node.Id + 1]);
			neighbors.Add(graphNodes[node.Id - 1]);
			neighbors.Add(graphNodes[node.Id + numTerminals]);
			neighbors.Add(graphNodes[node.Id - numTerminals]);
			neighbors.Add(graphNodes[node.Id + numTerminals * numTerminals]);
			neighbors.Add(graphNodes[node.Id - numTerminals * numTerminals]);
			return neighbors;
		}

	}

}
