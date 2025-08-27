using Hexus.Models;

namespace Hexus.Managers
{
    public class GameState
    {
        public Tile SelectedTile { get; set; }
        public Player CurrentTurn { get; set; } = Player.Human;
        public bool IsProcessingTurn { get; set; } = false;
        public bool GameOver { get; set; } = false;
        public Player Winner { get; set; }
    }
}
