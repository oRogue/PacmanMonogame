using GAlgoT2530.AI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Tiled;
using System.Collections.Generic;
using System.Diagnostics;

namespace PacmanGame
{
    public class NavigationHCFSM : HCFSM
    {
        // FSM states
        enum NavigationState { STOP, MOVING };

        // Navigation current state
        private NavigationState _currentState = NavigationState.STOP;

        // Navigation
        private Tile _srcTile;
        private Tile _destTile;
        private LinkedList<Tile> _path;

        // Reference to the Ghost this FSM operates on
        private Ghost _ghost;

        // Map references
        private TiledMap _tiledMap;
        private TileGraph _tileGraph;

        // Constructor — accepts a pointer to the Ghost object
        public NavigationHCFSM(Ghost ghost)
        {
            _ghost = ghost;
        }

        public override void Initialize()
        {
            // Grab map references via GameMap (same as Ghost did before)
            GameMap gameMap = (GameMap)GAlgoT2530.Engine.GameObjectCollection.FindByName("GameMap");
            _tiledMap = gameMap.TiledMap;
            _tileGraph = gameMap.TileGraph;

            // Initialize source tile to Ghost's start position
            _srcTile = new Tile(gameMap.StartColumn, gameMap.StartRow);
        }

        public override void Update()
        {
            MouseState mouse = Mouse.GetState();

            int tileWidth  = _tiledMap.TileWidth;
            int tileHeight = _tiledMap.TileHeight;

            if (_currentState == NavigationState.STOP)
            {
                // Left mouse button pressed
                if (mouse.LeftButton == ButtonState.Pressed)
                {
                    // Get destination tile as the mouse-selected tile
                    _destTile = Tile.ToTile(mouse.Position.ToVector2(), tileWidth, tileHeight);

                    if (_tileGraph.Nodes.Contains(_destTile) &&
                        !_destTile.Equals(_srcTile)
                       )
                    {
                        // Transition Actions
                        // 1. Compute an A* path
                        _path = AStar.Compute(_tileGraph, _srcTile, _destTile, AStarHeuristic.EuclideanSquared);
                        // 2. Remove the source tile from the path
                        _path.RemoveFirst();

                        // Switch animation based on the source tile and the next tile
                        _ghost.UpdateAnimatedSprite(_srcTile, _path.First.Value);

                        // Change to MOVING state
                        _currentState = NavigationState.MOVING;
                    }
                }
            }
            else if (_currentState == NavigationState.MOVING)
            {
                float elapsedSeconds = GAlgoT2530.Engine.ScalableGameTime.DeltaTime;

                if (_path.Count == 0 ||
                    _ghost.Position.Equals(Tile.ToPosition(_destTile, tileWidth, tileHeight))
                   )
                {
                    // Update source tile to destination tile
                    _srcTile = _destTile;
                    _destTile = null;

                    // Change to STOP state
                    _currentState = NavigationState.STOP;
                }

                // Action to execute on the MOVING state
                else
                {
                    Tile nextTile = _path.First.Value;

                    Vector2 nextTilePosition = Tile.ToPosition(nextTile, tileWidth, tileHeight);

                    if (_ghost.Position.Equals(nextTilePosition))
                    {
                        Debug.WriteLine($"Reached the next tile (Col = {nextTile.Col}, Row = {nextTile.Row}).");
                        Debug.WriteLine($"Removing this tile from the path and getting the new next tile from path.");

                        // Get the position of the new next tile from the path
                        _path.RemoveFirst();
                        Tile newNextTile    = _path.First.Value;
                        nextTilePosition    = Tile.ToPosition(newNextTile, tileWidth, tileHeight);

                        // Update the animation
                        _ghost.UpdateAnimatedSprite(nextTile, newNextTile);
                    }

                    // Move the ghost to the new tile location
                    _ghost.Position = _ghost.Move(_ghost.Position, nextTilePosition, elapsedSeconds);

                    // Run the animation
                    _ghost.AnimatedSprite.Update(GAlgoT2530.Engine.ScalableGameTime.GameTime);
                }
            }
        }
    }
}