using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace MTDBCore {

	public class JsonParser {

		public static Dictionary<string, double> ParseConvertionRates(string jsonData, string baseCurrency) {
			Dictionary<string, double> ret = new Dictionary<string, double>();
			JObject obj = JObject.Parse(jsonData);

			JToken rates = obj["rates"];
			foreach (JProperty currency in rates) {
				ret.Add(currency.Name, currency.Value.Value<float>());
			}

			ret.Add(baseCurrency, 1);
			return ret;
		}
	}
}
