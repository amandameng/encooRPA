//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    if (dic_ModuleConfigs == null)
    {
         dic_ModuleConfigs = new Dictionary<string, DataRow>();
    }
    var dt = new DataTable();
    // 项目设置，如有需要，可追加新列来配置其他设置，该设置将在所有的数据模块可被访问，有全局变量的角色


    if (dtTable_Config == null)
    {
       throw new Exception("配置文件不存在");
    }
    else
    {
        // 配置文件方式
        switch (sheet)
        {
            case "项目设置":
            {
                // 忽略第一行表头【配置项】【值】【说明】
                for (int i = 0; i < dtTable_Config.Rows.Count; i++)
                {
                    DataRow item = dtTable_Config.Rows[i];
                    if (!string.IsNullOrWhiteSpace(item[0].ToString())   &&   !dt.Columns.Contains(item[0].ToString()))
                    {
                        dt.Columns.Add(item[0].ToString(), typeof(string));
                    }
                }
                var row = dt.NewRow();
                for (int i = 0; i < dtTable_Config.Rows.Count; i++)
                {
                    DataRow item = dtTable_Config.Rows[i];
                    if (!string.IsNullOrWhiteSpace(item[0].ToString()))
                    {
                        row[item[0].ToString()] = item[1].ToString();
                    }
                }
                // 项目设置单独存放
                dtRow_ProjectConfig = row;
                break;
            }
			case "类目":
            {
                // 类目信息输出为数据表
                DataTable dtTable_Category = dtTable_Config.Clone();
                foreach (DataRow cateRow in dtTable_Config.Rows)
                {
                    if (!string.IsNullOrWhiteSpace(cateRow[0].ToString()))
                    {
                        // 过滤掉空的类目行信息
                        var row = dtTable_Category.NewRow();
                        row.ItemArray = cateRow.ItemArray;
                        dtTable_Category.Rows.Add(row);
                    }
                }
                if(!dtRow_ProjectConfig.Table.Columns.Contains("类目")){
                    dtRow_ProjectConfig.Table.Columns.Add("类目", typeof(DataTable));
                }
                dtRow_ProjectConfig["类目"] = dtTable_Category;
                break;
            }
            default:
            {
                foreach (DataRow item in dtTable_Config.Rows)
                {
                    if (!string.IsNullOrWhiteSpace(item[0].ToString()))
                    {
                        dt.Columns.Add(item[0].ToString(), typeof(string));
                    }
                }
                var row = dt.NewRow();
                foreach (DataRow item in dtTable_Config.Rows)
                {
                    if (!string.IsNullOrWhiteSpace(item[0].ToString()))
                    {
                        row[item[0].ToString()] = item[1].ToString();
                    }
                }
             
                // 模块配置存入字典
                dic_ModuleConfigs.Add(sheet, row);
                break;
            }
        }
    }
   
}
//在这里编写您的函数或者类