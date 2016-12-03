﻿using System;
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
                string[] inputTuple;
                if (this.inputTuples.Count != 0)
                {
                    checkSleeping();
                    lock (this.inputTuples)
                    {
                        inputTuple = (string[])this.inputTuples[0].Clone();
                    }
                    Console.WriteLine("Processing tuple -> " + constructTuple(inputTuple));
                    lock (this.outputTuples)
                    {
                        outputTuples.Add(inputTuple);
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
