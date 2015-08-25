using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Zombie_Survival_Sim.Weapon
{
    class Weapon
    {
        public int TimeUntilNextShot { get; set; }
        public int WeaponFireSpeed { get; set; }
        public int BulletsPerShot { get; set; }
        public int ShotType { get; set; }
        public int Range { get; set; }
        public int Speed { get; set; }
        public int Size { get; set; }
        public List<Bullet> Fire(Vector2 Location, Vector2 Direction)
        {
            if (ShotType == 1)
            {
                return FireSingle(Location, Direction);
            }
            else if (ShotType == 2)
            {
                return FireSpread(Location, Direction);
            }
            else
            {
                return new List<Bullet>();
            }
        }
        public List<Bullet> FireSingle(Vector2 Location, Vector2 Direction)
        {
            List<Bullet> Bullets = new List<Bullet>();
            for (int i = 0; i < BulletsPerShot; i++)
            {
                Bullet oBullet2 = new Bullet();
                oBullet2.Location = Location - new Vector2(Size/2,Size/2);
                oBullet2.Velocity = Direction * Speed;
                oBullet2.Range = Range;
                oBullet2.Speed = Speed;
                oBullet2.Size = Size;
                Bullets.Add(oBullet2);
            }
            return Bullets;
        }
        public List<Bullet> FireSpread(Vector2 Location, Vector2 Direction)
        {
            List<Bullet> Bullets = new List<Bullet>();
            for(int i = 0; i < BulletsPerShot;i++)
            {
                Vector2 ninetydegreeleft = new Vector2(Direction.Y, -Direction.X);
                Vector2 ninetydegreeright = new Vector2(-Direction.Y, Direction.X);

                Bullet oBullet = new Bullet();
                oBullet.Location = Location;
                oBullet.Velocity = Vector2.Normalize(Direction * 10 + ninetydegreeleft * 2) * Speed;
                oBullet.Range = Range;
                oBullet.Speed = Speed;
                oBullet.Size = Size;
                Bullets.Add(oBullet);

                Bullet oBullet1 = new Bullet();
                oBullet1.Location = Location;
                oBullet1.Velocity = Vector2.Normalize(Direction * 10 + ninetydegreeleft) * Speed;
                oBullet1.Range = Range;
                oBullet1.Speed = Speed;
                oBullet1.Size = Size;
                Bullets.Add(oBullet1);

                Bullet oBullet2 = new Bullet();
                oBullet2.Location = Location;
                oBullet2.Velocity = Direction * Speed;
                oBullet2.Range = Range;
                oBullet2.Speed = Speed;
                oBullet2.Size = Size;
                Bullets.Add(oBullet2);

                Bullet oBullet3 = new Bullet();
                oBullet3.Location = Location;
                oBullet3.Velocity = Vector2.Normalize(Direction * 10 + ninetydegreeright) * Speed;
                oBullet3.Range = Range;
                oBullet3.Speed = Speed;
                oBullet3.Size = Size;
                Bullets.Add(oBullet3);

                Bullet oBullet4 = new Bullet();
                oBullet4.Location = Location;
                oBullet4.Velocity = Vector2.Normalize(Direction * 10 + ninetydegreeright * 2) * Speed;
                oBullet4.Range = Range;
                oBullet4.Speed = Speed;
                oBullet4.Size = Size;
                Bullets.Add(oBullet4);
            }
            return Bullets;
        }
    }
}
