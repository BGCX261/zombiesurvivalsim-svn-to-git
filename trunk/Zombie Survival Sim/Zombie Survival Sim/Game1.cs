using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using Zombie_Survival_Sim.Characters;
using Zombie_Survival_Sim.Helpers;
using Zombie_Survival_Sim.Weapon;
using Zombie_Survival_Sim.Buildings;
using Lidgren.Library.Network;


namespace Zombie_Survival_Sim
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {

        #region Preferences
        float GameSpeed = 30.0f;
        bool MultiplayerGame = false;

        int PlayerSpeed = 4;
        int PlayerSize = 5;
        Color PlayerColor = Color.White;
        bool PlayerFieldOfView = true;
        int PlayerFieldOfViewSize = 150;



        Color BulletColour = Color.Black;
        
        int NumberOfZombies = 50;
        int ZombieSize = 5;
        int ZombieSpeed = 3;
        Color ZombieColor = Color.Red;
        int ZombieSightRange = 200;

        int GoalSize = 100;

        Color BuildingColor = Color.Black;

        #endregion

        #region Private Variables

        readonly MouseHelper _mouse = new MouseHelper();
        Random oRandom = new Random();
        TimeSpan GameTimeElapsed = new TimeSpan();
        SpriteFont font;
        public static Texture2D SpriteTexture;
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        
        bool Ispaused = false;
        bool GameOver = false;
        bool GameWon = false;

        Human Player;
        List<Human> Players;

        List<Zombie> Zombies;
        List<Bullet> Bullets;
        int TotalZombiesHit;

        List<Buildings.Building> Buildings;
        
        Weapon.Weapon Shotgun = new Zombie_Survival_Sim.Weapon.Weapon() { BulletsPerShot = 1, TimeUntilNextShot = 0, WeaponFireSpeed = 30, ShotType = 2, Range = 300, Speed = 20, Size = 2};
        Weapon.Weapon Pistol = new Zombie_Survival_Sim.Weapon.Weapon() { BulletsPerShot = 1, TimeUntilNextShot = 0, WeaponFireSpeed = 20, ShotType = 1, Range = 600, Speed = 20, Size = 2 };
        Weapon.Weapon MachineGun = new Zombie_Survival_Sim.Weapon.Weapon() { BulletsPerShot = 1, TimeUntilNextShot = 0, WeaponFireSpeed = 5, ShotType = 1, Range = 450, Speed = 20, Size = 2 };
        Weapon.Weapon RocketLauncher = new Zombie_Survival_Sim.Weapon.Weapon() { BulletsPerShot = 1, TimeUntilNextShot = 0, WeaponFireSpeed = 60, ShotType = 1, Range = 10000, Speed = 8, Size = 4 };

        #endregion

        #region Multiplayer
        private KeyboardState ks;
        public NetClient client;
        public NetServer server;
        private NetLog Log;
        private NetAppConfiguration nac;

        public void HostClicked()
        {
            this.Window.Title = "Hosting....";
            nac = new NetAppConfiguration("ZombieSim", 12345);
            nac.MaximumConnections = 32;
            nac.ServerName = "ZombieSim!";
            Log = new NetLog();
            server = new NetServer(nac, Log);
            server.StatusChanged += new
                EventHandler<NetStatusEventArgs>(server_StatusChanged);
        }

        void server_StatusChanged(object sender, NetStatusEventArgs e)
        {
            if (e.Connection.Status == NetConnectionStatus.Connecting)
            {
                NetMessage msg = new NetMessage();
                msg.Write("Hello Client, I See you are able to talk to me," +
                    " I Can Talk to you Too!");

                //Sending the packet in Reliable is okay for this because its 
                //one packet, When sending lots and lots of data you need to 
                //set this to Unreliable. This will garentee it arrives but 
                //not in a sepcific order.
                server.SendMessage(msg, e.Connection, NetChannel.ReliableUnordered);

            }
            if (e.Connection.Status == NetConnectionStatus.Disconnected)
            {
                this.Window.Title = "Client Has Disconnected to You";
            }
        }

        public void ClientClicked()
        {
            this.Window.Title = "Trying To Connect....";

            //Since this is a client we dont have to specify a port here
            nac = new NetAppConfiguration("ZombieSim");
            Log = new NetLog();

            //Im Using this to show how to log files, So we pick that we want 
            //to ignore nothing, Change this depending on what your 
            //wanting to log for various debugging.
            Log.IgnoreTypes = NetLogEntryTypes.None;

            //Specify if you want to enable output to a file which we do.
            Log.IsOutputToFileEnabled = true;

            //If we output to a file we have to pick a filename to output it to.
            Log.OutputFileName = "Client.html";

            //We Initiate the client here, it has not connected to the server yet.
            client = new NetClient(nac, Log);

            //We Want to Log Events that are fired from the client
            client.StatusChanged +=
                new EventHandler<NetStatusEventArgs>(client_StatusChanged);

            //Finally we connect to the server, Specify the IP Address and 
            //port if your wanting to connect to xxx.xxx.xxx.xxx 
            //we would change this line to "192.168.1.1",12345
            client.Connect("192.168.0.200", 12345);
        }

        void client_StatusChanged(object sender, NetStatusEventArgs e)
        {

            if (e.Connection.Status == NetConnectionStatus.Connected)
            {
                NetMessage msg = new NetMessage();
                msg.Write("Hello Server! I Am Connected And Able To Communicate!");
                client.SendMessage(msg, NetChannel.ReliableUnordered);

            }
            if (e.Connection.Status == NetConnectionStatus.Disconnected)
            {
                this.Window.Title = "We Were disconnected";
            }
        }

        public void UpdateNetwork()
        {
            if (client != null)
            {
                //this line here, Pumps the Session with any packets 
                //that we have received since last time.
                client.Heartbeat();

                //This will hold our NetMessage, or our packet
                NetMessage msg;

                //This will be called for every message we have reveived, 
                //when the message is received we read the message from the packet.
                while ((msg = client.ReadMessage()) != null)
                {
                    //We will call this routing to handle the message 
                    //and process it accordingly.
                    ClientHandleMSG(msg);
                }
            }
            //This is the same as uptop, just for the server.
            if (server != null)
            {
                server.Heartbeat();
                NetMessage msg;

                while ((msg = server.ReadMessage()) != null)
                {
                    ServerHandleMSG(msg);
                }
                foreach (NetConnection Conn in server.Connections)
                {
                    if (Conn != null)
                    {
                        SendGameState(Conn);
                    }
                }
                
            }
        }
        int GameState = new int();
        
        private void SendGameState(NetConnection Conn)
        {
            GameState++;
            NetMessage msg = new NetMessage();
            string Message = "P" + Player.Location.X + "," + Player.Location.Y;
            Message = Message + "P";

            foreach (Zombie Zombie in Zombies)
            {
                Message = Message + "Z" + Zombie.Location.X + "," + Zombie.Location.Y;
            }
            Message = Message + "P";

            foreach (Bullet Bullet in Bullets)
            {
                Message = Message + "B" + Bullet.Location.X + "," + Bullet.Location.Y;
            }
            msg.Write(Message);

            //Sending the packet in Reliable is okay for this because its 
            //one packet, When sending lots and lots of data you need to 
            //set this to Unreliable. This will garentee it arrives but 
            //not in a sepcific order.

            server.SendMessage(msg, Conn, NetChannel.Unreliable);
        }
        
        private void RecievedGameState(string str)
        {
            string[] Statestring = str.Split('P');
            string PlayerString = Statestring[1];
            string[] PlayerStringArr = PlayerString.Split(',');
            Player.Location = new Vector2(float.Parse(PlayerStringArr[0]),float.Parse(PlayerStringArr[1]));
            
            Zombies.Clear();
            string[] Zombiesstring = Statestring[2].Split('Z');
            foreach (string Zombie in Zombiesstring)
            {
                if (!string.IsNullOrEmpty(Zombie))
                {
                    string[] ZombieStringArr = Zombie.Split(',');
                    Vector2 ZombieLocation = new Vector2(float.Parse(ZombieStringArr[0]), float.Parse(ZombieStringArr[1]));
                    Zombies.Add(new Zombie() { Location = ZombieLocation });
                }
            }

            Bullets.Clear();
            string[] Bulletsstring = Statestring[3].Split('B');
            foreach (string Bullet in Bulletsstring)
            {
                if (!string.IsNullOrEmpty(Bullet))
                {
                    string[] BulletStringArr = Bullet.Split(',');
                    Vector2 Bulletlocation = new Vector2(float.Parse(BulletStringArr[0]), float.Parse(BulletStringArr[1]));
                    Bullets.Add(new Bullet() { Location = Bulletlocation });
                }
            }
        }

        private void SendInput(string Input)
        {
            if (client != null)
            {
                GameState++;
                NetMessage msg = new NetMessage(); ;
                msg.Write(Input);

                //Sending the packet in Reliable is okay for this because its 
                //one packet, When sending lots and lots of data you need to 
                //set this to Unreliable. This will garentee it arrives but 
                //not in a sepcific order.
                client.SendMessage(msg, NetChannel.Unreliable);
            }
        }

        public void ServerHandleMSG(NetMessage msg)
        {
            string str = msg.ReadString();
            MovePlayer(str);
        }

        public void ClientHandleMSG(NetMessage msg)
        {
            string str = msg.ReadString();
            if (str.StartsWith("P"))
            {
                RecievedGameState(str);
            }
        }



        #endregion

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            IsMouseVisible = true;
            this.TargetElapsedTime = TimeSpan.FromSeconds(1.0f / GameSpeed);

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            SpriteTexture = Content.Load<Texture2D>("whitepixel");

            font = Content.Load<SpriteFont>("SpriteFont1");

            Player = new Human();
            Player.Location = new Vector2(10, 10);
            Player.SelectedWeapon = Pistol;
            
            Bullets = new List<Bullet>();

            #region Create Multiplayer

            Players = new List<Human>();

            Human Player1 = new Human();
            Player1.Location = new Vector2(10, 10);
            Players.Add(Player1);



            Human Player2 = new Human();
            Player2.Location = new Vector2(10, 10);
            Players.Add(Player2);
            #endregion

            #region CreateZombies
            Zombies = new List<Zombie>();
            for (int i = 0; i < NumberOfZombies; i++)
            {
                Zombies.Add(new Zombie() 
                {
                    Location = new Vector2(oRandom.Next(200,graphics.PreferredBackBufferWidth), oRandom.Next(graphics.PreferredBackBufferHeight))
                    , Velocity = new Vector2(oRandom.Next(-5,5), oRandom.Next(-5,5)) 
                    , Direction = new Vector2(1,0)
                });
            }
            #endregion
            
            #region Create Buildings
            Buildings = new List<Buildings.Building>();

            Building Building = new Building();
            Building.Location = new Vector2(0,100);
            Building.Size = new Vector2(500,50);
            Buildings.Add(Building);

            Building Building1 = new Building();
            Building1.Location = new Vector2(550, 100);
            Building1.Size = new Vector2(500, 50);
            Buildings.Add(Building1);

            Building Building2 = new Building();
            Building2.Location = new Vector2(0, 300);
            Building2.Size = new Vector2(200, 50);
            Buildings.Add(Building2);

            Building Building3 = new Building();
            Building3.Location = new Vector2(250, 300);
            Building3.Size = new Vector2(800, 50);
            Buildings.Add(Building3);
            #endregion
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

                if (!MultiplayerGame)
                {
                    ProcessGameState(gameTime);
                }
                else 
                {
                    ProcessInputMultiplayer();
                    if (client != null)
                    {
                        UpdateNetwork();
                    }
                    else if(server != null)
                    {
                        UpdateNetwork();
                        ProcessGameState(gameTime);
                    }
                    else
                    {
                        ks = Keyboard.GetState();
                        if (ks.IsKeyDown(Keys.H))
                        {
                            HostClicked();
                        }
                        if (ks.IsKeyDown(Keys.C))
                        {
                            ClientClicked();
                        }
                    }

                }
        }

        private void ProcessGameState(GameTime gameTime)
        {
            CheckPause();
            if (!Ispaused && !GameOver && !GameWon)
            {
                GameTimeElapsed = GameTimeElapsed + gameTime.ElapsedGameTime;
                if (gameTime.IsRunningSlowly)
                {

                }
                // TODO: Add your update logic here
                ProcessInput();
                UpdatePlayer();
                ProcessAI();
                UpdateBullets();
            }
            base.Update(gameTime);
        }

        private void ProcessInputMultiplayer()
        {
            KeyboardState keyState = Keyboard.GetState();

            #region Movement
            if (keyState.IsKeyDown(Keys.Left) && keyState.IsKeyDown(Keys.Up))
            {
                SendInput("NW");
                //Player.Direction = Vector2.Normalize(new Vector2(-1, -1));
                //Player.Location = Player.Location + Player.Direction * PlayerSpeed;
            }
            else if (keyState.IsKeyDown(Keys.Left) && keyState.IsKeyDown(Keys.Down))
            {
                SendInput("SW");
                //Player.Direction = Vector2.Normalize(new Vector2(-1, 1));
                //Player.Location = Player.Location + Player.Direction * PlayerSpeed;
            }
            else if (keyState.IsKeyDown(Keys.Right) && keyState.IsKeyDown(Keys.Up))
            {
                SendInput("NE");
                //Player.Direction = Vector2.Normalize(new Vector2(1, -1));
                //Player.Location = Player.Location + Player.Direction * PlayerSpeed;
            }
            else if (keyState.IsKeyDown(Keys.Right) && keyState.IsKeyDown(Keys.Down))
            {
                SendInput("SE");
                //Player.Direction = Vector2.Normalize(new Vector2(1, 1));
                //Player.Location = Player.Location + Player.Direction * PlayerSpeed;
            }
            else if (keyState.IsKeyDown(Keys.Left))
            {
                SendInput("W");
                //Player.Direction = Vector2.Normalize(new Vector2(-1, 0));
                //Player.Location = Player.Location + Player.Direction * PlayerSpeed;
            }
            else if (keyState.IsKeyDown(Keys.Up))
            {
                SendInput("N");
                //Player.Direction = Vector2.Normalize(new Vector2(0, -1));
                //Player.Location = Player.Location + Player.Direction * PlayerSpeed;
            }
            else if (keyState.IsKeyDown(Keys.Right))
            {
                SendInput("E");
                //Player.Direction = Vector2.Normalize(new Vector2(1, 0));
                //Player.Location = Player.Location + Player.Direction * PlayerSpeed;
            }
            else if (keyState.IsKeyDown(Keys.Down))
            {
                SendInput("S");
                //Player.Direction = Vector2.Normalize(new Vector2(0, 1));
                //Player.Location = Player.Location + Player.Direction * PlayerSpeed;
            }
            #endregion
        }

        private void CheckPause()
        {
            KeyboardState keyState = Keyboard.GetState();
            if (keyState.IsKeyDown(Keys.Pause))
            {
                Ispaused = !Ispaused;
            }
            if (keyState.IsKeyDown(Keys.R))
            {
                GameTimeElapsed = new TimeSpan();
                Ispaused = false;
                LoadContent();
            }
            if (GameOver || GameWon)
            {
                if (keyState.IsKeyDown(Keys.Space))
                {
                    GameTimeElapsed = new TimeSpan();
                    GameOver = false;
                    GameWon = false;
                    LoadContent();
                }
            }
        }

        private void ProcessInput()
        {
            KeyboardState keyState = Keyboard.GetState();
            _mouse.UpdateMouse();
            #region Movement
            if (keyState.IsKeyDown(Keys.Left) && keyState.IsKeyDown(Keys.Up))
            {
                Player.Direction = Vector2.Normalize(new Vector2(-1, -1));
                Player.Location = Player.Location + Player.Direction * PlayerSpeed;
            }
            else if (keyState.IsKeyDown(Keys.Left) && keyState.IsKeyDown(Keys.Down))
            {
                Player.Direction = Vector2.Normalize(new Vector2(-1, 1));
                Player.Location = Player.Location + Player.Direction * PlayerSpeed;
            }
            else if (keyState.IsKeyDown(Keys.Right) && keyState.IsKeyDown(Keys.Up))
            {
                Player.Direction = Vector2.Normalize(new Vector2(1, -1));
                Player.Location = Player.Location + Player.Direction * PlayerSpeed;
            }
            else if (keyState.IsKeyDown(Keys.Right) && keyState.IsKeyDown(Keys.Down))
            {
                Player.Direction = Vector2.Normalize(new Vector2(1, 1));
                Player.Location = Player.Location + Player.Direction * PlayerSpeed;
            }
            else if (keyState.IsKeyDown(Keys.Left))
            {
                Player.Direction = Vector2.Normalize(new Vector2(-1, 0));
                Player.Location = Player.Location + Player.Direction * PlayerSpeed;
            }
            else if (keyState.IsKeyDown(Keys.Up))
            {
                Player.Direction = Vector2.Normalize(new Vector2(0, -1));
                Player.Location = Player.Location + Player.Direction * PlayerSpeed;
            }
            else if (keyState.IsKeyDown(Keys.Right))
            {
                Player.Direction = Vector2.Normalize(new Vector2(1, 0));
                Player.Location = Player.Location + Player.Direction * PlayerSpeed;
            }
            else if (keyState.IsKeyDown(Keys.Down))
            {
                Player.Direction = Vector2.Normalize(new Vector2(0, 1));
                Player.Location = Player.Location + Player.Direction * PlayerSpeed;
            }



            #endregion

            #region Weapon Control

            if (keyState.IsKeyDown(Keys.LeftControl) || keyState.IsKeyDown(Keys.RightControl) || _mouse.MousePressed)
            {
                FireBullet();
            }
            if (keyState.IsKeyDown(Keys.F1))
            {
                Player.SelectedWeapon = Pistol;
            }
            if (keyState.IsKeyDown(Keys.F2))
            {
                Player.SelectedWeapon = Shotgun;
            }
            if (keyState.IsKeyDown(Keys.F3))
            {
                Player.SelectedWeapon = MachineGun;
            }
            if (keyState.IsKeyDown(Keys.F4))
            {
                Player.SelectedWeapon = RocketLauncher;
            }

            #endregion



        }

        private void UpdatePlayer()
        {
            #region Weapon Fire Countdown Timer
            
            if (Player.SelectedWeapon.TimeUntilNextShot > 0)
            {
                Player.SelectedWeapon.TimeUntilNextShot--;
            }

            #endregion

            #region Wall Collision Detection

            if (Player.Location.X < 0)
            {
                Player.Location = new Vector2(0, Player.Location.Y);
            }
            if (Player.Location.X > graphics.PreferredBackBufferWidth - PlayerSize)
            {
                Player.Location = new Vector2(graphics.PreferredBackBufferWidth - PlayerSize, Player.Location.Y);
            }
            if (Player.Location.Y < 0)
            {
                Player.Location = new Vector2(Player.Location.X, 0);
            }
            if (Player.Location.Y > graphics.PreferredBackBufferHeight - PlayerSize)
            {
                Player.Location = new Vector2(Player.Location.X, graphics.PreferredBackBufferHeight - PlayerSize);
            }

            #endregion

            #region Building Collision Detection

            foreach (Building Building in Buildings)
            {
                Rectangle BuildingRect = new Rectangle((int)Building.Location.X, (int)Building.Location.Y, (int)Building.Size.X, (int)Building.Size.Y);
                Rectangle PlayerRect = new Rectangle((int)Player.Location.X, (int)Player.Location.Y, PlayerSize, PlayerSize);

                if (PlayerRect.Intersects(BuildingRect))
                {
                    RevertPlayerMove(BuildingRect);
                }
            }

            #endregion

            #region Goal Collision Detection

            Rectangle PlayerRect1 = new Rectangle((int)Player.Location.X, (int)Player.Location.Y, PlayerSize, PlayerSize);
            Rectangle GoalRect = new Rectangle((int)graphics.PreferredBackBufferWidth - GoalSize, (int)graphics.PreferredBackBufferHeight - GoalSize, GoalSize, GoalSize);

            if(PlayerRect1.Intersects(GoalRect))
            {
                GameWon = true;
            }

            #endregion
        }


        private void MovePlayer(string Direction)
        {
            if (Direction == "NW")
            {
                Player.Direction = Vector2.Normalize(new Vector2(-1, -1));
                Player.Location = Player.Location + Player.Direction * PlayerSpeed;
            }
            else if (Direction == "SW")
            {
                Player.Direction = Vector2.Normalize(new Vector2(-1, 1));
                Player.Location = Player.Location + Player.Direction * PlayerSpeed;
            }
            else if (Direction == "NE")
            {
                Player.Direction = Vector2.Normalize(new Vector2(1, -1));
                Player.Location = Player.Location + Player.Direction * PlayerSpeed;
            }
            else if (Direction == "SE")
            {
                Player.Direction = Vector2.Normalize(new Vector2(1, 1));
                Player.Location = Player.Location + Player.Direction * PlayerSpeed;
            }
            else if (Direction == "W")
            {
                Player.Direction = Vector2.Normalize(new Vector2(-1, 0));
                Player.Location = Player.Location + Player.Direction * PlayerSpeed;
            }
            else if (Direction == "N")
            {
                Player.Direction = Vector2.Normalize(new Vector2(0, -1));
                Player.Location = Player.Location + Player.Direction * PlayerSpeed;
            }
            else if (Direction == "E")
            {
                Player.Direction = Vector2.Normalize(new Vector2(1, 0));
                Player.Location = Player.Location + Player.Direction * PlayerSpeed;
            }
            else if (Direction == "S")
            {
                Player.Direction = Vector2.Normalize(new Vector2(0, 1));
                Player.Location = Player.Location + Player.Direction * PlayerSpeed;
            }
        }

        private void RevertPlayerMove(Rectangle BuildingRect)
        {
            Player.Location = Player.Location - Player.Direction;
            Rectangle PlayerRect = new Rectangle((int)Player.Location.X, (int)Player.Location.Y, PlayerSize, PlayerSize);

            if (PlayerRect.Intersects(BuildingRect))
            {
                RevertPlayerMove(BuildingRect);
            }
        }

        private void ProcessAI()
        {
            foreach (Zombie Zombie in Zombies)
            {
                Rectangle PlayerRect = new Rectangle((int)Player.Location.X, (int)Player.Location.Y, PlayerSize, PlayerSize);
                Rectangle ZombieSightRect = new Rectangle((int)Zombie.Location.X - ZombieSightRange / 2, (int)Zombie.Location.Y - ZombieSightRange / 2, ZombieSightRange, ZombieSightRange);
                if (PlayerRect.Intersects(ZombieSightRect))
                {
                    Zombie.Direction = new Vector2(Player.Location.X - Zombie.Location.X,Player.Location.Y - Zombie.Location.Y);
                }
                else
                {
                    //randomly turn.
                    int iRand = oRandom.Next(0, 100);
                    if (iRand < 5)//turn right
                    {
                        Zombie.Direction = new Vector2(Zombie.Direction.Y, -Zombie.Direction.X);
                    }
                    else if (iRand > 95)//turn left
                    {
                        Zombie.Direction = new Vector2(-Zombie.Direction.Y, Zombie.Direction.X);
                    }    
                }
                Zombie.Velocity = Vector2.Normalize(Zombie.Direction) * ZombieSpeed;


                if ((Zombie.Velocity + Zombie.Location).X < 0)
                {
                    Zombie.Direction = new Vector2(1,0);
                    Zombie.Velocity = Vector2.Normalize(Zombie.Direction) * ZombieSpeed;
                }
                if ((Zombie.Velocity + Zombie.Location).Y < 0)
                {
                    Zombie.Direction = new Vector2(0, 1);
                    Zombie.Velocity = Vector2.Normalize(Zombie.Direction) * ZombieSpeed;
                }
                if ((Zombie.Velocity + Zombie.Location).X > graphics.PreferredBackBufferWidth - ZombieSize)
                {
                    Zombie.Direction = new Vector2(-1, 0);
                    Zombie.Velocity = Vector2.Normalize(Zombie.Direction) * ZombieSpeed;
                }
                if ((Zombie.Velocity + Zombie.Location).Y > graphics.PreferredBackBufferHeight - ZombieSize)
                {
                    Zombie.Direction = new Vector2(0, -1);
                    Zombie.Velocity = Vector2.Normalize(Zombie.Direction) * ZombieSpeed;
                }
                else 
                {
                    //Move Zombie
                    Zombie.Location = Zombie.Velocity + Zombie.Location; 
                }


                Rectangle ZombieRect = new Rectangle((int)Zombie.Location.X, (int)Zombie.Location.Y, ZombieSize, ZombieSize);

                foreach (Building Building in Buildings)
                {
                    Rectangle BuildingRect = new Rectangle((int)Building.Location.X, (int)Building.Location.Y, (int)Building.Size.X, (int)Building.Size.Y);

                    if (ZombieRect.Intersects(BuildingRect))
                    {
                        RevertZombieMove(BuildingRect, Zombie);
                    }
                }
                

                if (ZombieRect.Intersects(PlayerRect))
                {
                    //TODO: Player Has been Eaten
                    GameOver = true;
                }
            }
        }

        private void RevertZombieMove(Rectangle BuildingRect, Zombie Zombie)
        {
            Zombie.Location = Zombie.Location - Zombie.Direction;
            Rectangle ZombieRect = new Rectangle((int)Zombie.Location.X, (int)Zombie.Location.Y, ZombieSize, ZombieSize);

            if (ZombieRect.Intersects(BuildingRect))
            {
                RevertZombieMove(BuildingRect, Zombie);
            }
        }

        private void FireBullet()
        {
            if (Player.SelectedWeapon.TimeUntilNextShot > 0)
            {
                
            }
            else
            {
                MouseState mouse = Mouse.GetState();
                
                Vector2 AimDirection =Vector2.Normalize(new Vector2(mouse.X - Player.Location.X,mouse.Y -  Player.Location.Y));
                Bullets.AddRange(Player.SelectedWeapon.Fire(Player.Location + new Vector2(PlayerSize / 2, PlayerSize / 2), AimDirection));

                Player.SelectedWeapon.TimeUntilNextShot = Player.SelectedWeapon.WeaponFireSpeed;
            }
        }

        Vector2 _intersectionPoint;
        
        private bool ProcessIntersection(Vector2 line1Point1, Vector2 line1Point2, Vector2 line2Point1, Vector2 line2Point2)
        {
            bool coincident;
            bool intersection = coincident = false;
            
            float ua = (line2Point2.X - line2Point1.X) * (line1Point1.Y - line2Point1.Y) - (line2Point2.Y - line2Point1.Y) * (line1Point1.X - line2Point1.X);
            float ub = (line1Point2.X - line1Point1.X) * (line1Point1.Y - line2Point1.Y) - (line1Point2.Y - line1Point1.Y) * (line1Point1.X - line2Point1.X);
            float denominator = (line2Point2.Y - line2Point1.Y) * (line1Point2.X - line1Point1.X) - (line2Point2.X - line2Point1.X) * (line1Point2.Y - line1Point1.Y);

           

            if (Math.Abs(denominator) <= 0.00001f)
            {
                if (Math.Abs(ua) <= 0.00001f && Math.Abs(ub) <= 0.00001f)
                {
                    intersection = coincident = true;
                    _intersectionPoint = (line1Point1 + line1Point2) / 2;
                }
            }
            else
            {
                ua /= denominator;
                ub /= denominator;

                if (ua >= 0 && ua <= 1 && ub >= 0 && ub <= 1)
                {
                    intersection = true;
                    _intersectionPoint.X = line1Point1.X + ua * (line1Point2.X - line1Point1.X);
                    _intersectionPoint.Y = line1Point1.Y + ua * (line1Point2.Y - line1Point1.Y);
                }
            }
            return intersection;
        }
        private void UpdateBullets()
        {
            List<Bullet> BulletsToRemove = new List<Bullet>();
            foreach (Bullet oBullet in Bullets)
            {
                //check the bullet hasn't reached the range limit
                //TODO: this will be killing the bullet one update short of its range
                //we could do with an update to make sure the bullet checks for collisions right up to the range limit
                if (oBullet.Range > oBullet.Speed)
                {
                    oBullet.Range = oBullet.Range - oBullet.Speed;
                    Vector2 OriginalBulletPosition = oBullet.Location;
                    //Move the bullet
                    oBullet.Location = oBullet.Velocity + oBullet.Location;
                    #region Bullet - Zombie collision detection
                    Rectangle Bulletrect = new Rectangle((int)oBullet.Location.X, (int)oBullet.Location.Y, oBullet.Size, oBullet.Size);
                    List<Zombie> DeadZombies = new List<Zombie>();
                    foreach (Zombie Zombie in Zombies)
                    {
                        Rectangle Zombierect = new Rectangle((int)Zombie.Location.X, (int)Zombie.Location.Y, ZombieSize, ZombieSize);
                        Vector2 zombieTopLeft = new Vector2(Zombierect.X, Zombierect.Y);
                        Vector2 zombieBottomLeft = new Vector2(Zombierect.X, Zombierect.Y + Zombierect.Height);
                        Vector2 zombieTopRight = new Vector2(Zombierect.X + Zombierect.Width, Zombierect.Y);
                        Vector2 zombieBottomRight = new Vector2(Zombierect.X + Zombierect.Width, Zombierect.Y + Zombierect.Height);
                        bool hit = false;
                        if (ProcessIntersection(OriginalBulletPosition, oBullet.Location, zombieTopLeft, zombieBottomLeft))
                        {
                            hit = true;
                        }
                        if (ProcessIntersection(OriginalBulletPosition, oBullet.Location, zombieBottomLeft, zombieBottomRight))
                        {
                            hit = true;
                        }
                        if (ProcessIntersection(OriginalBulletPosition, oBullet.Location, zombieBottomRight, zombieTopRight))
                        {
                            hit = true;
                        }
                        if (ProcessIntersection(OriginalBulletPosition, oBullet.Location, zombieTopLeft, zombieTopRight))
                        {
                            hit = true;
                        }

                        if (hit)
                        {
                            //TODO:if the bullet is piercing dont excecute this line
                            BulletsToRemove.Add(oBullet);

                            //bullet hit zombie
                            if (Zombie.Health != 0)
                            {
                                DeadZombies.Add(Zombie);
                                TotalZombiesHit++;
                                Zombie.Health = 0;
                            }
                        }
                    }
                    foreach (Zombie Zombie in DeadZombies)
                    {
                        Zombies.Remove(Zombie);
                    }
                    #endregion

                    #region Bullet - Building collision detection

                    foreach (Building building in Buildings)
                    {
                        Rectangle buildingRect = new Rectangle((int)building.Location.X, (int)building.Location.Y, (int)building.Size.X, (int)building.Size.Y);
                        Vector2 buildingTopLeft = new Vector2(buildingRect.X, buildingRect.Y);
                        Vector2 buildingBottomLeft = new Vector2(buildingRect.X, buildingRect.Y + buildingRect.Height);
                        Vector2 buildingTopRight = new Vector2(buildingRect.X + buildingRect.Width, buildingRect.Y);
                        Vector2 buildingBottomRight = new Vector2(buildingRect.X + buildingRect.Width, buildingRect.Y + buildingRect.Height);
                        bool hit = false;
                        if (ProcessIntersection(OriginalBulletPosition, oBullet.Location, buildingTopLeft, buildingBottomLeft))
                        {
                            hit = true;
                        }
                        if (ProcessIntersection(OriginalBulletPosition, oBullet.Location, buildingBottomLeft, buildingBottomRight))
                        {
                            hit = true;
                        }
                        if (ProcessIntersection(OriginalBulletPosition, oBullet.Location, buildingBottomRight, buildingTopRight))
                        {
                            hit = true;
                        }
                        if (ProcessIntersection(OriginalBulletPosition, oBullet.Location, buildingTopLeft, buildingTopRight))
                        {
                            hit = true;
                        }

                        if (hit)
                        {
                            //TODO:if the bullet is piercing dont excecute this line
                            BulletsToRemove.Add(oBullet);
                        }
                    }
                    
                    #endregion

                    //If Bullet reaches Edge of screen
                    if (oBullet.Location.X > graphics.PreferredBackBufferWidth || oBullet.Location.X < 0 || oBullet.Location.Y > graphics.PreferredBackBufferHeight || oBullet.Location.Y < 0)
                    {
                        BulletsToRemove.Add(oBullet);
                    }
                }
                else
                {
                    //Remove bullets that have reached their range limit
                    BulletsToRemove.Add(oBullet);
                }
            }
            //Remove Unwanted Bullets
            foreach (Bullet Bullet in BulletsToRemove)
            {
                 Bullets.Remove(Bullet);
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here


            spriteBatch.Begin();
            spriteBatch.Draw(Game1.SpriteTexture, new Rectangle((int)graphics.PreferredBackBufferWidth - GoalSize, (int)graphics.PreferredBackBufferHeight - GoalSize, GoalSize, GoalSize), PlayerColor);

            spriteBatch.Draw(Game1.SpriteTexture, new Rectangle((int)Player.Location.X, (int)Player.Location.Y, PlayerSize, PlayerSize), PlayerColor);

            foreach (Human Multiplayer in Players)
            {
                spriteBatch.Draw(Game1.SpriteTexture, new Rectangle((int)Multiplayer.Location.X, (int)Multiplayer.Location.Y, PlayerSize, PlayerSize), PlayerColor);
            }

            foreach(Zombie Zombie in Zombies)
            {
                if (PlayerFieldOfView)
                {
                    if (PlayerFieldOfViewSize > Player.Location.X - Zombie.Location.X
                        && Player.Location.X - Zombie.Location.X > -PlayerFieldOfViewSize
                        && PlayerFieldOfViewSize > Player.Location.Y - Zombie.Location.Y
                        && Player.Location.Y - Zombie.Location.Y > -PlayerFieldOfViewSize)
                    {
                        spriteBatch.Draw(Game1.SpriteTexture, new Rectangle((int)Zombie.Location.X, (int)Zombie.Location.Y, ZombieSize, ZombieSize), ZombieColor);
                    }
                }
                else
                {
                    spriteBatch.Draw(Game1.SpriteTexture, new Rectangle((int)Zombie.Location.X, (int)Zombie.Location.Y, ZombieSize, ZombieSize), ZombieColor);
                }
            }

            foreach (Bullet oBullet in Bullets)
            {
                spriteBatch.Draw(Game1.SpriteTexture, new Rectangle((int)oBullet.Location.X, (int)oBullet.Location.Y, oBullet.Size, oBullet.Size), BulletColour);
            }

            foreach (Building Building in Buildings)
            {
                spriteBatch.Draw(Game1.SpriteTexture, new Rectangle((int)Building.Location.X, (int)Building.Location.Y, (int)Building.Size.X, (int)Building.Size.Y), BuildingColor);

            }
            
            DrawText(gameTime);
            
            
            spriteBatch.End();

            

            base.Draw(gameTime);
        }

        private void DrawText(GameTime gameTime)
        {
            spriteBatch.DrawString(font, "Time : " + GameTimeElapsed, new Vector2(20, 20), Color.White);
            spriteBatch.DrawString(font, "Zombies Left : " + Zombies.Count, new Vector2(20, 40), Color.White);
            spriteBatch.DrawString(font, "Good Performance : " + !gameTime.IsRunningSlowly, new Vector2(20, 60), Color.White);
            spriteBatch.DrawString(font, "Safe Zone", new Vector2(710, 575), Color.Black);

            if (GameOver)
            {
                spriteBatch.DrawString(font, "Game Over!!!", new Vector2(300, 250), Color.Black);
                spriteBatch.DrawString(font, "Press Space to restart", new Vector2(270, 270), Color.Black);
            }
            else if(GameWon)
            {
                spriteBatch.DrawString(font, "You Win!!!", new Vector2(300, 250), Color.Black);
                spriteBatch.DrawString(font, "Press Space to restart", new Vector2(270, 270), Color.Black);

            }
            else if (Ispaused)
            {
                spriteBatch.DrawString(font, "Game Paused", new Vector2(300, 250), Color.Black);
                spriteBatch.DrawString(font, "Press Pause/Break to continue", new Vector2(250, 270), Color.Black);
            }
        }
    }
}
