using System;
using System.Linq;

namespace ExpirmentalModel
{
    public class InterServiceMessage
    {
        public string Name { get; set; }
        public Guid Id => Guid.NewGuid();
        public string Stack
        {
            get
            {
                var temp = "";
                new int[10] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
                    .ToList()
                    .ForEach(i => temp += "some stack text here" + Environment.NewLine);
                return temp;
            }
        }
    }
}
