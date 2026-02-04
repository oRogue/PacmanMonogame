using GAlgoT2530.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Animations;
using MonoGame.Extended.Content;
using MonoGame.Extended.Graphics;
using MonoGame.Extended.Tiled;
using System;


namespace PacmanGame
{
    public class Pacman : AnimationGameObject
    {
        public delegate void TileReachedHandler(Tile location);
        public event TileReachedHandler TileReached;

        public float Speed;
        public int StartColumn;
        public int StartRow;
        public string NavigableTileLayerName;

        private enum Direction { UpLeft, Up, UpRight, Left, None, Right, DownLeft, Down, DownRight };

        private readonly int[] NextRow = { -1, -1, -1, 0, 0, 0, 1, 1, 1 };
        private readonly int[] NextCol = { -1, 0, 1, -1, 0, 1, -1, 0, 1 };

        private Direction _currDirection;
        private Direction _prevDirection;

        private Tile _currTile;
        private Vector2 _nextTilePosition;

        private TiledMap _tiledMap;
        private TiledMapTileLayer _tiledMapNavigableLayer;

        public Pacman() : base("Pacman", "pacman-animations.sf")
        {
        }

        public override void Initialize()
        {
            _currDirection = Direction.None;
            _prevDirection = Direction.None;

            AnimatedSprite.SetAnimation("pacmanCentre");
            AnimatedSprite.TextureRegion = SpriteSheet.TextureAtlas[AnimatedSprite.Controller.CurrentFrame];

            GameMap gameMap = (GameMap)GameObjectCollection.FindByName("GameMap");
            _tiledMap = gameMap.TiledMap;
            _tiledMapNavigableLayer = _tiledMap.GetLayer<TiledMapTileLayer>(NavigableTileLayerName);

            _currTile = new Tile(StartColumn, StartRow);
            Position = Tile.ToPosition(_currTile, _tiledMap.TileWidth, _tiledMap.TileHeight);
            _nextTilePosition = Position;
        }

        public override void Update()
        {
            Direction newDirection = GetDirectionFromInput();
            UpdateDirection(newDirection);

            if (Position.Equals(_nextTilePosition))
            {
                _currTile = Tile.ToTile(Position, _tiledMap.TileWidth, _tiledMap.TileHeight);

                TileReached?.Invoke(_currTile);

                Tile nextTile = _currTile;

                Direction[] directions = { _currDirection, _prevDirection };

                foreach (Direction direction in directions)
                {
                    nextTile = GetNextTileFromDirection(direction);
                    ushort col = (ushort)nextTile.Col;
                    ushort row = (ushort)nextTile.Row;

                    if (_tiledMapNavigableLayer.TryGetTile(col, row, out TiledMapTile? nextTiledMapTile))
                    {
                        if (!nextTiledMapTile.Value.IsBlank)
                        {
                            if (direction == _currDirection)
                            {
                                _prevDirection = Direction.None;
                            }
                            break;
                        }
                        else
                        {
                            nextTile = _currTile;
                        }
                    }
                }

                UpdateAnimatedSprite(_currTile, nextTile);

                if (!nextTile.Equals(_currTile))
                {
                    _nextTilePosition = Tile.ToPosition(nextTile, _tiledMap.TileWidth, _tiledMap.TileHeight);
                }
                else
                {
                    _currDirection = Direction.None;
                    _prevDirection = Direction.None;
                }
            }

            Position = Move(Position, _nextTilePosition, ScalableGameTime.DeltaTime, Speed);
            AnimatedSprite.Update(ScalableGameTime.GameTime);
        }

        public override void Draw()
        {
            _game.SpriteBatch.Begin();
            _game.SpriteBatch.Draw(AnimatedSprite, Position, Orientation, Scale);
            _game.SpriteBatch.End();
        }

        private Direction GetDirectionFromInput()
        {
            KeyboardState keyboardState = Keyboard.GetState();
            if (keyboardState.IsKeyDown(Keys.W))
            {
                return Direction.Up;
            }
            else if (keyboardState.IsKeyDown(Keys.S))
            {
                return Direction.Down;
            }
            else if (keyboardState.IsKeyDown(Keys.A))
            {
                return Direction.Left;
            }
            else if (keyboardState.IsKeyDown(Keys.D))
            {
                return Direction.Right;
            }
            else
            {
                return Direction.None;
            }
        }

        private void UpdateDirection(Direction newDirection)
        {
            if (newDirection != Direction.None)
            {
                if (_currDirection == Direction.None)
                {
                    _currDirection = newDirection;
                }
                else if (_prevDirection == Direction.None && newDirection != _currDirection)
                {
                    _prevDirection = _currDirection;
                    _currDirection = newDirection;
                }
            }
        }

        private Tile GetNextTileFromDirection(Direction direction)
        {
            int directionIndex = (int)direction;
            Tile nextTile = new Tile(_currTile.Col + NextCol[directionIndex],
                                     _currTile.Row + NextRow[directionIndex]);
            return nextTile;
        }

        public Vector2 Move(Vector2 src, Vector2 dest, float elapsedSeconds, float speed)
        {
            Vector2 dP = dest - src;
            float distance = dP.Length();
            float step = speed * elapsedSeconds;

            if (step < distance)
            {
                dP.Normalize();
                return src + (dP * step);
            }
            else
            {
                return dest;
            }
        }

        public void UpdateAnimatedSprite(Tile currTile, Tile nextTile)
        {
            string[] directions = {"UpLeft", "Up", "UpRight",
                                   "Left", "Centre", "Right",
                                   "Downleft", "Down", "DownRight"};

            if (currTile == null || nextTile == null)
            {
                throw new ArgumentNullException("UpdateAnimatedSprite(): NULL in current tile or next tile.");
            }
            else
            {
                Tile difference = new Tile(nextTile.Col - currTile.Col, nextTile.Row - currTile.Row);
                int index = (difference.Col + 1) + 3 * (difference.Row + 1);

                string animationName = $"pacman{directions[index]}";

                if (AnimatedSprite.CurrentAnimation != animationName)
                {
                    AnimatedSprite.SetAnimation(animationName);
                    AnimatedSprite.TextureRegion = SpriteSheet.TextureAtlas[AnimatedSprite.Controller.CurrentFrame];
                }
            }
        }
    }
}