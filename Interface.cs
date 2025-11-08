using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveEQ
{
	internal static class Interface
	{
        public static int Width, Height;
        static Point tRelative = new Point();
        static Point relative = new Point();
        static bool holdClick = false;
        
		public static Rectangle Drag(this Rectangle element, Point mousePosition, bool mouseLeft)
        {
            Point point = mousePosition;
            if (mouseLeft)
            {
                if (element.Contains(point) || holdClick)
                {
                    holdClick = true;
                    return new Rectangle(mousePosition.X - relative.X, mousePosition.Y - relative.Y, element.Width, element.Height);
                }
            }
            else
            {
                holdClick = false;
                relative = RelativeMouse(element, mousePosition);
            }
            return element;
        }
        public static Point RelativeMouse(Rectangle element, Point mouse)
        {
            int x = mouse.X - element.Left;
            int y = mouse.Y - element.Top;
            return new Point(x, y);
        }
        public static Point RelativeMouse(this Rectangle element, Point mouse, int Width, int Height)
        {
            int x = Width - element.Left;
            int y = Height - element.Top;
            mouse.X -= x;
            mouse.Y -= y;
            return new Point(mouse.X, mouse.Y);
        }

        public static Rectangle Bounds(this Rectangle element)
        {
            return new Rectangle(element.Left, element.Top, element.Width, element.Height);
        }
	}
}
