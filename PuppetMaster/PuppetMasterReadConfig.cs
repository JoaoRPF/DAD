using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DADSTORM
{
    class PuppetMasterReadConfig
    {

        public Dictionary<string, string> readLine(string[] line)
        {
            if (line[0].Contains("OP"))
            {
                return readOperatorDefinition(line);
            }
            Dictionary<string, string> parsedLineDictionary = new Dictionary<string, string>();
            parsedLineDictionary.Add("LINE_ID", "LIXO");
            return parsedLineDictionary;
        }

        private Dictionary<string, string> readOperatorDefinition(string[] line) {
            Dictionary<string, string> parsedLineDictionary = new Dictionary<string, string>();
            parsedLineDictionary.Add("LINE_ID", "OP");
            string id = line[0];
            string input = line[3];
            string repFact = line[6];
            string routing = line[8];
            List<string> addressesList = new List<string>();
            for (int j = 10; j < 10 + Int32.Parse(repFact); j++)
            {
                if (line[j].Contains(","))
                {
                    addressesList.Add(line[j].Remove(line[j].Length - 1));
                }
                else
                {
                    addressesList.Add(line[j]);
                }
            }
            int newIndex = 10 + Int32.Parse(repFact);
            string type = line[newIndex + 2];
            string fieldNumber = "";
            string condition = "";
            string conditionValue = "";
            switch (type)
            {
                case "UNIQ":
                    fieldNumber = line[newIndex + 3];
                    break;

                case "COUNT":
                    break;

                case "DUP":
                    break;

                case "FILTER":
                    string[] filter = line[newIndex + 3].Split(',');
                    fieldNumber = filter[0];
                    condition = filter[1];
                    conditionValue = filter[2];
                    break;
            }
            parsedLineDictionary.Add("OPERATOR_ID", id);
            parsedLineDictionary.Add("INPUT", input);
            parsedLineDictionary.Add("REP_FACT", repFact);
            parsedLineDictionary.Add("ROUTING", routing);

            string addresses = "";
            foreach (string address in addressesList){
                addresses += address + "$";
            }
            addresses = addresses.Remove(addresses.Length - 1);

            parsedLineDictionary.Add("ADDRESSES", addresses);
            parsedLineDictionary.Add("TYPE", type);
            parsedLineDictionary.Add("FIELD_NUMBER", fieldNumber);
            parsedLineDictionary.Add("CONDITION", condition);
            parsedLineDictionary.Add("CONDITION_VALUE", conditionValue);

            return parsedLineDictionary;
        }
    }
}
