using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace DADSTORM
{
    class Uniq : Operator
    {
        private List<string[]> uniqTuples = new List<string[]>();

        public override void execute()
        {
           
            Console.WriteLine("Execute Uniq");

            while (true)
            {
                base.execute();
                string[] inputTuple;
                if (this.inputTuples.Count != 0)
                {
                    checkSleeping();
                    lock (this.inputTuples)
                    {
                        inputTuple = (string[])this.inputTuples[0].Clone();
                    }
                    Console.WriteLine("Processing tuple -> " + constructTuple(inputTuple));
                    if (!uniqTuples.Exists(x => x[this.fieldNumber - 1].Equals(inputTuple[this.fieldNumber - 1]))) //MAIS ORGULHO
                    {
                        uniqTuples.Add(inputTuple);
                        lock (this.outputTuples)
                        {
                            outputTuples.Add(inputTuple);
                        }
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
