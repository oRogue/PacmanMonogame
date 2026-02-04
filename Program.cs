// using var game = new PacmanGame.Game1();
using var game = new GAlgoT2530.Engine.GameEngine("Pacman Game", 1920, 1080);
game.AddScene("PacmanScene", new PacmanGame.PacmanScene());
game.Run();
