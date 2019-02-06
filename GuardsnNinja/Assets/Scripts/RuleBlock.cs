using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZeroOrderTSK
{
    class RuleBlock
    {
        private string name;
        private ArrayList rules;

        public RuleBlock(string name)
        {
            rules = new ArrayList();
            this.name = name;
        }
        public void AddRule(Rule rule)
        {
            rules.Add(rule);
        }
        public double FinalOutput()
        {
            double wzSum = 0;
            double wSum = 0;

            foreach(Rule rule in rules)
            {
                wzSum += rule.GetFiringStrength() * rule.GetOutputLevel();
                wSum += rule.GetFiringStrength();
            }
            return wzSum / wSum;
        }
    }
}
