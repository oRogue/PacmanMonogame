using GAlgoT2530.Engine;

namespace PacmanGame
{
    public class PacmanScene : GameScene
    {
        public override void CreateScene()
        {
            GameMap gameMap = new GameMap("GameMap");
            gameMap.StartColumn = 18;
            gameMap.StartRow = 11;

            PelletTracker pelletTracker = new PelletTracker("PelletTracker");
            pelletTracker.PowerPelletMaxTime = 10f;

            Ghost ghost = new Ghost();
        }
    }
}