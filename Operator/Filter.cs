using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADStorm
{
    class Filter :Operator
    {
        public override void execute()
        {
            Console.WriteLine("Executing Filter");

            while (true)
            {
                base.execute();
                if (this.inputTuples.Count != 0)
                {
                    checkSleeping();
                    ForwardTup inputTuple;
                    lock (this.inputTuples)
                    {
                        inputTuple = (ForwardTup)this.inputTuples[0].Clone();
                    }
                    Console.WriteLine("Processing tuple -> " + constructTuple(inputTuple.tup));


                    int compare = String.Compare(inputTuple.tup[this.fieldNumber - 1].Trim('"'), this.conditionValue);

                    if (this.condition.Equals("=") && compare == 0)
                    {
                        lock (this.outputTuples)
                        {
                            outputTuples.Add(inputTuple);
                        }
                    }   
                    if (this.condition.Equals(">") && compare > 0)
                    {
                        lock (this.outputTuples)
                        {
                            outputTuples.Add(inputTuple);
                        }
                    }
                    if (this.condition.Equals("<") && compare < 0)
                    {
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
