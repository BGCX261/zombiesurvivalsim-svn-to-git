using Microsoft.Xna.Framework.Input;

namespace Zombie_Survival_Sim.Helpers
{

        public class MouseHelper
        {
            public MouseState Mouse;
            public bool MouseClicked;
            public bool MousePressed;
            public bool MouseDown;
            public int MouseXOffset;
            public int MouseYOffset;


            public void UpdateMouse()
            {
                Mouse = Microsoft.Xna.Framework.Input.Mouse.GetState();

                if (Mouse.LeftButton == ButtonState.Pressed)
                {
                    MouseDown = false;
                    if (!MousePressed)
                    {
                        MouseDown = true;
                    }
                    MousePressed = true;
                }
                MouseClicked = false;
                if (Mouse.LeftButton == ButtonState.Released && MousePressed)
                {
                    MouseClicked = true;
                    MousePressed = false;
                }

            }
        }
    }
