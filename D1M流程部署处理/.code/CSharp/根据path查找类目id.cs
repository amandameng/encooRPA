//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    validRowsCount = 0;
    DataTable 类目DT = (DataTable)dtRow_ProjectConfig["类目"];
    
    foreach(DataRow dr in 类目DT.Rows){
        string 一级类目 = dr["一级类目"].ToString().Trim();
        string 二级类目 = dr["二级类目"].ToString().Trim();
        string 三级类目 = dr["三级类目"].ToString().Trim();

        if(string.IsNullOrEmpty(一级类目)){
          continue; 
        }
        List<string> 类目ls = new List<string>{};
        类目ls.Add(一级类目);
        if(!string.IsNullOrEmpty(二级类目)) 类目ls.Add(二级类目);
        if(!string.IsNullOrEmpty(三级类目)) 类目ls.Add(三级类目);

        string 类目path = string.Join("$", 类目ls);
        DataRow[] drs = tbCategoryDT.Select(string.Format("path = '{0}'", 类目path.Replace("'", "''")));
        if(drs.Length > 0){
            string 类目id = drs[0]["id"].ToString();
            类目idlist.Add(类目id);
        }else{
            errorMsgList.Add(string.Format("流程部署ID：{0}，错误消息：{1}", 流程部署ID, string.Format("根据类目（{0}）未查到类目ID", 类目path)));
        }
        validRowsCount += 1;
    }
   
}
//在这里编写您的函数或者类