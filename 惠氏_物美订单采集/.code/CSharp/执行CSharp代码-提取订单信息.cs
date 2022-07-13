//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    uniqOrdersDTFromSheets = origOrdersFromSheetDT.DefaultView.ToTable(true, new string[]{"订单号", "订货地"});
    uniqOrdersDTFromSheets.Columns.Add("大仓号", typeof(string));
    foreach(DataRow dr in uniqOrdersDTFromSheets.Rows){
       string[] addreddArr = dr["订货地"].ToString().Split(new string[]{" "}, StringSplitOptions.RemoveEmptyEntries);
        if(addreddArr.Length == 2){
            dr["大仓号"] = addreddArr[0];
        }
    }
    uniqOrdersDTFromSheets.Columns.Remove("订货地");
    
    
    newOrdersQuery = String.Format(@"select order_number, dc_no from {0}
                 where not exists
                 (select order_number ,dc_no from {1}
                 where {0}.order_number = {1}.order_number and 
                 {0}.dc_no = {1}.dc_no and {1}.order_date > subdate(now(), 180)
                 )", dtRow_ProjectSettings["订单列表临时数据表名"].ToString(), dtRow_ProjectSettings["订单数据库表名"].ToString());
    
    if(deletedOrderList != null && deletedOrderList.Count > 0){
            string order_numbers_str = string.Join("', '", deletedOrderList);
            order_numbers_str = "'" + order_numbers_str + "'";
            deletedOrdersQuery = String.Format(@"select * from {0} where order_number in ({1}) and order_number not in (select customer_order_number from exception_order  where customer_name like '{2}%' and exception_category like '%删单%')", 
            dtRow_ProjectSettings["订单数据库表名"].ToString(), order_numbers_str, dtRow_ModuleSettings["customer_name"].ToString());
    }

    Console.WriteLine("newOrdersQuery: {0}", newOrdersQuery);
    Console.WriteLine("deletedOrdersQuery: {0}", deletedOrdersQuery);

}
//在这里编写您的函数或者类