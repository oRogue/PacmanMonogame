using GAlgoT2530.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Tiled;
using System.Collections.Generic;

namespace PacmanGame
{
    public class PelletTracker : GameObject
    {
        public delegate void PelletsClearedHandler();
        public delegate void PowerPelletStartedHandler();
        public delegate void PowerPelletRunningHandler(float remainingSeconds);
        public delegate void PowerPelletEndedHandler();

        public event PelletsClearedHandler PelletsCleared;
        public event PowerPelletStartedHandler PowerPelletStarted;
        public event PowerPelletRunningHandler PowerPelletRunning;
        public event PowerPelletEndedHandler PowerPelletEnded;

        public float PowerPelletMaxTime;
        public Texture2D Texture;

        private HashSet<Tile> _coverTiles;
        private Rectangle _coverTileRect;
        private TiledMapTileLayer _pelletLayer;
        private Pacman _pacman;
        private TiledMap _tiledMap;
        private TileGraph _tileGraph;
        private float _powerPelletActiveTime;

        public PelletTracker(string name) : base(name)
        {
            _coverTiles = new HashSet<Tile>();
        }

        public override void LoadContent()
        {
            Texture = _game.Content.Load<Texture2D>("pacman-wall-24");
        }

        public override void Initialize()
        {
            _coverTileRect = new Rectangle(96, 0, 24, 24);
            _powerPelletActiveTime = 0f;

            GameMap gameMap = (GameMap)GameObjectCollection.FindByName("GameMap");
            _tiledMap = gameMap.TiledMap;
            _tileGraph = gameMap.TileGraph;

            _pelletLayer = _tiledMap.GetLayer<TiledMapTileLayer>("Food");
        }

        public override void Update()
        {
            if (_powerPelletActiveTime > 0f)
            {
                _powerPelletActiveTime -= ScalableGameTime.DeltaTime;

                if (_powerPelletActiveTime <= 0f)
                {
                    _powerPelletActiveTime = 0f;
                    PowerPelletEnded?.Invoke();
                }
                else
                {
                    PowerPelletRunning?.Invoke(_powerPelletActiveTime);
                }
            }
        }

        public override void Draw()
        {
            _game.SpriteBatch.Begin();

            foreach (Tile t in _coverTiles)
            {
                Vector2 position = Tile.ToPosition(t, _pelletLayer.TileWidth, _pelletLayer.TileHeight);
                _game.SpriteBatch.Draw(Texture, position, _coverTileRect, Color.White, Orientation, Origin, Scale, SpriteEffects.None, 1f);
            }

            _game.SpriteBatch.End();
        }

        public void RestartPowerPelletTime()
        {
            _powerPelletActiveTime = PowerPelletMaxTime;
            PowerPelletStarted?.Invoke();
        }

        public void CoverPelletTileWithEmptyTile(Tile pelletTileLocation)
        {
            bool hasTile = _pelletLayer.TryGetTile((ushort)pelletTileLocation.Col, (ushort)pelletTileLocation.Row, out TiledMapTile? pacmanTile);

            if (hasTile)
            {
                if (pacmanTile.Value.GlobalIdentifier == 3 || pacmanTile.Value.GlobalIdentifier == 4)
                {
                    if (!_coverTiles.Contains(pelletTileLocation))
                    {
                        _coverTiles.Add(pelletTileLocation);

                        if (_coverTiles.Count == _tileGraph.Nodes.Count)
                        {
                            PelletsCleared?.Invoke();
                        }

                        if (pacmanTile.Value.GlobalIdentifier == 4)
                        {
                            RestartPowerPelletTime();
                        }
                    }
                }
            }
        }
    }
}