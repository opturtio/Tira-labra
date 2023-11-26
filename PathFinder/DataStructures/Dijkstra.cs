﻿namespace PathFinder.Algorithms
{
    using PathFinder.DataStructures;

    /// <summary>
    /// Dijkstra algorithm.
    /// </summary>
    public class Dijkstra
    {
        private readonly Graph graph;

        /// <summary>
        /// Initializes a new instance of the <see cref="Dijkstra"/> class.
        /// </summary>
        /// <param name="graph">The graph to be processed by the Dijkstra algorithm</param>
        public Dijkstra(Graph graph)
        {
            this.graph = graph;
        }

        /// <summary>
        /// Finds the shortest path between two given nodes.
        /// </summary>
        /// <param name="start">The point where the path begins.</param>
        /// <param name="end">The point where the path ends.</param>
        public void FindShortestPath(Node start, Node end)
        {
            start.Cost = 0;

            // Create a queue that sorts points by how far they are
            var priorityQueue = new PriorityQueue<Node, double>();
            priorityQueue.Enqueue(start, 0);

            while (priorityQueue.Count > 0)
            {
                // Selects the node with the shortest distance from the queue.
                var currentNode = priorityQueue.Dequeue();

                if (currentNode == end)
                {
                    break;
                }

                // Check the nodes connected to the current point.
                foreach (var (neighborNode, cost) in graph.GetNeighborsWithCosts(currentNode))
                {
                    double newDistance = currentNode.Cost + cost;

                    // Update neighbor's distance and parent if a shorter path is found, then queue it for further exploration.
                    if (newDistance < neighborNode.Cost)
                    {
                        neighborNode.Cost = newDistance;
                        neighborNode.Parent = currentNode;
                        priorityQueue.Enqueue(neighborNode, newDistance);
                    }
                }
            }
        }
    }
}
