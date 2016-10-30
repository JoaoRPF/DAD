using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADSTORM
{
    class Uniq : Operator
    {
        private List<string[]> tuples = new List<string[]>();
        private List<string[]> uniqTuples = new List<string[]>();

        public override void execute()
        {
            Console.WriteLine("EXECUTE UNIQ");
            if (this.input.Contains(".dat"))
            {
                tuples = readInputFromFile(this.input);
            }
            //printTuples(tuples);
            foreach (string[] _tuple in tuples)
            {
                if (!uniqTuples.Exists(x => x[this.fieldNumber - 1].Equals(_tuple[this.fieldNumber - 1]))) //ORGULHO
                {
                    uniqTuples.Add(_tuple);
                }
            }
            printTuples(uniqTuples);
        }
    }
}
