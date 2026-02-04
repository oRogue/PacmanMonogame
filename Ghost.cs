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

        private HCFSM _fsm;
        public float MaxSpeed;

        public Ghost() : base("ghost-animations.sf")
        {
        }

        public override void Initialize()
        {
            MaxSpeed = 100.0f;

            AnimatedSprite.SetAnimation("ghostRedDown");
            AnimatedSprite.TextureRegion = SpriteSheet.TextureAtlas[AnimatedSprite.Controller.CurrentFrame];

            GameMap gameMap = (GameMap)GameObjectCollection.FindByName("GameMap");
            TiledMap tiledMap = gameMap.TiledMap;

            _fsm = new GhostStealingFSM(_game, this, tiledMap, gameMap.TileGraph);
            _fsm.Initialize();
        }

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