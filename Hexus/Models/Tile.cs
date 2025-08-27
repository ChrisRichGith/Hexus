namespace Hexus.Models
{
    public class Tile
    {
        public TileClass Class { get; }
        public Hex Position { get; set; }
        public int Health { get; private set; }
        public int MaxHealth { get; }

        public Tile(TileClass tileClass, Hex position)
        {
            Class = tileClass;
            Position = position;
            MaxHealth = 100;
            Health = MaxHealth;
        }

        public void TakeDamage(int amount)
        {
            Health -= amount;
            if (Health < 0)
            {
                Health = 0;
            }
        }

        public bool IsAlive => Health > 0;
    }
}
