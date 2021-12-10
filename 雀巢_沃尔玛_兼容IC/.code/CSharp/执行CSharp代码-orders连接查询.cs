//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    // List<string> 订单编号列表 = new List<string>{};
    if(String.IsNullOrEmpty(订单列名)){
        订单列名 = "order_number";
    }
     
    foreach(DataRow dr in 目标订单数据表.Rows){
        string 订单编号 = dr[订单列名].ToString();
        if(!订单编号列表.Contains(订单编号)){
            订单编号列表.Add(订单编号);
        }
    }

    string 订单编号字符串 = String.Join(",", 订单编号列表.ToArray());
    
    string rawSelectSql = @"SELECT ods.order_type, ods.document_link, ods.create_date, ods.create_date_time, ods.must_arrived_by as request_delivery_date,
                             ods.promotional_event,
                            `oli`.`order_number`,
                            `oli`.`line_number`,
                            `oli`.`product_code`,
                            `oli`.`gtin`,
                            `oli`.`supplier_stock`,
                            `oli`.`color`,
                            `oli`.`size`,
                            `oli`.`quantity_ordered`,
                            `oli`.`uom`,
                            `oli`.`pack`,
                            `oli`.`cost`,
                            `oli`.`extended_cost`,
                            `oli`.`item_description`,
                            `oli`.`tax_type`,
                            `oli`.`tax_percent`,
                            `oli`.`allowance_or_charge` oli_allowance_or_charge,
                            `oli`.`allowance_description` oli_allowance_description,
                            `oli`.`allowance_qty` oli_allowance_qty,
                            `oli`.`allowance_uom` oli_allowance_uom,
                            `oli`.`allowance_percent` oli_allowance_percent ,
                            `oli`.`allowance_total` oli_allowance_total,
                            `oli`.`item_instructions`,
                            `oli`.`customer_name`,
                            `stst`.`Sold_To_Code` as sold_to_code, `stst`.`Ship_To_Code` as ship_to_code, 
                            `stst`.`Nestle_Plant_No` as nestle_plant_no, `npmd`.`Nestle_Material_No` as 雀巢产品编码, npmd.`Nestle_BU` as BU,
                            `npmd`.`Nestle_Case_Configuration` as 雀巢产品箱规, `npmd`.`Nestle_NPS` as 雀巢产品箱价, `npmd`.`Adjustive_Price` as 雀巢产品调价, 
                            npmd.Tax_Point as Tax_Point, `npmd`.Remark, `npmd`.Remark_Option,`npmd`.Distribution_Channel
                            FROM walmart_orders ods
                            join walmart_order_items oli on ods.order_number = oli.order_number and oli.document_link = ods.document_link
                            left join sold_to_ship_to stst on stst.`Nestle_Plant_No` = ods.location and ods.customer_name = stst.`Customer_Name`
                            left join material_master_data npmd on npmd.`Customer_Material_No` = oli.product_code 
                            and npmd.`Customer_Name` = ods.customer_name and npmd.`Nestle_Plant_No`= `stst`.`Nestle_Plant_No`
                            where ods.customer_name='{0}' and ods.order_number in ({1})
                            group by order_number, ods.document_link, line_number
                            order by ods.create_date_time desc, line_number asc";
    
    selectSql = String.Format(rawSelectSql, customer_name, 订单编号字符串);
    Console.WriteLine(selectSql);
}
//在这里编写您的函数或者类