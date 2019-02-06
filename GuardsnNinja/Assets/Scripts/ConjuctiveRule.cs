using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZeroOrderTSK
{
    class ConjuctiveRule : Rule
    {
        public ConjuctiveRule() : base()
        {

        }

        public override void AddTerm(Term term)
        {
            terms.Add(term);
        }

        public override double GetFiringStrength()
        {
            double result = 1.0F;
            foreach (Term term in terms)
            {
                double termMembership = term.GetMembership();
                if(termMembership < result)
                {
                    result = termMembership;
                }
            }
            return result;
        }

        public override double GetOutputLevel()
        {
            return OutputLevel;
        }
    }
}
