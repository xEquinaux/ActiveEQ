using REWD.FoundationR;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CotF_dev
{
    public class Asset
    {
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern int memcpy(IntPtr dest, byte[] srce, int bytes);	

        public static Bitmap Request(string name)
        {
            return (Bitmap)Bitmap.FromFile("./Textures/" + name + ".png");
        }
        public static void Request(string name, string extension, out Bitmap image)
        {
            image = (Bitmap)Bitmap.FromFile("./Textures/" + name + extension);
        }
        public static Bitmap LoadFromFile(string path)
        {
            REW rew;
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                (rew = new REW()).ReadData(new BinaryReader(fs));
            }
            Bitmap bmp = new Bitmap(rew.Width, rew.Height);
            var data = bmp.LockBits(new Rectangle(0, 0, rew.Width, rew.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            memcpy(data.Scan0, rew.GetPixels(), rew.RealLength - 2);
            bmp.UnlockBits(data);
            return bmp;
        }
        public static void LoadFromFile(string path, out Bitmap image)
        {
            REW rew;
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                (rew = new REW()).ReadData(new BinaryReader(fs));
            }
            image = new Bitmap(rew.Width, rew.Height);
            var data = image.LockBits(new Rectangle(0, 0, rew.Width, rew.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            memcpy(data.Scan0, rew.GetPixels(), rew.RealLength - 2);
            image.UnlockBits(data);
        }
    }
}
