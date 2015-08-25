using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zombie_Survival_Sim.Weapon;

namespace Zombie_Survival_Sim.Characters
{
    class Human:Character
    {
        public Human()
        {
            Health = 100;
        }

        public Weapon.Weapon SelectedWeapon { get; set; }
    }
}
