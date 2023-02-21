//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    getWorkflowAndConfigFileInfo();
}
//在这里编写您的函数或者类

  public void getWorkflowAndConfigFileInfo()
        {
            
            string result = responseTxt;
            JObject jObject = (JObject)JsonConvert.DeserializeObject(result);
            var list = jObject["list"];
            List<WorkFlowItem> workflowInfoList = JsonConvert.DeserializeObject<List<WorkFlowItem>>(list.ToString());
            workFlowItemDT = new DataTable();
            workFlowItemDT.Columns.Add("Id");
            workFlowItemDT.Columns.Add("Name");
            workFlowItemDT.Columns.Add("流程配置路径");
            foreach (WorkFlowItem wi in workflowInfoList)
            {
                DataRow dr = workFlowItemDT.NewRow();
                dr["Id"] = wi.Id;
                dr["Name"] = wi.Name;
                List < WorkFlowArgument > workFlowArguments = wi.arguments;
                foreach(WorkFlowArgument wf in workFlowArguments)
                {
                    if(wf.Name.ToString() == "流程配置路径")
                    {
                        dr["流程配置路径"] = wf.DefaultValue;
                        break;
                    }
                }
                //if (!string.IsNullOrEmpty(dr["流程配置路径"].ToString()))
                //{
                    workFlowItemDT.Rows.Add(dr);
                //}
            }
            Console.WriteLine(workFlowItemDT.Rows.Count);
        }



        public class WorkFlowItem
        {
            [JsonProperty("id")]
            public object Id { get; set; }

            [JsonProperty("departmentId")]
            public object DepartmentId { get; set; }

            [JsonProperty("name")]
            public object Name { get; set; }

            [JsonProperty("arguments")]
            public List<WorkFlowArgument> arguments;
        }

        public class WorkFlowArgument
        {
            [JsonProperty("name")]
            public object Name { get; set; }

            [JsonProperty("type")]
            public object Type { get; set; }

            [JsonProperty("allowEdit")]
            public object AllowEdit { get; set; }

            [JsonProperty("shortType")]
            public object ShortType { get; set; }

            [JsonProperty("direction")]
            public object Direction { get; set; }

            [JsonProperty("defaultValue")]
            public object DefaultValue { get; set; }
        }