using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace JsonTest
{
    class ObjectItem
    {
        public string id { get; set; }
    }

	class Program
	{
		static void Main(string[] args)
		{
            var target = Newtonsoft.Json.JsonConvert.SerializeObject( new ObjectItem() { id = "xxx" });
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<ObjectItem>(target);

            var xml = new XAttribute("id", "xxx");
            var json = Newtonsoft.Json.JsonConvert.SerializeXNode(xml);//.Replace("@id", "id");
            var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<ObjectItem>(json);
		}
	}
}
