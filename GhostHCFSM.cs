using GAlgoT2530.AI;
using GAlgoT2530.Engine;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Tiled;
using System.Collections.Generic;
using System.Diagnostics;

namespace PacmanGame
{
    public class GhostHCFSM : HCFSM
    {
        public enum State { Roam, Chase, Evade };

        public State CurrentState;

        private GameEngine _game;
        private Ghost _ghost;
        private TiledMap _tiledMap;
        private TileGraph _tileGraph;

        private Tile _srcTile;
        private Tile _destTile;
        private LinkedList<Tile> _path;

        private Vector2 _destTilePosition;

        public GhostHCFSM(GameEngine game, Ghost ghost, TiledMap map, TileGraph graph)
        {
            _game = game;
            _ghost = ghost;
            _tiledMap = map;
            _tileGraph = graph;
        }

        public override void Initialize()
        {
            CurrentState = State.Roam;

            Roam_Initialize();
        }

        public override void Update()
        {
            Roam_Action();
        }

        /// ROAM STATE ///
        private void Roam_Initialize()
        {
            // Set the ghost Speed
            _ghost.MaxSpeed = 80.0f;

            // Initialize source tile from owner's current position
            _srcTile = Tile.ToTile(_ghost.Position, _tiledMap.TileWidth, _tiledMap.TileHeight);
        }

        private void Roam_Action()
        {
            if (Roam_IsPathEmpty())
            {
                // Update source tile
                _srcTile = Tile.ToTile(_ghost.Position, _tiledMap.TileWidth, _tiledMap.TileHeight);

                // Get new random path
                _path = Roam_GenerateRandomPath(_tileGraph, _srcTile);
                _path.RemoveFirst();

                // Update destination tile and its position
                _destTile = _path.Last.Value;
                _destTilePosition = Tile.ToPosition(_destTile, _tiledMap.TileWidth, _tiledMap.TileHeight);

                // Change animation
                Tile nextTile = _path.First.Value;
                _ghost.UpdateAnimatedSprite(_srcTile, nextTile);
            }

            Roam_Moving();
        }

        private bool Roam_IsPathEmpty()
        {
            return _path == null || _path.Count == 0;
        }

        private LinkedList<Tile> Roam_GenerateRandomPath(TileGraph graph, Tile srcTile)
        {
            // Randomly select a navigable tile as destination
            Tile randomTile = new Tile(-1, -1);
            while (!_tileGraph.Nodes.Contains(randomTile) ||
                   randomTile.Equals(srcTile)
                  )
            {
                randomTile.Col = _game.Random.Next(0, _tiledMap.Width);
                randomTile.Row = _game.Random.Next(0, _tiledMap.Height);
            }

            // Compute an A* path
            return AStar.Compute(_tileGraph, srcTile, randomTile, AStarHeuristic.EuclideanSquared);
        }

        private void Roam_Moving()
        {
            float elapsedSeconds = ScalableGameTime.DeltaTime;

            int tileWidth = _tiledMap.TileWidth;
            int tileHeight = _tiledMap.TileHeight;

            Tile headTile = _path.First.Value;
            Vector2 headTilePosition = Tile.ToPosition(headTile, tileWidth, tileHeight);

            if (_ghost.Position.Equals(headTilePosition))
            {
                Debug.WriteLine($"Reach head tile (Col = {headTile.Col}, Row = {headTile.Row}).");

                // Remove the head tile from path
                Debug.WriteLine($"Removing head tile. Get next node from path.");
                _path.RemoveFirst();

                // Update animation
                if (!Roam_IsPathEmpty())
                {
                    Tile nextTile = _path.First.Value;
                    _ghost.UpdateAnimatedSprite(headTile, nextTile);
                }
            }

            // Move the ghost to the new tile location
            _ghost.Position = _ghost.Move(_ghost.Position, headTilePosition, elapsedSeconds);
            _ghost.AnimatedSprite.Update(ScalableGameTime.GameTime);
        }
    }
}