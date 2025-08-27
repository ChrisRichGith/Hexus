using Microsoft.Xna.Framework;

namespace Hexus.Models
{
    public enum Player { Human, AI }

    public class TileClass
    {
        public string Name { get; }
        public Color Color { get; }
        public Player Owner { get; }
        public int BaseDamage { get; }

        public TileClass(string name, Color color, Player owner, int baseDamage)
        {
            Name = name;
            Color = color;
            Owner = owner;
            BaseDamage = baseDamage;
        }
    }

    public static class TileClasses
    {
        public static readonly TileClass PlayerTank = new TileClass("Tank", Color.White, Player.Human, 25);
        public static readonly TileClass PlayerMage = new TileClass("Mage", Color.CornflowerBlue, Player.Human, 40);
        public static readonly TileClass PlayerHealer = new TileClass("Healer", Color.LawnGreen, Player.Human, 15);
        public static readonly TileClass PlayerDD = new TileClass("DD", Color.Red, Player.Human, 34);

        public static readonly TileClass AIDD = new TileClass("DD", new Color(255, 102, 102), Player.AI, 34);
    }
}
