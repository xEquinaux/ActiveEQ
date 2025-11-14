using CotF_dev;
using SharpDX.Direct3D11;
using System;
using System.Linq;

namespace tUserInterface.ModUI
{
    public class ListBox
    {
        public ListBox(Rectangle bounds, Scroll scroll, string[] content, Brush[] textColor = null) 
        { 
            hitbox = bounds;
            this.content = content;
            this.scroll = scroll;
            this.textColor = textColor;
            selectedPage = this;
        }
        public ListBox(Rectangle bounds, Scroll scroll, Button[] item)
        {
            hitbox = bounds;
            this.scroll = scroll;
            this.item = item;
        }
        public Button[] AddButton(string buttonText, Brush color)
        {
            var _item = item.ToList();
            _item.Add(new Button("", default, color) { text2 = buttonText, innactiveDrawText = true });
            return _item.ToArray();
        }
        public bool active = true;
        public Rectangle hitbox;
        public Scroll scroll;
        public Color bgColor = Color.White;
        public string[] content;
        public Brush[] textColor;
        public Button[] item;
        public Button[] tab;
        public ListBox[] page;
        private ListBox selectedPage;
        public int offX, offY;
        public void Update(Point MouseScreen, bool mouseLeft)
        {
            if (!active) return;
            scroll.parent = hitbox;
            Scroll.KbInteract(scroll, MouseScreen);
            Scroll.MouseInteract(scroll, MouseScreen, mouseLeft);
            Scroll.ScrollInteract(scroll, MouseScreen);
        }
        public void Draw(Graphics sb, Font font, int xOffset = 0, int yOffset = 0, int height = 42)
        {
            if (!active) return;
            //  unused background texture drawing
            //sb.Draw(backgroundTex, hitbox, bgColor);
            for (int n = 0; n < content.Length; n++)
            {
                float y = hitbox.Y + n * height - content.Length * height * scroll.value;
                if (y >= hitbox.Top && y <= hitbox.Bottom - height)
                {
                    Rectangle box = new Rectangle(hitbox.X, (int)y, 32, 32);
                    if (textColor == null || textColor.Length != content.Length) { 
                        sb.DrawString(content[n], font, Brushes.Black, new Point(hitbox.X + xOffset, box.Y + yOffset));
                    }
                    else sb.DrawString(content[n], font, textColor[n], new Point(hitbox.X + xOffset, box.Y + yOffset));
                }
            }
        }
        public void Draw(Graphics sb, Font font, int xOffset = 0, int yOffset = 0, int height = 42, bool drawIcon = false)
        {
            if (!active || content == null)
                return;
            //Utils.DrawInvBG(sb, hitbox, bgColor);
            for (int n = 0; n < content.Length; n++)
            {
                float y = hitbox.Y + n * height - content.Length * height * scroll.value;
                if (y >= hitbox.Top && y <= hitbox.Bottom - height)
                {
                    Rectangle box = new Rectangle(hitbox.X, (int)y, 32, 32);
                    //  unused texture drawing icons
                    //if (drawIcon && icon != null && icon[n] != null && icon.Length == content.Length)
                    //{
                    //    sb.Draw(icon[n], new Vector2(hitbox.X, box.Y + yOffset), Color.White);
                    //}
                    if (!string.IsNullOrEmpty(content[n]))
                    { 
                        if (textColor == null || textColor.Length != content.Length)
                        {
                            sb.DrawString(content[n], font, Brushes.White, new Point(hitbox.X + xOffset, box.Y + yOffset));
                        }
                        else sb.DrawString(content[n], font,  textColor[n], new Point(hitbox.X + xOffset, box.Y + yOffset));
                    }
                }
            }
        }
        public void DrawItems(Graphics sb, Point MouseScreen, Font font, int xOffset = 0, int yOffset = 0, int height = 42)
        {
            if (!active) return;
            //Utils.DrawInvBG(sb, hitbox, bgColor);
            for (int n = 0; n < item.Length; n++)
            {
                float y = hitbox.Y + n * height - item.Length * height * scroll.value;
                if (y >= hitbox.Top && y <= hitbox.Bottom - height)
                {
                    Rectangle box = new Rectangle(hitbox.X + xOffset, (int)y + yOffset, hitbox.Width - xOffset * 4, (int)(height * 0.8f));
                    if (item[n] != null && (item[n].active || item[n].innactiveDrawText))
                    { 
                        item[n].box = box;
                        //  unused icon drawing next to items
                        //if (icon != null && icon[n] != null && icon.Length == item.Length)
                        //{
                        //    sb.Draw(icon[n], new Vector2(hitbox.X + offX, box.Y + yOffset + offY), Color.White);
                        //}
                        item[n].Draw(sb, font, MouseScreen, item[n].HoverOver(MouseScreen));
                    }
                }
            }
        }
        public void DrawItemsNoIcon(Graphics sb, Point MouseScreen, Font font, int xOffset = 0, int yOffset = 0, int height = 42)
        {
            if (!active)
                return;
            //Utils.DrawInvBG(sb, hitbox, bgColor);
            for (int n = 0; n < item.Length; n++)
            {
                float y = hitbox.Y + n * height - item.Length * height * scroll.value;
                if (y >= hitbox.Top && y <= hitbox.Bottom - height)
                {
                    Rectangle box = new Rectangle(hitbox.X + xOffset, (int)y + yOffset, hitbox.Width - xOffset * 4, (int)(height * 0.8f));
                    if (item[n] != null && (item[n].active || item[n].innactiveDrawText))
                    {
                        item[n].box = box;
                        item[n].Draw(sb, font, MouseScreen, item[n].HoverOver(MouseScreen));
                        if (item[n].box.Contains(MouseScreen))
                        {
                            sb.DrawRectangle(Pens.Blue, item[n].box);
                        }
                    }
                }
            }
        }
    }
    public class Scroll
    {
        public Scroll(Rectangle parent)
        {
            this.parent = parent;
        }
        public float value;
        private float x => parent.Right - Width;
        private float y => parent.Top + parent.Height * value;
        public int X => (int)x;
        public int Y => (int)y;
        public Rectangle parent;
        public Rectangle hitbox => new Rectangle(X, Y, Width, Height);
        public const int Width = 12;
        public const int Height = 32;
        public bool clicked;
        private bool flag;
        static int oldValue = 0;
        public static void DirectMouseInteract(Scroll bar, Point mouseScreen, bool mouseLeft)
        {
            if (mouseLeft && bar.hitbox.Contains((int)mouseScreen.X, (int)mouseScreen.Y))
                bar.clicked = true;
            bar.flag = mouseLeft;
            if (!mouseLeft)
                bar.clicked = false;
            if (bar.clicked && bar.flag)
            {
                Point mouse = new Point(mouseScreen.X, mouseScreen.Y - bar.parent.Top - Height / 2);
                bar.value = Math.Max(0f, Math.Min(mouse.Y / bar.parent.Height, 1f));
            }
        }
        public static void KbInteract(Scroll bar, Point mouseScreen)
        {
            if (bar.parent.Contains((int)mouseScreen.X, (int)mouseScreen.Y))
            {
                if (Keyboard.IsKeyPressed((int)VIRTUALKEY.VK_DOWN))
                {
                    if (bar.value * (bar.parent.Height - Height) < bar.parent.Height - Height)
                    {
                        bar.value += 0.00001f;
                    }
                }
                if (Keyboard.IsKeyPressed((int)VIRTUALKEY.VK_UP))
                {
                    if (bar.value > 0f)
                    {
                        bar.value -= 0.00001f;
                    }
                    else bar.value = 0f;
                }
            }
        }
        public static void MouseInteract(Scroll bar, Point mouseScreen, bool mouseLeft)
        {
            if (mouseLeft && bar.hitbox.Contains((int)mouseScreen.X, (int)mouseScreen.Y))
                bar.clicked = true;
            bar.flag = mouseLeft;
            if (!mouseLeft)
                bar.clicked = false;
            if (bar.clicked && bar.flag)
            {
                Point mouse = new Point(mouseScreen.X, mouseScreen.Y - bar.parent.Top - Height / 2);
                bar.value = Math.Max(0f, Math.Min(mouse.Y / bar.parent.Height, 1f));
            }
        }
        [Obsolete("Unused. Currently an empty method.")]
        public static void ScrollInteract(Scroll bar, Point mouseScreen)
        {
            /*
            if (bar.parent.Contains((int)mouseScreen.X, (int)mouseScreen.Y))
            {
                if (Mouse.GetState().ScrollWheelValue < oldValue)
                {
                    bar.value = Math.Min(1f, bar.value + 0.1f);
                }
                else if (Mouse.GetState().ScrollWheelValue > oldValue)
                {
                    bar.value = Math.Max(0f, bar.value - 0.1f);
                }
                oldValue = Mouse.GetState().ScrollWheelValue;
            } */
        }
        public void Draw(Graphics sb, Brush color)
        {
            sb.FillRectangle(color, hitbox);
        }
    }
    [Obsolete("No current means of collecting keyboard inputs required by a textbox.")]
    public class TextBox
    {
        public bool active;
        public string text = "";
        //public Color color => active ? color2 * 0.67f : color2 * 0.33f;
        private Brush color2 = Brushes.DodgerBlue;
        public Rectangle box;
        //Texture2D magicPixel;
        public static void Initialize(/*Texture2D magicPixel*/)
        {
        //    TextBox.magicPixel = magicPixel;
        }
        public TextBox(Rectangle box, Brush color)
        {
            this.box = box;
            this.color2 = color;
        }
        public bool LeftClick(Point MouseScreen, bool mouseLeft)
        {
            return box.Contains(MouseScreen) && mouseLeft;
        }
        public bool HoverOver(Point MouseScreen)
        {
            return box.Contains(MouseScreen);
        }
        public void UpdateInput()
        {
            if (active)
            {
                /*
                foreach (Keys key in keyState.GetPressedKeys())
                {
                    if (oldState.IsKeyUp(key))
                    {
                        if (key == Keys.F3)
                            return;
                        if (key == Keys.Back)
                        {
                            if (text.Length > 0)
                                text = text.Remove(text.Length - 1);
                            oldState = keyState;
                            return;
                        }
                        else if (key == Keys.Space)
                            text += " ";
                        else if (key == Keys.OemPeriod)
                            text += ".";
                        else if (text.Length < 24 && key != Keys.OemPeriod)
                        {
                            string n = key.ToString().ToLower();
                            if (n.StartsWith("d") && n.Length == 2)
                                n = n.Substring(1);
                            text += n;
                        }
                    }
                }
                oldState = keyState; */
            }              
        }
        public void DrawText(Graphics sb, Font font, bool drawMagicPixel = false)
        {
            if (!active) return;
            if (font != null)
            { 
                sb.FillRectangle(color2, box);
                sb.DrawString(text, font, Brushes.White, new Point(box.X + 2, box.Y + 1));
            }
            else       
            {
                /*
                if (drawMagicPixel)
                { 
                    sb.Draw(TextureAssets.MagicPixel.Value, box, color);
                }
                Utils.DrawBorderString(sb, text, new Vector2(box.X + 2, box.Y + 1), Color.White);
                */
            }
        }
    }
    [Obsolete("Empty class due to this being ported from my mods.")]
    public class Container
    {
        /*
        public bool active;
        public bool reserved = false;
        private bool flag;
        public string text = "";
        //public Brush color => active ? color2 * 0.67f : color2 * 0.33f;
        private Brush color2 = Color.DodgerBlue;
        public Rectangle box;
        public Rectangle boxBugFix => new Rectangle(box.X - box.Width, box.Y - box.Height, box.Width, box.Height);
        public static Texture2D magicPixel;
        public static void Initialize(Texture2D magicPixel)
        {
            Container.magicPixel = magicPixel;
        }

        public Container(Rectangle box, Color color)
        {
            this.box = box;
            this.color2 = color;
        }
        public bool LeftClick()
        {
            return box.Contains(Main.MouseScreen.ToPoint()) && Main.mouseLeft;
        }
        public bool RightClick()
        {
            return box.Contains(Main.MouseScreen.ToPoint()) && Main.mouseRight;
        }
        public bool HoverOver()
        {
            return box.Contains(Main.MouseScreen.ToPoint());
        }
        public bool HoverOverBugFix()
        {
            return boxBugFix.Contains(Main.MouseScreen.ToPoint());
        }
        public void UpdateInput(Player player, bool success, bool turnItemToAir = false, bool giveItemBack = false)
        {
            if (!success) 
                return;
            if (active)
            {
                if (!flag && HoverOver() && LeftClick())
                {
                    player.controlUseItem = false;
                    if (!string.IsNullOrEmpty(Main.mouseItem.Name))
                    {
                        content = Main.mouseItem.Clone();
                        content.SetDefaults(Main.mouseItem.type);
                        content.prefix = Main.mouseItem.prefix;
                        content.stack = Main.mouseItem.stack;
                        if (turnItemToAir)
                        { 
                            Main.mouseItem.TurnToAir();
                            Main.mouseItem.Refresh();
                        }
                        flag = true;
                        return;
                    }
                    if (string.IsNullOrEmpty(Main.mouseItem.Name) && content != null && !string.IsNullOrEmpty(content.Name) && string.IsNullOrEmpty(Main.mouseItem.Name))
                    {
                        if (giveItemBack)
                        { 
                            player.QuickSpawnItem(player.GetSource_DropAsItem(), content.type, content.stack);
                            //Main.mouseItem = content.Clone();
                            //Main.mouseItem.SetDefaults(content.type);
                            //Main.mouseItem.prefix = content.prefix;
                            //Main.mouseItem.stack = content.stack;
                        }
                        content.TurnToAir();
                    }
                    flag = true;
                }
                if (!LeftClick())
                    flag = false;
            }
        }
        public void Draw(Texture2D texture = null, Color color = default, bool drawStack = false)
        {
            sb.Draw(magicPixel, box, color);
            if (drawStack && content != null && content.stack > 0 && content.type != ItemID.None)
                Utils.DrawBorderString(sb, content.stack.ToString(), box.BottomRight(), Color.White, 0.5f);
            if (texture == null)
                return;
            sb.Draw(texture, box, Color.White);
        }
        public void DrawItem(Color color = default, bool drawStack = false)
        {
            Utils.DrawInvBG(sb, box, color);
            if (content != null && content.stack > 0 && content.type != ItemID.None)
            { 
                if (drawStack)
                { 
                    Utils.DrawBorderString(sb, content.stack.ToString(), box.BottomRight() - new Vector2(50 - 10, 24), Color.White, 0.8f);
                }
                Texture2D tex = TextureAssets.Item[content.type].Value;
                sb.Draw(tex, new Rectangle(box.Center.X - tex.Width / 2, box.Center.Y - tex.Height / 2, tex.Width, tex.Height), Color.White);
            }
        }*/
    }
    public class Button
    {
        public bool active = true;
        public bool innactiveDrawText = false;
        public bool drawMagicPixel = false;
        public string text = "";
        public string text2 = "";
        public int reserved;
        private int tick = 0;
        private Brush color = Brushes.DeepSkyBlue;
        private Brush color2 = Brushes.DodgerBlue;
        public int offX = 0;
        public int offY = 0;
        public static Texture2D magicPixel;
        public static void Initialize(Texture2D magicPixel)
        {
            Button.magicPixel = magicPixel;
        }
        public Rectangle boundCorrect => new Rectangle(box.X - box.Width + offX, box.Y - box.Height * 2 + offY, box.Width, box.Height);
        public Brush Select(Point MouseScreen, bool select = true)
        {
            if (select)
                return boundCorrect.Contains(MouseScreen) ? color : color2;
            else
            {
                return color2;
            }
        }
        public Rectangle box;
        public Texture2D texture;
        public bool LeftClick(Point MouseScreen, bool mouseLeft)
        {
            return active && box.Contains(MouseScreen) && mouseLeft;
        }
        public bool LeftClick(Rectangle hitbox, Point MouseScreen, bool mouseLeft)
        {
            return active && hitbox.Contains(MouseScreen) && mouseLeft;
        }
        public bool HoverOver(Point MouseScreen)
        {
            return box.Contains(MouseScreen);
        }
        public bool HoverOver(Rectangle bound, Point MouseScreen)
        {
            return box.Contains(MouseScreen);
        }
        public Button(string text, Rectangle box, Brush color)
        {
            this.color2 = color;
            if (texture == null)
                this.texture = magicPixel;
            this.text = text;
            this.box = box;
        }
        public Button(string text, Rectangle box, Texture2D texture = null)
        {
            this.texture = texture;
            if (texture == null)
                this.texture = magicPixel; 
            this.text = text;
            this.box = box;
        }
        [Obsolete("TODO: add button sound effects.")]
        public void HoverPlaySound(Point MouseScreen)
        {
            if (active && HoverOver(box, MouseScreen))
            { 
                if (tick == 0)
                {
                    //Terraria.Audio.SoundEngine.PlaySound(sound, position);
                    tick = 1;
                }
            }
            else tick = 0;
        }
        public void Draw(Graphics sb, Font font, Point MouseScreen, bool select = true)
        {
            if (!active) return;
            if (drawMagicPixel)
            { 
                sb.FillRectangle(Select(MouseScreen, select), box);
            }
            sb.DrawString(text, font, Brushes.White, new Point(box.X + 2 + offX, box.Y + 2 + offY));
        }
        /*
        public void Draw(Font font, bool select = true)
        {
            if (!active) return;
            if (font != null)
            { 
                sb.Draw(texture, box, color(select));
                sb.DrawString(font, text, new Vector2(box.X + 2 + offX, box.Y + 2 + offY), Color.White * 0.90f);
            }
            else
            {
                if (drawMagicPixel)
                {
                    sb.Draw(TextureAssets.MagicPixel.Value, box, color(select));
                }
                Utils.DrawBorderString(sb, text, new Vector2(box.X + 2 + offX, box.Y + 2 + offY), Color.White * 0.90f);
            }
        } */
        /*
        public void Draw(DynamicSpriteFont font, bool select = true)
        {
            if (!active)
            {
                if (innactiveDrawText) 
                { 
                    innactiveDrawFont(font, select);
                }
                return;
            }
            drawFont(font, select);
        }
        private void innactiveDrawFont(DynamicSpriteFont font, bool select)
        {
            if (font != null) 
            {
                if (text != string.Empty)
                     sb.DrawString(font, text, new Vector2(box.X + 2 + offX, box.Y + 2 + offY), Color.White * 0.90f);
                else sb.DrawString(font, text2, new Vector2(box.X + 2 + offX, box.Y + 2 + offY), Color.White * 0.90f);
            }
            else 
            {
                if (text != string.Empty)
                     Utils.DrawBorderString(sb, text, new Vector2(box.X + 2 + offX, box.Y + 2 + offY), Color.White * 0.90f);
                else Utils.DrawBorderString(sb, text2, new Vector2(box.X + 2 + offX, box.Y + 2 + offY), Color.White * 0.90f);
            }
        }
        private void drawFont(DynamicSpriteFont font, bool select)
        {
            if (font != null)
            {
                sb.Draw(texture, box, color(select));
                if (text != string.Empty)
                     sb.DrawString(font, text, new Vector2(box.X + 2 + offX, box.Y + 2 + offY), Color.White * 0.90f);
                else sb.DrawString(font, text2, new Vector2(box.X + 2 + offX, box.Y + 2 + offY), Color.White * 0.90f);
            }                                
            else
            {
                if (drawMagicPixel)
                {
                    sb.Draw(TextureAssets.MagicPixel.Value, box, color(select));
                }
                if (text != string.Empty)
                     Utils.DrawBorderString(sb, text, new Vector2(box.X + 2 + offX, box.Y + 2 + offY), Color.White * 0.90f); 
                else Utils.DrawBorderString(sb, text2, new Vector2(box.X + 2 + offX, box.Y + 2 + offY), Color.White * 0.90f);
            }
        }*/
    }
    [Obsolete("No current ways to capture keyboard inputs in a textbox style yet.")]
    public class InputBox
    {
        /*
        public bool active;
        public string text = "";
        public Color color
        {
            get { return active ? Color.DodgerBlue * 0.67f : Color.DodgerBlue * 0.33f; }
        }
        public Rectangle box;
        private KeyboardState oldState;
        private KeyboardState keyState
        {
            get { return Keyboard.GetState(); }
        }
        private SpriteBatch sb
        {
            get { return Main.spriteBatch; }
        }
        public InputBox(Rectangle box)
        {
            this.box = box;
        }
        public bool LeftClick()
        {
            return box.Contains(Main.MouseScreen.ToPoint()) && Main.mouseLeft;
        }
        public bool HoverOver()
        {
            return box.Contains(Main.MouseScreen.ToPoint());
        }
        public void UpdateInput()
        {
            if (active)
            {
                foreach (Keys key in keyState.GetPressedKeys())
                {
                    if (oldState.IsKeyUp(key))
                    {
                        if (key == Keys.F3)
                            return;
                        if (key == Keys.Back)
                        {
                            if (text.Length > 0)
                                text = text.Remove(text.Length - 1);
                            oldState = keyState;
                            return;
                        }
                        else if (key == Keys.Space)
                            text += " ";
                        else if (key == Keys.OemPeriod)
                            text += ".";
                        else if (text.Length < 24 && (key.ToString().StartsWith('D') || key.ToString().Length == 1))
                        {
                            string n = key.ToString().ToLower();
                            if (n.StartsWith("d") && n.Length == 2)
                                n = n.Substring(1);
                            text += n;
                        }
                    }
                }
                oldState = keyState;
            }
        }
        public void DrawText(Texture2D background, SpriteFont font, bool drawMagicPixel = false)
        {
            if (background != null && font != null)
            { 
                sb.Draw(background, box, color);
                sb.DrawString(font, text, new Vector2(box.X + 2, box.Y + 1), Color.White);
            }
            else
            {
                if (drawMagicPixel)
                { 
                    sb.Draw(TextureAssets.MagicPixel.Value, box, color);
                }
                Utils.DrawBorderString(sb, text, new Vector2(box.X + 2, box.Y + 1), Color.White);
            }
        }          */
    }
}