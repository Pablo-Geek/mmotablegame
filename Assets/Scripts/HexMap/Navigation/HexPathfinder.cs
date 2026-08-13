using System.Collections.Generic;
using UnityEngine;

namespace MMOTableGame.Hexes.Navigation
{
    public static class HexPathfinder
    {
        public static bool TryFindPath(
            HexNavigationGraph graph,
            HexNavigationNode start,
            HexNavigationNode goal,
            List<HexNavigationNode> path)
        {
            path.Clear();
            if (graph == null || start == null || goal == null)
            {
                return false;
            }

            if (start == goal)
            {
                path.Add(start);
                return true;
            }

            List<HexNavigationNode> open = new() { start };
            HashSet<HexNavigationNode> closed = new();
            Dictionary<HexNavigationNode, HexNavigationNode> cameFrom = new();
            Dictionary<HexNavigationNode, int> costs = new() { [start] = 0 };
            List<HexNavigationNode> neighbors = new(6);

            while (open.Count > 0)
            {
                HexNavigationNode current = FindLowestScore(open, costs, goal);
                if (current == goal)
                {
                    ReconstructPath(cameFrom, current, path);
                    return true;
                }

                open.Remove(current);
                closed.Add(current);
                graph.GetNeighbors(current, neighbors);

                foreach (HexNavigationNode neighbor in neighbors)
                {
                    if (closed.Contains(neighbor))
                    {
                        continue;
                    }

                    int tentativeCost = costs[current] + 1;
                    if (!costs.TryGetValue(neighbor, out int knownCost) || tentativeCost < knownCost)
                    {
                        cameFrom[neighbor] = current;
                        costs[neighbor] = tentativeCost;
                        if (!open.Contains(neighbor))
                        {
                            open.Add(neighbor);
                        }
                    }
                }
            }

            return false;
        }

        public static int HexDistance(HexCoordinates from, HexCoordinates to)
        {
            int qDistance = Mathf.Abs(from.Q - to.Q);
            int rDistance = Mathf.Abs(from.R - to.R);
            int sDistance = Mathf.Abs(from.S - to.S);
            return Mathf.Max(qDistance, rDistance, sDistance);
        }

        private static HexNavigationNode FindLowestScore(
            List<HexNavigationNode> open,
            Dictionary<HexNavigationNode, int> costs,
            HexNavigationNode goal)
        {
            HexNavigationNode best = open[0];
            int bestHeuristic = HexDistance(best.Coordinates, goal.Coordinates);
            int bestScore = costs[best] + bestHeuristic;

            for (int index = 1; index < open.Count; index++)
            {
                HexNavigationNode candidate = open[index];
                int heuristic = HexDistance(candidate.Coordinates, goal.Coordinates);
                int score = costs[candidate] + heuristic;
                if (score < bestScore || score == bestScore && heuristic < bestHeuristic)
                {
                    best = candidate;
                    bestScore = score;
                    bestHeuristic = heuristic;
                }
            }

            return best;
        }

        private static void ReconstructPath(
            Dictionary<HexNavigationNode, HexNavigationNode> cameFrom,
            HexNavigationNode current,
            List<HexNavigationNode> path)
        {
            path.Add(current);
            while (cameFrom.TryGetValue(current, out HexNavigationNode previous))
            {
                current = previous;
                path.Add(current);
            }

            path.Reverse();
        }
    }
}
