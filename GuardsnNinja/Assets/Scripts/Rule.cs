using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZeroOrderTSK
{
    abstract class Rule
    {

        protected ArrayList terms;
        protected double OutputLevel;

        protected Rule()
        {
            terms = new ArrayList();
        }

        public abstract double GetOutputLevel();
        public abstract double GetFiringStrength();
        public abstract void AddTerm(Term term);
        public void SetOutputLevel(double z)
        {
            OutputLevel = z;
        }

    }
}
