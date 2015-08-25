using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Zombie_Survival_Sim.Weapon
{
    class Bullet
    {
        public int Range { get; set; }
        public int Speed { get; set; }
        public int Size { get; set; }
        public Vector2 Location { get; set; }

        public Vector2 Velocity { get; set; }
    }
}
