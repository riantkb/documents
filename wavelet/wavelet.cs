using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace wavelet
{
    class Program
    {
        static void Main()
        {
            int lev = 10;
            using (Bitmap img = new Bitmap(@"test.jpg"))
            {
                var format = img.RawFormat;
                var col = Col.conv(img);
                var wavelet = Col.transform(col, lev);
                var wl_img = Col.conv(wavelet);
                wl_img.Save("wavelet.jpg", format);
                wavelet = Col.threshold(wavelet, 1);
                var tlansed = Col.inv_transform(wavelet, lev);
                var new_img = Col.conv(tlansed);
                new_img.Save("new_img.jpg", format);
            }
        }
    }
    class Col {
        double r, g, b;
        public Col() {}
        public Col(double r, double g, double b) {
            this.r = r;
            this.g = g;
            this.b = b;
        }
        public Col(Col c) : this(c.r, c.g, c.b) {}
        public Col(Color c) : this(c.R, c.G, c.B) {}
        public Color conv() => Color.FromArgb(Col.f(r), Col.f(g), Col.f(b));
        public static int f(double d) {
            // return (int)Math.Max(0, Math.Min(255, d));
            return (int)d & 255;
        }
        public static Col[][] copy(Col[][] c) {
            var ret = new Col[c.Length][];
            for (int i = 0; i < c.Length; i++)
            {
                ret[i] = new Col[c[i].Length];
                for (int j = 0; j < c[i].Length; j++)
                    ret[i][j] = new Col(c[i][j]);
            }
            return ret;
        }
        public static Col[][] conv(Bitmap b) {
            var ret = new Col[b.Height][];
            for (int i = 0; i < b.Height; i++)
            {
                ret[i] = new Col[b.Width];
                for (int j = 0; j < b.Width; j++)
                    ret[i][j] = new Col(b.GetPixel(j, i));
            }
            return ret;
        }
        public static Bitmap conv(Col[][] c) {
            var ret = new Bitmap(c[0].Length, c.Length);
            for (int i = 0; i < c.Length; i++)
                for (int j = 0; j < c[i].Length; j++)
                    ret.SetPixel(j, i, c[i][j].conv());

            return ret;
        }
        public static Col[][] threshold(Col[][] c, int p) {
            int up = c.Length * c[0].Length * 3 * p / 100;
            var all = new List<double>();
            for (int i = 0; i < c.Length; i++)
            {
                for (int j = 0; j < c[i].Length; j++)
                {
                    all.Add(c[i][j].r * c[i][j].r);
                    all.Add(c[i][j].g * c[i][j].g);
                    all.Add(c[i][j].b * c[i][j].b);
                }
            }
            all.Sort();
            all.Reverse();
            var th = up < all.Count ? all[up] : -1;
            var ret = Col.copy(c);
            for (int i = 0; i < c.Length; i++)
            {
                for (int j = 0; j < c[i].Length; j++)
                {
                    if (ret[i][j].r * ret[i][j].r < th)
                        ret[i][j].r = 0;
                    if (ret[i][j].g * ret[i][j].g < th)
                        ret[i][j].g = 0;
                    if (ret[i][j].b * ret[i][j].b < th)
                        ret[i][j].b = 0;
                }
            }
            return ret;
        }
        public static Col operator+(Col a, Col b) => new Col(a.r + b.r, a.g + b.g, a.b + b.b);
        public static Col operator-(Col a, Col b) => new Col(a.r - b.r, a.g - b.g, a.b - b.b);
        public static Col operator*(Col a, double d) => new Col(a.r * d, a.g * d, a.b * d);
        public static Col operator/(Col a, double d) => a * (1.0 / d);

        public static Col[][] transform(Col[][] inp, int level) {
            int h = inp.Length, w = inp[0].Length;
            var col = Col.copy(inp);
            var tmp = Col.copy(inp);
            for (int lev = 0; lev < level; lev++)
            {
                for (int i = 0; i < (h >> lev + 1); i++)
                {
                    for (int j = 0; j < (w >> lev + 1); j++)
                    {
                        var d00 = col[i * 2][j * 2];
                        var d10 = col[i * 2 + 1][j * 2];
                        var d01 = col[i * 2][j * 2 + 1];
                        var d11 = col[i * 2 + 1][j * 2 + 1];
                        tmp[i][j] = (d00 + d10 + d01 + d11) / 2.0;
                        tmp[i + (h >> lev + 1)][j] = (d00 - d10 + d01 - d11) / 2.0;
                        tmp[i][j + (w >> lev + 1)] = (d00 + d10 - d01 - d11) / 2.0;
                        tmp[i + (h >> lev + 1)][j + (w >> lev + 1)] = (d00 - d10 - d01 + d11) / 2.0;
                    }
                }
                for (int i = 0; i < (h >> lev); i++)
                    for (int j = 0; j < (w >> lev); j++)
                        col[i][j] = new Col(tmp[i][j]);
            }
            return col;
        }
        public static Col[][] inv_transform(Col[][] inp, int level) {
            int h = inp.Length, w = inp[0].Length;
            var col = Col.copy(inp);
            var tmp = Col.copy(inp);
            for (int lev = level - 1; lev >= 0; lev--)
            {
                for (int i = 0; i < (h >> lev + 1); i++)
                {
                    for (int j = 0; j < (w >> lev + 1); j++)
                    {
                        var n00 = col[i][j];
                        var n10 = col[i + (h >> lev + 1)][j];
                        var n01 = col[i][j + (w >> lev + 1)];
                        var n11 = col[i + (h >> lev + 1)][j + (w >> lev + 1)];

                        tmp[i * 2][j * 2] = (n00 + n10 + n01 + n11) / 2.0;
                        tmp[i * 2 + 1][j * 2] = (n00 - n10 + n01 - n11) / 2.0;
                        tmp[i * 2][j * 2 + 1] = (n00 + n10 - n01 - n11) / 2.0;
                        tmp[i * 2 + 1][j * 2 + 1] = (n00 - n10 - n01 + n11) / 2.0;
                    }
                }
                for (int i = 0; i < (h >> lev); i++)
                    for (int j = 0; j < (w >> lev); j++)
                        col[i][j] = new Col(tmp[i][j]);
            }
            return col;
        }
    }
}
