using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace DADStorm
{
    class Custom : Operator
    {
        public override void execute()
        {
            Console.WriteLine("Execute Custom with the following arguments:");
            Console.WriteLine("DLL = " + _operator.dllCustom + ", Class = " + _operator.classCustom + ", Method = " + _operator.methodCustom);
            Console.WriteLine("Dll custom this = " + this.dllCustom);
            byte[] code = File.ReadAllBytes(this.dllCustom); // carregamento da dll
            Assembly assembly = Assembly.Load(code);
            // Walk through each type in the assembly looking for our class
            try
            {
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
                                    ForwardTup inputTuple;
                                    lock (this.inputTuples)
                                    {
                                        inputTuple = (ForwardTup)this.inputTuples[0].Clone();
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
                                        ForwardTup oTup = new ForwardTup(outputTuple, inputTuple.tupleID);
                                        lock (this.outputTuples)
                                        {
                                            outputTuples.Add(oTup);
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
            }
            catch (ReflectionTypeLoadException ex)
            {
                StringBuilder sb = new StringBuilder();
                foreach (Exception exSub in ex.LoaderExceptions)
                {
                    sb.AppendLine(exSub.Message);
                    FileNotFoundException exFileNotFound = exSub as FileNotFoundException;
                    if (exFileNotFound != null)
                    {
                        if (!string.IsNullOrEmpty(exFileNotFound.FusionLog))
                        {
                            sb.AppendLine("Fusion Log:");
                            sb.AppendLine(exFileNotFound.FusionLog);
                        }
                    }
                    sb.AppendLine();
                }
                string errorMessage = sb.ToString();
                Console.WriteLine(errorMessage);
            }
            
            //throw (new System.Exception("could not invoke method"));
        }
    }
}
