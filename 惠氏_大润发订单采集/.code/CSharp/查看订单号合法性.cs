//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    DataTable uniqOrdersDT = origDT.DefaultView.ToTable(true, new string[]{"采购单号", "门店"});
    // 如果订单号为空，则抛出异常重试
    if(uniqOrdersDT.Rows.Count > 0){
        foreach(DataRow dr in uniqOrdersDT.Rows){
            string 采购单号 = dr["采购单号"].ToString().Trim();
            if(string.IsNullOrEmpty(采购单号)){
                throw new Exception("订单号为空，需要重新下载文件");
            }
        }
    }
}
//在这里编写您的函数或者类