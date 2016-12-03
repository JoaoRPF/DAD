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
                    string[] inputTuple;
                    lock (this.inputTuples)
                    {
                        inputTuple = (string[])this.inputTuples[0].Clone();
                    }
                    Console.WriteLine("Processing tuple -> " + constructTuple(inputTuple));


                    int compare = String.Compare(inputTuple[this.fieldNumber - 1].Trim('"'), this.conditionValue);

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
