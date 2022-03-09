//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    // List<string> 订单编号列表 = new List<string>{};
    if(String.IsNullOrEmpty(订单列名)){
        订单列名 = 目标订单数据表.Columns.Contains("采购单号") ? "采购单号" : "order_number"; // 数据库查出的是order_number, 网站下载的是采购单号
    }
     
    foreach(DataRow dr in 目标订单数据表.Rows){
        string 订单编号 = dr[订单列名].ToString();
        if(!订单编号列表.Contains(订单编号)){
            订单编号列表.Add(订单编号);
        }
    }

    string 订单编号字符串 = String.Join(",", 订单编号列表);
    
    string rawSelectSql = @"SELECT oli.*, ods.order_type, ods.create_date, eo.order_expire_date as request_delivery_date, ods.logistics_warehouse,
                            `stst`.`Sold_To_Code` as sold_to_code, `stst`.`Ship_To_Code` as ship_to_code, `stst`.`Order_Type_Short` as order_type_short,
                            `stst`.`Request_Delivery_Date` as ststRDD,
                            `stst`.`Nestle_Plant_No` as nestle_plant_no, `npmd`.`Nestle_Material_No` as 雀巢产品编码, npmd.`Nestle_BU` as BU,
                            `npmd`.`Nestle_Case_Configuration` as 雀巢产品箱规, `npmd`.`Nestle_NPS` as 雀巢产品箱价, `npmd`.`Adjustive_Price` as 雀巢产品调价, 
                            `npmd`.Tax_Point, `npmd`.Remark, `npmd`.User_Remark
                            FROM orders ods
                            join (select distinct order_number, order_expire_date from exported_orders) eo on eo.order_number=ods.order_number
                            join order_line_items oli on ods.order_number = oli.order_number
                            left join sold_to_ship_to stst on stst.`Customer_Logistics_Warehouse` = ods.logistics_warehouse and ods.customer_name = stst.`Customer_Name`
                            left join material_master_data npmd on npmd.`Customer_Material_No` = oli.product_code and npmd.`Customer_Name` = ods.customer_name and replace(npmd.`Nestle_Plant_No`, ' ', '') = replace(`stst`.`Nestle_Plant_No`, ' ', '')
                            where ods.customer_name='{0}' and ods.order_number in ({1})
                            order by ods.create_date asc, cast(line_number as signed) asc";
    string 客户平台 = Convert.ToString(GlobalVariable.VariableHelper.GetVariableValue("客户平台"));
    selectSql = String.Format(rawSelectSql, 客户平台, 订单编号字符串);
    Console.WriteLine(selectSql);
}
//在这里编写您的函数或者类