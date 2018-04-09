using MLWebAPI.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;

namespace MLWebAPI.Controllers
{
    //allow *ALL* Cross-Origin Resource Sharing CORS Requests
    [EnableCors("*","*","*")]
    public class IncomePredictionController : ApiController
    {
        public class StringTable
        {
            public string[] ColumnNames { get; set; }
            public string[,] Values { get; set; }
        }
        public static string outMLResultData = "";

        [HttpGet]
        public async Task<IncomePredictionResults> GetPrediction()
        {
            //http://localhost:60880/api/IncomePrediction/GetPrediction?age=53&workclass=Self-emp-not-inc&fnlwgt=209642&education=HS-grad&education-num=9&marital-status=Married-civ-spouse&occupation=Exec-managerial&relationship=Husband&race=White&sex=Male&capital-gain=0&capital-loss=0&hours-per-week=45&native-country=United-States

            //Prepare a new ML Response Data Structure for the results
            IncomePredictionResults incomePredResults = new IncomePredictionResults();

            //Parse the input parameters from the request
            NameValueCollection nvc = HttpUtility.ParseQueryString(Request.RequestUri.Query);

            //Validate Number of Input parameters (TODO: Add more validations)
            if (nvc.Count < 14) { }

            // Extract Input Values
            string inAge = nvc[0];
            string inWorkClass = nvc[1];
            string infnlwgt = nvc[2];
            string inEducation = nvc[3];
            string inEducationNum = nvc[4];
            string inMaritalStatus = nvc[5];
            string inOccupation = nvc[6];
            string inRelationship = nvc[7];
            string inRace = nvc[8];
            string inSex = nvc[9];
            string inCapitalGain = nvc[10];
            string inCapitalLoss = nvc[11];
            string inHoursPerWeek = nvc[12];
            string inNativeCountry = nvc[13];

            using (var client = new HttpClient())
            {
                var scoreRequest = new
                {
                    Inputs = new Dictionary<string, StringTable>()
                    {
                        {
                            "input1",
                            new StringTable()
                            {
                                //ColumnNames = new string[] { "age", "workclass", "fnlwgt", "education", "education-num", "marital-status", "occupation", "relationship", "race", "sex", "capital-gain", "capital-loss", "hours-per-week", "native-country", "income"},
                                ColumnNames = new string[] {"Age",
                                                            "Workclass",
                                                            "Fnlwgt",
                                                            "Education",
                                                            "Education-num",
                                                            "Marital-status",
                                                            "Occupation",
                                                            "Relationship",
                                                            "Race",
                                                            "Sex",
                                                            "Capital-gain",
                                                            "Capital-loss",
                                                            "Hours-per-week",
                                                            "Native-country",
                                                            "Income"},
                                //Values = new string[,] { { "0", "value", "0", "value", "0", "value", "value", "value", "value", "value", "0", "0", "0", "value", "value" }, { "0", "value", "0", "value", "0", "value", "value", "value", "value", "value", "0", "0", "0", "value", "value" } }
                                
                                Values = new string[,] { {
                                        inAge,
                                        inWorkClass,
                                        infnlwgt,
                                        inEducation,
                                        inEducationNum,
                                        inMaritalStatus,
                                        inOccupation,
                                        inRelationship,
                                        inRace,
                                        inSex,
                                        inCapitalGain,
                                        inCapitalLoss,
                                        inHoursPerWeek,
                                        inNativeCountry,
                                        "0"
                                    }
                                }
                                //Values = new string[,] { { "52", "Self-emp-not-inc", "209642", "HS-grad", "12", "Married-civ-spouse", "Exec-managerial", "Husband", "White", "Male", "0", "0", "45", "United-States", "0" } }
                            }
                        }
                    },
                    GlobalParameters = new Dictionary<string, string>()
                    {

                    }
                };
                var sw = new Stopwatch();
                const string apiKey = "gE7GS0g98G/Kcpy/F1oWSC23BYhWIvB7rKIVv9WcXFMPFTT1jnesknXmNSYKkop6XLI0valyd/Va1s32ek5sSQ==";
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                client.BaseAddress = new Uri("https://ussouthcentral.services.azureml.net/workspaces/46ac4bd491e14dc8b1edbcbb921dcea5/services/4e38080d3670486c81aac3f571d471b0/execute?api-version=2.0&details=true");

                //Time the ML Web Service Call
                sw.Start();
                HttpResponseMessage response = await client.PostAsJsonAsync("", scoreRequest).ConfigureAwait(false);
                sw.Stop();
                string elapsed = sw.Elapsed.TotalSeconds.ToString();

                //Check Status of Azure ML Web Service Call 
                if (response.IsSuccessStatusCode)
                {
                    //Read the HTTP response
                    string MLResp = await response.Content.ReadAsStringAsync();//.ConfigureAwait(false);

                    //Parse ML Web Service Response and return a populated IncomePredictionResults response record
                    incomePredResults = ParseMLResponse(MLResp);

                    //Update for ML Service Response Time
                    incomePredResults.MLResponseTime = elapsed;
                }else
                {
                    incomePredResults.MLPrediction = response.ReasonPhrase.ToString();
                }

                client.Dispose();
                //return Ok(incomePredResults); 
                return incomePredResults;
            }
        }

        private static IncomePredictionResults ParseMLResponse(string result) {
            IncomePredictionResults incomePredResult = new IncomePredictionResults();            

            JObject json = JObject.Parse(result);
            JObject results = (JObject)json["Results"];
            JObject output1 = (JObject)results["output1"];
            JObject value = (JObject)output1["value"];
            JArray columnNames = (JArray)value["ColumnNames"];
            IList<string> columnNamesList = columnNames.Select(c => (string)c).ToList();
            JArray values = (JArray)value["Values"];

            foreach (JArray array in values)
            {
                incomePredResult.Age = array[columnNamesList.IndexOf("Age")].Value<string>();
                incomePredResult.WorkClass = array[columnNamesList.IndexOf("Workclass")].Value<string>();
                incomePredResult.Fnlwgt = array[columnNamesList.IndexOf("Fnlwgt")].Value<string>();
                incomePredResult.Education = array[columnNamesList.IndexOf("Education")].Value<string>();
                incomePredResult.EducationNum = array[columnNamesList.IndexOf("Education-num")].Value<string>();
                incomePredResult.MaritalStatus = array[columnNamesList.IndexOf("Marital-status")].Value<string>();
                incomePredResult.Occupation = array[columnNamesList.IndexOf("Occupation")].Value<string>();
                incomePredResult.Relationship = array[columnNamesList.IndexOf("Relationship")].Value<string>();
                incomePredResult.Race = array[columnNamesList.IndexOf("Race")].Value<string>();
                incomePredResult.Sex = array[columnNamesList.IndexOf("Sex")].Value<string>();
                incomePredResult.CapitalGain = array[columnNamesList.IndexOf("Capital-gain")].Value<string>();
                incomePredResult.CapitalLoss = array[columnNamesList.IndexOf("Capital-loss")].Value<string>();
                incomePredResult.HoursPerWeek = array[columnNamesList.IndexOf("Hours-per-week")].Value<string>();
                incomePredResult.NativeCountry = array[columnNamesList.IndexOf("Native-country")].Value<string>();
                incomePredResult.MLPrediction = array[columnNamesList.IndexOf("Scored Labels")].Value<string>();
                incomePredResult.MLConfidence = array[columnNamesList.IndexOf("Scored Probabilities")].Value<string>();
            }

            return incomePredResult;
        }
    }
}
