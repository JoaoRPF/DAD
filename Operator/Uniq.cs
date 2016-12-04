using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace DADStorm
{
    class Uniq : Operator
    {
        private List<Tup> uniqTuples = new List<Tup>();

        public override void execute()
        {
           
            Console.WriteLine("Execute Uniq");

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
                    if (!uniqTuples.Exists(x => x.fields[this.fieldNumber - 1].Equals(inputTuple.fields[this.fieldNumber - 1]))) //MAIS ORGULHO
                    {
                        uniqTuples.Add(inputTuple);
                        lock (this.outputTuples)
                        {
                            /*foreach (Tup tup in _operator.outputTuples)
                            {
                                Console.WriteLine("tuppppp = " + tup.id);
                            }*/
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
