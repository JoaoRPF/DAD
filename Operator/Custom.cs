using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace DADSTORM
{
    class Custom : Operator
    {
        public override void execute()
        {
            Console.WriteLine("Execute Custom with the following arguments:");
            Console.WriteLine("DLL = " + _operator.dllCustom + ", Class = " + _operator.classCustom + ", Method = " + _operator.methodCustom);
            byte[] code = File.ReadAllBytes(this.dllCustom); // carregamento da dll
            Assembly assembly = Assembly.Load(code);
            // Walk through each type in the assembly looking for our class
            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsClass == true)
                {
                    if (type.FullName.EndsWith("." + this.classCustom))
                    {
                        // create an instance of the object
                        object ClassObj = Activator.CreateInstance(type);

                        // Dynamically Invoke the method
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
                                object[] args = new object[] { inputTuple };
                                object resultObject = type.InvokeMember(this.methodCustom,
                                  BindingFlags.Default | BindingFlags.InvokeMethod,
                                       null,
                                       ClassObj,
                                       args);
                                IList<IList<string>> result = (IList<IList<string>>)resultObject;
                                Console.WriteLine("Result of executing Custom Operator: ");
                                foreach (IList<string> tuple in result)
                                {
                                    string[] outputTuple = new string[tuple.Count];
                                    tuple.CopyTo(outputTuple, 0);
                                    Console.Write("tuple: ");
                                    Console.WriteLine(constructTuple(outputTuple));
                                    lock (this.outputTuples)
                                    {
                                        outputTuples.Add(outputTuple);
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
            //throw (new System.Exception("could not invoke method"));
        }
    }
}
