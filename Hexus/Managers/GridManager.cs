using Hexus.Models;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Hexus.Managers
{
    public class GridManager
    {
        public int GridWidth { get; }
        public int GridHeight { get; }
        public float HexSize { get; }

        public Dictionary<Hex, Tile> Tiles { get; } = new();
        public List<Hex> AllHexes { get; } = new();

        public GridManager(int gridWidth, int gridHeight, float hexSize)
        {
            GridWidth = gridWidth;
            GridHeight = gridHeight;
            HexSize = hexSize;
        }

        public HashSet<Hex> ControlPoints { get; } = new();

        public void CreateGrid()
        {
            AllHexes.Clear();
            Tiles.Clear();
            ControlPoints.Clear();

            // Create a rectangular grid using offset coordinates, which are then converted to axial (Hex)
            for (int row = 0; row < GridHeight; row++)
            {
                var offset = row / 2; // This creates the offset for every second row
                for (int col = 0; col < GridWidth; col++)
                {
                    AllHexes.Add(new Hex(col - offset, row));
                }
            }

            // Define Control Points
            ControlPoints.Add(new Hex(5, 2));
            ControlPoints.Add(new Hex(5, 8));
        }

        public void AddInitialTiles()
        {
            void AddTile(TileClass tileClass, Hex hex)
            {
                if (AllHexes.Contains(hex)) Tiles[hex] = new Tile(tileClass, hex);
            }

            AddTile(TileClasses.PlayerTank, new Hex(1, 1));
            AddTile(TileClasses.PlayerMage, new Hex(2, 2));
            AddTile(TileClasses.PlayerDD, new Hex(2, 1));

            AddTile(TileClasses.AIDD, new Hex(7, 7));
            AddTile(TileClasses.AIDD, new Hex(6, 6));
        }
    }
}
