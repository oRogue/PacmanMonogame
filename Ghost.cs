using GAlgoT2530.AI;
using GAlgoT2530.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Animations;
using MonoGame.Extended.Graphics;
using MonoGame.Extended.Tiled;
using System;

namespace PacmanGame
{
    public class Ghost : AnimationGameObject
    {
        // HCFSM base-class pointer — can point to NavigationHCFSM now,
        // or GhostHCFSM later in Problem 5
        private HCFSM _fsm;

        // Attributes
        public float MaxSpeed;

        public Ghost() : base("ghost-animations.sf")
        {
        }

        public override void Initialize()
        {
            MaxSpeed = 100.0f;

            // Initialize Animation to "ghostRedDown"
            AnimatedSprite.SetAnimation("ghostRedDown");
            AnimatedSprite.TextureRegion = SpriteSheet.TextureAtlas[AnimatedSprite.Controller.CurrentFrame];

            // Grab GameMap to set starting position
            GameMap gameMap = (GameMap)GameObjectCollection.FindByName("GameMap");
            TiledMap tiledMap = gameMap.TiledMap;
            Pacman pacman = (Pacman)GameObjectCollection.FindByName("Pacman");

            // Initialize Position from start tile
            Tile srcTile = new Tile(gameMap.StartColumn, gameMap.StartRow);
            Position = Tile.ToPosition(srcTile, tiledMap.TileWidth, tiledMap.TileHeight);

            // Construct and initialise the Ghost FSM (Problem 5)
            _fsm = new GhostHCFSM(_game, this, tiledMap, gameMap.TileGraph, pacman);
            _fsm.Initialize();
        }

        // Ghost.Update() now only delegates to the FSM
        public override void Update()
        {
            _fsm.Update();
        }

        public override void Draw()
        {
            _game.SpriteBatch.Begin();
            _game.SpriteBatch.Draw(AnimatedSprite, Position, Orientation, Scale);
            _game.SpriteBatch.End();
        }

        // ---------------------------------------------------------------
        // Helper: move from src toward dest at MaxSpeed over elapsedSeconds.
        // Returns dest if the ghost can reach or overshoot it this frame.
        // ---------------------------------------------------------------
        public Vector2 Move(Vector2 src, Vector2 dest, float elapsedSeconds)
        {
            Vector2 dP       = dest - src;
            float   distance = dP.Length();
            float   step     = MaxSpeed * elapsedSeconds;

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

        // ---------------------------------------------------------------
        // Helper: pick the correct animation based on which tile the ghost
        // is on (ghostTile) and which tile it is heading toward (nextTile).
        // ---------------------------------------------------------------
        public void UpdateAnimatedSprite(Tile ghostTile, Tile nextTile)
        {
            string[] directions = { "NorthWest", "Up"    , "NorthEast",
                                    "Left"     , "Centre", "Right"    ,
                                    "SouthWest", "Down"  , "SouthEast" };

            if (ghostTile == null || nextTile == null)
            {
                throw new ArgumentNullException("UpdateAnimatedSprite(): ghostTile or nextTile is null.");
            }

            Tile difference = new Tile(nextTile.Col - ghostTile.Col, nextTile.Row - ghostTile.Row);
            int  index      = (difference.Col + 1) + 3 * (difference.Row + 1);

            string animationName = $"ghostRed{directions[index]}";

            if (AnimatedSprite.CurrentAnimation != animationName)
            {
                AnimatedSprite.SetAnimation(animationName);
                AnimatedSprite.TextureRegion = SpriteSheet.TextureAtlas[AnimatedSprite.Controller.CurrentFrame];
            }
        }
    }
}