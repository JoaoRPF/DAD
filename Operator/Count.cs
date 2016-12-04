using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADStorm
{
    class Count : Operator 
    {
        private int count = 0;
        public override void execute()
        {
            Console.WriteLine("Execute Count");

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
                    count++;
                    Console.WriteLine("Number of Tuples = " + count);
                    
                    lock (this.outputTuples)
                    {
                        int oldID = inputTuple.id;
                        outputTuples.Add(new Tup(oldID, new string[] {count.ToString()}));
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
