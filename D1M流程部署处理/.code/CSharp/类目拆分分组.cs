//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    类目数据表 = 类目数据表.DefaultView.ToTable(true, new string[]{"一级类目", "二级类目"});
    类目字典 = new Dictionary<string, DataTable>{};
    IEnumerable<IGrouping<string, DataRow>> result = 类目数据表.Rows.Cast<DataRow>().GroupBy<DataRow, string>(dr => dr["一级类目"].ToString());
    foreach(var item in result){
        string 一级类目 = item.Key;
        DataRow[] catRows = item.ToArray();
        Console.WriteLine(catRows.Length);
        DataTable dt = 类目数据表.Clone();
        dt.Columns.Add("文件路径");
        dt.Columns.Add("三级类目");
        foreach(DataRow dr in catRows){
            string 文件名 = FileNameValidation(一级类目);
            string 类目文件夹 =  Path.Combine(System.IO.Path.GetDirectoryName(类目文件路径), "类目文件夹");
            string 文件路径 = Path.Combine(类目文件夹, 文件名+".xlsx");
            if(!Directory.Exists(类目文件夹)) {
                Directory.CreateDirectory(Path.GetDirectoryName(类目文件夹));
            }
            Console.WriteLine(文件路径);
            dt.Rows.Add(new object[]{一级类目, dr["二级类目"], 文件路径, null});
        }
        类目字典.Add(一级类目, dt);
    }
    // Convert.ToInt16("asa");
}
//在这里编写您的函数或者类
    
    public string FileNameValidation(string fileName)
        {
            Regex regex1 = new Regex("\r\n");
            fileName = regex1.Replace(fileName, string.Empty);
            Regex regex2 = new Regex("\n");
            fileName = regex2.Replace(fileName, string.Empty);
            Regex reg = new Regex(@"[/\*:?<>|]");
            fileName = reg.Replace(fileName, "_");
            fileName = fileName.Trim();
            return fileName;
        }