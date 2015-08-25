using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Zombie_Survival_Sim.Characters
{
    class Character
    {
        public int Health { get; set; }

        public Vector2 Location { get; set; }

        public Vector2 Velocity { get; set; }

        public Vector2 Direction { get; set; }
    }
}
