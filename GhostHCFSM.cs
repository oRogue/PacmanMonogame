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

        private GameObject _pacman;
        private Tile _nextTile;
        private Vector2 _nextTilePosition;

        public float RoamMaxSeconds;
        public float ChaseMaxSeconds;
        private float _roamRemainingSeconds;
        private float _chaseRemainingSeconds;

        private bool _isPowerPelletRunning;

        public GhostHCFSM(GameEngine game, Ghost ghost, TiledMap map, TileGraph graph, Pacman pacman)
        {
            _path = new LinkedList<Tile>();

            _game = game;
            _ghost = ghost;
            _tiledMap = map;
            _tileGraph = graph;
            _pacman = pacman;
        }

        public override void Initialize()
        {
            CurrentState = State.Roam;

            RoamMaxSeconds = 3f;
            ChaseMaxSeconds = 5f;
            _roamRemainingSeconds = 0f;
            _chaseRemainingSeconds = 0f;
            Roam_Initialize();

            PelletTracker pelletTracker = (PelletTracker)GameObjectCollection.FindByName("PelletTracker");

            pelletTracker.PowerPelletStarted += this.Evade_EnablePowerPelletRunning;
            pelletTracker.PowerPelletRunning += this.Evade_HandlePowerPelletExpiring;
            pelletTracker.PowerPelletEnded += this.Evade_TogglePowerPelletRunning;

            _isPowerPelletRunning = false;
        }

        public override void Update()
        {
            if (CurrentState == State.Roam)
            {
                if (TransitionTriggered_RoamToChase())
                {
                    Roam_Exit();
                    Chase_Initialize();
                    CurrentState = State.Chase;
                }
                else if (TransitionTriggered_RoamToEvade())
                {
                    Debug.WriteLine($"Transiting from Roam to Evade: _isPowerPelletRunning = {_isPowerPelletRunning}");
                    Roam_Exit();
                    Evade_Initialize();
                    CurrentState = State.Evade;
                }
                else
                {
                    Roam_Action();
                }
            }
            else if (CurrentState == State.Chase)
            {
                if (TransitionTriggered_ChaseToRoam())
                {
                    Chase_Exit();
                    Roam_Initialize();
                    CurrentState = State.Roam;
                }
                else if (TransitionTriggered_ChaseToEvade())
                {
                    Chase_Exit();
                    Evade_Initialize();
                    CurrentState = State.Evade;
                }
                else
                {
                    Chase_Action();
                }
            }
            else if (CurrentState == State.Evade)
            {
                if (TransitionTriggered_EvadeToRoam())
                {
                    Evade_Exit();
                    Roam_Initialize();
                    CurrentState = State.Roam;
                }
                else if (TransitionTriggered_EvadeToChase())
                {
                    Evade_Exit();
                    Chase_Initialize();
                    CurrentState = State.Chase;
                }
                else
                {
                    Evade_Action();
                }
            }
        }

        private void Roam_Initialize()
        {
            _ghost.MaxSpeed = 80.0f;

            _srcTile = Tile.ToTile(_ghost.Position, _tiledMap.TileWidth, _tiledMap.TileHeight);
        }

        private void Roam_Action()
        {
            _roamRemainingSeconds -= ScalableGameTime.DeltaTime;

            if (Roam_IsPathEmpty())
            {
                _srcTile = Tile.ToTile(_ghost.Position, _tiledMap.TileWidth, _tiledMap.TileHeight);

                _path = Roam_GenerateRandomPath(_tileGraph, _srcTile);
                _path.RemoveFirst();

                _destTile = _path.Last.Value;
                _destTilePosition = Tile.ToPosition(_destTile, _tiledMap.TileWidth, _tiledMap.TileHeight);

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
            Tile randomTile = new Tile(-1, -1);
            while (!_tileGraph.Nodes.Contains(randomTile) ||
                   randomTile.Equals(srcTile)
                  )
            {
                randomTile.Col = _game.Random.Next(0, _tiledMap.Width);
                randomTile.Row = _game.Random.Next(0, _tiledMap.Height);
            }

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

                Debug.WriteLine($"Removing head tile. Get next node from path.");
                _path.RemoveFirst();

                if (!Roam_IsPathEmpty())
                {
                    Tile nextTile = _path.First.Value;
                    _ghost.UpdateAnimatedSprite(headTile, nextTile);
                }
            }

            _ghost.Position = _ghost.Move(_ghost.Position, headTilePosition, elapsedSeconds);
            _ghost.AnimatedSprite.Update(ScalableGameTime.GameTime);
        }

        private void Roam_Exit()
        {
            _path.Clear();
        }

        private void Chase_Initialize()
        {
            _ghost.MaxSpeed = 120.0f;

            _nextTilePosition = _ghost.Position;
            _nextTile = Tile.ToTile(_nextTilePosition, _tiledMap.TileWidth, _tiledMap.TileHeight);
        }

        private void Chase_Action()
        {
            float elapsedSeconds = ScalableGameTime.DeltaTime;

            _chaseRemainingSeconds -= ScalableGameTime.DeltaTime;

            if (_ghost.Position.Equals(_nextTilePosition))
            {
                if (Chase_ShouldRecalculatePathTowardsPacman())
                {
                    Chase_RecalculatePathTowardsPacman();
                }
                else
                {
                    _srcTile = _nextTile;
                    _path.RemoveFirst();
                    if (!_srcTile.Equals(_destTile))
                    {
                        _nextTile = _path.First.Value;
                        _nextTilePosition = Tile.ToPosition(_nextTile, _tiledMap.TileWidth, _tiledMap.TileHeight);
                        _ghost.UpdateAnimatedSprite(_srcTile, _nextTile);
                    }
                }
            }

            _ghost.Position = _ghost.Move(_ghost.Position, _nextTilePosition, elapsedSeconds);
            _ghost.AnimatedSprite.Update(ScalableGameTime.GameTime);
        }

        private bool Chase_ShouldRecalculatePathTowardsPacman()
        {
            if (_path != null)
            {
                _destTile = Tile.ToTile(_pacman.Position, _tiledMap.TileWidth, _tiledMap.TileHeight);

                LinkedListNode<Tile> destTileNode = _path.Find(_destTile);
                while (_path.Last != destTileNode)
                {
                    _path.RemoveLast();
                }

                return destTileNode == null;
            }
            else
            {
                return true;
            }
        }

        private void Chase_RecalculatePathTowardsPacman()
        {
            _srcTile = Tile.ToTile(_ghost.Position, _tiledMap.TileWidth, _tiledMap.TileHeight);
            _destTile = Tile.ToTile(_pacman.Position, _tiledMap.TileWidth, _tiledMap.TileHeight);

            _path.Clear();
            _path = AStar.Compute(_tileGraph, _srcTile, _destTile, AStarHeuristic.EuclideanSquared);

            _path.RemoveFirst();

            if (!_srcTile.Equals(_destTile))
            {
                _nextTile = _path.First.Value;
                _nextTilePosition = Tile.ToPosition(_nextTile, _tiledMap.TileWidth, _tiledMap.TileHeight);

                _ghost.UpdateAnimatedSprite(_srcTile, _nextTile);
            }
        }

        private void Chase_Exit()
        {
            _path.Clear();
        }


        private void Evade_Initialize()
        {
            _ghost.MaxSpeed = 60.0f;
            _ghost.AnimatedSprite.SetAnimation("ghostEvade");
            _ghost.AnimatedSprite.TextureRegion = _ghost.SpriteSheet.TextureAtlas[_ghost.AnimatedSprite.Controller.CurrentFrame];

            _srcTile = Tile.ToTile(_ghost.Position, _tiledMap.TileWidth, _tiledMap.TileHeight);

            _path = Roam_GenerateRandomPath(_tileGraph, _srcTile);
            _path.RemoveFirst();
        }

        private void Evade_Action()
        {
            if (Roam_IsPathEmpty())
            {
                _srcTile = Tile.ToTile(_ghost.Position, _tiledMap.TileWidth, _tiledMap.TileHeight);

                _path = Roam_GenerateRandomPath(_tileGraph, _srcTile);
                _path.RemoveFirst();

                _destTile = _path.Last.Value;
                _destTilePosition = Tile.ToPosition(_destTile, _tiledMap.TileWidth, _tiledMap.TileHeight);
            }

            Evade_Moving();
        }

        private void Evade_Moving()
        {
            int tileWidth = _tiledMap.TileWidth;
            int tileHeight = _tiledMap.TileHeight;

            Tile headTile = _path.First.Value;
            Vector2 headTilePosition = Tile.ToPosition(headTile, tileWidth, tileHeight);

            if (_ghost.Position.Equals(headTilePosition))
            {
                Debug.WriteLine($"Reach head tile (Col = {headTile.Col}, Row = {headTile.Row}).");

                Debug.WriteLine($"Removing head tile. Get next node from path.");
                _path.RemoveFirst();
            }

            _ghost.Position = _ghost.Move(_ghost.Position, headTilePosition, ScalableGameTime.DeltaTime);
            _ghost.AnimatedSprite.Update(ScalableGameTime.GameTime);
        }

        private void Evade_HandlePowerPelletExpiring(float remainingSeconds)
        {
            if (remainingSeconds <= 5f)
            {
                if (_ghost.AnimatedSprite.CurrentAnimation != "ghostRecovering")
                    _ghost.AnimatedSprite.SetAnimation("ghostRecovering");
            }
            else
            {
                if (_ghost.AnimatedSprite.CurrentAnimation != "ghostEvade")
                    _ghost.AnimatedSprite.SetAnimation("ghostEvade");
            }

            _ghost.AnimatedSprite.TextureRegion = _ghost.SpriteSheet.TextureAtlas[_ghost.AnimatedSprite.Controller.CurrentFrame];
        }

        private void Evade_EnablePowerPelletRunning()
        {
            _isPowerPelletRunning = true;
        }

        private void Evade_TogglePowerPelletRunning()
        {
            _isPowerPelletRunning = !_isPowerPelletRunning;
        }

        private void Evade_Exit()
        {
            _path.Clear();
        }


        private bool TransitionTriggered_RoamToChase()
        {
            if (_roamRemainingSeconds <= 0f)
            {
                _roamRemainingSeconds = RoamMaxSeconds;
                return true;
            }

            return false;
        }

        private bool TransitionTriggered_ChaseToRoam()
        {
            if (_chaseRemainingSeconds <= 0f)
            {
                _chaseRemainingSeconds = ChaseMaxSeconds;
                return true;
            }

            return false;
        }

        private bool TransitionTriggered_RoamToEvade()
        {
            return _isPowerPelletRunning;
        }

        private bool TransitionTriggered_ChaseToEvade()
        {
            return _isPowerPelletRunning;
        }

        private bool TransitionTriggered_EvadeToRoam()
        {
            return (!_isPowerPelletRunning && _roamRemainingSeconds > 0f);
        }

        private bool TransitionTriggered_EvadeToChase()
        {
            return (!_isPowerPelletRunning && _chaseRemainingSeconds > 0f);
        }
    }
}