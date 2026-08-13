using System.Collections.Generic;
using MMOTableGame.Hexes;
using MMOTableGame.Hexes.Navigation;
using NUnit.Framework;
using UnityEngine;

namespace MMOTableGame.Tests
{
    public sealed class HexPathfinderTests
    {
        private readonly List<GameObject> roots = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject root in roots)
            {
                Object.DestroyImmediate(root);
            }

            roots.Clear();
        }

        [Test]
        public void FindsShortestPathAcrossConnectedTiles()
        {
            HexNavigationGraph graph = CreateGraph(0,
                (new HexCoordinates(0, 0), 0),
                (new HexCoordinates(1, 0), 0),
                (new HexCoordinates(2, 0), 0));

            graph.TryGetNode(new HexCoordinates(0, 0), out HexNavigationNode start);
            graph.TryGetNode(new HexCoordinates(2, 0), out HexNavigationNode goal);
            List<HexNavigationNode> path = new();

            Assert.That(HexPathfinder.TryFindPath(graph, start, goal, path), Is.True);
            Assert.That(path.Count, Is.EqualTo(3));
            Assert.That(path[0], Is.SameAs(start));
            Assert.That(path[2], Is.SameAs(goal));
        }

        [Test]
        public void RejectsDisconnectedDestination()
        {
            HexNavigationGraph graph = CreateGraph(0,
                (new HexCoordinates(0, 0), 0),
                (new HexCoordinates(3, 0), 0));

            graph.TryGetNode(new HexCoordinates(0, 0), out HexNavigationNode start);
            graph.TryGetNode(new HexCoordinates(3, 0), out HexNavigationNode goal);
            List<HexNavigationNode> path = new();

            Assert.That(HexPathfinder.TryFindPath(graph, start, goal, path), Is.False);
            Assert.That(path, Is.Empty);
        }

        [Test]
        public void RejectsLayerStepWithoutRamp()
        {
            HexNavigationGraph graph = CreateGraph(1,
                (new HexCoordinates(0, 0), 0),
                (new HexCoordinates(1, 0), 1));

            graph.TryGetNode(new HexCoordinates(0, 0), out HexNavigationNode start);
            graph.TryGetNode(new HexCoordinates(1, 0), out HexNavigationNode goal);
            List<HexNavigationNode> path = new();

            Assert.That(HexPathfinder.TryFindPath(graph, start, goal, path), Is.False);
        }

        [Test]
        public void RampConnectsItsLowAndHighEndpointsInBothDirections()
        {
            HexCoordinates lowCoordinates = new(0, 1);
            HexCoordinates rampCoordinates = new(0, 0);
            HexCoordinates highCoordinates = new(0, -1);
            HexNavigationGraph graph = CreateGraph(1,
                (lowCoordinates, 0),
                (rampCoordinates, 0),
                (highCoordinates, 1));
            AddRamp(graph, rampCoordinates, 2);

            graph.TryGetNode(lowCoordinates, out HexNavigationNode low);
            graph.TryGetNode(highCoordinates, out HexNavigationNode high);
            List<HexNavigationNode> path = new();

            Assert.That(HexPathfinder.TryFindPath(graph, low, high, path), Is.True);
            Assert.That(path.Count, Is.EqualTo(3));
            Assert.That(path[1].Coordinates, Is.EqualTo(rampCoordinates));

            Assert.That(HexPathfinder.TryFindPath(graph, high, low, path), Is.True);
            Assert.That(path.Count, Is.EqualTo(3));
        }

        [Test]
        public void RampDoesNotConnectSideOrWrongLayerTiles()
        {
            HexCoordinates rampCoordinates = new(0, 0);
            HexNavigationGraph graph = CreateGraph(1,
                (new HexCoordinates(1, 0), 0),
                (rampCoordinates, 0),
                (new HexCoordinates(0, -1), 0));
            AddRamp(graph, rampCoordinates, 2);

            graph.TryGetNode(new HexCoordinates(1, 0), out HexNavigationNode side);
            graph.TryGetNode(new HexCoordinates(0, -1), out HexNavigationNode wrongLayer);
            List<HexNavigationNode> path = new();

            Assert.That(HexPathfinder.TryFindPath(graph, side, wrongLayer, path), Is.False);
        }

        [Test]
        public void RampCannotExceedGraphMaximumLayerStep()
        {
            HexCoordinates lowCoordinates = new(0, 1);
            HexCoordinates rampCoordinates = new(0, 0);
            HexCoordinates highCoordinates = new(0, -1);
            HexNavigationGraph graph = CreateGraph(1,
                (lowCoordinates, 0),
                (rampCoordinates, 0),
                (highCoordinates, 2));
            AddRamp(graph, rampCoordinates, 2, 2);

            graph.TryGetNode(lowCoordinates, out HexNavigationNode low);
            graph.TryGetNode(highCoordinates, out HexNavigationNode high);
            List<HexNavigationNode> path = new();

            Assert.That(HexPathfinder.TryFindPath(graph, low, high, path), Is.False);
        }

        [Test]
        public void UsesOnlyTopTileInAColumn()
        {
            HexNavigationGraph graph = CreateGraph(1,
                (new HexCoordinates(0, 0), 0),
                (new HexCoordinates(0, 0), 2));

            Assert.That(graph.Nodes.Count, Is.EqualTo(1));
            Assert.That(graph.TryGetNode(new HexCoordinates(0, 0), out HexNavigationNode node), Is.True);
            Assert.That(node.Layer, Is.EqualTo(2));
        }

        private HexNavigationGraph CreateGraph(
            int maxStepLayers,
            params (HexCoordinates coordinates, int layer)[] placements)
        {
            GameObject root = new("Test Hex Map");
            roots.Add(root);
            HexMap map = root.AddComponent<HexMap>();
            HexNavigationGraph graph = root.AddComponent<HexNavigationGraph>();

            foreach ((HexCoordinates coordinates, int layer) in placements)
            {
                GameObject tileObject = new($"Tile {coordinates} L{layer}");
                tileObject.transform.SetParent(root.transform);
                HexTileInstance tile = tileObject.AddComponent<HexTileInstance>();
                tile.SetPlacement(coordinates, layer);
                tileObject.transform.localPosition = map.CoordinatesToLocalPosition(coordinates, layer);
            }

            graph.Configure(map, maxStepLayers);
            return graph;
        }

        private static void AddRamp(
            HexNavigationGraph graph,
            HexCoordinates coordinates,
            int uphillDirection,
            int layerDelta = 1)
        {
            Assert.That(graph.TryGetNode(coordinates, out HexNavigationNode rampNode), Is.True);
            HexRamp ramp = rampNode.Tile.gameObject.AddComponent<HexRamp>();
            ramp.Configure(uphillDirection, layerDelta);
            graph.Rebuild();
        }
    }
}
