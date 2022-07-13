//代码执行入口，请勿修改或删除
public void Run()
{
    
    //在这里编写您的代码
    foreach(DataRow dr in allOrdersDT.Rows){
        string order_number = dr["采购平台订单号"].ToString();        
        
        List<Dictionary<string, string>> findCode = 采购单号类型list.Where(item => item["采购单号"] == order_number).ToList();
        // Console.WriteLine("count: {0}", findCode.Count);
        if(findCode.Count > 0)
        {
            string 订单类型 = findCode[0]["采购单类型"];
            dr["订单类型"] = 订单类型;
        }
       
    }
}
//在这里编写您的函数或者类