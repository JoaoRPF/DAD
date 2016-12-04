using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADStorm
{
    class Duplicate : Operator
    {
        private List<string[]> tuples = new List<string[]>();

        public override void execute()
        {
            Console.WriteLine("Execute Duplicate");

            while (true)
            {
                base.execute();
                Tup inputTuple;
                if (this.inputTuples.Count != 0)
                {
                    checkSleeping();
                    lock (this.inputTuples)
                    {
                        inputTuple = (Tup)this.inputTuples[0].Clone();
                    }
                    Console.WriteLine("Processing tuple -> " + constructTuple(inputTuple));
                    lock (this.outputTuples)
                    {
                        outputTuples.Add(inputTuple);
                        _operator.outputTuples.Sort((s1, s2) => s1.id.CompareTo(s2.id));
                    }
                    lock (this.inputTuples)
                    {
                        inputTuples.RemoveAt(0);
                    }
                }
            }
        }
    }
}
