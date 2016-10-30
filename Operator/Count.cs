using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADSTORM
{
    class Count : Operator 
    {
        private List<string[]> tuples = new List<string[]>();

        public override void execute()
        {
            Console.WriteLine("EXECUTE COUNT");
            if (this.input.Contains(".dat")){
                tuples = readInputFromFile(this.input);
            }
            else
            {
                tuples = tuplesInput;
            }
            printTuples(tuples);
            int count = tuples.Count;
            Console.WriteLine("Count = " + count);
        }
    }
}
