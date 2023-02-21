//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    店铺配置文件字典 = new Dictionary<string, string>{};
    foreach(DataRow dr in 店铺类目数据表.Rows){
        string 店铺名 = dr["店铺名"].ToString();
        string[] 原始店铺名数组 = 店铺名.Split(new string[]{",", "，"}, StringSplitOptions.RemoveEmptyEntries);
        foreach(string 原始店铺名 in 原始店铺名数组){
            string 新店铺名 = 原始店铺名.Replace(" ", "").ToLower();
        
            foreach(string configFile in 配置文件列表数组){
                string fileName = System.IO.Path.GetFileNameWithoutExtension(configFile);
                fileName = fileName.Replace(" ", "").ToLower();
                if(fileName.Contains(新店铺名)){
                    店铺配置文件字典.Add(原始店铺名, configFile);
                    break;
                }
            }
        }
    }
}
//在这里编写您的函数或者类