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
                    Tup inputTuple;
                    lock (this.inputTuples)
                    {
                        inputTuple = (Tup)this.inputTuples[0].Clone();
                    }
                    Console.WriteLine("Processing tuple -> " + constructTuple(inputTuple));


                    int compare = String.Compare(inputTuple.fields[this.fieldNumber - 1].Trim('"'), this.conditionValue);

                    if (this.condition.Equals("=") && compare == 0)
                    {
                        lock (this.outputTuples)
                        {
                            outputTuples.Add(inputTuple);
                            _operator.outputTuples.Sort((s1, s2) => s1.id.CompareTo(s2.id));
                        }
                    }   
                    if (this.condition.Equals(">") && compare > 0)
                    {
                        lock (this.outputTuples)
                        {
                            outputTuples.Add(inputTuple);
                            _operator.outputTuples.Sort((s1, s2) => s1.id.CompareTo(s2.id));
                        }
                    }
                    if (this.condition.Equals("<") && compare < 0)
                    {
                        lock (this.outputTuples)
                        {
                            outputTuples.Add(inputTuple);
                            _operator.outputTuples.Sort((s1, s2) => s1.id.CompareTo(s2.id));
                        }
                    }
                    else
                    {
                        destroyTupleInAllReplicas(inputTuple.id);
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
