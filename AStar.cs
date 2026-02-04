using MonoGame.Extended.Tiled;
using System;
using System.Collections.Generic;
using System.Text;

namespace PacmanGame
{
    public class AStar
    {
        private enum State {None = 0, Opened, Closed};
        private class AStarData : IComparable<AStarData>
        {
            public Tile self;
            public Tile parent;
            public ulong g; 
            public ulong h; 
            public State state;

            public AStarData(Tile self, ulong g, ulong h)
            {
                this.self = self;
                this.parent = null;
                this.g = g;
                this.h = h;
                this.state = State.None;
            }

            public int CompareTo(AStarData rhs)
            {
                ulong f1 = this.g + this.h;
                ulong f2 = rhs.g + rhs.h;
                return (int)(f1 - f2);
            }

            public override string ToString()
            {
                return self.ToString();
            }
        }
        public delegate ulong Heuristic(Tile start, Tile end);

        public static LinkedList<Tile> Compute(TileGraph graph, Tile start, Tile end, Heuristic heuristic)
        {
            // Create node records to store data used by AStar
            Dictionary<Tile, AStarData> astarNodeDictionary = new Dictionary<Tile, AStarData>();

        /********************************************************************************
            Problem 1(a) : For each node, initialize its 'g' and 'h' value.
                        (For the start node, 'g' is 0; for other nodes. 
                         For other nodes, 'g' is the largest possible value.
                         In C#, T.MaxValue.is the largest value for a numeric type T.)

            HOWTOSOLVE : 1. Copy the code below.
                         2. Paste it below this block comment.
                         3. Fill in the blanks.

            foreach (Tile node in ________)
                astarNodeDictionary.Add(node, new AStarData(________, ulong.MaxValue, heuristic(________, end)));
            astarNodeDictionary[start] = new AStarData(start, ________, heuristic(________, end));

        ********************************************************************************/
            foreach (Tile node in graph.Nodes)
                astarNodeDictionary.Add(node, new AStarData(node, ulong.MaxValue, heuristic(node, end)));
            astarNodeDictionary[start] = new AStarData(start, 0, heuristic(start, end));
            
			
			

            // Priority Queue (PQ) for deciding which node to process
            PriorityQueue<AStarData> pq = new PriorityQueue<AStarData>();

            // Push the node record for start node to the priority queue
            AStarData startNode = astarNodeDictionary[start];
            pq.Push(startNode);
            startNode.state = State.Opened;

            // Current node and neighbour node to be used in A*
            AStarData curData, neighbourData;

            // For constructing neighbour node
            int[] MoveRow = { -1, -1, -1, 0, 0, 1, 1, 1 };
            int[] MoveCol = { -1, 0, 1, -1, 1, -1, 0, 1 };

            while (!pq.Empty())
            {
                // Get current node data
                curData = pq.Top();
                pq.Pop();
                curData.state = State.Closed;

            /********************************************************************************
                Problem 1(b) : Stop A* algorithm if the current node is the end.
                    
                HOWTOSOLVE : 1. Copy the code below.
                             2. Paste it below this block comment.
                             3. Fill in the blanks.

                if (curData.self == ________)
                    break;
            ********************************************************************************/
                
                if (curData.self == end)
                    break;
				


                // Get the weights to all neighbours of current node
                ulong[] weights = graph.Connections[curData.self];

                for (int i = 0; i < weights.Length; ++i)
                {
                    if (weights[i] != 0)
                    {
                        int colNeighbour = curData.self.Col + MoveCol[i];
                        int rowNeighbour = curData.self.Row + MoveRow[i];

                        Tile neighbourTile = new Tile(colNeighbour, rowNeighbour);
                        neighbourData = astarNodeDictionary[neighbourTile];
                    }
                    else
                    {
                        continue;
                    }

                    // Ignore neighbours in Closed state
                    if (neighbourData.state == State.Closed)
                    {
                        continue;
                    }

                /********************************************************************************
                    Problem 1(c) : Update 'g' and the parent node if a shorter 'g' value is found.

                    HOWTOSOLVE : 1. Copy the code below.
                                 2. Paste it below this block comment.
                                 3. fill in the blanks.

                    ulong gNew = ________ + weights[i];
                    if (neighbourData.g > ________)
                    {
                        neighbourData.g = gNew;
                        neighbourData.parent = ________;
                    }
                ********************************************************************************/

                    ulong gNew = curData.g + weights[i];
                    if (neighbourData.g > gNew)
                    {
                        neighbourData.g = gNew;
                        neighbourData.parent = curData.self;
                    }
                    if (neighbourData.state == State.Opened)
                    {
                        pq.Update(neighbourData);
                    }
                    else 
                    {
                        pq.Push(neighbourData);
                        neighbourData.state = State.Opened;
                    }
                }
            }

            return ConstructPath(astarNodeDictionary, start, end);
        }

        private static LinkedList<Tile> ConstructPath(Dictionary<Tile, AStarData> astarNodeDictionary, Tile start, Tile end)
        {
            LinkedList<Tile> path = new LinkedList<Tile>();

        /********************************************************************************
            Problem 2 : Construct the optimum path by linking the tiles
                        from end to start.
            HOWTOSOLVE : Uncomment the code block below and fill in the blanks.

            AStarData cur = ________;
            while (cur.parent != ________)
            {
                path.________(cur.self);
                cur = ________;
            }
            path.AddFirst(________);
        ********************************************************************************/

            
			
			AStarData cur = astarNodeDictionary[end];
            while (cur.parent != null)
            {
                path.AddFirst(cur.self);
                cur = astarNodeDictionary[cur.parent];
            }
            path.AddFirst(start);
			
			
            
            return path;
        }
    }
}
