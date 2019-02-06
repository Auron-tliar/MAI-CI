using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZeroOrderTSK
{
    class TrapezoidTerm : Term
    {
        int a, b, c, d;

        public TrapezoidTerm(string name, int a, int b, int c, int d)
        {
            this.name = name;
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }

        public override double GetMembership()
        {
            double value = variable.GetValue();
            if(HighestTerm(value))
            {
                return 1;
            }else if(DecreasingTerm(value))
            {
                return 1 - (((double)value - a) / ((double)b - a));
            }
            else if (IncreasingTerm(value))
            {
                return ((double)value - a) / ((double)b - a);
            }
            else
            {
                return 0;
            }
        }

        private bool HighestTerm(double value)
        {
            return value >= b && value <= c;
        }
        private bool IncreasingTerm(double value)
        {
            return value > a && value < b;
        }
        private bool DecreasingTerm(double value)
        {
            return value > c && value < d;
        }

        public override string ToString()
        {
            return name + " (" + a.ToString() + ", " + b.ToString() + ", " + c.ToString() + ", " + d.ToString() + ")";
        }
    }
}
