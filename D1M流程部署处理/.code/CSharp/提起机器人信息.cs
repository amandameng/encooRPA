//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    getRobotInfo(robotJsonFile);
}
//在这里编写您的函数或者类

public void getRobotInfo(string jsonFile)
        {
            string result = File.ReadAllText(jsonFile);
            JObject jObject = (JObject)JsonConvert.DeserializeObject(result);
            var list = jObject["list"];
            List<RobotInfo>  robotInfoList = JsonConvert.DeserializeObject<List<RobotInfo>>(list.ToString());
            robotDT = new DataTable();
            robotDT.Columns.Add("Id");
            robotDT.Columns.Add("Name");
            robotDT.Columns.Add("ConnectionString");

            foreach(RobotInfo ri in robotInfoList)
            {
                DataRow dr = robotDT.NewRow();
                dr["Id"] = ri.Id;
                dr["Name"] = ri.Name;
                dr["ConnectionString"] = ri.ConnectionString;
                robotDT.Rows.Add(dr);
            }


        }

        public class RobotInfo
        {
            [JsonProperty("id")]
            public object Id { get; set; }

            [JsonProperty("departmentId")]
            public object DepartmentId { get; set; }

            [JsonProperty("name")]
            public object Name { get; set; }

            [JsonProperty("connectionString")]
            public object ConnectionString { get; set; }
        }