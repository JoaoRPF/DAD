using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADSTORM
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

                string[] inputTuple;
                if (this.inputTuples.Count != 0)
                {
                    checkSleeping();
                    lock (this.inputTuples)
                    {
                        inputTuple = (string[])this.inputTuples[0].Clone();
                    }
                    count++;
                    Console.WriteLine("Number of Tuples = " + count);
                    lock (this.inputTuples)
                    {
                        inputTuples.RemoveAt(0);
                    }
                }
            }
        }
    }
}
