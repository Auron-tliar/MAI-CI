using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZeroOrderTSK
{
    abstract class Term : MembershipFunction
    {
        protected string name;
        protected FuzzyVariable variable;

        public abstract double GetMembership();

        public void SetVariable(FuzzyVariable variable)
        {
            this.variable = variable;
        }

        public override string ToString()
        {
            return variable.GetName() + "." + name;
        }
    }
}
