/*
Honour code acknowledgement
this took forever, idk why the map size has to be so big
recieved help from youtube, stack overflow and monogame forums
i declare that this is my own work
*/

using GAlgoT2530.AI;
using GAlgoT2530.Engine;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Tiled;
using System.Collections.Generic;
using System.Linq;

namespace PacmanGame
{
    public class GhostStealingFSM : HCFSM
    {
        private GameEngine game;
        private Ghost ghost;
        private TiledMap map;
        private TileGraph graph;
        
        private List<Tile> powerPellets;
        private Tile homeTile;
        private Tile goalTile;
        private int currentPelletIndex;
        
        private LinkedList<Tile> path;
        private Tile nextTile;
        private Vector2 nextPos;
        private bool goingHome;
        private PelletTracker pelletTracker;
        
        public GhostStealingFSM(GameEngine g, Ghost gh, TiledMap m, TileGraph gr)
        {
            game = g;
            ghost = gh;
            map = m;
            graph = gr;
            path = new LinkedList<Tile>();
            powerPellets = new List<Tile>();
            currentPelletIndex = 0;
            goingHome = false;
        }
        
        public override void Initialize()
        {
            ghost.MaxSpeed = 100.0f;
            
            var waypointLayer = map.GetLayer<TiledMapObjectLayer>("Waypoints");
            
            List<Tile> pelletList = new List<Tile>();
            
            foreach (var obj in waypointLayer.Objects)
            {
                int col = (int)(obj.Position.X / map.TileWidth);
                int row = (int)(obj.Position.Y / map.TileHeight);
                Tile t = new Tile(col, row);
                
                if (obj.Name == "Home")
                {
                    homeTile = t;
                }
                else if (obj.Name == "Goal")
                {
                    goalTile = t;
                }
                else
                {
                    pelletList.Add(t);
                }
            }
            
            List<PathInfo> pathInfos = new List<PathInfo>();
            for (int i = 0; i < pelletList.Count; i++)
            {
                var p = AStar.Compute(graph, homeTile, pelletList[i], AStarHeuristic.EuclideanSquared);
                PathInfo info = new PathInfo();
                info.pellet = pelletList[i];
                info.pathLength = p.Count;
                pathInfos.Add(info);
            }
            
            pathInfos.Sort((a, b) => a.pathLength.CompareTo(b.pathLength));
            
            for (int i = 0; i < pathInfos.Count; i++)
            {
                powerPellets.Add(pathInfos[i].pellet);
            }
            
            ghost.Position = Tile.ToPosition(homeTile, map.TileWidth, map.TileHeight);
            nextPos = ghost.Position;
            
            pelletTracker = (PelletTracker)GameObjectCollection.FindByName("PelletTracker");
            
            CalculateNextPath();
        }
        
        private void CalculateNextPath()
        {
            Tile start = Tile.ToTile(ghost.Position, map.TileWidth, map.TileHeight);
            Tile destination;
            
            if (goingHome)
            {
                destination = homeTile;
            }
            else
            {
                if (currentPelletIndex < powerPellets.Count)
                {
                    destination = powerPellets[currentPelletIndex];
                }
                else
                {
                    destination = goalTile;
                }
            }
            
            path.Clear();
            path = AStar.Compute(graph, start, destination, AStarHeuristic.EuclideanSquared);
            
            if (path.Count > 0)
            {
                path.RemoveFirst();
            }
            
            if (path.Count > 0)
            {
                nextTile = path.First.Value;
                nextPos = Tile.ToPosition(nextTile, map.TileWidth, map.TileHeight);
                ghost.UpdateAnimatedSprite(start, nextTile);
            }
        }
        
        public override void Update()
        {
            if (ghost.Position.Equals(nextPos))
            {
                if (path.Count > 0)
                {
                    Tile oldTile = nextTile;
                    path.RemoveFirst();
                    
                    if (path.Count > 0)
                    {
                        nextTile = path.First.Value;
                        nextPos = Tile.ToPosition(nextTile, map.TileWidth, map.TileHeight);
                        ghost.UpdateAnimatedSprite(oldTile, nextTile);
                    }
                    else
                    {
                        if (!goingHome)
                        {
                            if (currentPelletIndex < powerPellets.Count)
                            {
                                Tile pelletTile = powerPellets[currentPelletIndex];
                                pelletTracker.CoverPelletTileWithEmptyTile(pelletTile);
                                
                                if (currentPelletIndex == powerPellets.Count - 1)
                                {
                                    currentPelletIndex++;
                                    goingHome = false;
                                }
                                else
                                {
                                    goingHome = true;
                                }
                                
                                CalculateNextPath();
                            }
                        }
                        else
                        {
                            currentPelletIndex++;
                            goingHome = false;
                            CalculateNextPath();
                        }
                    }
                }
            }
            
            ghost.Position = ghost.Move(ghost.Position, nextPos, ScalableGameTime.DeltaTime);
            ghost.AnimatedSprite.Update(ScalableGameTime.GameTime);
        }
        
        private class PathInfo
        {
            public Tile pellet;
            public int pathLength;
        }
    }
}