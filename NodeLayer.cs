using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Learnr
{
    public class NodeLayer : RenderObject
    {
        private static bool defSet = false;
        private static UiSettings defval_ui;
        private static float defval_x;
        private static float defval_rad;
        private static Color defval_col;

        private static float defvaldist = 40;

        public int membersCount = -1;
        public new RectangleF rect { get { return new RectangleF(mx, my - mh / 2, mw, mh); } }


        private double[] members;
        private double[] pmembers;
        private double pmax, pmin;
        
        public int Count { get; set; }
        private int fbnodescount;
        private bool heatMapRender = false;

        public NodeLayer(UiSettings uiSettings, int membersCount) : base(uiSettings)
        {
            this.membersCount = membersCount;
            members = new double[membersCount];
            pmembers = new double[membersCount];
            if (defSet)
            {
                x = defval_x;
                mw = defvaldist;
                defval_x += defvaldist;
                radius = defval_rad;
                color = defval_col;
                brush = new SolidBrush(color);
                pen = new Pen(brush);
            }
        }

        public NodeLayer(int membersCount, int fbnodescount) : this(membersCount)
        {
            this.fbnodescount = fbnodescount;
        }

        public NodeLayer(int membersCount) : base(defval_ui)
        {
            this.membersCount = membersCount;
            members = new double[membersCount];
            pmembers = new double[membersCount];
            if (defSet)
            {
                x = defval_x;
                defval_x += defvaldist;
                radius = defval_rad;
                color = defval_col;
                brush = new SolidBrush(color);
                pen = new Pen(brush);
            }
        }

        public NodeLayer(UiSettings uiSettings, float x, float radius, Color color) : base(uiSettings, x, radius, color)
        {

        }

        public static void SetDefaultProps(ref UiSettings uiSettings, float x, float radius, Color color)
        {
            defSet = true;
            defval_ui = uiSettings;
            defval_x = x;
            defval_rad = radius;
            defval_col = color;
        }

        public override void UpdateElement(int delta)
        {
        }

        public void UpdateElement(int delta, ref NeuralNetworkLSTM net, bool drawlines)
        {
            if (playState != 0)
            {
                gameTime += playState * delta / 1000.0f;
                my += dy;
            }
        }

        public PointF GetNodePosition(int node)
        {
            float ih = 3.0f * radius;
            float hh = (membersCount - 1) * ih / 2;
            mh = hh * 2;
            return new PointF(mx, my - hh + ih * node);
        }

        
        public void DrawElement(Graphics g, NeuralNetworkLSTM net, bool drawlines, bool linesdynamic, bool heatMap)
        {
            if (net.nodes != null)
                Array.Copy(net.GetNodes(Index), members, membersCount);
            heatMapRender = heatMap && Index != 0 && Index != Count - 1;
            if (drawlines && Index < net.Size - 1 && net.nodes != null)
            {
                Pen p = Pens.Red;
                double el_min = -1;
                double el_range = -1;
                if (linesdynamic)
                {
                    if (heatMap)
                    {
                        CalcHeatMapRange(false, false);
                        el_min = pmin;
                        el_range = pmax - el_min + 0.001;
                    }
                    else
                    {
                        el_min = members.Min();
                        el_range = members.Max() - el_min + 0.001;
                    }
                }
                el_range *= 1.5;
                el_min -= el_range * 0.25;

                for (int i = 0; i < membersCount; i++)
                {
                    double[] lines = net.GetAxons(Index, i);
                    double min = lines.Min();
                    double range = lines.Max() - min + 0.001;
                    double el_val = -1;
                    if (linesdynamic)
                    {
                        if (heatMap) el_val = Math.Min(1, Math.Max(0, ((members[i] - pmembers[i]) - el_min) / el_range));
                        else el_val = Math.Min(1, Math.Max(0, (members[i] - el_min) / el_range));
                    }

                    for (int ii = 0; ii < lines.Length; ii++)
                    {
                        if (lines[ii] > 0)
                            p = new Pen(Color.FromArgb(
                                (int)(255 * (linesdynamic ? el_val : 1) * (lines[ii] - min) / range),
                                255, 115, 50));
                        else
                            p = new Pen(Color.FromArgb(
                                (int)(255 * (linesdynamic ? el_val : 1) * (lines[ii] - min) / range)
                                , 50, 110, 255));
                        g.DrawLine(p, GetNodePosition(i), net.nodes[Index + 1].GetNodePosition(ii));
                    }
                }
            }
            DrawElement(g);
            Array.Copy(members, pmembers, membersCount);
        }

        public override void DrawElement(Graphics g)
        {
            float ih = 3.0f * radius;
            float hh = (membersCount - 1) * ih / 2;
            float coldim = GlobalVars.R.nodelayer_colourdim;
            Brush b = (Brush)brush.Clone();
            double el_min, el_range;
            if (heatMapRender)
            {
                el_min = pmin;
                el_range = pmax - el_min;

                el_min = 0;
                el_range = Math.Max(pmax * pmax, pmin * pmin);
            }
            else
            {
                el_min = members.Min();
                el_range = members.Max() - el_min;
            }
            el_range *= 1.5;
            el_min -= el_range * 0.25;
            for (int i = 0; i < membersCount; i++)
            {
                int brightness;
                if (heatMapRender)
                {
                    double v = (members[i] - pmembers[i]);
                    v *= v;
                    brightness = (int)(255 * (v - el_min) / el_range);
                }
                else brightness = (int)(255 * (members[i] - el_min) / el_range);
                if (el_range == 0) brightness = 0;
                if (heatMapRender) b = new SolidBrush(Color.FromArgb(255, (int)(brightness * coldim), brightness, brightness));
                else b = new SolidBrush(Color.FromArgb(255, brightness, brightness, brightness));
                if ((Index == 0 && i > membersCount - 2 - fbnodescount && i < membersCount - 1) ||
                    (Index == Count - 1 && i > membersCount - 1 - fbnodescount))
                {
                    if (heatMapRender) brightness = (int)(255 * Math.Max(0, Math.Min(1, members[i] - pmembers[i])));
                    else brightness = (int)(255 * Math.Max(0, Math.Min(1, members[i])));
                    b = new SolidBrush(Color.FromArgb(255, brightness, (int)(brightness * coldim * 1.1f), (int)(brightness * coldim)));
                }
                g.FillRectangle(b, mx - radius, my - hh + ih * i + -radius, 2 * radius, 2 * radius);
            }
        }

        public void CalcHeatMapRange(bool holdPeaks, bool square)
        {
            double diff = members[0] - pmembers[0];
            double max = diff, min = diff;
            for (int i = 1; i < membersCount; i++)
            {
                diff = members[i] - pmembers[i];
                if (square) diff *= diff;
                if (diff > max) max = diff;
                if (diff < min) min = diff;
            }
            if (holdPeaks)
            {
                if (max > pmax) pmax = max;
                if (min < pmin) pmin = min;
            }
            else
            {
                pmax = max;
                pmin = min;
            }
        }

        public override void Reset()
        {

        }
    }

    public class NodeLayerCollection : List<NodeLayer>
    {
        public NodeLayerCollection() : base()
        {

        }
    }

}
