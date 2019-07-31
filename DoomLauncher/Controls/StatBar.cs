﻿using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DoomLauncher
{
    class StatBar : UserControl
    {
        private int m_count, m_total, m_maxHeight;
        private string m_text;

        public StatBar()
        {
            Paint += StatBar_Paint;
            SizeChanged += StatBar_SizeChanged;
        }

        //this is to get around some sort of bug with the designer initilization blowing away the max size somewhere...
        private void StatBar_SizeChanged(object sender, EventArgs e)
        {
            if (m_maxHeight == 0)
                m_maxHeight = MaximumSize.Height;
            if (Height > m_maxHeight)
                Height = m_maxHeight;
        }

        public void SetStats(int count, int total, string text)
        {
            m_count = count;
            m_total = total;
            m_text = text;
        }

        private void StatBar_Paint(object sender, PaintEventArgs e)
        {
            if (m_text != null)
                DrawProgress(new Point(0, 0), m_count, m_total, m_text);
        }

        private void DrawProgress(Point pt, int count, int total, string text)
        {
            int height = Height - 1;
            int width = Width - 1;
            Graphics g = this.CreateGraphics();
            Pen pen = new Pen(Color.Black, 1.0f);
            Rectangle rect = new Rectangle(pt, new Size(width, height));

            g.DrawRectangle(pen, rect);

            double percent = 0;
            if (total > 0)
                percent = count / (double)total;
            width = (int)((Width - 1) * percent);

            pt.Offset(1, 1);
            rect = new Rectangle(pt, new Size(rect.Width - 1, rect.Height - 1));
            Brush bgBrush = new LinearGradientBrush(rect, Color.DarkGray, Color.LightGray, 90.0f);
            Rectangle percentRect = new Rectangle(rect.Location, new Size(width, rect.Height));
            Brush brush = GetPercentBrush(rect, percent);

            g.FillRectangle(bgBrush, rect);
            if (percent > 0)
            {
                g.FillRectangle(brush, percentRect);
                pt.Offset(-1, -1);
                g.DrawRectangle(GetPrecentPen(percent), new Rectangle(pt, new Size(percentRect.Width + 1, percentRect.Height + 1)));
            }

            Brush fontBrush = new SolidBrush(Color.Black);
            g.DrawString(text, new Font(FontFamily.GenericSerif, 10.0f, FontStyle.Bold), fontBrush, new PointF(pt.X + 8, pt.Y + 2.5f));
        }

        private static Brush GetPercentBrush(Rectangle rect, double percent)
        {
            if (percent >= 1.0)
                return new LinearGradientBrush(rect, Color.LightGreen, Color.Green, LinearGradientMode.ForwardDiagonal);
            else
                return new LinearGradientBrush(rect, Color.LightBlue, Color.Blue, LinearGradientMode.ForwardDiagonal);
        }

        private static Pen GetPrecentPen(double percent)
        {
            if (percent >= 1.0)
                return new Pen(Color.Green);
            else
                return new Pen(Color.Blue);
        }
    }
}
