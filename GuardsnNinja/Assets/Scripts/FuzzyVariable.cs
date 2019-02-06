using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZeroOrderTSK
{
    class FuzzyVariable
    {
        private string name;
        private ArrayList terms;
        private double value;

        public FuzzyVariable(string name)
        {
            terms = new ArrayList();
            this.name = name;
        }

        public void AddTerm(Term term)
        {
            terms.Add(term);
            term.SetVariable(this);
        }

        public void SetValue(double value)
        {
            this.value = value;
        }

        public double GetValue()
        {
            return value;
        }

        public string GetName()
        {
            return name;
        }

        public override string ToString()
        {
            string str = "";
            str += name + ": " + value.ToString() + "\n";
            foreach(Term term in terms)
            {
                str += term.ToString() + "\n";
            }
            return str;
        }
    }
}
