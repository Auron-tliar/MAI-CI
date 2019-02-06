using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZeroOrderTSK
{
    class GuardInferenceSystem
    {

        private FuzzyVariable hearing;
        private FuzzyVariable vision;
        private RuleBlock guardRB;

        public GuardInferenceSystem()
        {
            hearing = new FuzzyVariable("Hearing");
            vision = new FuzzyVariable("Vision");

            TrapezoidTerm hearingNone = new TrapezoidTerm("None", 0, 0, 15, 25);
            TrapezoidTerm hearingLow = new TrapezoidTerm("Low", 15, 25, 45, 55);
            TrapezoidTerm hearingMedium = new TrapezoidTerm("Medium", 45, 55, 75, 85);
            TrapezoidTerm hearingHigh = new TrapezoidTerm("High", 75, 85, 100, 100);

            hearing.AddTerm(hearingNone);
            hearing.AddTerm(hearingLow);
            hearing.AddTerm(hearingMedium);
            hearing.AddTerm(hearingHigh);

            TrapezoidTerm visionLow = new TrapezoidTerm("Low", 0, 0, 20, 30);
            TrapezoidTerm visionMedium = new TrapezoidTerm("Medium", 20, 30, 50, 80);
            TrapezoidTerm visionHigh = new TrapezoidTerm("High", 50, 80, 100, 100);

            vision.AddTerm(visionLow);
            vision.AddTerm(visionMedium);
            vision.AddTerm(visionHigh);

            guardRB = new RuleBlock("Guard Perception Rule Block");

            ConjuctiveRule R1 = new ConjuctiveRule();
            R1.AddTerm(visionLow);
            R1.AddTerm(hearingNone);
            R1.SetOutputLevel(2);

            ConjuctiveRule R2 = new ConjuctiveRule();
            R2.AddTerm(visionLow);
            R2.AddTerm(hearingLow);
            R2.SetOutputLevel(3);

            ConjuctiveRule R3 = new ConjuctiveRule();
            R3.AddTerm(visionLow);
            R3.AddTerm(hearingMedium);
            R3.SetOutputLevel(4);

            ConjuctiveRule R4 = new ConjuctiveRule();
            R4.AddTerm(visionLow);
            R4.AddTerm(hearingHigh);
            R4.SetOutputLevel(5);

            ConjuctiveRule R5 = new ConjuctiveRule();
            R5.AddTerm(visionMedium);
            R5.AddTerm(hearingNone);
            R5.SetOutputLevel(4);

            ConjuctiveRule R6 = new ConjuctiveRule();
            R6.AddTerm(visionMedium);
            R6.AddTerm(hearingLow);
            R6.SetOutputLevel(5);

            ConjuctiveRule R7 = new ConjuctiveRule();
            R7.AddTerm(visionMedium);
            R7.AddTerm(hearingMedium);
            R7.SetOutputLevel(6);

            ConjuctiveRule R8 = new ConjuctiveRule();
            R8.AddTerm(visionMedium);
            R8.AddTerm(hearingHigh);
            R8.SetOutputLevel(7);

            ConjuctiveRule R9 = new ConjuctiveRule();
            R9.AddTerm(visionHigh);
            R9.AddTerm(hearingNone);
            R9.SetOutputLevel(6);

            ConjuctiveRule R10 = new ConjuctiveRule();
            R10.AddTerm(visionHigh);
            R10.AddTerm(hearingLow);
            R10.SetOutputLevel(7);

            ConjuctiveRule R11 = new ConjuctiveRule();
            R11.AddTerm(visionHigh);
            R11.AddTerm(hearingMedium);
            R11.SetOutputLevel(8);

            ConjuctiveRule R12 = new ConjuctiveRule();
            R12.AddTerm(visionHigh);
            R12.AddTerm(hearingHigh);
            R12.SetOutputLevel(9);

            guardRB.AddRule(R1);
            guardRB.AddRule(R2);
            guardRB.AddRule(R3);
            guardRB.AddRule(R4);
            guardRB.AddRule(R5);
            guardRB.AddRule(R6);
            guardRB.AddRule(R7);
            guardRB.AddRule(R8);
            guardRB.AddRule(R9);
            guardRB.AddRule(R10);
            guardRB.AddRule(R11);
            guardRB.AddRule(R12);
        }

        // takes two values from 0 to 100 and outputs a value from 2 to 9
        public double getAlarm(double hearingVal, double visionVal)
        {
            hearing.SetValue(hearingVal);
            vision.SetValue(visionVal);
            return guardRB.FinalOutput();
        }
    }
}
