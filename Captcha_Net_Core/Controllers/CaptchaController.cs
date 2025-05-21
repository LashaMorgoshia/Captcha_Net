using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using static System.Net.Mime.MediaTypeNames;
using Font = System.Drawing.Font;

namespace Captcha_Net_Core.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CaptchaController : ControllerBase
    {
        private static readonly Random _rand = new Random();

        [HttpGet]
        public ActionResult Image()
        {
            string code = GenerateRandomCode(6);
            HttpContext.Session.SetString("Captcha", code);

            int width = 280; // wider to fit 6 chars comfortably
            int height = 60;
            using (Bitmap bmp = new Bitmap(width, height))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Gradient background
                var rect = new Rectangle(0, 0, width, height);
                using (var brush = new LinearGradientBrush(rect, Color.LightGray, Color.White, 45f))
                {
                    g.FillRectangle(brush, rect);
                }

                Random _rand = new Random();

                // Noise lines (40)
                for (int i = 0; i < 40; i++)
                {
                    int x1 = _rand.Next(width);
                    int y1 = _rand.Next(height);
                    int x2 = _rand.Next(width);
                    int y2 = _rand.Next(height);
                    using (var pen = new Pen(Color.FromArgb(_rand.Next(30, 100), _rand.Next(255), _rand.Next(255), _rand.Next(255))))
                    {
                        g.DrawLine(pen, x1, y1, x2, y2);
                    }
                }

                // Noise arcs (15)
                for (int i = 0; i < 15; i++)
                {
                    int x = _rand.Next(width);
                    int y = _rand.Next(height);
                    int w = _rand.Next(10, 30);
                    int h = _rand.Next(10, 30);
                    int startAngle = _rand.Next(360);
                    int sweepAngle = _rand.Next(30, 180);

                    using (var pen = new Pen(Color.FromArgb(_rand.Next(20, 80), _rand.Next(255), _rand.Next(255), _rand.Next(255)), 1f))
                    {
                        g.DrawArc(pen, x, y, w, h, startAngle, sweepAngle);
                    }
                }

                string[] fonts = { "Arial", "Georgia", "Tahoma", "Comic Sans MS", "Verdana" };
                int charX = 5;

                for (int i = 0; i < code.Length; i++)
                {
                    string c = code[i].ToString();

                    string fontName = fonts[_rand.Next(fonts.Length)];
                    int fontSize = _rand.Next(18, 25);

                    using (Font font = new Font(fontName, fontSize, FontStyle.Bold))
                    {
                        float angle = _rand.Next(-60, 60);

                        SizeF charSize = g.MeasureString(c, font);
                        int bmpSize = (int)Math.Ceiling(Math.Sqrt(charSize.Width * charSize.Width + charSize.Height * charSize.Height)) + 4;

                        using (Bitmap charBmp = new Bitmap(bmpSize, bmpSize))
                        using (Graphics charG = Graphics.FromImage(charBmp))
                        {
                            charG.Clear(Color.Transparent);
                            charG.SmoothingMode = SmoothingMode.AntiAlias;

                            Color lightColor = Color.FromArgb(
    _rand.Next(1, 90),            // very low alpha for faintness
    _rand.Next(200, 255),        // R pastel range
    _rand.Next(200, 255),        // G pastel range
    _rand.Next(200, 255)         // B pastel range
);

                            using (Brush brush = new SolidBrush(lightColor))
                            {
                                for (int offsetX = -1; offsetX <= 1; offsetX++)
                                {
                                    for (int offsetY = -1; offsetY <= 1; offsetY++)
                                    {
                                        if (offsetX == 0 && offsetY == 0) continue;
                                        charG.DrawString(c, font, brush, 2 + offsetX * 0.3f, 2 + offsetY * 0.3f);
                                    }
                                }
                                //charG.DrawString(c, font, brush, 2, 2);
                                charG.DrawString(c, font, brush, 2, 2);
                            }

                            Bitmap rotated = RotateImage(charBmp, angle);
                            g.DrawImage(rotated, charX, (height - rotated.Height) / 2);

                            charX += rotated.Width - 15;
                            rotated.Dispose();
                        }
                    }
                }

                // Noise dots (300)
                for (int i = 0; i < 300; i++)
                {
                    int x = _rand.Next(width);
                    int y = _rand.Next(height);
                    Color dotColor = Color.FromArgb(_rand.Next(30, 80), _rand.Next(255), _rand.Next(255), _rand.Next(255));
                    bmp.SetPixel(x, y, dotColor);
                }

                // Noise ellipses (30)
                for (int i = 0; i < 30; i++)
                {
                    int x = _rand.Next(width);
                    int y = _rand.Next(height);
                    int w = _rand.Next(2, 6);
                    int h = _rand.Next(2, 6);

                    using (var brush = new SolidBrush(Color.FromArgb(_rand.Next(20, 70), _rand.Next(255), _rand.Next(255), _rand.Next(255))))
                    {
                        g.FillEllipse(brush, x, y, w, h);
                    }
                }

                Bitmap distorted = TwistImage(bmp, true, 6, 4);
                using (var ms = new System.IO.MemoryStream())
                {
                    distorted.Save(ms, ImageFormat.Png);
                    distorted.Dispose();
                    return File(ms.ToArray(), "image/png");
                }
            }
        }

        private static string GenerateRandomCode(int length)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no confusing chars
            char[] codeChars = new char[length];
            for (int i = 0; i < length; i++)
                codeChars[i] = chars[_rand.Next(chars.Length)];
            return new string(codeChars);
        }

        // Rotate an image around its center
        private static Bitmap RotateImage(Bitmap bmp, float angle)
        {
            Bitmap rotated = new Bitmap(bmp.Width, bmp.Height);
            rotated.SetResolution(bmp.HorizontalResolution, bmp.VerticalResolution);

            using (Graphics g = Graphics.FromImage(rotated))
            {
                g.Clear(Color.Transparent);
                g.TranslateTransform(bmp.Width / 2f, bmp.Height / 2f);
                g.RotateTransform(angle);
                g.TranslateTransform(-bmp.Width / 2f, -bmp.Height / 2f);
                g.DrawImage(bmp, 0, 0);
            }
            return rotated;
        }

        // Apply wave distortion to image (horizontal or vertical)
        private static Bitmap TwistImage(Bitmap srcBmp, bool horizontal, double amplitude, double phase)
        {
            Bitmap destBmp = new Bitmap(srcBmp.Width, srcBmp.Height);
            destBmp.SetResolution(srcBmp.HorizontalResolution, srcBmp.VerticalResolution);

            double baseAxisLen = horizontal ? destBmp.Height : destBmp.Width;

            for (int x = 0; x < destBmp.Width; x++)
            {
                for (int y = 0; y < destBmp.Height; y++)
                {
                    double dx = 0;
                    if (horizontal)
                    {
                        dx = (double)(amplitude * Math.Sin(2 * Math.PI * y / baseAxisLen + phase));
                    }
                    else
                    {
                        dx = (double)(amplitude * Math.Sin(2 * Math.PI * x / baseAxisLen + phase));
                    }

                    int newX = horizontal ? x + (int)dx : x;
                    int newY = horizontal ? y : y + (int)dx;

                    if (newX >= 0 && newX < destBmp.Width && newY >= 0 && newY < destBmp.Height)
                    {
                        destBmp.SetPixel(newX, newY, srcBmp.GetPixel(x, y));
                    }
                }
            }

            return destBmp;
        }
    }
}
